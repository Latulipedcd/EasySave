using System.Collections.Generic;
using System.Threading.Tasks;
using Core.Models;

namespace EasySave.Application.Interfaces;

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
    ///
    /// Remarks:
    /// - This synchronous helper calls the async variant internally and blocks until completion.
    /// - It is kept for compatibility with existing console usage. Prefer <see cref="ExecuteBackupJobsAsync"/>.
    /// - The returned <paramref name="results"/> contains the final <see cref="BackupState"/> for each selected job
    ///   after the execution completed (Completed / Error / Cancelled).
    /// - <paramref name="errorMessage"/> is set when the input parsing fails or other startup validation fails.
    /// </summary>
    /// <param name="userInput">Job selection string</param>
    /// <param name="results">List of backup states (output)</param>
    /// <param name="errorMessage">Error message if parsing fails (output)</param>
    /// <returns>True if execution completed successfully, false otherwise.</returns>
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
    /// Pauses a running job by <paramref name="jobName"/>.
    ///
    /// Notes:
    /// - This sets a manual pause flag for the named job; the job will block at the next pause-check point.
    /// - If the job is already paused by the business software monitor, calling ResumeJob will only resume when
    ///   both manual and business pauses are cleared.
    /// </summary>
    /// <param name="jobName">The name of the job to pause (exact match).</param>
    void PauseJob(string jobName);

    /// <summary>
    /// Resumes a paused job by <paramref name="jobName"/> (clears the manual pause flag).
    /// If the business monitor also has the job paused, the job will remain paused until that condition clears.
    /// </summary>
    /// <param name="jobName">The name of the job to resume (exact match).</param>
    void ResumeJob(string jobName);

    /// <summary>
    /// Stops (cancels) a running job by <paramref name="jobName"/>.
    /// This signals cancellation to the job's CancellationTokenSource; the job will stop at
    /// its next cancellation checkpoint and return a state with Status = Cancelled.
    /// </summary>
    /// <param name="jobName">The name of the job to stop.</param>
    void StopJob(string jobName);

    /// <summary>
    /// Pauses all currently running jobs by setting their manual pause flag.
    /// Use <see cref="ResumeAllJobs"/> to clear the manual pause flag for all jobs.
    /// </summary>
    void PauseAllJobs();

    /// <summary>
    /// Resumes all jobs by clearing their manual pause flags. Jobs that remain paused due to
    /// business software detection will remain paused until that condition clears.
    /// </summary>
    void ResumeAllJobs();

    /// <summary>
    /// Stops (cancels) all currently running jobs. This signals cancellation for each job's
    /// <see cref="CancellationTokenSource"/> and is equivalent to calling <see cref="StopJob(string)"/> for every job.
    /// </summary>
    void StopAllJobs();
}
