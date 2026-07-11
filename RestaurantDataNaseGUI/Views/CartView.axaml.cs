using Avalonia.Controls;
using Avalonia.Interactivity;
using RestaurantDataNaseGUI.ViewModels;

namespace RestaurantDataNaseGUI.Views;

public partial class CartView : UserControl
{
    private bool _incarcatDejaCosul;

    public CartView()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private void OnLoaded(object? sender, RoutedEventArgs e)
    {
        if (_incarcatDejaCosul)
        {
            return;
        }

        if (DataContext is CartViewModel viewModel && viewModel.IncarcaCosCommand.CanExecute(null))
        {
            _incarcatDejaCosul = true;
            viewModel.IncarcaCosCommand.Execute(null);
        }
    }
}
