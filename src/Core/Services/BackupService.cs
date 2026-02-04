using Core.Enums;
using Core.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace Core.Services
{
    public class BackupService
    {
        //private readonly LogService _logService;

        public BackupService(
            //LogService logService
            )
        {
            //_logService = logService;
        }

        public BackupState ExecuteBackup(BackupJob job)
        {
            var state = new BackupState(job)
            {
                Status = BackupStatus.Active
            };

            var files = Directory.GetFiles(job.SourceDirectory, "*", SearchOption.AllDirectories);
            state.TotalFiles = files.Length;
            state.FilesRemaining = files.Length;

            foreach (var file in files)
            {
                var relativePath = Path.GetRelativePath(job.SourceDirectory, file);
                var targetPath = Path.Combine(job.TargetDirectory, relativePath);
                Directory.CreateDirectory(Path.GetDirectoryName(targetPath)!);

                state.CurrentFileSource = file;
                state.CurrentFileTarget = targetPath;
                bool success = CopyFile(file, targetPath);

                if (!success)
                    state.Status = BackupStatus.Error;

                state.FilesRemaining--;
                state.BytesRemaining = files.Length; //A revoir
                //_logService.LogFileCopy(state, success);
            }

            if (state.Status != BackupStatus.Error)
                state.Status = BackupStatus.Completed;

            return state;
        }

        private bool CopyFile(string source, string target)
        {
            try
            {
                File.Copy(source, target, true);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
