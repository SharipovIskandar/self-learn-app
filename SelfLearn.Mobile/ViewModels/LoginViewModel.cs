using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SelfLearn.Mobile.Services;

namespace SelfLearn.Mobile.ViewModels;

public partial class LoginViewModel : ViewModelBase
{
    private readonly AuthService _auth;
    private readonly INavigator _navigator;

    public LoginViewModel(AuthService auth, INavigator navigator)
    {
        _auth = auth;
        _navigator = navigator;
    }

    [ObservableProperty]
    public partial string Email { get; set; } = "";

    [ObservableProperty]
    public partial string Password { get; set; } = "";

    [ObservableProperty]
    public partial bool ShowPassword { get; set; }

    [ObservableProperty]
    public partial bool IsBusy { get; set; }

    [ObservableProperty]
    public partial string ErrorMessage { get; set; } = "";

    // Async command bajarilayotganda o'zi o'chiq turadi: tugmani ikki marta bosib bo'lmaydi.
    [RelayCommand]
    private async Task LoginAsync()
    {
        ErrorMessage = "";
        var email = Email.Trim();

        if (email.Length == 0 || Password.Length == 0)
        {
            ErrorMessage = "Email va parolni kiriting";
            return;
        }

        if (!email.Contains('@'))
        {
            ErrorMessage = "Email manzilni to'g'ri kiriting";
            return;
        }

        IsBusy = true;
        try
        {
            await _auth.LoginAsync(email, Password);

            Password = "";
            _navigator.GoTo<MainTabsViewModel>();
        }
        catch (ApiException ex)
        {
            // Matn tayyor o'zbekcha: "Email yoki parol noto'g'ri", "Juda ko'p urinish...", "Serverga ulanib bo'lmadi..."
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }
}
