using Avalonia.Controls;
using Avalonia.Interactivity;
using RestaurantDataNaseGUI.ViewModels;

namespace RestaurantDataNaseGUI.Views;

public partial class MenuView : UserControl
{
    private bool _incarcatDejaMeniul;

    public MenuView()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private void OnLoaded(object? sender, RoutedEventArgs e)
    {
        if (_incarcatDejaMeniul)
        {
            return;
        }

        if (DataContext is MenuViewModel viewModel && viewModel.IncarcaMeniuCommand.CanExecute(null))
        {
            _incarcatDejaMeniul = true;
            viewModel.IncarcaMeniuCommand.Execute(null);
        }
    }
}
