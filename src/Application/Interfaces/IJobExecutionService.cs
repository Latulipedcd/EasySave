using Core.Models;

namespace EasySave.Application.Interfaces
{
    /// <summary>
    /// Service that executes backup jobs and provides runtime control (pause/resume/stop).
    /// </summary>
    public interface IJobExecutionService
    {
        /// <summary>
        /// Executes backup jobs synchronously based on the provided input configuration.
        /// </summary>
        /// <param name="userInput">The input string detailing the backup job parameters or configuration.</param>
        /// <param name="results">When this method returns, contains the list of backup states resulting from the execution.</param>
        /// <param name="errorMessage">When this method returns, contains an error message if the execution failed; otherwise, null or empty.</param>
        /// <returns><c>true</c> if the backup jobs executed successfully; otherwise, <c>false</c>.</returns>
        bool ExecuteBackupJobs(string userInput, out List<BackupState> results, out string errorMessage);
        
        /// <summary>
        /// Executes backup jobs asynchronously based on the provided input configuration.
        /// </summary>
        /// <param name="userInput">The input string detailing the backup job parameters or configuration.</param>
        /// <returns>A task that represents the asynchronous operation. 
        /// The task result contains a tuple with the success status, a list of backup states, and an error message if applicable.
        /// </returns>
        Task<(bool success, List<BackupState> results, string errorMessage)> ExecuteBackupJobsAsync(string userInput);
        
        /// <summary>
        /// Temporarily suspends the execution of a specific backup job.
        /// </summary>
        /// <param name="jobName">The name or identifier of the job to pause.</param>
        void PauseJob(string jobName);

        /// <summary>
        /// Resumes the execution of a previously paused backup job.
        /// </summary>
        /// <param name="jobName">The name or identifier of the job to resume.</param>
        void ResumeJob(string jobName);

        /// <summary>
        /// Permanently halts and cancels the execution of a specific backup job.
        /// </summary>
        /// <param name="jobName">The name or identifier of the job to stop.</param>
        void StopJob(string jobName);

        /// <summary>
        /// Temporarily suspends the execution of all currently active backup jobs.
        /// </summary>
        void PauseAllJobs();

        /// <summary>
        /// Resumes the execution of all previously paused backup jobs.
        /// </summary>
        void ResumeAllJobs();

        /// <summary>
        /// Permanently halts and cancels the execution of all backup jobs.
        /// </summary>
        void StopAllJobs();
    }
}
