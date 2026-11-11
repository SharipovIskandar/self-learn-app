using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace SelfLearn.Mobile.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    [ObservableProperty]
    public partial string Email { get; set; } = "";

    [ObservableProperty]
    public partial string Password { get; set; } = "";

    [ObservableProperty]
    public partial string StatusMessage { get; set; } = "";

    [ObservableProperty]
    public partial bool ShowPassword { get; set; }

    [ObservableProperty]
    public partial bool IsBusy { get; set; }

    // [RelayCommand] "Login" nomli command yaratadi: LoginCommand ("Async" qo'shimchasi olib tashlanadi).
    // Async command bajarilayotganda o'zi o'chiq turadi, ya'ni tugmani ikki marta bosib bo'lmaydi.
    [RelayCommand]
    private async Task LoginAsync()
    {
        if (string.IsNullOrWhiteSpace(Email) || string.IsNullOrWhiteSpace(Password))
        {
            StatusMessage = "Email va parolni kiriting";
            return;
        }

        IsBusy = true;
        StatusMessage = "";

        // Hozircha soxta kutish. Keyingi bosqichda shu yerga haqiqiy API chaqiruvi keladi.
        await Task.Delay(1000);

        StatusMessage = $"Salom, {Email}";
        IsBusy = false;
    }
}
