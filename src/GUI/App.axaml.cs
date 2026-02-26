using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using EasySave.Presentation.Features.CatSpeed;
using EasySave.Presentation.ViewModels;
using GUI.Features.CatSpeed;

namespace GUI;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow(MainWindowViewModel.CreateDefault());
        }

        CatSpeedFeature.ConfigurePopup(CatSpeedPopupWindow.Show);
        CatSpeedPopupWindow.Prewarm();

        base.OnFrameworkInitializationCompleted();
    }
}