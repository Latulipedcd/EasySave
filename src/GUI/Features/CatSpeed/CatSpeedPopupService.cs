using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Threading;
using LibVLCSharp.Shared;
using System;
using System.IO;
using System.Threading.Tasks;

namespace GUI.Features.CatSpeed;

internal static class CatSpeedPopupService
{
    private static bool _isShowing;
    private static bool _isPrewarming;
    private static LibVLC? _libVlc;
    private static readonly object LibVlcLock = new();

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
                // Warm native VLC pipeline once so the first trigger is fast.
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
            // Best effort only.
            _isShowing = false;
        }
    }

    private static LibVLC GetLibVlc()
    {
        lock (LibVlcLock)
        {
            if (_libVlc == null)
            {
                LibVLCSharp.Shared.Core.Initialize();
                _libVlc = new LibVLC("--quiet", "--no-video-title-show");
            }

            return _libVlc;
        }
    }
}
