namespace SelfLearn.Mobile.Services;

/// <summary>Server manzili. Qiymat build konfiguratsiyasidan keladi (SelfLearn.Mobile.csproj, <c>ApiBaseUrl</c>).</summary>
public sealed class ApiOptions
{
    public ApiOptions(Uri baseUri)
    {
        BaseUri = baseUri;
    }

    /// <summary>Doim "/" bilan tugaydi, shunda "api/v1/..." nisbiy yo'llari to'g'ri qo'shiladi.</summary>
    public Uri BaseUri { get; }

    public static ApiOptions FromBuild()
    {
        var configured = BuildConfig.ApiBaseUrl;

#if DEBUG
        // Faqat Debug'da: Desktop'da boshqa serverga tez ulanish uchun (SELFLEARN_API_URL=http://...).
        var fromEnvironment = Environment.GetEnvironmentVariable("SELFLEARN_API_URL");
        if (!string.IsNullOrWhiteSpace(fromEnvironment))
        {
            configured = fromEnvironment;
        }
#endif

        return new ApiOptions(Resolve(configured, OperatingSystem.IsAndroid()));
    }

    public static Uri Resolve(string configured, bool isAndroid)
    {
        if (!Uri.TryCreate(configured.Trim(), UriKind.Absolute, out var uri)
            || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            throw new ArgumentException($"Noto'g'ri server manzili: '{configured}'", nameof(configured));
        }

        var builder = new UriBuilder(uri);

        // Android emulatorda "localhost" emulatorning o'zi. Kompyuterga (host) 10.0.2.2 orqali chiqiladi.
        if (isAndroid && builder.Host is "localhost" or "127.0.0.1")
        {
            builder.Host = "10.0.2.2";
        }

        if (!builder.Path.EndsWith('/'))
        {
            builder.Path += "/";
        }

        return builder.Uri;
    }
}
