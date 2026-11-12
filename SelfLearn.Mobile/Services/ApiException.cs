namespace SelfLearn.Mobile.Services;

public enum ApiErrorKind
{
    /// <summary>Internet yo'q, server javob bermadi yoki vaqt tugadi.</summary>
    Network,

    /// <summary>401: token yo'q yoki yaroqsiz.</summary>
    Unauthorized,

    /// <summary>403, masalan <c>role_not_supported</c>.</summary>
    Forbidden,

    /// <summary>409: amalni hozirgi holatda bajarib bo'lmaydi, masalan <c>onboarding_required</c>.</summary>
    Conflict,

    /// <summary>422: kiritilgan ma'lumot noto'g'ri yoki email/parol xato.</summary>
    Validation,

    /// <summary>429: juda ko'p urinish.</summary>
    RateLimited,

    /// <summary>5xx: serverda xatolik.</summary>
    Server,

    /// <summary>Boshqa hamma narsa: 404 (API hali yangilanmagan), HTML javob, buzilgan JSON.</summary>
    Unexpected,
}

/// <summary>
/// API xatosi. <see cref="Exception.Message"/> foydalanuvchiga to'g'ridan-to'g'ri ko'rsatishga tayyor o'zbekcha matn.
/// </summary>
public sealed class ApiException : Exception
{
    private static readonly IReadOnlyDictionary<string, string[]> NoErrors = new Dictionary<string, string[]>();

    public ApiException(
        ApiErrorKind kind,
        string message,
        int? statusCode = null,
        string? code = null,
        IReadOnlyDictionary<string, string[]>? fieldErrors = null,
        int? retryAfterSeconds = null,
        Exception? inner = null)
        : base(message, inner)
    {
        Kind = kind;
        StatusCode = statusCode;
        Code = code;
        FieldErrors = fieldErrors ?? NoErrors;
        RetryAfterSeconds = retryAfterSeconds;
    }

    public ApiErrorKind Kind { get; }

    /// <summary>HTTP status; tarmoq xatosida null.</summary>
    public int? StatusCode { get; }

    /// <summary>Backendning barqaror xato kodi (<c>role_not_supported</c>, <c>too_many_attempts</c>), bo'lmasa null.</summary>
    public string? Code { get; }

    /// <summary>422 dagi maydon xatolari: maydon nomi -> xabarlar.</summary>
    public IReadOnlyDictionary<string, string[]> FieldErrors { get; }

    public int? RetryAfterSeconds { get; }
}

/// <summary>Foydalanuvchiga ko'rsatiladigan standart xato matnlari.</summary>
internal static class ApiMessages
{
    public const string NoConnection = "Serverga ulanib bo'lmadi. Internetni tekshirib, qayta urinib ko'ring.";
    public const string Timeout = "Server javob bermadi. Birozdan keyin qayta urinib ko'ring.";
    public const string SessionExpired = "Sessiya tugagan. Iltimos, qayta kiring.";
    public const string Forbidden = "Bu amal uchun ruxsat yo'q.";
    public const string Validation = "Kiritilgan ma'lumotlar noto'g'ri.";
    public const string TooManyRequests = "Juda ko'p urinish. Birozdan keyin qayta urinib ko'ring.";
    public const string Server = "Serverda xatolik yuz berdi. Birozdan keyin qayta urinib ko'ring.";
    public const string Unexpected = "Serverdan kutilmagan javob keldi.";
}
