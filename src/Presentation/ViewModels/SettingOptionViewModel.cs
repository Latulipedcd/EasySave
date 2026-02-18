namespace EasySave.Presentation.ViewModels;

/// <summary>
/// Option displayed for a settings entry.
/// Value is persisted, Label is shown in the UI.
/// </summary>
public sealed class SettingOptionViewModel
{
    public SettingOptionViewModel(string value, string label)
    {
        Value = value;
        Label = label;
    }

    public string Value { get; }
    public string Label { get; }

    public override string ToString() => Label;
}
