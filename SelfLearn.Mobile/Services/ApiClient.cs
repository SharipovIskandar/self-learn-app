using System.Net;
using System.Net.Http.Headers;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using SelfLearn.Mobile.Models;

namespace SelfLearn.Mobile.Services;

/// <summary>
/// Backend API'ga so'rovlar. Tokenni <see cref="AuthSession"/> dan olib Authorization sarlavhasiga qo'yadi va
/// barcha xatolarni o'zbekcha xabarli <see cref="ApiException"/> ga aylantiradi.
/// </summary>
public sealed class ApiClient
{
    private readonly HttpClient _http;
    private readonly AuthSession _session;

    /// <summary>
    /// Token bilan yuborilgan so'rovga 401 qaytsa chiqariladi: token serverda yaroqsiz (o'chirilgan/muddati tugagan).
    /// <see cref="AuthService"/> shu orqali foydalanuvchini Login'ga qaytaradi.
    /// </summary>
    public event Action? Unauthorized;

    public ApiClient(HttpClient http, AuthSession session)
    {
        _http = http;
        _session = session;
    }

    public async Task<TResponse> GetAsync<TResponse>(string path, CancellationToken ct = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, path);

        return await ReadAsync<TResponse>(request, ct);
    }

    public async Task<TResponse> PostAsync<TRequest, TResponse>(string path, TRequest body, CancellationToken ct = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, path)
        {
            Content = new StringContent(JsonSerializer.Serialize(body, TypeInfo<TRequest>()), Encoding.UTF8, "application/json"),
        };

        return await ReadAsync<TResponse>(request, ct);
    }

    /// <summary>Javob tanasi kutilmaydigan POST (masalan logout: 204).</summary>
    public async Task PostAsync(string path, CancellationToken ct = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, path);
        using var response = await SendAsync(request, ct);
    }

    private async Task<T> ReadAsync<T>(HttpRequestMessage request, CancellationToken ct)
    {
        using var response = await SendAsync(request, ct);

        try
        {
            await using var stream = await response.Content.ReadAsStreamAsync(ct);
            var result = await JsonSerializer.DeserializeAsync(stream, TypeInfo<T>(), ct);

            return result ?? throw new ApiException(ApiErrorKind.Unexpected, ApiMessages.Unexpected, (int)response.StatusCode);
        }
        catch (JsonException ex)
        {
            throw new ApiException(ApiErrorKind.Unexpected, ApiMessages.Unexpected, (int)response.StatusCode, inner: ex);
        }
        catch (Exception ex) when (IsNetworkFailure(ex))
        {
            throw new ApiException(ApiErrorKind.Network, ApiMessages.NoConnection, inner: ex);
        }
    }

    /// <summary>So'rovni yuboradi. Muvaffaqiyatsiz status bo'lsa <see cref="ApiException"/> tashlaydi.</summary>
    private async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
    {
        var hadToken = false;
        if (_session.Token is { Length: > 0 } token)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            hadToken = true;
        }

        HttpResponseMessage response;
        try
        {
            response = await _http.SendAsync(request, ct);
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            // HttpClient.Timeout tugadi (so'rovni foydalanuvchi bekor qilmagan).
            throw new ApiException(ApiErrorKind.Network, ApiMessages.Timeout);
        }
        catch (Exception ex) when (IsNetworkFailure(ex))
        {
            throw new ApiException(ApiErrorKind.Network, ApiMessages.NoConnection, inner: ex);
        }

        if (response.IsSuccessStatusCode)
        {
            return response;
        }

        try
        {
            if (hadToken && response.StatusCode == HttpStatusCode.Unauthorized)
            {
                Unauthorized?.Invoke();
            }

            throw await ToExceptionAsync(response, ct);
        }
        finally
        {
            response.Dispose();
        }
    }

    /// <summary>
    /// Backendning xabari (o'zbekcha) faqat o'zimiz boshqaradigan holatlarda (403, 422, 429) ko'rsatiladi.
    /// 401, 404, 5xx uchun Laravel'ning inglizcha matni o'rniga o'zimizning matn ishlatiladi.
    /// </summary>
    private static async Task<ApiException> ToExceptionAsync(HttpResponseMessage response, CancellationToken ct)
    {
        var status = (int)response.StatusCode;
        var body = await TryReadErrorBodyAsync(response, ct);
        var serverMessage = string.IsNullOrWhiteSpace(body?.Message) ? null : body.Message;

        return status switch
        {
            401 => new ApiException(ApiErrorKind.Unauthorized, ApiMessages.SessionExpired, status),
            403 => new ApiException(ApiErrorKind.Forbidden, serverMessage ?? ApiMessages.Forbidden, status, body?.Code),
            409 => new ApiException(ApiErrorKind.Conflict, serverMessage ?? ApiMessages.Unexpected, status, body?.Code),
            422 => new ApiException(ApiErrorKind.Validation, serverMessage ?? ApiMessages.Validation, status, body?.Code, body?.Errors),
            429 => new ApiException(
                ApiErrorKind.RateLimited,
                serverMessage ?? ApiMessages.TooManyRequests,
                status,
                body?.Code,
                retryAfterSeconds: body?.RetryAfter ?? RetryAfterHeader(response)),
            >= 500 => new ApiException(ApiErrorKind.Server, ApiMessages.Server, status),
            _ => new ApiException(ApiErrorKind.Unexpected, $"{ApiMessages.Unexpected} ({status})", status),
        };
    }

    private static async Task<ApiErrorBody?> TryReadErrorBodyAsync(HttpResponseMessage response, CancellationToken ct)
    {
        try
        {
            var text = await response.Content.ReadAsStringAsync(ct);

            return JsonSerializer.Deserialize(text, TypeInfo<ApiErrorBody>());
        }
        catch (JsonException)
        {
            // JSON emas (masalan Cloudflare yoki texnik ishlar HTML sahifasi): status bo'yicha standart xabar beriladi.
            return null;
        }
        catch (Exception ex) when (IsNetworkFailure(ex))
        {
            return null;
        }
    }

    private static int? RetryAfterHeader(HttpResponseMessage response)
    {
        var delta = response.Headers.RetryAfter?.Delta;

        return delta is null ? null : (int)Math.Ceiling(delta.Value.TotalSeconds);
    }

    private static bool IsNetworkFailure(Exception ex) =>
        ex is HttpRequestException or IOException or SocketException
        // Android'da tarmoq xatolari ba'zan Java istisnolari bo'lib keladi (Java.Net.UnknownHostException va hokazo).
        || ex.GetType().Namespace?.StartsWith("Java.", StringComparison.Ordinal) == true;

    private static JsonTypeInfo<T> TypeInfo<T>() =>
        (JsonTypeInfo<T>)ApiJsonContext.Default.Options.GetTypeInfo(typeof(T));
}
