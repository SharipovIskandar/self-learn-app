using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using SelfLearn.Mobile.ViewModels;
using SelfLearn.Mobile.Views;

namespace SelfLearn.Mobile;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
#if DEBUG
        this.AttachDeveloperTools();
#endif
    }

    public override void OnFrameworkInitializationCompleted()
    {
        // Butun ilova uchun bitta konteyner va bitta qobiq: Android activity qayta yaratilsa ham holat saqlanadi.
        var services = new ServiceCollection().AddSelfLearn().BuildServiceProvider();
        var shell = services.GetRequiredService<ShellViewModel>();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow
            {
                DataContext = shell
            };
        }
        else if (ApplicationLifetime is IActivityApplicationLifetime singleViewFactoryApplicationLifetime)
        {
            singleViewFactoryApplicationLifetime.MainViewFactory = () => new MainView { DataContext = shell };
        }
        else if (ApplicationLifetime is ISingleViewApplicationLifetime singleViewPlatform)
        {
            singleViewPlatform.MainView = new MainView
            {
                DataContext = shell
            };
        }

        // Birinchi ekran: saqlangan sessiyani tekshiradi va Home yoki Login'ga o'tkazadi.
        shell.GoTo<StartupViewModel>();

        base.OnFrameworkInitializationCompleted();
    }
}
