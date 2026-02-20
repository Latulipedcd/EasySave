using Core.Interfaces;
using Core.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace Core.Services
{
    /// <summary>
    /// Repository service for managing backup job persistence and CRUD operations.
    /// </summary>
    public class BackupJobRepository : IBackupJobRepository
    {
        private readonly IJobStorage _storage;

        /// <summary>
        /// Initializes a new instance of the BackupJobRepository class.
        /// </summary>
        /// <param name="storage">The storage provider for job persistence.</param>
        public BackupJobRepository(IJobStorage storage)
        {
            _storage = storage;
            EnsureStorageExists();
        }

        /// <summary>
        /// Retrieves all backup jobs from storage.
        /// </summary>
        /// <returns>A read-only list of all backup jobs.</returns>
        public IReadOnlyList<BackupJob> GetAll()
        {
            return LoadJobs();
        }

        /// <summary>
        /// Adds a new backup job to storage.
        /// </summary>
        /// <param name="job">The backup job to add.</param>
        /// <exception cref="InvalidOperationException">Thrown when a job with the same name already exists.</exception>
        public void Add(BackupJob job)
        {
            var jobs = LoadJobs();

            if (jobs.Any(j => j.Name == job.Name))
                throw new InvalidOperationException(
                    "A backup job with the same name already exists.");

            jobs.Add(job);
            SaveJobs(jobs);
        }

        /// <summary>
        /// Updates an existing backup job in storage.
        /// </summary>
        /// <param name="job">The backup job with updated values.</param>
        /// <exception cref="InvalidOperationException">Thrown when the job is not found.</exception>
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

        /// <summary>
        /// Deletes a backup job from storage.
        /// </summary>
        /// <param name="jobName">The name of the job to delete.</param>
        /// <exception cref="InvalidOperationException">Thrown when the job is not found.</exception>
        public void Delete(string jobName)
        {
            var jobs = LoadJobs();

            if (jobs.RemoveAll(j => j.Name == jobName) == 0)
                throw new InvalidOperationException(
                    "Backup job not found.");

            SaveJobs(jobs);
        }

        /// <summary>
        /// Replaces all jobs with the provided ordered list.
        /// </summary>
        /// <param name="jobs">Ordered list of jobs to persist.</param>
        public void ReplaceAll(IReadOnlyList<BackupJob> jobs)
        {
            var orderedJobs = jobs?.ToList() ?? new List<BackupJob>();
            SaveJobs(orderedJobs);
        }


        /// <summary>
        /// Ensures the storage directory and file exist, creating them if necessary.
        /// </summary>
        private void EnsureStorageExists()
        {
            if (!Directory.Exists(_storage.JobsDirectory))
                Directory.CreateDirectory(_storage.JobsDirectory);

            if (!File.Exists(_storage.JobsFilePath))
                File.WriteAllText(_storage.JobsFilePath, "[]");
        }

        /// <summary>
        /// Loads all backup jobs from the JSON file.
        /// </summary>
        /// <returns>A list of backup jobs, or an empty list if the file is empty or invalid.</returns>
        private List<BackupJob> LoadJobs()
        {
            var json = File.ReadAllText(_storage.JobsFilePath);

            return JsonSerializer.Deserialize<List<BackupJob>>(json)
                   ?? new List<BackupJob>();
        }

        /// <summary>
        /// Saves the list of backup jobs to the JSON file with indented formatting.
        /// </summary>
        /// <param name="jobs">The list of jobs to save.</param>
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
