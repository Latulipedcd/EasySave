using System;
using System.Collections.Generic;
using System.Text;

namespace Core.HELPERS
{
    public static class JobStorageHelper
    {
        public static string JobsDirectory =>
            Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "EasySave",
                "Jobs");

        public static string JobsFilePath =>
            Path.Combine(JobsDirectory, "jobs.json");
    }
}
