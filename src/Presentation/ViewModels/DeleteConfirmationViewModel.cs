using System;
using System.Collections.Generic;
using System.Linq;

namespace EasySave.Presentation.ViewModels;

/// <summary>
/// ViewModel for the delete confirmation dialog.
/// Owns all text formatting and job name display logic so the view is pure AXAML.
/// </summary>
public class DeleteConfirmationViewModel : ViewModelBase
{
    public string Title { get; }
    public string Message { get; }
    public string JobsLabel { get; }
    public string JobNamesText { get; }
    public string ConfirmLabel { get; }
    public string CancelLabel { get; }

    public DeleteConfirmationViewModel(IReadOnlyList<string> jobNames, Func<string, string> getText)
    {
        Title = getText("GuiDeleteConfirmTitle");
        JobsLabel = getText("GuiDeleteConfirmJobsLabel");
        ConfirmLabel = getText("GuiDeleteConfirmYes");
        CancelLabel = getText("GuiDeleteConfirmNo");

        Message = jobNames.Count == 1
            ? string.Format(getText("GuiDeleteConfirmMessageSingle"), jobNames[0])
            : string.Format(getText("GuiDeleteConfirmMessageMultiple"), jobNames.Count);

        JobNamesText = jobNames.Count > 0
            ? string.Join(Environment.NewLine, jobNames.Select(name => $"• {name}"))
            : "-";
    }
}
