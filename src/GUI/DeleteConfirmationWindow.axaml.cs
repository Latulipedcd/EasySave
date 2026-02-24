using Avalonia.Controls;
using Avalonia.Interactivity;
using EasySave.Presentation.ViewModels;

namespace GUI;

/// <summary>
/// Delete confirmation dialog.
/// Code-behind is intentionally minimal: the view only closes itself with a bool result.
/// All text and logic live in DeleteConfirmationViewModel.
/// </summary>
public partial class DeleteConfirmationWindow : Window
{
    public DeleteConfirmationWindow(DeleteConfirmationViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }

    private void Confirm_Click(object? sender, RoutedEventArgs e) => Close(true);
    private void Cancel_Click(object? sender, RoutedEventArgs e) => Close(false);
}
