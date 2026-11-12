using SelfLearn.Mobile.Models;

namespace SelfLearn.Mobile.Services;

/// <summary>Bosh sahifa ma'lumoti (GET /api/v1/solo/dashboard).</summary>
public sealed class DashboardService
{
    private readonly ApiClient _api;

    public DashboardService(ApiClient api)
    {
        _api = api;
    }

    public Task<DashboardResponse> GetAsync(CancellationToken ct = default) =>
        _api.GetAsync<DashboardResponse>("api/v1/solo/dashboard", ct);
}
