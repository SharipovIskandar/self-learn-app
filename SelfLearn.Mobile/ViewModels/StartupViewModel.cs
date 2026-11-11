using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SelfLearn.Mobile.Services;

namespace SelfLearn.Mobile.ViewModels;

/// <summary>
/// Ilova ochilgandagi birinchi ekran: saqlangan sessiyani tekshiradi va Home yoki Login'ga yo'naltiradi.
/// Internet yo'q bo'lsa foydalanuvchini tizimdan chiqarib yubormaydi, "Qayta urinish" ko'rsatadi.
/// </summary>
public partial class StartupViewModel : ViewModelBase, INavigationAware
{
    private readonly AuthService _auth;
    private readonly INavigator _navigator;

    public StartupViewModel(AuthService auth, INavigator navigator)
    {
        _auth = auth;
        _navigator = navigator;
    }

    [ObservableProperty]
    public partial bool IsLoading { get; set; } = true;

    [ObservableProperty]
    public partial string ErrorMessage { get; set; } = "";

    public Task OnNavigatedToAsync() => LoadAsync();

    [RelayCommand]
    private async Task LoadAsync()
    {
        IsLoading = true;
        ErrorMessage = "";

        try
        {
            var user = await _auth.RestoreSessionAsync();

            if (user is null)
            {
                _navigator.GoTo<LoginViewModel>();
            }
            else
            {
                _navigator.GoTo<HomeViewModel>();
            }
        }
        catch (ApiException ex)
        {
            ErrorMessage = ex.Message;
            IsLoading = false;
        }
    }
}
