using EasySave.Application.ViewModels;
using System;
using System.Collections.Generic;
using System.Text;

namespace EasySave.ConsoleApp
{

    public class ConsoleLogic
    {
        private readonly MainViewModel _vm;

        public ConsoleLogic()
        {
            _vm = new MainViewModel();
        }



        public void DisplayJobs()
        {
            Console.Clear();
            Console.WriteLine(_vm.GetText("JobListTitle"));
            Console.WriteLine();

            var jobs = _vm.GetBackupJobs();

            if (jobs.Count == 0)
            {
                Console.WriteLine(_vm.GetText("NoJobsFound"));
                return;
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

        }


        public void CreateJob()
        {
            Console.Clear();

            Console.WriteLine(_vm.GetText("AskJobName"));
            var jobTitle = Console.ReadLine();

            Console.WriteLine(_vm.GetText("AskSrcPath"));
            var jobSrcPath = Console.ReadLine();

            Console.WriteLine(_vm.GetText("AskTargetPath"));
            var jobDestPath = Console.ReadLine();

            Console.WriteLine(_vm.GetText("AskJobType"));
            var jobTypeInput = Console.ReadLine();

            if (!int.TryParse(jobTypeInput, out int jobType))
            {
                Console.WriteLine(_vm.GetText("ErrorInvalidOption"));
                return;
            }

            bool success = _vm.CreateBackupJob(
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
            Console.Clear();
            var jobs = _vm.GetBackupJobs();
            Console.WriteLine(_vm.GetText("JobListTitle"));
            Console.WriteLine();

            if (jobs.Count == 0)
            {
                Console.WriteLine(_vm.GetText("NoJobsFound"));
                return;
            }

            int index = 1;
            foreach (var job in jobs)
            {
                Console.WriteLine($"{index}. {job.Name}");
                index++;
            }

            Console.WriteLine();

            Console.WriteLine(_vm.GetText("AskJobNameToDelete"));
            var jobName = Console.ReadLine();

            if (string.IsNullOrWhiteSpace(jobName))
            {
                Console.WriteLine(_vm.GetText("ErrorInvalidOption"));
                
                return;
            }

            bool success = _vm.DeleteBackupJob(
                jobName,
                out string resultMessage
            );

            Console.WriteLine(resultMessage);
          
        }

        public void UpdateJob()
        {
            Console.Clear();
            var jobs = _vm.GetBackupJobs();
            Console.WriteLine(_vm.GetText("JobListTitle"));
            Console.WriteLine();

            if (jobs.Count == 0)
            {
                Console.WriteLine(_vm.GetText("NoJobsFound"));
                return;
            }

            int index = 1;
            foreach (var job in jobs)
            {
                Console.WriteLine($"{index}. {job.Name}");
                index++;
            }

            Console.WriteLine();

            Console.WriteLine(_vm.GetText("AskJobNameToUpdate"));
            var jobName = Console.ReadLine();

            if (string.IsNullOrWhiteSpace(jobName))
            {
                Console.WriteLine(_vm.GetText("ErrorInvalidOption"));
               
                return;
            }

            if (!_vm.BackupJobExists(jobName))
            {
                Console.WriteLine(_vm.GetText("ErrorJobNotFound"));
                
                return;
            }

            Console.WriteLine(_vm.GetText("AskSrcPath"));
            var newSrcPath = Console.ReadLine();

            Console.WriteLine(_vm.GetText("AskTargetPath"));
            var newTargetPath = Console.ReadLine();

            Console.WriteLine(_vm.GetText("AskJobType"));
            var jobTypeInput = Console.ReadLine();

            if (!int.TryParse(jobTypeInput, out int jobType))
            {
                Console.WriteLine(_vm.GetText("ErrorInvalidOption"));
               
                return;
            }

            bool success = _vm.UpdateBackupJob(
                jobName,
                newSrcPath!,
                newTargetPath!,
                jobType,
                out string resultMessage
            );

            Console.WriteLine(resultMessage);
            
        }


        public void ExecuteJobs()
        {
            Console.Clear();

            var jobs = _vm.GetBackupJobs();
            Console.WriteLine(_vm.GetText("JobListTitle"));
            Console.WriteLine();

            if (jobs.Count == 0)
            {
                Console.WriteLine(_vm.GetText("NoJobsFound"));
                return;
            }

            int index = 1;
            foreach (var job in jobs)
            {
                Console.WriteLine($"{index}. {job.Name}");
                index++;
            }

            Console.WriteLine();

            Console.WriteLine(_vm.GetText("AskJobsToExecute"));
            Console.WriteLine(_vm.GetText("ExecuteHelp"));
            var input = Console.ReadLine();

            if (string.IsNullOrWhiteSpace(input))
            {
                Console.WriteLine(_vm.GetText("ErrorInvalidOption"));
               
                return;
            }

            bool success = _vm.ExecuteBackupJobs(
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
