namespace SelfLearn.Mobile.Models;

// Backend API v1 shartnomasi (docs/PLAN.md, "API v1"). JSON'da nomlar snake_case:
// OnboardingCompleted <-> onboarding_completed, DeviceName <-> device_name va hokazo.

/// <summary>Backenddagi `user` obyekti.</summary>
public sealed record UserDto(int Id, string Name, string Email, string Role, bool OnboardingCompleted);

/// <summary>POST /api/v1/auth/login tanasi.</summary>
public sealed record LoginRequest(string Email, string Password, string DeviceName);

/// <summary>POST /api/v1/auth/login javobi.</summary>
public sealed record LoginResponse(string Token, string TokenType, UserDto User);

/// <summary>GET /api/v1/me javobi.</summary>
public sealed record MeResponse(UserDto User);

/// <summary>Xato javobi tanasi: {message, code?, errors?, retry_after?}.</summary>
public sealed record ApiErrorBody(string? Message, string? Code, Dictionary<string, string[]>? Errors, int? RetryAfter);
