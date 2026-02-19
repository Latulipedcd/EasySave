using Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace EasySave.ConsoleApp
{

    public class ConsoleLogic
    {
        //All the logical part of the console
        private readonly IJobManagementService _jobService;
        private readonly ILanguageService _languageService;

        public ConsoleLogic(IJobManagementService jobService, ILanguageService languageService)
        {
            _jobService = jobService ?? throw new ArgumentNullException(nameof(jobService));
            _languageService = languageService ?? throw new ArgumentNullException(nameof(languageService));
        }



        public bool DisplayJobs()
        {
            Console.Clear();
            Console.WriteLine(_languageService.GetString("JobListTitle"));
            Console.WriteLine();

            var jobs = _jobService.GetBackupJobs();

            if (jobs.Count == 0)
            {
                Console.WriteLine(_languageService.GetString("NoJobsFound"));
                return false;
            }

            int index = 1;
            foreach (var job in jobs)
            {
                Console.WriteLine($"{index}. {job.Name}");
                Console.WriteLine($"   Source : {job.SourceDirectory}");
                Console.WriteLine($"   Target : {job.TargetDirectory}");
                Console.WriteLine($"   Type   : {job.Type}");
                Console.WriteLine();
                index++;
            }

            return true;
        }


        public void CreateJob()
        {
            Console.Clear();

            Console.WriteLine(_languageService.GetString("AskJobName"));
            var jobTitle = Console.ReadLine();

            Console.WriteLine(_languageService.GetString("AskSrcPath"));
            var jobSrcPath = Console.ReadLine();

            Console.WriteLine(_languageService.GetString("AskTargetPath"));
            var jobDestPath = Console.ReadLine();

            Console.WriteLine(_languageService.GetString("AskJobType"));
            var jobTypeInput = Console.ReadLine();

            if (!int.TryParse(jobTypeInput, out int jobType))
            {
                Console.WriteLine(_languageService.GetString("ErrorInvalidOption"));
                return;
            }

            bool success = _jobService.CreateBackupJob(
                jobTitle!,
                jobSrcPath!,
                jobDestPath!,
                jobType,
                out string resultMessage
            );

            Console.WriteLine(resultMessage);
        }

        public void DeleteJob()
        {
            bool hasJobs = DisplayJobs();
            if (!hasJobs)
            {
                return;
            }

            Console.WriteLine(_languageService.GetString("AskJobNameToDelete"));
            var jobIdInput = Console.ReadLine();

            if (!int.TryParse(jobIdInput, out int jobId))
            {
                Console.WriteLine(_languageService.GetString("ErrorInvalidOption"));

                return;
            }

            bool success = _jobService.DeleteBackupJob(
                jobId,
                out string resultMessage
            );

            Console.WriteLine(resultMessage);

        }

        public void UpdateJob()
        {
            bool hasJobs = DisplayJobs();
            if (!hasJobs)
            {
                return;
            }

            Console.WriteLine(_languageService.GetString("AskJobNameToUpdate"));
            var jobIdInput = Console.ReadLine();

            if (!int.TryParse(jobIdInput, out int jobId))
            {
                Console.WriteLine(_languageService.GetString("ErrorInvalidOption"));

                return;
            }

            Console.WriteLine(_languageService.GetString("AskSrcPath"));
            var newSrcPath = Console.ReadLine();

            Console.WriteLine(_languageService.GetString("AskTargetPath"));
            var newTargetPath = Console.ReadLine();

            Console.WriteLine(_languageService.GetString("AskJobType"));
            var jobTypeInput = Console.ReadLine();

            if (!int.TryParse(jobTypeInput, out int jobType))
            {
                Console.WriteLine(_languageService.GetString("ErrorInvalidOption"));

                return;
            }

            bool success = _jobService.UpdateBackupJob(
                jobId,
                newSrcPath!,
                newTargetPath!,
                jobType,
                out string resultMessage
            );

            Console.WriteLine(resultMessage);

        }


        public void ExecuteJobs()
        {
            bool hasJobs = DisplayJobs();
            if (!hasJobs)
            {
                return;
            }

            Console.WriteLine(_languageService.GetString("AskJobsToExecute"));
            Console.WriteLine(_languageService.GetString("ExecuteHelp"));
            var input = Console.ReadLine();

            if (string.IsNullOrWhiteSpace(input))
            {
                Console.WriteLine(_languageService.GetString("ErrorInvalidOption"));

                return;
            }

            bool success = _jobService.ExecuteBackupJobs(
                input,
                out var results,
                out string errorMessage
            );

            if (!success)
            {
                Console.WriteLine(errorMessage);

                return;
            }

            Console.WriteLine();

            foreach (var state in results)
            {
                Console.WriteLine($"{state.Job.Name} : {state.Status}");
            }


        }
    }
}
