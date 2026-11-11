using System.Text.Json.Serialization;
using SelfLearn.Mobile.Models;

namespace SelfLearn.Mobile.Services;

/// <summary>
/// JSON uchun source generator: reflection ishlatilmaydi, shuning uchun Release'dagi trimming/AOT'da ham xavfsiz.
/// Yangi endpoint modeli qo'shilganda shu yerga [JsonSerializable] qo'shiladi.
/// </summary>
[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.SnakeCaseLower,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
[JsonSerializable(typeof(LoginRequest))]
[JsonSerializable(typeof(LoginResponse))]
[JsonSerializable(typeof(MeResponse))]
[JsonSerializable(typeof(ApiErrorBody))]
internal partial class ApiJsonContext : JsonSerializerContext
{
}
