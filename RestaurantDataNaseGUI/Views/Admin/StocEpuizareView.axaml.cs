using Avalonia.Controls;
using Avalonia.Interactivity;
using RestaurantDataNaseGUI.ViewModels.Admin;

namespace RestaurantDataNaseGUI.Views.Admin;

public partial class StocEpuizareView : UserControl
{
    private bool _incarcatDejaStocul;

    public StocEpuizareView()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private void OnLoaded(object? sender, RoutedEventArgs e)
    {
        if (_incarcatDejaStocul)
        {
            return;
        }

        if (DataContext is StocEpuizareViewModel viewModel && viewModel.IncarcaStocCommand.CanExecute(null))
        {
            _incarcatDejaStocul = true;
            viewModel.IncarcaStocCommand.Execute(null);
        }
    }
}
