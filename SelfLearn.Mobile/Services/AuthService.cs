using System.Diagnostics;
using SelfLearn.Mobile.Models;

namespace SelfLearn.Mobile.Services;

/// <summary>Kirish, sessiyani tiklash va chiqish.</summary>
public sealed class AuthService
{
    private readonly ApiClient _api;
    private readonly ITokenStore _tokens;
    private readonly AuthSession _session;
    private readonly DeviceIdentity _device;

    public AuthService(ApiClient api, ITokenStore tokens, AuthSession session, DeviceIdentity device)
    {
        _api = api;
        _tokens = tokens;
        _session = session;
        _device = device;

        _api.Unauthorized += OnUnauthorized;
    }

    /// <summary>Kirgan foydalanuvchining tokeni serverda yaroqsiz bo'lib qoldi. Ilova Login'ga qaytishi kerak.</summary>
    public event Action? SessionExpired;

    public UserDto? CurrentUser => _session.User;

    /// <summary>Email va parol bilan kiradi, tokenni qurilmada saqlaydi. Xato bo'lsa <see cref="ApiException"/>.</summary>
    public async Task<UserDto> LoginAsync(string email, string password, CancellationToken ct = default)
    {
        var response = await _api.PostAsync<LoginRequest, LoginResponse>(
            "api/v1/auth/login",
            new LoginRequest(email.Trim(), password, _device.DeviceName),
            ct);

        _session.Token = response.Token;
        _session.User = response.User;

        try
        {
            await _tokens.SaveAsync(response.Token);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // Saqlab bo'lmadi: bu sessiya ishlayveradi, faqat ilova qayta ochilganda qayta kirish kerak bo'ladi.
            Debug.WriteLine($"Tokenni saqlab bo'lmadi: {ex.Message}");
        }

        return response.User;
    }

    /// <summary>
    /// Ilova ochilganda saqlangan token bo'yicha foydalanuvchini tiklaydi.
    /// Token yo'q yoki serverda yaroqsiz bo'lsa null qaytaradi. Tarmoq/server xatosida <see cref="ApiException"/>
    /// tashlaydi va tokenni o'chirmaydi (internet qaytgach qayta urinish mumkin).
    /// </summary>
    public async Task<UserDto?> RestoreSessionAsync(CancellationToken ct = default)
    {
        var token = await _tokens.LoadAsync();
        if (string.IsNullOrWhiteSpace(token))
        {
            return null;
        }

        _session.Token = token;

        try
        {
            var response = await _api.GetAsync<MeResponse>("api/v1/me", ct);
            _session.User = response.User;

            return response.User;
        }
        catch (ApiException ex) when (ex.Kind == ApiErrorKind.Unauthorized)
        {
            // Token serverda o'chirilgan yoki yaroqsiz: lokal ma'lumotni tozalab, qayta kirishni so'raymiz.
            await ClearLocalAsync();

            return null;
        }
    }

    /// <summary>
    /// Chiqish. Serverga yetib bormasa ham (internet yo'q) qurilmadagi sessiya baribir tozalanadi.
    /// </summary>
    public async Task LogoutAsync(CancellationToken ct = default)
    {
        // Foydalanuvchi o'zi chiqyapti: serverdan 401 kelsa ham "sessiya tugadi" deb e'lon qilinmasin.
        _session.User = null;

        try
        {
            await _api.PostAsync("api/v1/auth/logout", ct);
        }
        catch (ApiException)
        {
            // Token serverda qolib ketishi mumkin, lekin qurilmada endi yo'q.
        }

        await ClearLocalAsync();
    }

    private void OnUnauthorized()
    {
        // Kirish va sessiyani tiklash paytida (User hali yo'q) 401 ni ularning o'zi hal qiladi.
        if (_session.User is null)
        {
            return;
        }

        _session.Clear();
        _ = _tokens.ClearAsync();
        SessionExpired?.Invoke();
    }

    private async Task ClearLocalAsync()
    {
        _session.Clear();
        await _tokens.ClearAsync();
    }
}
