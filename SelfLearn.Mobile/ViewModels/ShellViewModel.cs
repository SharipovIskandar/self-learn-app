using System;
using System.Diagnostics;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.DependencyInjection;

namespace SelfLearn.Mobile.ViewModels;

/// <summary>
/// Ilovaning qobig'i: hozir ko'rsatilayotgan ekranni (<see cref="Current"/>) ushlab turadi.
/// MainView/MainWindow shu ViewModel bilan bog'lanadi, mos View'ni <see cref="ViewLocator"/> topadi.
/// </summary>
public partial class ShellViewModel : ViewModelBase, INavigator
{
    private readonly IServiceProvider _services;

    public ShellViewModel(IServiceProvider services)
    {
        _services = services;
    }

    [ObservableProperty]
    public partial ViewModelBase? Current { get; set; }

    public void GoTo<TViewModel>() where TViewModel : ViewModelBase
    {
        var viewModel = _services.GetRequiredService<TViewModel>();
        Current = viewModel;

        if (viewModel is INavigationAware aware)
        {
            _ = RunAsync(aware);
        }
    }

    private static async Task RunAsync(INavigationAware aware)
    {
        try
        {
            await aware.OnNavigatedToAsync();
        }
        catch (Exception ex)
        {
            // ViewModel o'z xatolarini o'zi ko'rsatishi kerak; bu faqat ilova qulab tushmasligi uchun oxirgi chora.
            Debug.WriteLine($"Ekran ochilishida kutilmagan xato: {ex}");
        }
    }
}
