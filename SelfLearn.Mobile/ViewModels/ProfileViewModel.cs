using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SelfLearn.Mobile.Services;

namespace SelfLearn.Mobile.ViewModels;

public partial class ProfileViewModel : TabViewModelBase
{
    private readonly AuthService _auth;
    private readonly INavigator _navigator;

    public ProfileViewModel(AuthService auth, INavigator navigator)
    {
        _auth = auth;
        _navigator = navigator;

        var user = auth.CurrentUser;
        Name = user?.Name ?? "";
        Email = user?.Email ?? "";
    }

    public override string Title => "Profil";

    public string Name { get; }

    public string Email { get; }

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
