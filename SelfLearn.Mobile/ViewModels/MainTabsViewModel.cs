using CommunityToolkit.Mvvm.ComponentModel;

namespace SelfLearn.Mobile.ViewModels;

/// <summary>Kirgandan keyingi asosiy ekran: pastki tablar (Bosh sahifa, Darslar, So'zlar, Profil).</summary>
public partial class MainTabsViewModel : ViewModelBase, INavigationAware
{
    private readonly DashboardViewModel _dashboard;

    public MainTabsViewModel(
        DashboardViewModel dashboard,
        LessonsViewModel lessons,
        VocabViewModel vocab,
        ProfileViewModel profile)
    {
        _dashboard = dashboard;
        Tabs = [dashboard, lessons, vocab, profile];
        SelectedTab = dashboard;
    }

    public IReadOnlyList<TabViewModelBase> Tabs { get; }

    [ObservableProperty]
    public partial TabViewModelBase SelectedTab { get; set; }

    public Task OnNavigatedToAsync() => _dashboard.LoadAsync();
}
