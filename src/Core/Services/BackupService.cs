using Core.Enums;
using Core.Interfaces;
using Core.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using Log.Interfaces;

namespace Core.Services
{
    public class BackupService : IBackupService
    {
        private readonly ILog _logService;
        private readonly IFileService _fileService;
        private readonly ICopyService _copyService;
        private readonly IProgressWriter _progressWriter;

        public BackupService(
            ILog logService,
            IFileService fileService,
            ICopyService copyService,
            IProgressWriter progressWriter
            )
        {
            _logService = logService;
            _fileService = fileService;
            _copyService = copyService;
            _progressWriter = progressWriter;   
        }

        public BackupState ExecuteBackup(BackupJob job)
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

            foreach (var file in files)
            {
                var relativePath = Path.GetRelativePath(job.SourceDirectory, file);
                var targetPath = Path.Combine(job.TargetDirectory, relativePath);
                Directory.CreateDirectory(Path.GetDirectoryName(targetPath)!);

                var stopwatch = Stopwatch.StartNew();

                bool shouldCopy = true;

                if (job.Type == BackupType.Differencial && File.Exists(targetPath))
                {
                    var sourceInfo = new FileInfo(file);
                    var targetInfo = new FileInfo(targetPath);

                    shouldCopy = sourceInfo.LastWriteTime > targetInfo.LastWriteTime;
                }

                if (!shouldCopy)
                {
                    state.FilesRemaining--;
                    continue;
                }

                state.CurrentFileSource = file;
                state.CurrentFileTarget = targetPath;

                bool success = _copyService.CopyFiles(file, targetPath);

                if (!success)
                    state.Status = BackupStatus.Error;

                state.FilesRemaining--;

                stopwatch.Stop();

                FileInfo fileInfo = new FileInfo(state.CurrentFileSource);
                long sizeInBytes = fileInfo.Length;

                var logEntry = new LogEntry
                {
                    BackupName = job.Name,
                    Source = file,
                    Target = targetPath,
                    Duration = stopwatch.Elapsed,
                    Timestamp = DateTime.Now,
                    FileSize = sizeInBytes,
                    WorkType = WorkType.file_transfer
                };

                _logService.LogBackup(logEntry);

                state.BytesRemaining -= sizeInBytes;

                var backupState = new BackupState(job)
                {
                    Status = state.Status,
                    TotalFiles = state.TotalFiles,
                    FilesRemaining = state.FilesRemaining,
                    TotalBytes = state.TotalBytes,
                    BytesRemaining = state.BytesRemaining,
                    CurrentFileSource = state.CurrentFileSource,
                    CurrentFileTarget = state.CurrentFileTarget
                };

                _progressWriter.Write(backupState);

            }

            if (state.Status != BackupStatus.Error)
                state.Status = BackupStatus.Completed;

            return state;
        }
    }
}
