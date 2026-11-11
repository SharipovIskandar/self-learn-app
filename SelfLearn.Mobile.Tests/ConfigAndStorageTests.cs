using System.Text.RegularExpressions;
using SelfLearn.Mobile.Services;

namespace SelfLearn.Mobile.Tests;

public class ApiOptionsTests
{
    [Fact]
    public void The_production_address_gets_a_trailing_slash()
    {
        Assert.Equal("https://self-learn.uz/", ApiOptions.Resolve("https://self-learn.uz", isAndroid: true).ToString());
        Assert.Equal("https://self-learn.uz/", ApiOptions.Resolve("https://self-learn.uz/", isAndroid: false).ToString());
    }

    [Fact]
    public void Relative_api_paths_resolve_under_the_base_address()
    {
        var baseUri = ApiOptions.Resolve("https://self-learn.uz", isAndroid: true);

        Assert.Equal("https://self-learn.uz/api/v1/auth/login", new Uri(baseUri, "api/v1/auth/login").ToString());
    }

    [Theory]
    [InlineData("http://localhost:8000", "http://10.0.2.2:8000/")]
    [InlineData("http://127.0.0.1:8000", "http://10.0.2.2:8000/")]
    public void On_android_localhost_becomes_the_emulator_host(string configured, string expected)
    {
        Assert.Equal(expected, ApiOptions.Resolve(configured, isAndroid: true).ToString());
    }

    [Fact]
    public void On_desktop_localhost_is_kept()
    {
        Assert.Equal("http://localhost:8000/", ApiOptions.Resolve("http://localhost:8000", isAndroid: false).ToString());
    }

    [Fact]
    public void On_android_real_hosts_are_never_rewritten()
    {
        Assert.Equal("https://self-learn.uz/", ApiOptions.Resolve("https://self-learn.uz", isAndroid: true).ToString());
        Assert.Equal("http://192.168.1.20:8000/", ApiOptions.Resolve("http://192.168.1.20:8000", isAndroid: true).ToString());
    }

    [Theory]
    [InlineData("")]
    [InlineData("self-learn.uz")]
    [InlineData("ftp://self-learn.uz")]
    public void Garbage_is_rejected(string configured)
    {
        Assert.Throws<ArgumentException>(() => ApiOptions.Resolve(configured, isAndroid: false));
    }

    [Fact]
    public void The_default_server_follows_the_build_configuration()
    {
        // Release -> production, Debug -> lokal backend. (-p:ApiBaseUrl=... bilan o'zgartirilgan build'da bu test mos kelmaydi.)
#if DEBUG
        Assert.Equal("http://localhost:8000", BuildConfig.ApiBaseUrl);
#else
        Assert.Equal("https://self-learn.uz", BuildConfig.ApiBaseUrl);
#endif
    }
}

public class DeviceIdentityTests
{
    [Fact]
    public void The_name_has_a_platform_and_a_short_install_id()
    {
        using var directory = new TempDirectory();

        var name = new DeviceIdentity(directory.Path).DeviceName;

        Assert.Matches(new Regex("^(Android|iOS|macOS|Windows|Linux|Browser|Qurilma) [0-9a-f]{6}$"), name);
    }

    [Fact]
    public void The_name_survives_restarts()
    {
        using var directory = new TempDirectory();

        var first = new DeviceIdentity(directory.Path).DeviceName;
        var second = new DeviceIdentity(directory.Path).DeviceName;

        Assert.Equal(first, second);
    }

    [Fact]
    public void Two_installs_get_different_names()
    {
        using var a = new TempDirectory();
        using var b = new TempDirectory();

        Assert.NotEqual(new DeviceIdentity(a.Path).DeviceName, new DeviceIdentity(b.Path).DeviceName);
    }
}

public class FileTokenStoreTests
{
    [Fact]
    public async Task A_missing_token_loads_as_null()
    {
        using var directory = new TempDirectory();

        Assert.Null(await new FileTokenStore(directory.Path).LoadAsync());
    }

    [Fact]
    public async Task A_saved_token_is_loaded_back_by_a_new_instance()
    {
        using var directory = new TempDirectory();

        await new FileTokenStore(directory.Path).SaveAsync("1|abc");

        Assert.Equal("1|abc", await new FileTokenStore(directory.Path).LoadAsync());
    }

    [Fact]
    public async Task Saving_again_replaces_the_token()
    {
        using var directory = new TempDirectory();
        var store = new FileTokenStore(directory.Path);

        await store.SaveAsync("a-very-long-first-token");
        await store.SaveAsync("2|new");

        Assert.Equal("2|new", await store.LoadAsync());
    }

    [Fact]
    public async Task Clear_removes_the_token_and_is_safe_to_repeat()
    {
        using var directory = new TempDirectory();
        var store = new FileTokenStore(directory.Path);
        await store.SaveAsync("1|abc");

        await store.ClearAsync();
        await store.ClearAsync();

        Assert.Null(await store.LoadAsync());
    }

    [Fact]
    public async Task The_token_file_is_readable_only_by_its_owner()
    {
        if (OperatingSystem.IsWindows())
        {
            return;
        }

        using var directory = new TempDirectory();
        await new FileTokenStore(directory.Path).SaveAsync("1|abc");

        var mode = File.GetUnixFileMode(Path.Combine(directory.Path, "auth-token"));

        Assert.Equal(UnixFileMode.UserRead | UnixFileMode.UserWrite, mode);
    }
}
