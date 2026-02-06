using Core.Interfaces;
using Core.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace Core.Services
{
    public class BackupJobRepository : IBackupJobRepository
    {
        private const int MaxJobs = 5;
        private readonly IJobStorage _storage;

        public BackupJobRepository(IJobStorage storage)
        {
            _storage = storage;
            EnsureStorageExists();
        }

        public IReadOnlyList<BackupJob> GetAll()
        {
            return LoadJobs();
        }

        public void Add(BackupJob job)
        {
            var jobs = LoadJobs();

            if (jobs.Count >= MaxJobs)
                throw new InvalidOperationException(
                    "Maximum number of backup jobs reached (5).");

            if (jobs.Any(j => j.Name == job.Name))
                throw new InvalidOperationException(
                    "A backup job with the same name already exists.");

            jobs.Add(job);
            SaveJobs(jobs);
        }

        public void Update(BackupJob job)
        {
            var jobs = LoadJobs();
            var index = jobs.FindIndex(j => j.Name == job.Name);

            if (index == -1)
                throw new InvalidOperationException(
                    "Backup job not found.");

            jobs[index] = job;
            SaveJobs(jobs);
        }

        public void Delete(string jobName)
        {
            var jobs = LoadJobs();

            if (jobs.RemoveAll(j => j.Name == jobName) == 0)
                throw new InvalidOperationException(
                    "Backup job not found.");

            SaveJobs(jobs);
        }

        // --------------------------------------------------
        // Private helpers
        // --------------------------------------------------

        private void EnsureStorageExists()
        {
            if (!Directory.Exists(_storage.JobsDirectory))
                Directory.CreateDirectory(_storage.JobsDirectory);

            if (!File.Exists(_storage.JobsFilePath))
                File.WriteAllText(_storage.JobsFilePath, "[]");
        }

        private List<BackupJob> LoadJobs()
        {
            var json = File.ReadAllText(_storage.JobsFilePath);

            return JsonSerializer.Deserialize<List<BackupJob>>(json)
                   ?? new List<BackupJob>();
        }

        private void SaveJobs(List<BackupJob> jobs)
        {
            var json = JsonSerializer.Serialize(
                jobs,
                new JsonSerializerOptions
                {
                    WriteIndented = true
                });

            File.WriteAllText(_storage.JobsFilePath, json);
        }
    }
}
