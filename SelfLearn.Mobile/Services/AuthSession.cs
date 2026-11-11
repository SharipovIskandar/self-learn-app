using SelfLearn.Mobile.Models;

namespace SelfLearn.Mobile.Services;

/// <summary>Joriy sessiya (xotirada): token va kirgan foydalanuvchi. <see cref="ApiClient"/> tokenni shu yerdan oladi.</summary>
public sealed class AuthSession
{
    public string? Token { get; set; }

    public UserDto? User { get; set; }

    public void Clear()
    {
        Token = null;
        User = null;
    }
}
