using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Threading;
using GUI;
using LibVLCSharp.Shared;
using System;
using System.IO;
using System.Threading.Tasks;

namespace GUI.Services;

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
                var libVlc = GetLibVlc();

                // Warm native VLC pipeline once so the first CatSpeed trigger is fast.
                using var player = new MediaPlayer(libVlc);

                string? videoPath = ResolveCatVideoPath();
                if (!string.IsNullOrWhiteSpace(videoPath))
                {
                    using var media = new Media(libVlc, new Uri(videoPath));
                }
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

    private static string? ResolveCatVideoPath()
    {
        string[] candidates =
        {
            Path.Combine(AppContext.BaseDirectory, "Assets", "Animations", "cat.mp4"),
            Path.Combine(AppContext.BaseDirectory, "cat.mp4")
        };

        return System.Array.Find(candidates, File.Exists);
    }
}
