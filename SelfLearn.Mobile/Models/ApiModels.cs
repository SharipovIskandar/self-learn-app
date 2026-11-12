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

// ---- GET /api/v1/solo/dashboard ----

public sealed record DashboardUser(string Name, int XpPoints, int Level, string LevelName, int LevelProgress, int StreakDays);

public sealed record DashboardCefr(string Current, string? Next, int MasteryPct);

/// <summary>Takrorlashga tayyor ("due") va jami so'zlar soni.</summary>
public sealed record DashboardSrs(int Due, int Total);

public sealed record NextLesson(string Slug, string Title, string? Topic, string Level, bool Resume);

public sealed record DashboardResponse(
    string Greeting,
    DashboardUser User,
    DashboardCefr Cefr,
    DashboardSrs Srs,
    NextLesson? NextLesson,
    int TodayPracticeDone,
    Dictionary<string, int> SkillMastery);
