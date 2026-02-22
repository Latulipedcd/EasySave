using Core.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace EasySave.Application.Interfaces
{
    /// <summary>
    /// Service that executes backup jobs and provides runtime control (pause/resume/stop).
    /// </summary>
    public interface IJobExecutionService
    {
        bool ExecuteBackupJobs(string userInput, out List<BackupState> results, out string errorMessage);
        Task<(bool success, List<BackupState> results, string errorMessage)> ExecuteBackupJobsAsync(string userInput);

        void PauseJob(string jobName);
        void ResumeJob(string jobName);
        void StopJob(string jobName);

        void PauseAllJobs();
        void ResumeAllJobs();
        void StopAllJobs();
    }
}
