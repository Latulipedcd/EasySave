using System;
using System.IO;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using LibVLCSharp.Shared;

namespace GUI.Features.CatSpeed;

public partial class CatSpeedPopupWindow : Window
{
    private static bool _isShowing;
    private static bool _isPrewarming;
    private static LibVLC? _libVlcSingleton;
    private static readonly object LibVlcLock = new();

    private readonly LibVLC? _libVlc;
    private readonly string? _videoPath;
    private MediaPlayer? _mediaPlayer;
    private Media? _media;
    private bool _isClosing;
    private bool _isStarted;

    public static void Prewarm()
    {
        if (_isPrewarming)
        {
            return;
        }

        _isPrewarming = true;
        Task.Run(() =>
        {
            try
            {
                using var _ = new MediaPlayer(GetLibVlc());
            }
            catch
            {
                // Best effort only.
            }
        });
    }

    public static void Show(string videoPath)
    {
        if (string.IsNullOrWhiteSpace(videoPath) || !File.Exists(videoPath))
        {
            return;
        }

        Dispatcher.UIThread.Post(() => ShowOnUiThread(videoPath));
    }

    private static void ShowOnUiThread(string videoPath)
    {
        if (_isShowing)
        {
            return;
        }

        _isShowing = true;

        try
        {
            var libVlc = GetLibVlc();
            var window = new CatSpeedPopupWindow(libVlc, videoPath);
            window.Closed += (_, _) =>
            {
                _isShowing = false;
            };

            if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop
                && desktop.MainWindow is not null)
            {
                window.Show(desktop.MainWindow);
            }
            else
            {
                window.Show();
            }
        }
        catch
        {
            _isShowing = false;
        }
    }

    private static LibVLC GetLibVlc()
    {
        lock (LibVlcLock)
        {
            if (_libVlcSingleton == null)
            {
                LibVLCSharp.Shared.Core.Initialize();
                _libVlcSingleton = new LibVLC("--quiet", "--no-video-title-show");
            }

            return _libVlcSingleton;
        }
    }

    public CatSpeedPopupWindow()
    {
        InitializeComponent();
        KeyDown += OnKeyDown;
        Closed += OnClosed;
    }

    public CatSpeedPopupWindow(LibVLC libVlc, string videoPath) : this()
    {
        _libVlc = libVlc;
        _videoPath = videoPath;
        Opened += OnOpened;
    }

    private void CloseButton_Click(object? sender, RoutedEventArgs e)
    {
        Close();
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            Close();
        }
    }

    private void OnOpened(object? sender, EventArgs e)
    {
        if (_isStarted)
        {
            return;
        }

        _isStarted = true;
        StartVideo();
    }

    private void StartVideo()
    {
        if (_libVlc == null || string.IsNullOrWhiteSpace(_videoPath))
        {
            return;
        }

        try
        {
            _mediaPlayer = new MediaPlayer(_libVlc);
            _media = new Media(_libVlc, new Uri(_videoPath));
            _mediaPlayer.Mute = false;
            _mediaPlayer.Volume = 100;

            PlayerView.MediaPlayer = _mediaPlayer;
            _mediaPlayer.Play(_media);
        }
        catch
        {
            // Do not crash the app if media playback fails.
        }
    }

    private void OnClosed(object? sender, EventArgs e)
    {
        if (_isClosing)
        {
            return;
        }

        _isClosing = true;
        Opened -= OnOpened;
        KeyDown -= OnKeyDown;
        Closed -= OnClosed;

        var player = _mediaPlayer;
        var media = _media;
        _mediaPlayer = null;
        _media = null;

        try
        {
            PlayerView.MediaPlayer = null;
        }
        catch
        {
        }

        Dispatcher.UIThread.Post(() =>
        {
            try
            {
                player?.Stop();
            }
            catch
            {
            }

            try
            {
                media?.Dispose();
            }
            catch
            {
            }

            try
            {
                player?.Dispose();
            }
            catch
            {
            }
        }, DispatcherPriority.Background);
    }
}
