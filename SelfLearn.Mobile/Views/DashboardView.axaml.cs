using Avalonia.Controls;
using SelfLearn.Mobile.ViewModels;

namespace SelfLearn.Mobile.Views;

public partial class DashboardView : UserControl
{
    public DashboardView()
    {
        InitializeComponent();
    }

    // Pastga tortilganda yangilash: yuklanguncha "deferral" aylanib turadi.
    private async void OnRefreshRequested(object? sender, RefreshRequestedEventArgs e)
    {
        var deferral = e.GetDeferral();

        try
        {
            if (DataContext is DashboardViewModel viewModel)
            {
                await viewModel.LoadCommand.ExecuteAsync(null);
            }
        }
        finally
        {
            deferral.Complete();
        }
    }
}
