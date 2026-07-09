using Avalonia.Controls;
using Avalonia.Interactivity;
using RestaurantDataNaseGUI.ViewModels.Admin;

namespace RestaurantDataNaseGUI.Views.Admin;

public partial class ToateComenzileView : UserControl
{
    private bool _incarcateDejaComenzile;

    public ToateComenzileView()
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

        if (DataContext is ToateComenzileViewModel viewModel && viewModel.IncarcaComenziCommand.CanExecute(null))
        {
            _incarcateDejaComenzile = true;
            viewModel.IncarcaComenziCommand.Execute(null);
        }
    }
}
