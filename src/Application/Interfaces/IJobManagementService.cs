using Core.Models;

namespace EasySave.Application.Interfaces;

/// <summary>
/// Service contract for managing backup jobs (CRUD and Execution).
/// This is the primary interface for ViewModels and Console UI to interact with backup operations.
/// </summary>
public interface IJobManagementService : IJobExecutionService
{
    /// <summary>
    /// Retrieves all backup jobs.
    /// </summary>
    IReadOnlyList<BackupJob> GetBackupJobs();

    /// <summary>
    /// Creates a new backup job.
    /// </summary>
    /// <param name="jobTitle">The name of the job</param>
    /// <param name="jobSrcPath">Source path</param>
    /// <param name="jobTargetPath">Target path</param>
    /// <param name="jobType">0 for Full, 1 for Differential</param>
    /// <param name="message">Output message (success or error)</param>
    /// <returns>True if successful, false otherwise</returns>
    bool CreateBackupJob(string jobTitle, string jobSrcPath, string jobTargetPath, int jobType, out string message);

    /// <summary>
    /// Deletes a backup job by its ID (1-based index).
    /// </summary>
    /// <param name="jobId">The 1-based index of the job</param>
    /// <param name="message">Output message (success or error)</param>
    /// <returns>True if successful, false otherwise</returns>
    bool DeleteBackupJob(int jobId, out string message);

    /// <summary>
    /// Updates an existing backup job.
    /// </summary>
    /// <param name="jobId">The 1-based index of the job</param>
    /// <param name="newSrcPath">New source path</param>
    /// <param name="newTargetPath">New target path</param>
    /// <param name="jobType">0 for Full, 1 for Differential</param>
    /// <param name="message">Output message (success or error)</param>
    /// <returns>True if successful, false otherwise</returns>
    bool UpdateBackupJob(int jobId, string newSrcPath, string newTargetPath, int jobType, out string message);

    }
