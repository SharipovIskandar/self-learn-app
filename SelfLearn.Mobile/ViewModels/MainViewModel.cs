using CommunityToolkit.Mvvm.ComponentModel;

namespace SelfLearn.Mobile.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    [ObservableProperty]
    public partial string Greeting { get; set; } = "Welcom to self-learn app!";
}
