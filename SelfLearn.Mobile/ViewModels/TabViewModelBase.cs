namespace SelfLearn.Mobile.ViewModels;

/// <summary>Pastki navigatsiyadagi bo'lim. <see cref="Title"/> tab sarlavhasi.</summary>
public abstract class TabViewModelBase : ViewModelBase
{
    public abstract string Title { get; }
}
