using EasySave.Application.ViewModels;
using System;
using System.Collections.Generic;
using System.Text;

namespace EasySave.ConsoleApp
{

    public class ConsoleLogic
    {
        //All the logical part of the console
        private readonly MainViewModel _vm;

        public ConsoleLogic()
        {
            _vm = new MainViewModel();
        }



        public bool DisplayJobs()
        {
            Console.Clear();
            Console.WriteLine(_vm.GetText("JobListTitle"));
            Console.WriteLine();

            var jobs = _vm.GetBackupJobs();

            if (jobs.Count == 0)
            {
                Console.WriteLine(_vm.GetText("NoJobsFound"));
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
            bool hasJobs = DisplayJobs();
            if (!hasJobs)
            {
                return;
            }

            Console.WriteLine(_vm.GetText("AskJobNameToDelete"));
            var jobIdInput = Console.ReadLine();

            if (!int.TryParse(jobIdInput, out int jobId))
            {
                Console.WriteLine(_vm.GetText("ErrorInvalidOption"));
                
                return;
            }

            bool success = _vm.DeleteBackupJob(
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

            Console.WriteLine(_vm.GetText("AskJobNameToUpdate"));
            var jobIdInput = Console.ReadLine();

            if (!int.TryParse(jobIdInput, out int jobId))
            {
                Console.WriteLine(_vm.GetText("ErrorInvalidOption"));
               
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
