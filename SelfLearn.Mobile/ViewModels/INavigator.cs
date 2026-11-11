namespace SelfLearn.Mobile.ViewModels;

/// <summary>Ekranlar orasida o'tish. ViewModel boshqa ekranga o'tmoqchi bo'lsa shu interfeysdan foydalanadi.</summary>
public interface INavigator
{
    void GoTo<TViewModel>() where TViewModel : ViewModelBase;
}

/// <summary>
/// Ekran ochilganda bajariladigan ishi bor ViewModel (masalan serverdan ma'lumot yuklash) shuni amalga oshiradi.
/// <see cref="ShellViewModel"/> ekranni ko'rsatgandan keyin avtomatik chaqiradi.
/// </summary>
public interface INavigationAware
{
    Task OnNavigatedToAsync();
}
