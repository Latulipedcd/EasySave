using Core.Enums;
using Core.Models;

namespace EasySave.Application.ViewModels;

/// <summary>
/// ViewModel responsible for job creation and editing operations
/// </summary>
public class JobEditorViewModel : ViewModelBase
{
    /// <summary>
    /// Name of the new or edited job
    /// </summary>
    private string _jobName = string.Empty;
    public string JobName
    {
        get => _jobName;
        set => SetProperty(ref _jobName, value);
    }

    /// <summary>
    /// Source directory path
    /// </summary>
    private string _sourceDirectory = string.Empty;
    public string SourceDirectory
    {
        get => _sourceDirectory;
        set => SetProperty(ref _sourceDirectory, value);
    }

    /// <summary>
    /// Target/destination directory path
    /// </summary>
    private string _targetDirectory = string.Empty;
    public string TargetDirectory
    {
        get => _targetDirectory;
        set => SetProperty(ref _targetDirectory, value);
    }

    /// <summary>
    /// Index of backup type (0=Full, 1=Differential)
    /// </summary>
    private int _backupTypeIndex;
    public int BackupTypeIndex
    {
        get => _backupTypeIndex;
        set => SetProperty(ref _backupTypeIndex, value);
    }

    /// <summary>
    /// ID of the job being edited (null if creating new)
    /// </summary>
    private int? _editingJobId;
    public int? EditingJobId
    {
        get => _editingJobId;
        private set => SetProperty(ref _editingJobId, value, () => OnPropertyChanged(nameof(IsEditing)));
    }

    /// <summary>
    /// Indicates if currently in edit mode
    /// </summary>
    public bool IsEditing => EditingJobId.HasValue;

    public JobEditorViewModel()
    {
        _backupTypeIndex = 0;
    }

    /// <summary>
    /// Loads a job for editing
    /// </summary>
    public void LoadJobForEdit(BackupJob job, int jobId)
    {
        if (job == null)
        {
            ClearForm();
            return;
        }

        EditingJobId = jobId;
        JobName = job.Name;
        SourceDirectory = job.SourceDirectory;
        TargetDirectory = job.TargetDirectory;
        BackupTypeIndex = job.Type == BackupType.Full ? 0 : 1;
    }

    /// <summary>
    /// Clears the form and exits edit mode
    /// </summary>
    public void ClearForm()
    {
        EditingJobId = null;
        JobName = string.Empty;
        SourceDirectory = string.Empty;
        TargetDirectory = string.Empty;
        BackupTypeIndex = 0;
    }

    /// <summary>
    /// Exits edit mode without clearing the form
    /// </summary>
    public void ExitEditMode()
    {
        EditingJobId = null;
    }

    /// <summary>
    /// Sets form values programmatically
    /// </summary>
    public void SetFormValues(string name, string source, string target, int typeIndex)
    {
        JobName = name;
        SourceDirectory = source;
        TargetDirectory = target;
        BackupTypeIndex = typeIndex;
    }
}
