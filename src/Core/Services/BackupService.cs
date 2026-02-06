using Core.Enums;
using Core.Interfaces;
using Core.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace Core.Services
{
    public class BackupService : IBackupService
    {
        //private readonly LogService _logService;
        private readonly IFileService _fileService;
        private readonly ICopyService _copyService;

        public BackupService(
            //LogService logService
            IFileService fileService,
            ICopyService copyService
            )
        {
            //_logService = logService;
            _fileService = fileService;
            _copyService = copyService;
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

            foreach (var file in files)
            {
                var relativePath = Path.GetRelativePath(job.SourceDirectory, file);
                var targetPath = Path.Combine(job.TargetDirectory, relativePath);
                Directory.CreateDirectory(Path.GetDirectoryName(targetPath)!);

                bool shouldCopy = true;

                if (job.Type == BackupType.Differenciate && File.Exists(targetPath))
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
            }

            if (state.Status != BackupStatus.Error)
                state.Status = BackupStatus.Completed;

            return state;
        }
    }
}
