using Avalonia.Controls;
using Avalonia.Interactivity;
using RestaurantDataNaseGUI.ViewModels;

namespace RestaurantDataNaseGUI.Views;

public partial class MyOrdersView : UserControl
{
    private bool _incarcateDejaComenzile;

    public MyOrdersView()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private void OnLoaded(object? sender, RoutedEventArgs e)
    {
        if (_incarcateDejaComenzile)
        {
            return;
        }

        if (DataContext is MyOrdersViewModel viewModel && viewModel.IncarcaComenziCommand.CanExecute(null))
        {
            _incarcateDejaComenzile = true;
            viewModel.IncarcaComenziCommand.Execute(null);
        }
    }
}
