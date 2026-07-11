using Avalonia.Controls;
using Avalonia.Interactivity;
using RestaurantDataNaseGUI.ViewModels.Admin;

namespace RestaurantDataNaseGUI.Views.Admin;

public partial class PreparatAdminView : UserControl
{
    private bool _incarcateDejaPreparatele;

    public PreparatAdminView()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private void OnLoaded(object? sender, RoutedEventArgs e)
    {
        if (_incarcateDejaPreparatele)
        {
            return;
        }

        if (DataContext is PreparatAdminViewModel viewModel && viewModel.IncarcaCommand.CanExecute(null))
        {
            _incarcateDejaPreparatele = true;
            viewModel.IncarcaCommand.Execute(null);
        }
    }
}
