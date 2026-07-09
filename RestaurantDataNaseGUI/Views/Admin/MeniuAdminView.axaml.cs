using Avalonia.Controls;
using Avalonia.Interactivity;
using RestaurantDataNaseGUI.ViewModels.Admin;

namespace RestaurantDataNaseGUI.Views.Admin;

public partial class MeniuAdminView : UserControl
{
    private bool _incarcateDejaMeniurile;

    public MeniuAdminView()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private void OnLoaded(object? sender, RoutedEventArgs e)
    {
        if (_incarcateDejaMeniurile)
        {
            return;
        }

        if (DataContext is MeniuAdminViewModel viewModel && viewModel.IncarcaCommand.CanExecute(null))
        {
            _incarcateDejaMeniurile = true;
            viewModel.IncarcaCommand.Execute(null);
        }
    }
}
