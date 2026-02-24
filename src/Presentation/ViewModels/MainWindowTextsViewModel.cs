using System;

namespace EasySave.Presentation.ViewModels;

/// <summary>
/// Holds all static localized label strings for the main window.
/// Call Refresh() after a language change to trigger re-binding of all labels.
/// </summary>
public class MainWindowTextsViewModel : ViewModelBase
{
    private readonly Func<string, string> _getText;

    public MainWindowTextsViewModel(Func<string, string> getText)
    {
        _getText = getText;
    }

    /// <summary>Invalidates all label properties so XAML bindings re-read their values.</summary>
    public void Refresh() => OnPropertyChanged(string.Empty);

    // Window / navigation
    public string WindowTitle => _getText("GuiWindowTitle");
    public string SettingsButtonLabel => _getText("GuiSettingsButton");
    public string SettingsMenuTitle => _getText("GuiSettingsMenuTitle");

    // Toolbar
    public string NewJobButtonLabel => _getText("GuiNewJobButton");
    public string EditJobButtonLabel => _getText("GuiEditJobButton");
    public string ExecuteSelectedLabel => _getText("GuiExecuteSelected");
    public string ExecuteAllLabel => _getText("GuiExecuteAll");
    public string DeleteSelectedLabel => _getText("GuiDeleteSelected");

    // List headers
    public string HeaderName => _getText("GuiHeaderName");
    public string HeaderSource => _getText("GuiHeaderSource");
    public string HeaderDestination => _getText("GuiHeaderDestination");
    public string HeaderType => _getText("GuiHeaderType");

    // Details panel
    public string JobDetailsTitle => _getText("GuiJobDetailsTitle");
    public string JobDetailsNoSelection => _getText("GuiJobDetailsNoSelection");
    public string JobDetailsStartLabel => _getText("GuiJobActionStart");
    public string JobDetailsStopLabel => _getText("GuiJobActionStop");
    public string JobDetailsBreakLabel => _getText("GuiJobActionBreak");

    // Field labels (shared between detail panel and editor)
    public string LabelName => _getText("GuiLabelName");
    public string LabelSourceFolder => _getText("GuiLabelSourceFolder");
    public string LabelDestinationFolder => _getText("GuiLabelDestinationFolder");
    public string LabelType => _getText("GuiLabelType");

    // Execution detail fields
    public string JobDetailsTotalFiles => _getText("GuiJobDetailsTotalFiles");
    public string JobDetailsFilesRemaining => _getText("GuiJobDetailsFilesRemaining");
    public string JobDetailsTotalSize => _getText("GuiJobDetailsTotalSize");
    public string JobDetailsSizeRemaining => _getText("GuiJobDetailsSizeRemaining");
    public string JobDetailsCurrentFile => _getText("GuiJobDetailsCurrentFile");
    public string JobDetailsLastUpdate => _getText("GuiJobDetailsLastUpdate");

    // Cat widget
    public string CatWidgetTitle => _getText("GuiCatWidgetTitle");

    // Job editor window
    public string JobTypeFullLabel => _getText("GuiJobTypeFull");
    public string JobTypeDifferentialLabel => _getText("GuiJobTypeDifferential");
    public string BrowseLabel => _getText("GuiBrowse");
    public string SaveLabel => _getText("GuiSave");
    public string CancelLabel => _getText("GuiCancel");

    // Settings menu labels (passed programmatically to SettingsViewModel)
    public string LanguageLabel => _getText("GuiLanguageLabel");
    public string LogFormatLabel => _getText("GuiLogFormatLabel");
    public string LogFormatJsonLabel => _getText("GuiLogFormatJson");
    public string LogFormatXmlLabel => _getText("GuiLogFormatXml");
    public string BusinessSoftwareLabel => _getText("GuiBusinessSoftwareLabel");
    public string CryptoExtensionsLabel => _getText("GuiCryptoExtensionsLabel");
    public string PriorityExtensionsLabel => _getText("GuiPriorityExtensionsLabel");
    public string MaxParallelFileSizeLabel => _getText("GuiMaxParallelFileSizeLabel");
    public string StorageModeLabel => _getText("GuiStorageModeLabel");
    public string StorageModeLocalLabel => _getText("GuiStorageModeLocal");
    public string StorageModeDockerLabel => _getText("GuiStorageModeDocker");
    public string StorageModeBothLabel => _getText("GuiStorageModeBoth");
}
