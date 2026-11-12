using System;
using System.Net.Http;
using System.Net.Http.Headers;
using Microsoft.Extensions.DependencyInjection;
using SelfLearn.Mobile.Services;
using SelfLearn.Mobile.ViewModels;

namespace SelfLearn.Mobile;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Ilovaning hamma servislari va ViewModel'larini ro'yxatdan o'tkazadi.
    /// <paramref name="dataDirectory"/> faqat testlar uchun: berilmasa ilovaning haqiqiy shaxsiy papkasi ishlatiladi.
    /// </summary>
    public static IServiceCollection AddSelfLearn(this IServiceCollection services, string? dataDirectory = null)
    {
        dataDirectory ??= AppPaths.DataDirectory;

        // Servislar: butun ilova uchun bitta nusxa.
        services.AddSingleton(ApiOptions.FromBuild());
        services.AddSingleton<ITokenStore>(new FileTokenStore(dataDirectory));
        services.AddSingleton(new DeviceIdentity(dataDirectory));
        services.AddSingleton<AuthSession>();
        services.AddSingleton(sp => CreateHttpClient(sp.GetRequiredService<ApiOptions>()));
        services.AddSingleton<ApiClient>();
        services.AddSingleton<AuthService>();
        services.AddSingleton<DashboardService>();

        // Qobiq va navigatsiya: bitta ShellViewModel ham o'zi, ham INavigator sifatida.
        services.AddSingleton<ShellViewModel>();
        services.AddSingleton<INavigator>(sp => sp.GetRequiredService<ShellViewModel>());

        // Ekranlar: har safar ochilganda yangi (forma tozalangan) holatda.
        services.AddTransient<StartupViewModel>();
        services.AddTransient<LoginViewModel>();
        services.AddTransient<MainTabsViewModel>();
        services.AddTransient<DashboardViewModel>();
        services.AddTransient<LessonsViewModel>();
        services.AddTransient<VocabViewModel>();
        services.AddTransient<ProfileViewModel>();

        return services;
    }

    private static HttpClient CreateHttpClient(ApiOptions options)
    {
        var client = new HttpClient
        {
            BaseAddress = options.BaseUri,
            Timeout = TimeSpan.FromSeconds(20),
        };

        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("SelfLearn", AppVersion()));

        return client;
    }

    private static string AppVersion() =>
        typeof(ServiceCollectionExtensions).Assembly.GetName().Version?.ToString(3) ?? "0.0.0";
}
