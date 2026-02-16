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

        public BackupState ExecuteBackup(BackupJob job, LogFormat format, string? businessSoftware, List<string> CryptoSoftExtensions, string? cryptoSoftPath)
        {
            var state = InitializeBackupState(job, format, out var files);

            // Early exit if business software is blocking
            if (CheckBusinessSoftwareBlocking(job, businessSoftware, state))
                return state;

            foreach (var file in files)
            {
                var relativePath = Path.GetRelativePath(job.SourceDirectory, file);
                var targetPath = Path.Combine(job.TargetDirectory, relativePath);

                // Create target directory if needed
                CreateTargetDirectory(job, file, targetPath);

                // Check if file should be processed (differential backup logic)
                if (!ShouldProcessFile(job, targetPath, file))
                {
                    state.FilesRemaining--;
                    continue;
                }

                // Process the file (copy or encrypt)
                var stopwatch = Stopwatch.StartNew();
                bool success = ProcessFile(file, targetPath, CryptoSoftExtensions, cryptoSoftPath, out bool wasEncrypted, out long encryptionTimeMs);
                stopwatch.Stop();

                if (!success)
                    state.Status = BackupStatus.Error;

                // Log and update progress
                var fileInfo = new FileInfo(file);
                LogFileOperation(job, file, targetPath, stopwatch.Elapsed, fileInfo.Length, wasEncrypted, encryptionTimeMs);

                state.FilesRemaining--;
                state.BytesRemaining -= fileInfo.Length;
                state.CurrentFileSource = file;
                state.CurrentFileTarget = targetPath;

                UpdateProgress(job, state);
            }

            FinalizeBackup(job, state);
            return state;
        }

        /// <summary>
        /// Initializes the backup state with file discovery and totals.
        /// </summary>
        private BackupState InitializeBackupState(BackupJob job, LogFormat format, out string[] files)
        {
            files = _fileService.GetFiles(job.SourceDirectory);
            long totalBytes = files.Sum(f => new FileInfo(f).Length);

            _logService.Configure(format);

            return new BackupState(job)
            {
                Status = BackupStatus.Active,
                TotalFiles = files.Length,
                FilesRemaining = files.Length,
                TotalBytes = totalBytes,
                BytesRemaining = totalBytes
            };
        }

        /// <summary>
        /// Checks if business software is blocking the backup.
        /// </summary>
        /// <returns>True if backup should be blocked, false otherwise.</returns>
        private bool CheckBusinessSoftwareBlocking(BackupJob job, string? businessSoftware, BackupState state)
        {
            if (businessSoftware == null)
                return false;

            if (!_businessSoftwareMonitor.IsBusinessSoftwareRunning(businessSoftware))
                return false;

            // Business software is running - block backup
            state.Status = BackupStatus.Error;

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
            _logService.LogBackup(logError);

            var errorState = new BackupState(job)
            {
                Status = BackupStatus.Error,
                TimeStamp = DateTime.Now,
                ErrorMessage = "Backup stopped due to running business software."
            };
            _progressWriter.Write(errorState);

            return true;
        }

        /// <summary>
        /// Creates the target directory if it doesn't exist and logs the operation.
        /// </summary>
        private void CreateTargetDirectory(BackupJob job, string sourceFile, string targetPath)
        {
            var folderPath = Path.GetDirectoryName(targetPath)!;
            if (Directory.Exists(folderPath))
                return;

            var stopwatch = Stopwatch.StartNew();
            Directory.CreateDirectory(folderPath);
            stopwatch.Stop();

            var logEntryFolder = new LogEntry
            {
                BackupName = job.Name,
                Source = PathHelper.ToUncPath(sourceFile),
                Target = PathHelper.ToUncPath(folderPath),
                Duration = stopwatch.Elapsed,
                Timestamp = DateTime.Now,
                FileSize = 0,
                WorkType = WorkType.folder_creation
            };
            _logService.LogBackup(logEntryFolder);
        }

        /// <summary>
        /// Determines if a file should be processed based on backup type (differential logic).
        /// </summary>
        private static bool ShouldProcessFile(BackupJob job, string targetPath, string sourceFile)
        {
            if (job.Type != BackupType.Differencial)
                return true;

            if (!File.Exists(targetPath))
                return true;

            var sourceInfo = new FileInfo(sourceFile);
            var targetInfo = new FileInfo(targetPath);

            return sourceInfo.LastWriteTime > targetInfo.LastWriteTime;
        }

        /// <summary>
        /// Processes a single file - either encrypts or copies it.
        /// </summary>
        /// <param name="wasEncrypted">Output parameter indicating if the file was encrypted.</param>
        /// <param name="encryptionTimeMs">Output parameter with encryption time: 0=no encryption, >0=time in ms, <0=error code.</param>
        /// <returns>True if operation succeeded, false otherwise.</returns>
        private bool ProcessFile(string sourceFile, string targetPath, List<string> cryptoExtensions, string? cryptoSoftPath, out bool wasEncrypted, out long encryptionTimeMs)
        {
            wasEncrypted = RequiresEncryption(sourceFile, cryptoExtensions);

            if (wasEncrypted)
            {
                return EncryptFile(sourceFile, targetPath, cryptoSoftPath, out encryptionTimeMs);
            }
            else
            {
                encryptionTimeMs = 0; // No encryption
                return _copyService.CopyFiles(sourceFile, targetPath);
            }
        }

        /// <summary>
        /// Logs a file operation (copy or encryption).
        /// </summary>
        private void LogFileOperation(BackupJob job, string sourceFile, string targetPath, TimeSpan duration, long fileSize, bool wasEncrypted, long encryptionTimeMs)
        {
            var logEntry = new LogEntry
            {
                BackupName = job.Name,
                Source = PathHelper.ToUncPath(sourceFile),
                Target = PathHelper.ToUncPath(targetPath),
                Duration = duration,
                Timestamp = DateTime.Now,
                FileSize = fileSize,
                WorkType = wasEncrypted ? WorkType.encryption : WorkType.file_transfer,
                EncryptionTimeMs = encryptionTimeMs
            };
            _logService.LogBackup(logEntry);
        }

        /// <summary>
        /// Updates progress information after processing a file.
        /// </summary>
        private void UpdateProgress(BackupJob job, BackupState state)
        {
            var progressState = new BackupState(job)
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

            _progressWriter.Write(progressState);
        }

        /// <summary>
        /// Finalizes the backup by setting completion status and writing final progress.
        /// </summary>
        private void FinalizeBackup(BackupJob job, BackupState state)
        {
            if (state.Status != BackupStatus.Error)
                state.Status = BackupStatus.Completed;

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

            _progressWriter.Write(resetInfo);
        }

        /// <summary>
        /// Checks if a file requires encryption based on its extension.
        /// </summary>
        private static bool RequiresEncryption(string filePath, List<string> cryptoExtensions)
        {
            if (cryptoExtensions == null || cryptoExtensions.Count == 0)
                return false;

            string extension = Path.GetExtension(filePath).ToLowerInvariant();

            // Normalize extensions in the list to lowercase and ensure they start with a dot
            return cryptoExtensions.Any(ext => 
            {
                string normalizedExt = ext.ToLowerInvariant();
                if (!normalizedExt.StartsWith('.'))
                    normalizedExt = '.' + normalizedExt;

                return normalizedExt == extension;
            });
        }

        /// <summary>
        /// Encrypts a file using CryptoSoft.exe and saves it to the target path.
        /// </summary>
        /// <param name="encryptionTimeMs">Output: 0=no encryption, >0=time in ms, <0=error code (-1=path error, -2=process error, -3=exit code error, -99=exception)</param>
        private bool EncryptFile(string sourceFilePath, string targetFilePath, string? cryptoSoftPath, out long encryptionTimeMs)
        {
            // DEBUG: Log the actual path being used
            Console.WriteLine($"[DEBUG] EncryptFile called with cryptoSoftPath: '{cryptoSoftPath}'");

            // Validate CryptoSoft.exe path
            if (string.IsNullOrEmpty(cryptoSoftPath) || !File.Exists(cryptoSoftPath))
            {
                encryptionTimeMs = -1; // Error code: CryptoSoft.exe not found
                // Fallback to normal copy if CryptoSoft is not available
                return _copyService.CopyFiles(sourceFilePath, targetFilePath);
            }

            var encryptionStopwatch = Stopwatch.StartNew();
            try
            {
                // TODO: Replace "default-key" with actual encryption key from configuration
                var startInfo = new ProcessStartInfo
                {
                    FileName = cryptoSoftPath,
                    Arguments = $"\"{sourceFilePath}\" \"{targetFilePath}\" \"default-key\"",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                };

                using var process = Process.Start(startInfo);
                if (process == null)
                {
                    encryptionStopwatch.Stop();
                    encryptionTimeMs = -2; // Error code: Failed to start process
                    return false;
                }

                process.WaitForExit();
                encryptionStopwatch.Stop();

                // Read output for debugging
                string stdout = process.StandardOutput.ReadToEnd();
                string stderr = process.StandardError.ReadToEnd();

                if (process.ExitCode == 0)
                {
                    encryptionTimeMs = encryptionStopwatch.ElapsedMilliseconds;
                    return true;
                }
                else
                {
                    // Log the actual exit code as negative value to distinguish from success
                    // This preserves the real error information from CryptoSoft.exe
                    encryptionTimeMs = process.ExitCode < 0 ? process.ExitCode : -process.ExitCode;

                    // Log detailed error information
                    if (!string.IsNullOrEmpty(stderr))
                        Console.WriteLine($"[ENCRYPTION ERROR] CryptoSoft stderr: {stderr}");
                    if (!string.IsNullOrEmpty(stdout))
                        Console.WriteLine($"[ENCRYPTION ERROR] CryptoSoft stdout: {stdout}");

                    return false;
                }
            }
            catch (Exception ex)
            {
                encryptionStopwatch.Stop();
                encryptionTimeMs = -99; // Error code: Exception occurred
                return false;
            }
        }


    }
}
