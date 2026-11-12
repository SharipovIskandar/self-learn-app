using System.Net;
using Microsoft.Extensions.DependencyInjection;
using SelfLearn.Mobile.Services;
using SelfLearn.Mobile.ViewModels;

namespace SelfLearn.Mobile.Tests;

public class DashboardViewModelTests
{
    /// <summary>Login va dashboard'ga javob beradigan soxta server; tizimga kirgan foydalanuvchi bilan.</summary>
    private static async Task<(DashboardViewModel Vm, Harness H)> SignedIn(string dashboardJson, string loginJson = Samples.LoginOk, HttpStatusCode dashboardStatus = HttpStatusCode.OK)
    {
        var handler = StubHandler.Routes(path => path switch
        {
            "/api/v1/auth/login" => StubHandler.Json(HttpStatusCode.OK, loginJson),
            "/api/v1/solo/dashboard" => StubHandler.Json(dashboardStatus, dashboardJson),
            _ => StubHandler.Json(HttpStatusCode.NotFound, null),
        });
        var harness = new Harness(handler);
        await harness.Auth.LoginAsync("solo@example.test", "pw");

        return (new DashboardViewModel(new DashboardService(harness.Api), harness.Auth), harness);
    }

    [Fact]
    public async Task It_formats_the_server_data_for_the_screen()
    {
        var (vm, h) = await SignedIn(Samples.Dashboard);
        using var _ = h;

        await vm.LoadAsync();

        Assert.True(vm.HasData);
        Assert.Equal("Xayrli tong, Solo Test!", vm.GreetingText);
        Assert.Equal("2-daraja · Qiziquvchi", vm.LevelText);
        Assert.Equal(40, vm.LevelProgress);
        Assert.Equal("120 XP", vm.XpText);
        Assert.Equal("4 kun ketma-ket", vm.StreakText);
        Assert.Equal("Til darajasi: B1 → B2", vm.CefrText);
        Assert.Equal(35, vm.CefrMastery);
        Assert.Equal("5 ta so'zni takrorlash vaqti keldi", vm.SrsText);
        Assert.True(vm.HasNextLesson);
        Assert.Equal("To be", vm.NextLessonTitle);
        Assert.Equal("A1 · yangi dars", vm.NextLessonMeta);
        Assert.Equal("", vm.ErrorMessage);
        Assert.False(vm.IsLoading);
    }

    [Fact]
    public async Task A_brand_new_learner_sees_friendly_empty_states()
    {
        var (vm, h) = await SignedIn(Samples.DashboardEmpty);
        using var _ = h;

        await vm.LoadAsync();

        Assert.True(vm.HasData);
        Assert.False(vm.HasNextLesson);
        Assert.Equal("Bugun boshlang!", vm.StreakText);
        Assert.Equal("Lug'atingiz hali bo'sh", vm.SrsText);
    }

    [Fact]
    public async Task Nothing_to_review_today_says_so()
    {
        var json = Samples.Dashboard.Replace("\"srs\":{\"due\":5,\"total\":12}", "\"srs\":{\"due\":0,\"total\":12}");
        var (vm, h) = await SignedIn(json);
        using var _ = h;

        await vm.LoadAsync();

        Assert.Equal("Bugun takrorlash kerak emas", vm.SrsText);
    }

    [Fact]
    public async Task It_requests_the_dashboard_with_the_token()
    {
        var (vm, h) = await SignedIn(Samples.Dashboard);
        using var _ = h;

        await vm.LoadAsync();

        var request = h.Handler.Requests.Last();
        Assert.Equal("/api/v1/solo/dashboard", request.Path);
        Assert.Equal("Bearer 1|abcdef123456", request.Authorization);
    }

    [Fact]
    public async Task A_learner_without_placement_test_sees_the_notice_and_no_request_is_made()
    {
        var (vm, h) = await SignedIn(Samples.Dashboard, Samples.LoginOkNeedsOnboarding);
        using var _ = h;
        var before = h.Handler.Requests.Count;

        await vm.LoadAsync();

        Assert.True(vm.NeedsPlacementTest);
        Assert.False(vm.HasData);
        Assert.Equal(before, h.Handler.Requests.Count);
    }

    [Fact]
    public async Task A_409_from_the_server_also_shows_the_placement_notice()
    {
        var (vm, h) = await SignedIn(Samples.OnboardingRequired, dashboardStatus: HttpStatusCode.Conflict);
        using var _ = h;

        await vm.LoadAsync();

        Assert.True(vm.NeedsPlacementTest);
        Assert.Equal("", vm.ErrorMessage);
    }

    [Fact]
    public async Task A_failed_refresh_keeps_the_old_data_and_shows_the_error()
    {
        var offline = false;
        var handler = StubHandler.Routes(path => path switch
        {
            "/api/v1/auth/login" => StubHandler.Json(HttpStatusCode.OK, Samples.LoginOk),
            _ when offline => throw new HttpRequestException("offline"),
            _ => StubHandler.Json(HttpStatusCode.OK, Samples.Dashboard),
        });
        using var h = new Harness(handler);
        await h.Auth.LoginAsync("solo@example.test", "pw");
        var vm = new DashboardViewModel(new DashboardService(h.Api), h.Auth);
        await vm.LoadAsync();

        offline = true;
        await vm.LoadAsync();

        Assert.True(vm.HasData);
        Assert.Equal("Xayrli tong, Solo Test!", vm.GreetingText);
        Assert.Contains("Serverga ulanib bo'lmadi", vm.ErrorMessage);

        offline = false;
        await vm.LoadAsync();

        Assert.Equal("", vm.ErrorMessage);
    }

    [Fact]
    public async Task The_load_command_is_what_pull_to_refresh_and_retry_call()
    {
        var (vm, h) = await SignedIn(Samples.Dashboard);
        using var _ = h;

        await vm.LoadCommand.ExecuteAsync(null);

        Assert.True(vm.HasData);
    }
}

public class MainTabsViewModelTests
{
    private static (MainTabsViewModel Vm, Harness H) Create()
    {
        var handler = StubHandler.Routes(path => path switch
        {
            "/api/v1/auth/login" => StubHandler.Json(HttpStatusCode.OK, Samples.LoginOk),
            "/api/v1/solo/dashboard" => StubHandler.Json(HttpStatusCode.OK, Samples.Dashboard),
            _ => StubHandler.Json(HttpStatusCode.NotFound, null),
        });
        var h = new Harness(handler);
        var nav = new FakeNavigator();
        var dashboard = new DashboardViewModel(new DashboardService(h.Api), h.Auth);

        return (new MainTabsViewModel(dashboard, new LessonsViewModel(), new VocabViewModel(), new ProfileViewModel(h.Auth, nav)), h);
    }

    [Fact]
    public void It_has_the_four_tabs_in_order_and_starts_on_the_dashboard()
    {
        var (vm, h) = Create();
        using var _ = h;

        Assert.Equal(["Bosh sahifa", "Darslar", "So'zlar", "Profil"], vm.Tabs.Select(t => t.Title));
        Assert.IsType<DashboardViewModel>(vm.SelectedTab);
    }

    [Fact]
    public async Task Opening_the_screen_loads_the_dashboard()
    {
        var (vm, h) = Create();
        using var _ = h;
        await h.Auth.LoginAsync("solo@example.test", "pw");

        await vm.OnNavigatedToAsync();

        Assert.True(((DashboardViewModel)vm.Tabs[0]).HasData);
    }
}

public class ProfileViewModelTests
{
    [Fact]
    public async Task It_shows_the_signed_in_user()
    {
        using var h = new Harness(new StubHandler(HttpStatusCode.OK, Samples.LoginOk));
        await h.Auth.LoginAsync("solo@example.test", "pw");

        var vm = new ProfileViewModel(h.Auth, new FakeNavigator());

        Assert.Equal("Solo Test", vm.Name);
        Assert.Equal("solo@example.test", vm.Email);
    }

    [Fact]
    public async Task Logout_returns_to_login_and_forgets_the_session()
    {
        using var h = new Harness(StubHandler.Routes(path => path == "/api/v1/auth/login"
            ? StubHandler.Json(HttpStatusCode.OK, Samples.LoginOk)
            : StubHandler.Json(HttpStatusCode.NoContent, null)));
        await h.Auth.LoginAsync("solo@example.test", "pw");
        var nav = new FakeNavigator();
        var vm = new ProfileViewModel(h.Auth, nav);

        await vm.LogoutCommand.ExecuteAsync(null);

        Assert.Equal(typeof(LoginViewModel), Assert.Single(nav.Visited));
        Assert.Null(h.Auth.CurrentUser);
        Assert.Null(h.Tokens.Current);
    }
}

public class SessionExpiryTests
{
    private static Harness SignedInHarness(HttpStatusCode afterLogin)
    {
        var handler = StubHandler.Routes(path => path == "/api/v1/auth/login"
            ? StubHandler.Json(HttpStatusCode.OK, Samples.LoginOk)
            : StubHandler.Json(afterLogin, Samples.Unauthenticated));

        return new Harness(handler);
    }

    [Fact]
    public async Task ApiClient_raises_unauthorized_when_a_token_request_gets_401()
    {
        using var h = new Harness(new StubHandler(HttpStatusCode.Unauthorized, Samples.Unauthenticated));
        h.Session.Token = "stale";
        var raised = 0;
        h.Api.Unauthorized += () => raised++;

        await Assert.ThrowsAsync<ApiException>(() => h.Api.GetAsync<SelfLearn.Mobile.Models.MeResponse>("api/v1/me"));

        Assert.Equal(1, raised);
    }

    [Fact]
    public async Task ApiClient_does_not_raise_unauthorized_without_a_token()
    {
        using var h = new Harness(new StubHandler(HttpStatusCode.Unauthorized, Samples.Unauthenticated));
        var raised = 0;
        h.Api.Unauthorized += () => raised++;

        await Assert.ThrowsAsync<ApiException>(() => h.Api.GetAsync<SelfLearn.Mobile.Models.MeResponse>("api/v1/me"));

        Assert.Equal(0, raised);
    }

    [Fact]
    public async Task A_signed_in_user_whose_token_is_revoked_is_signed_out_and_told()
    {
        using var h = SignedInHarness(HttpStatusCode.Unauthorized);
        await h.Auth.LoginAsync("solo@example.test", "pw");
        var expired = 0;
        h.Auth.SessionExpired += () => expired++;

        await Assert.ThrowsAsync<ApiException>(() => new DashboardService(h.Api).GetAsync());

        Assert.Equal(1, expired);
        Assert.Null(h.Auth.CurrentUser);
        Assert.Null(h.Session.Token);
        Assert.Null(h.Tokens.Current);
    }

    [Fact]
    public async Task A_rejected_token_at_startup_is_not_announced_as_an_expiry()
    {
        using var h = new Harness(new StubHandler(HttpStatusCode.Unauthorized, Samples.Unauthenticated), savedToken: "old");
        var expired = 0;
        h.Auth.SessionExpired += () => expired++;

        await h.Auth.RestoreSessionAsync();

        Assert.Equal(0, expired);
    }

    [Fact]
    public async Task Logging_out_with_an_already_revoked_token_is_not_announced_as_an_expiry()
    {
        using var h = SignedInHarness(HttpStatusCode.Unauthorized);
        await h.Auth.LoginAsync("solo@example.test", "pw");
        var expired = 0;
        h.Auth.SessionExpired += () => expired++;

        await h.Auth.LogoutAsync();

        Assert.Equal(0, expired);
        Assert.Null(h.Tokens.Current);
    }

    [Fact]
    public async Task The_shell_returns_to_login_with_a_message_when_the_session_expires()
    {
        using var h = SignedInHarness(HttpStatusCode.Unauthorized);
        var services = new ServiceCollection();
        services.AddSingleton(h.Auth);
        services.AddSingleton<ShellViewModel>();
        services.AddSingleton<INavigator>(sp => sp.GetRequiredService<ShellViewModel>());
        services.AddTransient<LoginViewModel>();
        using var provider = services.BuildServiceProvider();
        var shell = provider.GetRequiredService<ShellViewModel>();
        await h.Auth.LoginAsync("solo@example.test", "pw");

        await Assert.ThrowsAsync<ApiException>(() => new DashboardService(h.Api).GetAsync());

        var login = Assert.IsType<LoginViewModel>(shell.Current);
        Assert.Equal("Sessiya tugagan. Iltimos, qayta kiring.", login.ErrorMessage);
    }
}
