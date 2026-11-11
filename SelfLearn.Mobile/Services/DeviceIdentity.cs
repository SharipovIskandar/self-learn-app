namespace SelfLearn.Mobile.Services;

/// <summary>
/// Qurilma nomi (masalan "Android 3f9a1c"). Backend tokenni shu nom bilan saqlaydi va shu nomdagi eski tokenni
/// almashtiradi, shuning uchun nom har bir o'rnatish uchun noyob bo'lishi kerak: telefon va planshet bir-birini
/// tizimdan chiqarib yubormasin. Noyob qism birinchi ishga tushishda yaratilib, faylda saqlanadi.
/// </summary>
public sealed class DeviceIdentity
{
    public DeviceIdentity(string directory)
    {
        DeviceName = $"{PlatformLabel()} {LoadOrCreateInstallId(Path.Combine(directory, "install-id"))}";
    }

    public string DeviceName { get; }

    private static string PlatformLabel()
    {
        if (OperatingSystem.IsAndroid())
        {
            return "Android";
        }

        if (OperatingSystem.IsIOS())
        {
            return "iOS";
        }

        if (OperatingSystem.IsMacOS())
        {
            return "macOS";
        }

        if (OperatingSystem.IsWindows())
        {
            return "Windows";
        }

        if (OperatingSystem.IsLinux())
        {
            return "Linux";
        }

        return OperatingSystem.IsBrowser() ? "Browser" : "Qurilma";
    }

    private static string LoadOrCreateInstallId(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                var existing = File.ReadAllText(path).Trim();
                if (existing.Length > 0)
                {
                    return existing;
                }
            }

            var created = Guid.NewGuid().ToString("N")[..6];
            File.WriteAllText(path, created);

            return created;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // Saqlab bo'lmasa shu ishga tushish uchun tasodifiy qiymat: ilova ishlayveradi.
            return Guid.NewGuid().ToString("N")[..6];
        }
    }
}
