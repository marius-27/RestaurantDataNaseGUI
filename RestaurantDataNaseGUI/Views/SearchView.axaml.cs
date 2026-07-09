using Avalonia.Controls;
using Avalonia.Interactivity;
using RestaurantDataNaseGUI.ViewModels;

namespace RestaurantDataNaseGUI.Views;

public partial class SearchView : UserControl
{
    private bool _incarcatDejaAlergenii;

    public SearchView()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private void OnLoaded(object? sender, RoutedEventArgs e)
    {
        if (_incarcatDejaAlergenii)
        {
            return;
        }

        if (DataContext is SearchViewModel viewModel && viewModel.IncarcaAlergeniCommand.CanExecute(null))
        {
            _incarcatDejaAlergenii = true;
            viewModel.IncarcaAlergeniCommand.Execute(null);
        }
    }
}
