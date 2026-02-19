using Core.Models;

namespace EasySave.Presentation.ViewModels;

/// <summary>
/// Wrapper pour l'affichage d'un BackupJob avec son ID
/// </summary>
public class BackupJobDisplayItem
{
    public BackupJob Job { get; }
    public int Id { get; }
    public string DisplayName { get; }
    public string Name => Job.Name;
    public string SourceDirectory => Job.SourceDirectory;
    public string TargetDirectory => Job.TargetDirectory;
    public string Type => Job.Type.ToString();

    public BackupJobDisplayItem(BackupJob job, int id)
    {
        Job = job;
        Id = id;
        DisplayName = $"{id}. {job.Name}";
    }
}
