using System.Net;
using SelfLearn.Mobile.Models;
using SelfLearn.Mobile.Services;

namespace SelfLearn.Mobile.Tests;

public class ApiClientTests
{
    private static ApiClient Client(StubHandler handler, AuthSession? session = null) =>
        new(new HttpClient(handler) { BaseAddress = new Uri("https://self-learn.test/") }, session ?? new AuthSession());

    private static Task<MeResponse> GetMe(ApiClient client, CancellationToken ct = default) =>
        client.GetAsync<MeResponse>("api/v1/me", ct);

    // ---- so'rov ----

    [Fact]
    public async Task Sends_the_bearer_token_when_the_session_has_one()
    {
        var handler = new StubHandler(HttpStatusCode.OK, Samples.Me);

        await GetMe(Client(handler, new AuthSession { Token = "7|secret" }));

        Assert.Equal("Bearer 7|secret", Assert.Single(handler.Requests).Authorization);
    }

    [Fact]
    public async Task Sends_no_authorization_header_without_a_token()
    {
        var handler = new StubHandler(HttpStatusCode.OK, Samples.Me);

        await GetMe(Client(handler));

        Assert.Null(Assert.Single(handler.Requests).Authorization);
    }

    [Fact]
    public async Task Post_body_is_snake_case_json()
    {
        var handler = new StubHandler(HttpStatusCode.OK, Samples.LoginOk);

        await Client(handler).PostAsync<LoginRequest, LoginResponse>(
            "api/v1/auth/login",
            new LoginRequest("a@b.uz", "pw", "Pixel 7"));

        var request = Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Post, request.Method);
        Assert.Equal("/api/v1/auth/login", request.Path);
        Assert.Contains("\"email\":\"a@b.uz\"", request.Body);
        Assert.Contains("\"password\":\"pw\"", request.Body);
        Assert.Contains("\"device_name\":\"Pixel 7\"", request.Body);
    }

    // ---- muvaffaqiyatli javob ----

    [Fact]
    public async Task Parses_the_real_login_response()
    {
        var handler = new StubHandler(HttpStatusCode.OK, Samples.LoginOk);

        var result = await Client(handler).PostAsync<LoginRequest, LoginResponse>(
            "api/v1/auth/login",
            new LoginRequest("solo@example.test", "pw", "Pixel 7"));

        Assert.Equal("1|abcdef123456", result.Token);
        Assert.Equal("Bearer", result.TokenType);
        Assert.Equal(new UserDto(1, "Solo Test", "solo@example.test", "solo_learner", true), result.User);
    }

    [Fact]
    public async Task Post_without_a_response_body_accepts_204()
    {
        var handler = new StubHandler(HttpStatusCode.NoContent);

        await Client(handler, new AuthSession { Token = "t" }).PostAsync("api/v1/auth/logout");

        Assert.Equal("/api/v1/auth/logout", Assert.Single(handler.Requests).Path);
    }

    [Fact]
    public async Task A_success_response_that_is_not_json_is_unexpected()
    {
        var handler = new StubHandler(_ => StubHandler.Html(HttpStatusCode.OK));

        var error = await Assert.ThrowsAsync<ApiException>(() => GetMe(Client(handler)));

        Assert.Equal(ApiErrorKind.Unexpected, error.Kind);
    }

    // ---- xatolar ----

    [Fact]
    public async Task Maps_422_to_a_validation_error_with_field_errors()
    {
        var handler = new StubHandler(HttpStatusCode.UnprocessableEntity, Samples.InvalidCredentials);

        var error = await Assert.ThrowsAsync<ApiException>(() => GetMe(Client(handler)));

        Assert.Equal(ApiErrorKind.Validation, error.Kind);
        Assert.Equal(422, error.StatusCode);
        Assert.Equal("Email yoki parol noto'g'ri.", error.Message);
        Assert.Equal("Email yoki parol noto'g'ri.", Assert.Single(error.FieldErrors["email"]));
    }

    [Fact]
    public async Task Maps_403_to_forbidden_and_keeps_the_error_code()
    {
        var handler = new StubHandler(HttpStatusCode.Forbidden, Samples.RoleNotSupported);

        var error = await Assert.ThrowsAsync<ApiException>(() => GetMe(Client(handler)));

        Assert.Equal(ApiErrorKind.Forbidden, error.Kind);
        Assert.Equal("role_not_supported", error.Code);
        Assert.Contains("faqat mustaqil o'quvchilar uchun", error.Message);
    }

    [Fact]
    public async Task Maps_429_and_reads_retry_after_from_the_body()
    {
        var handler = new StubHandler((HttpStatusCode)429, Samples.TooManyAttempts);

        var error = await Assert.ThrowsAsync<ApiException>(() => GetMe(Client(handler)));

        Assert.Equal(ApiErrorKind.RateLimited, error.Kind);
        Assert.Equal("too_many_attempts", error.Code);
        Assert.Equal(60, error.RetryAfterSeconds);
    }

    [Fact]
    public async Task Maps_429_and_falls_back_to_the_retry_after_header()
    {
        var handler = new StubHandler(_ =>
        {
            var response = StubHandler.Html((HttpStatusCode)429);
            response.Headers.RetryAfter = new System.Net.Http.Headers.RetryConditionHeaderValue(TimeSpan.FromSeconds(30));

            return response;
        });

        var error = await Assert.ThrowsAsync<ApiException>(() => GetMe(Client(handler)));

        Assert.Equal(ApiErrorKind.RateLimited, error.Kind);
        Assert.Equal(30, error.RetryAfterSeconds);
    }

    [Fact]
    public async Task Maps_401_to_unauthorized_and_hides_the_english_server_message()
    {
        var handler = new StubHandler(HttpStatusCode.Unauthorized, Samples.Unauthenticated);

        var error = await Assert.ThrowsAsync<ApiException>(() => GetMe(Client(handler)));

        Assert.Equal(ApiErrorKind.Unauthorized, error.Kind);
        Assert.DoesNotContain("Unauthenticated", error.Message);
    }

    [Fact]
    public async Task Maps_a_5xx_html_page_to_a_server_error()
    {
        var handler = new StubHandler(_ => StubHandler.Html(HttpStatusCode.BadGateway));

        var error = await Assert.ThrowsAsync<ApiException>(() => GetMe(Client(handler)));

        Assert.Equal(ApiErrorKind.Server, error.Kind);
        Assert.Equal(502, error.StatusCode);
    }

    [Fact]
    public async Task Maps_404_to_unexpected_without_the_server_text()
    {
        // API hali productionga chiqarilmagan bo'lsa shu holat bo'ladi.
        var handler = new StubHandler(HttpStatusCode.NotFound, """{"message":"The route api/v1/me could not be found."}""");

        var error = await Assert.ThrowsAsync<ApiException>(() => GetMe(Client(handler)));

        Assert.Equal(ApiErrorKind.Unexpected, error.Kind);
        Assert.Contains("404", error.Message);
        Assert.DoesNotContain("route", error.Message);
    }

    // ---- tarmoq ----

    [Fact]
    public async Task Maps_a_connection_failure_to_a_network_error()
    {
        var handler = new StubHandler(_ => throw new HttpRequestException("connection refused"));

        var error = await Assert.ThrowsAsync<ApiException>(() => GetMe(Client(handler)));

        Assert.Equal(ApiErrorKind.Network, error.Kind);
        Assert.Null(error.StatusCode);
        Assert.IsType<HttpRequestException>(error.InnerException);
    }

    [Fact]
    public async Task Maps_a_timeout_to_a_network_error()
    {
        var handler = new StubHandler(_ => throw new TaskCanceledException("timeout"));

        var error = await Assert.ThrowsAsync<ApiException>(() => GetMe(Client(handler)));

        Assert.Equal(ApiErrorKind.Network, error.Kind);
        Assert.Contains("javob bermadi", error.Message);
    }

    [Fact]
    public async Task Maps_android_java_exceptions_to_a_network_error()
    {
        var handler = new StubHandler(_ => throw new Java.Net.UnknownHostException());

        var error = await Assert.ThrowsAsync<ApiException>(() => GetMe(Client(handler)));

        Assert.Equal(ApiErrorKind.Network, error.Kind);
    }

    [Fact]
    public async Task Cancellation_by_the_caller_is_not_reported_as_a_network_error()
    {
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();
        var handler = new StubHandler(HttpStatusCode.OK, Samples.Me);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => GetMe(Client(handler), cts.Token));
    }
}
