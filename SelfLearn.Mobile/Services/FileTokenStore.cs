using System.Text;

namespace SelfLearn.Mobile.Services;

/// <summary>Kirish tokenini qurilmada saqlaydi, shunda ilova qayta ochilganda foydalanuvchi tizimda qoladi.</summary>
public interface ITokenStore
{
    Task<string?> LoadAsync();

    Task SaveAsync(string token);

    Task ClearAsync();
}

/// <summary>
/// Token ilovaning shaxsiy papkasidagi oddiy faylda saqlanadi (Unix'da faqat egasi o'qiy oladi).
/// TODO: release'dan oldin Android Keystore bilan almashtiriladi (docs/PLAN.md, 6-bosqich).
/// </summary>
public sealed class FileTokenStore : ITokenStore
{
    private readonly string _path;

    public FileTokenStore(string directory)
    {
        Directory.CreateDirectory(directory);
        _path = Path.Combine(directory, "auth-token");
    }

    public async Task<string?> LoadAsync()
    {
        try
        {
            if (!File.Exists(_path))
            {
                return null;
            }

            var token = (await File.ReadAllTextAsync(_path)).Trim();

            return token.Length == 0 ? null : token;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return null;
        }
    }

    public async Task SaveAsync(string token)
    {
        var options = new FileStreamOptions { Mode = FileMode.Create, Access = FileAccess.Write };
        if (!OperatingSystem.IsWindows())
        {
            // Fayl yaratilayotgan paytdayoq boshqalar o'qiy olmasligi uchun.
            options.UnixCreateMode = UnixFileMode.UserRead | UnixFileMode.UserWrite;
        }

        await using var stream = new FileStream(_path, options);
        await stream.WriteAsync(Encoding.UTF8.GetBytes(token));
    }

    public Task ClearAsync()
    {
        try
        {
            File.Delete(_path);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // O'chirib bo'lmasa ham lokal sessiya xotiradan tozalanadi; keyingi ishga tushishda token baribir rad etiladi.
        }

        return Task.CompletedTask;
    }
}
