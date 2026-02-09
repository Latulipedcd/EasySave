using Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace Core.Services
{
    public class JobStorage : IJobStorage
    {
        public string JobsDirectory => //Set directory for the jobs
            Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "EasySave",
                "Jobs");

        public string JobsFilePath =>
            Path.Combine(JobsDirectory, "jobs.json");
    }

}
