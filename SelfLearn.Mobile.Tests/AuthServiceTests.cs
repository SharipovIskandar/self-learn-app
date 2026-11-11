using System.Net;
using SelfLearn.Mobile.Services;

namespace SelfLearn.Mobile.Tests;

public class AuthServiceTests
{
    [Fact]
    public async Task Login_saves_the_token_and_the_user()
    {
        using var h = new Harness(new StubHandler(HttpStatusCode.OK, Samples.LoginOk));

        var user = await h.Auth.LoginAsync("  solo@example.test ", "password123");

        Assert.Equal("Solo Test", user.Name);
        Assert.Equal(user, h.Auth.CurrentUser);
        Assert.Equal("1|abcdef123456", h.Session.Token);
        Assert.Equal("1|abcdef123456", h.Tokens.Current);
    }

    [Fact]
    public async Task Login_sends_a_trimmed_email_and_the_device_name()
    {
        using var h = new Harness(new StubHandler(HttpStatusCode.OK, Samples.LoginOk));

        await h.Auth.LoginAsync("  solo@example.test ", "password123");

        var request = Assert.Single(h.Handler.Requests);
        Assert.Equal("/api/v1/auth/login", request.Path);
        Assert.Contains("\"email\":\"solo@example.test\"", request.Body);
        Assert.Contains($"\"device_name\":\"{h.Device.DeviceName}\"", request.Body);
    }

    [Fact]
    public async Task A_failed_login_saves_nothing()
    {
        using var h = new Harness(new StubHandler(HttpStatusCode.UnprocessableEntity, Samples.InvalidCredentials));

        var error = await Assert.ThrowsAsync<ApiException>(() => h.Auth.LoginAsync("solo@example.test", "wrong"));

        Assert.Equal(ApiErrorKind.Validation, error.Kind);
        Assert.Null(h.Session.Token);
        Assert.Null(h.Session.User);
        Assert.Null(h.Tokens.Current);
    }

    [Fact]
    public async Task Restore_without_a_saved_token_makes_no_request()
    {
        using var h = new Harness(new StubHandler(HttpStatusCode.OK, Samples.Me));

        var user = await h.Auth.RestoreSessionAsync();

        Assert.Null(user);
        Assert.Empty(h.Handler.Requests);
    }

    [Fact]
    public async Task Restore_with_a_valid_token_returns_the_user()
    {
        using var h = new Harness(new StubHandler(HttpStatusCode.OK, Samples.Me), savedToken: "1|abcdef123456");

        var user = await h.Auth.RestoreSessionAsync();

        Assert.Equal("solo@example.test", user?.Email);
        Assert.Equal(user, h.Auth.CurrentUser);
        var request = Assert.Single(h.Handler.Requests);
        Assert.Equal("/api/v1/me", request.Path);
        Assert.Equal("Bearer 1|abcdef123456", request.Authorization);
    }

    [Fact]
    public async Task Restore_with_a_rejected_token_clears_it()
    {
        using var h = new Harness(new StubHandler(HttpStatusCode.Unauthorized, Samples.Unauthenticated), savedToken: "old");

        var user = await h.Auth.RestoreSessionAsync();

        Assert.Null(user);
        Assert.Null(h.Tokens.Current);
        Assert.Null(h.Session.Token);
    }

    [Fact]
    public async Task Restore_keeps_the_token_when_the_network_fails()
    {
        using var h = new Harness(new StubHandler(_ => throw new HttpRequestException("offline")), savedToken: "keep-me");

        var error = await Assert.ThrowsAsync<ApiException>(() => h.Auth.RestoreSessionAsync());

        Assert.Equal(ApiErrorKind.Network, error.Kind);
        Assert.Equal("keep-me", h.Tokens.Current);
    }

    [Fact]
    public async Task Restore_keeps_the_token_when_the_server_fails()
    {
        using var h = new Harness(new StubHandler(_ => StubHandler.Html(HttpStatusCode.BadGateway)), savedToken: "keep-me");

        await Assert.ThrowsAsync<ApiException>(() => h.Auth.RestoreSessionAsync());

        Assert.Equal("keep-me", h.Tokens.Current);
    }

    [Fact]
    public async Task Logout_calls_the_server_with_the_token_and_clears_everything()
    {
        using var h = new Harness(new StubHandler(HttpStatusCode.NoContent), savedToken: "1|abc");
        h.Session.Token = "1|abc";

        await h.Auth.LogoutAsync();

        var request = Assert.Single(h.Handler.Requests);
        Assert.Equal("/api/v1/auth/logout", request.Path);
        Assert.Equal("Bearer 1|abc", request.Authorization);
        Assert.Null(h.Session.Token);
        Assert.Null(h.Tokens.Current);
    }

    [Fact]
    public async Task Logout_clears_the_device_even_when_the_server_is_unreachable()
    {
        using var h = new Harness(new StubHandler(_ => throw new HttpRequestException("offline")), savedToken: "1|abc");
        h.Session.Token = "1|abc";

        await h.Auth.LogoutAsync();

        Assert.Null(h.Session.Token);
        Assert.Null(h.Tokens.Current);
    }
}
