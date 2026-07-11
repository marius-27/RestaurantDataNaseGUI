using Avalonia.Controls;
using Avalonia.Interactivity;
using RestaurantDataNaseGUI.ViewModels.Admin;

namespace RestaurantDataNaseGUI.Views.Admin;

public partial class AlergenAdminView : UserControl
{
    private bool _incarcatDejaAlergenii;

    public AlergenAdminView()
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

        if (DataContext is AlergenAdminViewModel viewModel && viewModel.IncarcaCommand.CanExecute(null))
        {
            _incarcatDejaAlergenii = true;
            viewModel.IncarcaCommand.Execute(null);
        }
    }
}
