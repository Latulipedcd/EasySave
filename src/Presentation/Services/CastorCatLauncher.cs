using System;
using System.IO;
using System.Runtime.InteropServices;

namespace EasySave.Presentation.Services;

public static class CastorCatLauncher
{
    private const string CastorJobName = "Castor";
    private const byte VkVolumeUp = 0xAF;
    private const uint KeyeventfKeyup = 0x0002;
    private static Action<string>? _inAppPopupLauncher;

    public static bool IsCastorJobName(string? jobName)
    {
        if (string.IsNullOrWhiteSpace(jobName))
        {
            return false;
        }

        return string.Equals(jobName.Trim(), CastorJobName, StringComparison.OrdinalIgnoreCase);
    }

    public static void LaunchCatVideoSurprise()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        string? catVideoPath = ResolveCatVideoPath();
        if (string.IsNullOrWhiteSpace(catVideoPath))
        {
            return;
        }

        try
        {
            SetSystemVolumeToMax();
        }
        catch
        {
            // Non-blocking best effort.
        }

        try
        {
            _inAppPopupLauncher?.Invoke(catVideoPath);
        }
        catch
        {
            // Non-blocking best effort.
        }
    }

    public static void ConfigureInAppPopupLauncher(Action<string> launcher)
    {
        _inAppPopupLauncher = launcher;
    }

    private static string? ResolveCatVideoPath()
    {
        string[] candidates =
        {
            Path.Combine(AppContext.BaseDirectory, "Assets", "Animations", "cat.mp4"),
            Path.Combine(AppContext.BaseDirectory, "cat.mp4"),
            Path.Combine(Environment.CurrentDirectory, "src", "GUI", "Assets", "Animations", "cat.mp4")
        };

        return Array.Find(candidates, File.Exists);
    }

    private static void SetSystemVolumeToMax()
    {
        for (int i = 0; i < 60; i++)
        {
            keybd_event(VkVolumeUp, 0, 0, UIntPtr.Zero);
            keybd_event(VkVolumeUp, 0, KeyeventfKeyup, UIntPtr.Zero);
        }
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern void keybd_event(
        byte virtualKey,
        byte scanCode,
        uint flags,
        UIntPtr extraInfo);
}
