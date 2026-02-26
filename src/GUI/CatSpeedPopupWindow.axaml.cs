using System;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using LibVLCSharp.Shared;

namespace GUI;

public partial class CatSpeedPopupWindow : Window
{
    private readonly LibVLC _libVlc;
    private readonly string _videoPath;
    private MediaPlayer? _mediaPlayer;
    private Media? _media;
    private bool _isClosing;
    private bool _isStarted;

    public CatSpeedPopupWindow(LibVLC libVlc, string videoPath)
    {
        _libVlc = libVlc;
        _videoPath = videoPath;
        InitializeComponent();
        KeyDown += OnKeyDown;
        Closed += OnClosed;
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
        if (string.IsNullOrWhiteSpace(_videoPath))
        {
            return;
        }

        try
        {
            _mediaPlayer = new MediaPlayer(_libVlc);
            _media = new Media(_libVlc, new Uri(_videoPath));

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
