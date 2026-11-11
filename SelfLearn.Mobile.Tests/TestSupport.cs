using System.Net;
using System.Text;
using SelfLearn.Mobile.Services;
using SelfLearn.Mobile.ViewModels;

namespace SelfLearn.Mobile.Tests;

/// <summary>Backend'ning haqiqiy javoblaridan olingan namunalar (docs/PLAN.md, "API v1").</summary>
internal static class Samples
{
    public const string LoginOk =
        """{"token":"1|abcdef123456","token_type":"Bearer","user":{"id":1,"name":"Solo Test","email":"solo@example.test","role":"solo_learner","onboarding_completed":true}}""";

    public const string LoginOkNeedsOnboarding =
        """{"token":"2|zzz","token_type":"Bearer","user":{"id":2,"name":"Newbie Test","email":"newbie@example.test","role":"solo_learner","onboarding_completed":false}}""";

    public const string Me =
        """{"user":{"id":1,"name":"Solo Test","email":"solo@example.test","role":"solo_learner","onboarding_completed":true}}""";

    public const string InvalidCredentials =
        """{"message":"Email yoki parol noto'g'ri.","errors":{"email":["Email yoki parol noto'g'ri."]}}""";

    public const string RoleNotSupported =
        """{"message":"Mobil ilova hozircha faqat mustaqil o'quvchilar uchun. Iltimos, veb-saytdan foydalaning.","code":"role_not_supported"}""";

    public const string TooManyAttempts =
        """{"message":"Juda ko'p kirish urinishi. 60 soniyadan keyin qayta urinib ko'ring.","code":"too_many_attempts","retry_after":60}""";

    public const string Unauthenticated = """{"message":"Unauthenticated."}""";
}

internal sealed record RecordedRequest(HttpMethod Method, string Path, string? Authorization, string? Body);

/// <summary>Soxta HTTP server: so'rovlarni yozib oladi va berilgan javobni qaytaradi.</summary>
internal sealed class StubHandler : HttpMessageHandler
{
    private readonly Func<HttpRequestMessage, HttpResponseMessage> _respond;

    public StubHandler(Func<HttpRequestMessage, HttpResponseMessage> respond)
    {
        _respond = respond;
    }

    public StubHandler(HttpStatusCode status, string? json = null)
        : this(_ => Json(status, json))
    {
    }

    public List<RecordedRequest> Requests { get; } = new();

    public static HttpResponseMessage Json(HttpStatusCode status, string? json) =>
        new(status) { Content = json is null ? null : new StringContent(json, Encoding.UTF8, "application/json") };

    public static HttpResponseMessage Html(HttpStatusCode status) =>
        new(status) { Content = new StringContent("<html><body>Bad gateway</body></html>", Encoding.UTF8, "text/html") };

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var body = request.Content is null ? null : await request.Content.ReadAsStringAsync(cancellationToken);
        Requests.Add(new RecordedRequest(request.Method, request.RequestUri!.AbsolutePath, request.Headers.Authorization?.ToString(), body));

        return _respond(request);
    }
}

internal sealed class InMemoryTokenStore : ITokenStore
{
    public string? Current { get; set; }

    public Task<string?> LoadAsync() => Task.FromResult(Current);

    public Task SaveAsync(string token)
    {
        Current = token;

        return Task.CompletedTask;
    }

    public Task ClearAsync()
    {
        Current = null;

        return Task.CompletedTask;
    }
}

internal sealed class FakeNavigator : INavigator
{
    public List<Type> Visited { get; } = new();

    public void GoTo<TViewModel>() where TViewModel : ViewModelBase => Visited.Add(typeof(TViewModel));
}

internal sealed class TempDirectory : IDisposable
{
    public TempDirectory()
    {
        Path = Directory.CreateTempSubdirectory("selflearn-tests-").FullName;
    }

    public string Path { get; }

    public void Dispose()
    {
        try
        {
            Directory.Delete(Path, recursive: true);
        }
        catch (IOException)
        {
            // Vaqtinchalik papka: o'chmasa ham test natijasiga ta'sir qilmaydi.
        }
    }
}

/// <summary>Haqiqiy ApiClient + AuthService, faqat HTTP va token saqlash soxta.</summary>
internal sealed class Harness : IDisposable
{
    private readonly TempDirectory _directory = new();

    public Harness(StubHandler handler, string? savedToken = null)
    {
        Handler = handler;
        Tokens = new InMemoryTokenStore { Current = savedToken };
        Device = new DeviceIdentity(_directory.Path);

        var http = new HttpClient(handler) { BaseAddress = new Uri("https://self-learn.test/") };
        Api = new ApiClient(http, Session);
        Auth = new AuthService(Api, Tokens, Session, Device);
    }

    public StubHandler Handler { get; }

    public AuthSession Session { get; } = new();

    public InMemoryTokenStore Tokens { get; }

    public DeviceIdentity Device { get; }

    public ApiClient Api { get; }

    public AuthService Auth { get; }

    public void Dispose() => _directory.Dispose();
}
