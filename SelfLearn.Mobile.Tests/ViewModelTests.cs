using System.Net;
using Microsoft.Extensions.DependencyInjection;
using SelfLearn.Mobile.ViewModels;

namespace SelfLearn.Mobile.Tests;

public class LoginViewModelTests
{
    private static (LoginViewModel Vm, Harness H, FakeNavigator Nav) Create(StubHandler handler)
    {
        var harness = new Harness(handler);
        var nav = new FakeNavigator();

        return (new LoginViewModel(harness.Auth, nav), harness, nav);
    }

    [Fact]
    public async Task Empty_fields_show_a_message_and_make_no_request()
    {
        var (vm, h, nav) = Create(new StubHandler(HttpStatusCode.OK, Samples.LoginOk));
        using var _ = h;

        await vm.LoginCommand.ExecuteAsync(null);

        Assert.Equal("Email va parolni kiriting", vm.ErrorMessage);
        Assert.Empty(h.Handler.Requests);
        Assert.Empty(nav.Visited);
    }

    [Fact]
    public async Task An_email_without_an_at_sign_is_rejected_locally()
    {
        var (vm, h, _) = Create(new StubHandler(HttpStatusCode.OK, Samples.LoginOk));
        using var _ = h;
        vm.Email = "solo.example.test";
        vm.Password = "pw";

        await vm.LoginCommand.ExecuteAsync(null);

        Assert.Equal("Email manzilni to'g'ri kiriting", vm.ErrorMessage);
        Assert.Empty(h.Handler.Requests);
    }

    [Fact]
    public async Task Wrong_credentials_show_the_server_message_and_stay_on_the_login_screen()
    {
        var (vm, h, nav) = Create(new StubHandler(HttpStatusCode.UnprocessableEntity, Samples.InvalidCredentials));
        using var _ = h;
        vm.Email = "solo@example.test";
        vm.Password = "wrong";

        await vm.LoginCommand.ExecuteAsync(null);

        Assert.Equal("Email yoki parol noto'g'ri.", vm.ErrorMessage);
        Assert.False(vm.IsBusy);
        Assert.Empty(nav.Visited);
        Assert.Equal("wrong", vm.Password);
    }

    [Fact]
    public async Task A_successful_login_opens_home_and_clears_the_password()
    {
        var (vm, h, nav) = Create(new StubHandler(HttpStatusCode.OK, Samples.LoginOk));
        using var _ = h;
        vm.Email = "solo@example.test";
        vm.Password = "password123";

        await vm.LoginCommand.ExecuteAsync(null);

        Assert.Equal(typeof(HomeViewModel), Assert.Single(nav.Visited));
        Assert.Equal("", vm.Password);
        Assert.Equal("", vm.ErrorMessage);
        Assert.False(vm.IsBusy);
    }

    [Fact]
    public async Task An_unsupported_role_shows_the_server_message()
    {
        var (vm, h, nav) = Create(new StubHandler(HttpStatusCode.Forbidden, Samples.RoleNotSupported));
        using var _ = h;
        vm.Email = "teacher@example.test";
        vm.Password = "password123";

        await vm.LoginCommand.ExecuteAsync(null);

        Assert.Contains("faqat mustaqil o'quvchilar uchun", vm.ErrorMessage);
        Assert.Empty(nav.Visited);
    }

    [Fact]
    public async Task A_network_failure_shows_the_connection_message_and_frees_the_button()
    {
        var (vm, h, nav) = Create(new StubHandler(_ => throw new HttpRequestException("offline")));
        using var _ = h;
        vm.Email = "solo@example.test";
        vm.Password = "password123";

        await vm.LoginCommand.ExecuteAsync(null);

        Assert.Contains("Serverga ulanib bo'lmadi", vm.ErrorMessage);
        Assert.False(vm.IsBusy);
        Assert.True(vm.LoginCommand.CanExecute(null));
        Assert.Empty(nav.Visited);
    }
}

public class StartupViewModelTests
{
    [Fact]
    public async Task Without_a_saved_session_it_opens_login()
    {
        using var h = new Harness(new StubHandler(HttpStatusCode.OK, Samples.Me));
        var nav = new FakeNavigator();

        await new StartupViewModel(h.Auth, nav).OnNavigatedToAsync();

        Assert.Equal(typeof(LoginViewModel), Assert.Single(nav.Visited));
    }

    [Fact]
    public async Task With_a_valid_session_it_opens_home()
    {
        using var h = new Harness(new StubHandler(HttpStatusCode.OK, Samples.Me), savedToken: "1|abc");
        var nav = new FakeNavigator();

        await new StartupViewModel(h.Auth, nav).OnNavigatedToAsync();

        Assert.Equal(typeof(HomeViewModel), Assert.Single(nav.Visited));
    }

    [Fact]
    public async Task With_a_rejected_session_it_opens_login()
    {
        using var h = new Harness(new StubHandler(HttpStatusCode.Unauthorized, Samples.Unauthenticated), savedToken: "old");
        var nav = new FakeNavigator();

        await new StartupViewModel(h.Auth, nav).OnNavigatedToAsync();

        Assert.Equal(typeof(LoginViewModel), Assert.Single(nav.Visited));
    }

    [Fact]
    public async Task Without_internet_it_offers_a_retry_and_keeps_the_user_signed_in()
    {
        var offline = true;
        using var h = new Harness(
            new StubHandler(_ => offline ? throw new HttpRequestException("offline") : StubHandler.Json(HttpStatusCode.OK, Samples.Me)),
            savedToken: "1|abc");
        var nav = new FakeNavigator();
        var vm = new StartupViewModel(h.Auth, nav);

        await vm.OnNavigatedToAsync();

        Assert.Contains("Serverga ulanib bo'lmadi", vm.ErrorMessage);
        Assert.False(vm.IsLoading);
        Assert.Empty(nav.Visited);
        Assert.Equal("1|abc", h.Tokens.Current);

        // Internet qaytdi: "Qayta urinish" tugmasi.
        offline = false;
        await vm.LoadCommand.ExecuteAsync(null);

        Assert.Equal(typeof(HomeViewModel), Assert.Single(nav.Visited));
    }
}

public class HomeViewModelTests
{
    [Fact]
    public async Task It_greets_the_user_and_hides_the_placement_hint_when_onboarding_is_done()
    {
        using var h = new Harness(new StubHandler(HttpStatusCode.OK, Samples.LoginOk));
        await h.Auth.LoginAsync("solo@example.test", "pw");

        var vm = new HomeViewModel(h.Auth, new FakeNavigator());

        Assert.Equal("Salom, Solo Test!", vm.Greeting);
        Assert.False(vm.NeedsPlacementTest);
    }

    [Fact]
    public async Task It_shows_the_placement_hint_for_a_new_learner()
    {
        using var h = new Harness(new StubHandler(HttpStatusCode.OK, Samples.LoginOkNeedsOnboarding));
        await h.Auth.LoginAsync("newbie@example.test", "pw");

        var vm = new HomeViewModel(h.Auth, new FakeNavigator());

        Assert.True(vm.NeedsPlacementTest);
    }

    [Fact]
    public async Task Logout_returns_to_login_and_forgets_the_session()
    {
        using var h = new Harness(new StubHandler(_ => StubHandler.Json(HttpStatusCode.OK, Samples.LoginOk)));
        await h.Auth.LoginAsync("solo@example.test", "pw");
        var nav = new FakeNavigator();
        var vm = new HomeViewModel(h.Auth, nav);

        await vm.LogoutCommand.ExecuteAsync(null);

        Assert.Equal(typeof(LoginViewModel), Assert.Single(nav.Visited));
        Assert.Null(h.Auth.CurrentUser);
        Assert.Null(h.Tokens.Current);
    }
}

public class ShellViewModelTests
{
    private sealed class ProbeViewModel : ViewModelBase, INavigationAware
    {
        public bool Opened { get; private set; }

        public Task OnNavigatedToAsync()
        {
            Opened = true;

            return Task.CompletedTask;
        }
    }

    [Fact]
    public void GoTo_shows_the_screen_and_tells_it_that_it_was_opened()
    {
        using var provider = new ServiceCollection().AddTransient<ProbeViewModel>().BuildServiceProvider();
        var shell = new ShellViewModel(provider);

        shell.GoTo<ProbeViewModel>();

        var current = Assert.IsType<ProbeViewModel>(shell.Current);
        Assert.True(current.Opened);
    }

    [Fact]
    public void Every_visit_creates_a_fresh_screen()
    {
        using var provider = new ServiceCollection().AddTransient<ProbeViewModel>().BuildServiceProvider();
        var shell = new ShellViewModel(provider);

        shell.GoTo<ProbeViewModel>();
        var first = shell.Current;
        shell.GoTo<ProbeViewModel>();

        Assert.NotSame(first, shell.Current);
    }

    [Fact]
    public void The_real_container_can_build_every_screen()
    {
        using var directory = new TempDirectory();
        using var provider = new ServiceCollection().AddSelfLearn(directory.Path).BuildServiceProvider();

        Assert.NotNull(provider.GetRequiredService<ShellViewModel>());
        Assert.NotNull(provider.GetRequiredService<StartupViewModel>());
        Assert.NotNull(provider.GetRequiredService<LoginViewModel>());
        Assert.NotNull(provider.GetRequiredService<HomeViewModel>());
        Assert.Same(provider.GetRequiredService<ShellViewModel>(), provider.GetRequiredService<INavigator>());
    }
}
