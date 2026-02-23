using System;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Threading;
using EasySave.Presentation.ViewModels;
using System.Threading.Tasks;

namespace GUI;

/// <summary>
/// Dedicated window that shows live progress while "Run all" is executing.
/// </summary>
public partial class RunAllProgressWindow : Window
{
    private readonly RunAllProgressWindowViewModel _viewModel;
    private readonly DispatcherTimer _refreshTimer;
    private bool _closeConfirmed;
    private bool _confirmationInProgress;

    public RunAllProgressWindow(RunAllProgressWindowViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = viewModel;

        _refreshTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(250)
        };
        _refreshTimer.Tick += OnRefreshTick;

        Opened += OnOpened;
        Closed += OnClosed;
        Closing += OnClosing;
    }

    private async void OnOpened(object? sender, EventArgs e)
    {
        _refreshTimer.Start();
        await _viewModel.StartExecutionAsync();
    }

    private void OnClosed(object? sender, EventArgs e)
    {
        _refreshTimer.Stop();
    }

    private void OnClosing(object? sender, WindowClosingEventArgs e)
    {
        if (_closeConfirmed || !_viewModel.IsRunning)
            return;

        e.Cancel = true;

        if (_confirmationInProgress)
            return;

        _ = ConfirmCloseAndCancelAsync();
    }

    private void OnRefreshTick(object? sender, EventArgs e)
    {
        _viewModel.RefreshProgressSnapshot();

        if (_viewModel.IsExecutionCompleted)
            _refreshTimer.Stop();
    }

    private async void Close_Click(object? sender, RoutedEventArgs e)
    {
        await ConfirmCloseAndCancelAsync();
    }

    private void StartJob_Click(object? sender, RoutedEventArgs e)
    {
        _viewModel.StartSelectedJob();
    }

    private void PauseJob_Click(object? sender, RoutedEventArgs e)
    {
        _viewModel.PauseSelectedJob();
    }

    private void CancelJob_Click(object? sender, RoutedEventArgs e)
    {
        _viewModel.CancelSelectedJob();
    }

    private async Task ConfirmCloseAndCancelAsync()
    {
        if (_closeConfirmed)
            return;

        if (!_viewModel.IsRunning)
        {
            _closeConfirmed = true;
            Close();
            return;
        }

        if (_confirmationInProgress)
            return;

        _confirmationInProgress = true;
        try
        {
            var confirmed = await ShowCloseConfirmationAsync();
            if (!confirmed)
                return;

            _viewModel.CancelAllRunningJobs();
            _closeConfirmed = true;
            Close();
        }
        finally
        {
            _confirmationInProgress = false;
        }
    }

    private async Task<bool> ShowCloseConfirmationAsync()
    {
        var cancelButton = new Button
        {
            Content = _viewModel.CloseConfirmCancelLabel,
            MinWidth = 160,
            HorizontalContentAlignment = HorizontalAlignment.Center
        };
        cancelButton.Classes.Add("secondary");

        var confirmButton = new Button
        {
            Content = _viewModel.CloseConfirmConfirmLabel,
            MinWidth = 220,
            HorizontalContentAlignment = HorizontalAlignment.Center
        };
        confirmButton.Classes.Add("danger");

        var dialog = new Window
        {
            Title = _viewModel.CloseConfirmTitle,
            Width = 560,
            Height = 240,
            CanResize = false,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Content = new Border
            {
                Classes = { "card" },
                Margin = new Avalonia.Thickness(14),
                Child = new StackPanel
                {
                    Spacing = 16,
                    Children =
                    {
                        new TextBlock
                        {
                            Text = _viewModel.CloseConfirmMessage,
                            Classes = { "subtitle" },
                            TextWrapping = Avalonia.Media.TextWrapping.Wrap
                        },
                        new StackPanel
                        {
                            Orientation = Orientation.Horizontal,
                            HorizontalAlignment = HorizontalAlignment.Center,
                            Spacing = 12,
                            Margin = new Avalonia.Thickness(0, 8, 0, 0),
                            Children =
                            {
                                cancelButton,
                                confirmButton
                            }
                        }
                    }
                }
            }
        };

        var completion = new TaskCompletionSource<bool>();

        cancelButton.Click += (_, _) =>
        {
            completion.TrySetResult(false);
            dialog.Close();
        };

        confirmButton.Click += (_, _) =>
        {
            completion.TrySetResult(true);
            dialog.Close();
        };

        dialog.Closed += (_, _) => completion.TrySetResult(false);

        await dialog.ShowDialog(this);
        return await completion.Task;
    }
}
