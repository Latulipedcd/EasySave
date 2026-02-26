using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Threading;
using GUI;
using LibVLCSharp.Shared;
using System.IO;

namespace GUI.Services;

internal static class CastorSurprisePopupService
{
    private static bool _isShowing;
    private static LibVLC? _libVlc;

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
            var window = new CastorPopupWindow(libVlc, videoPath);
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
        if (_libVlc == null)
        {
            LibVLCSharp.Shared.Core.Initialize();
            _libVlc = new LibVLC("--quiet", "--no-video-title-show");
        }

        return _libVlc;
    }
}
