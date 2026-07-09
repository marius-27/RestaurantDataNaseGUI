using Avalonia.Controls;
using Avalonia.Interactivity;
using RestaurantDataNaseGUI.ViewModels.Admin;

namespace RestaurantDataNaseGUI.Views.Admin;

public partial class CategorieAdminView : UserControl
{
    private bool _incarcatDejaCategoriile;

    public CategorieAdminView()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private void OnLoaded(object? sender, RoutedEventArgs e)
    {
        if (_incarcatDejaCategoriile)
        {
            return;
        }

        if (DataContext is CategorieAdminViewModel viewModel && viewModel.IncarcaCommand.CanExecute(null))
        {
            _incarcatDejaCategoriile = true;
            viewModel.IncarcaCommand.Execute(null);
        }
    }
}
