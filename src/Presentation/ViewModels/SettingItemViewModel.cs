using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace EasySave.Presentation.ViewModels;

/// <summary>
/// Represents a modular setting entry in the Settings menu.
/// </summary>
public sealed class SettingItemViewModel : ViewModelBase
{
    private readonly Action<string> _onSelectionChanged;
    private string _label;
    private SettingOptionViewModel? _selectedOption;
    private string _textValue = string.Empty;
    private bool _isTextInput;

    public SettingItemViewModel(
        string label,
        IEnumerable<SettingOptionViewModel> options,
        string selectedValue,
        Action<string> onSelectionChanged)
    {
        _label = label;
        _onSelectionChanged = onSelectionChanged;
        _isTextInput = false;
        Options = new ObservableCollection<SettingOptionViewModel>(options);
        SetSelectedValue(selectedValue);
    }

    // Constructor for text input mode
    public SettingItemViewModel(
        string label,
        string initialValue,
        Action<string> onValueChanged)
    {
        _label = label;
        _onSelectionChanged = onValueChanged;
        _isTextInput = true;
        _textValue = initialValue ?? string.Empty;
        Options = new ObservableCollection<SettingOptionViewModel>();
    }

    public ObservableCollection<SettingOptionViewModel> Options { get; }

    public bool IsTextInput
    {
        get => _isTextInput;
        private set => SetProperty(ref _isTextInput, value);
    }

    public string TextValue
    {
        get => _textValue;
        set
        {
            if (SetProperty(ref _textValue, value))
            {
                _onSelectionChanged(value);
            }
        }
    }

    public string Label
    {
        get => _label;
        private set => SetProperty(ref _label, value);
    }

    public SettingOptionViewModel? SelectedOption
    {
        get => _selectedOption;
        set
        {
            if (value == null || _selectedOption?.Value == value.Value)
                return;

            if (SetProperty(ref _selectedOption, value))
            {
                _onSelectionChanged(value.Value);
            }
        }
    }

    public void UpdateLabel(string label) => Label = label;

    public void SetTextValue(string textValue)
    {
        if (_textValue == textValue)
            return;

        _textValue = textValue;
        OnPropertyChanged(nameof(TextValue));
    }

    public void ReplaceOptions(IEnumerable<SettingOptionViewModel> options, string selectedValue)
    {
        Options.Clear();
        foreach (var option in options)
            Options.Add(option);

        SetSelectedValue(selectedValue);
    }

    public void SetSelectedValue(string selectedValue)
    {
        var match = Options.FirstOrDefault(option =>
            string.Equals(option.Value, selectedValue, StringComparison.OrdinalIgnoreCase));

        var fallback = match ?? Options.FirstOrDefault();
        if (fallback == null)
            return;

        // Keep the selected item reference in sync with the current options collection.
        if (ReferenceEquals(_selectedOption, fallback))
            return;

        _selectedOption = fallback;
        OnPropertyChanged(nameof(SelectedOption));
    }
}
