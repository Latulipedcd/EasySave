using System.Collections.Generic;
using System.Threading.Tasks;
using Core.Models;

namespace Core.Interfaces;

/// <summary>
/// Service contract for managing backup jobs (CRUD and Execution).
/// This is the primary interface for ViewModels and Console UI to interact with backup operations.
/// </summary>
public interface IJobManagementService
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

    /// <summary>
    /// Executes backup jobs based on user input (e.g., "1;3-5").
    /// </summary>
    /// <param name="userInput">Job selection string</param>
    /// <param name="results">List of backup states</param>
    /// <param name="errorMessage">Error message if parsing fails</param>
    /// <returns>True if execution started successfully, false otherwise</returns>
    bool ExecuteBackupJobs(string userInput, out List<BackupState> results, out string errorMessage);

    /// <summary>
    /// Executes backup jobs in parallel based on user input (e.g., "1;3-5").
    /// Jobs run concurrently and can be individually paused, resumed, or stopped.
    /// A background thread monitors for business software and pauses jobs when detected.
    /// </summary>
    /// <param name="userInput">Job selection string</param>
    /// <returns>Tuple with success flag, list of backup states, and error message.</returns>
    Task<(bool success, List<BackupState> results, string errorMessage)> ExecuteBackupJobsAsync(string userInput);

    /// <summary>
    /// Pauses a running job by name.
    /// </summary>
    void PauseJob(string jobName);

    /// <summary>
    /// Resumes a paused job by name.
    /// </summary>
    void ResumeJob(string jobName);

    /// <summary>
    /// Stops (cancels) a running job by name.
    /// </summary>
    void StopJob(string jobName);

    /// <summary>
    /// Pauses all currently running jobs.
    /// </summary>
    void PauseAllJobs();

    /// <summary>
    /// Resumes all currently paused jobs.
    /// </summary>
    void ResumeAllJobs();

    /// <summary>
    /// Stops (cancels) all currently running jobs.
    /// </summary>
    void StopAllJobs();
}
