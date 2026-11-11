namespace SelfLearn.Mobile.Services;

public static class AppPaths
{
    /// <summary>
    /// Ilovaning shaxsiy ma'lumotlar papkasi. Android'da bu ilovaning yopiq papkasi (boshqa ilovalar o'qiy olmaydi),
    /// Desktop'da foydalanuvchining LocalApplicationData papkasi.
    /// </summary>
    public static string DataDirectory
    {
        get
        {
            var directory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "self-learn");
            Directory.CreateDirectory(directory);

            return directory;
        }
    }
}
