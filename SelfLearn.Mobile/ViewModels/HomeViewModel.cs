using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SelfLearn.Mobile.Services;

namespace SelfLearn.Mobile.ViewModels;

/// <summary>Kirgandan keyingi vaqtinchalik bosh ekran. 2-bosqichda haqiqiy dashboard bilan almashtiriladi.</summary>
public partial class HomeViewModel : ViewModelBase
{
    private readonly AuthService _auth;
    private readonly INavigator _navigator;

    public HomeViewModel(AuthService auth, INavigator navigator)
    {
        _auth = auth;
        _navigator = navigator;

        var user = auth.CurrentUser;
        Greeting = user is null ? "Salom!" : $"Salom, {user.Name}!";
        NeedsPlacementTest = user is { OnboardingCompleted: false };
    }

    public string Greeting { get; }

    /// <summary>Daraja aniqlash testi ilovada hali yo'q (5-bosqich), shuning uchun veb-saytga yo'naltiramiz.</summary>
    public bool NeedsPlacementTest { get; }

    [ObservableProperty]
    public partial bool IsBusy { get; set; }

    [RelayCommand]
    private async Task LogoutAsync()
    {
        IsBusy = true;

        await _auth.LogoutAsync();

        _navigator.GoTo<LoginViewModel>();
    }
}
