using Core.Enums;
using Core.Interfaces;
using Core.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using Log.Interfaces;
using Log.Services;
using Log.Enums;

namespace Core.Services
{
    public class BackupService : IBackupService
    {
        private readonly ILog _logService;
        private readonly IFileService _fileService;
        private readonly ICopyService _copyService;
        private readonly IProgressWriter _progressWriter;
        private readonly IBusinessSoftwareMonitor _businessSoftwareMonitor;

        public BackupService(
            ILog logService,
            IFileService fileService,
            ICopyService copyService,
            IProgressWriter progressWriter,
            IBusinessSoftwareMonitor businessSoftwareMonitor
            )
        {
            _logService = logService;
            _fileService = fileService;
            _copyService = copyService;
            _progressWriter = progressWriter;
            _businessSoftwareMonitor = businessSoftwareMonitor;
        }

        public BackupState ExecuteBackup(BackupJob job, LogFormat format, string? businessSoftware)
        {
            var state = new BackupState(job)
            {
                Status = BackupStatus.Active
            };


            var files = _fileService.GetFiles(job.SourceDirectory);
            state.TotalFiles = files.Length;
            state.FilesRemaining = files.Length;

            long totalBytes = files.Sum(f => new FileInfo(f).Length);
            state.TotalBytes = totalBytes;
            state.BytesRemaining = totalBytes;

            _logService.Configure(format); // Configure log format based on user preference

            // Check if business software is running before starting the backup
            if (businessSoftware != null) 
            {
                if (_businessSoftwareMonitor.IsBusinessSoftwareRunning("CalculatorApp"))
                {
                    state.Status = BackupStatus.Error;
                    
                    // Log the error
                    var logError = new LogEntry
                    {
                        BackupName = job.Name,
                        Source = PathHelper.ToUncPath(job.SourceDirectory),
                        Target = PathHelper.ToUncPath(job.TargetDirectory),
                        Duration = TimeSpan.Zero,
                        Timestamp = DateTime.Now,
                        FileSize = 0,
                        WorkType = WorkType.file_transfer,
                        ErrorMessage = "Backup stopped due to running business software."
                    };
                    _logService.LogBackup(logError); // Log the error in the log service

                    // Update progress with error state
                    var errorState = new BackupState(job)
                    {
                        Status = state.Status,
                        TimeStamp = DateTime.Now,
                        TotalFiles = 0,
                        FilesRemaining = 0,
                        TotalBytes = 0,
                        BytesRemaining = 0,
                        CurrentFileSource = null,
                        CurrentFileTarget = null,
                        ErrorMessage = "Backup stopped due to running business software."
                    };
                    _progressWriter.Write(errorState);

                    return state; // Exit early if business software is running
                }
            }

            foreach (var file in files)
            {
                var relativePath = Path.GetRelativePath(job.SourceDirectory, file);
                var targetPath = Path.Combine(job.TargetDirectory, relativePath);

                var folderPath = Path.GetDirectoryName(targetPath)!;
                if (!Directory.Exists(folderPath))
                {
                    var stopwatchFolder = Stopwatch.StartNew();// Start timing the folder creation

                    Directory.CreateDirectory(folderPath);

                    stopwatchFolder.Stop();// Stop timing the folder creation

                    // Log the folder creation
                    var logEntryFolder = new
                    {
                        BackupName = job.Name,
                        Source = PathHelper.ToUncPath(file),
                        Target = PathHelper.ToUncPath(folderPath),
                        Duration = stopwatchFolder.Elapsed,
                        Timestamp = DateTime.Now,
                        FileSize = 0,
                        WorkType = WorkType.folder_creation
                    };
                    _logService.LogBackup(logEntryFolder);
                }

                var stopwatch = Stopwatch.StartNew(); // Start timing the file transfer

                bool shouldCopy = true; //In use when backup type is Differencial

                if (job.Type == BackupType.Differencial && File.Exists(targetPath))
                {
                    var sourceInfo = new FileInfo(file);
                    var targetInfo = new FileInfo(targetPath);

                    shouldCopy = sourceInfo.LastWriteTime > targetInfo.LastWriteTime; //Verify if the file in the save directory is newer than the one in the original directory
                }

                if (!shouldCopy)
                {
                    state.FilesRemaining--; //Reduction of number of file to copy if the file shouldn't be copied
                    continue;
                }

                state.CurrentFileSource = file;
                state.CurrentFileTarget = targetPath;

                bool success = _copyService.CopyFiles(file, targetPath);

                if (!success)
                    state.Status = BackupStatus.Error;

                state.FilesRemaining--;

                stopwatch.Stop();// Stop timing the file transfer

                FileInfo fileInfo = new FileInfo(state.CurrentFileSource);
                long sizeInBytes = fileInfo.Length;

                // Log the file transfer
                var logEntry = new LogEntry
                {
                    BackupName = job.Name,
                    Source = PathHelper.ToUncPath(file),
                    Target = PathHelper.ToUncPath(targetPath),
                    Duration = stopwatch.Elapsed, //Calculate copy time by the time between the two spotwatch
                    Timestamp = DateTime.Now,
                    FileSize = sizeInBytes,
                    WorkType = WorkType.file_transfer
                };
                _logService.LogBackup(logEntry);

                state.BytesRemaining -= sizeInBytes;

                // Update progress after each file
                var backupState = new BackupState(job)
                {
                    Status = state.Status,
                    TimeStamp = DateTime.Now,
                    TotalFiles = state.TotalFiles,
                    FilesRemaining = state.FilesRemaining,
                    TotalBytes = state.TotalBytes,
                    BytesRemaining = state.BytesRemaining,
                    CurrentFileSource = state.CurrentFileSource,
                    CurrentFileTarget = state.CurrentFileTarget
                };

                _progressWriter.Write(backupState); //Current job progress and informations

            }

            if (state.Status != BackupStatus.Error)
                state.Status = BackupStatus.Completed;

            // Final progress update to ensure 100% completion is reflected
            var resetInfo = new BackupState(job)
            {
                Status = state.Status,
                TimeStamp = DateTime.Now,
                TotalFiles = 0,
                FilesRemaining = 0,
                TotalBytes = 0,
                BytesRemaining = 0,
                CurrentFileSource = null,
                CurrentFileTarget = null
            };

            _progressWriter.Write(resetInfo); //Remove some informations that need to disapear once job is done

            return state;
        }

        
    }
}
