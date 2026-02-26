using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Core.Enums;
using Core.Interfaces;
using Core.Models;
using EasySave.Application.Interfaces;
using EasySave.Application.Services;
using Log.Enums;
using Moq;

namespace EasySave.Tests;

public class JobExecutionServiceTests
{
    [Fact]
    public async Task ExecuteBackupJobsAsync_WhenJobAlreadyRunning_ReturnsFailureAndDoesNotClearProgress()
    {
        var job = new BackupJob("Job1", "/src", "/dst", BackupType.Full);

        var userConfigRepo = new Mock<IUserConfigRepository>(MockBehavior.Strict);
        userConfigRepo.Setup(r => r.LoadLogFormat()).Returns(LogFormat.Json);
        userConfigRepo.Setup(r => r.LoadStorageMode()).Returns(LogStorageMode.Local);
        userConfigRepo.Setup(r => r.LoadBusinessSoftware()).Returns((string?)null);
        userConfigRepo.Setup(r => r.LoadCryptoSoftExtensions()).Returns(new List<string>());
        userConfigRepo.Setup(r => r.LoadPriorityExtensions()).Returns(new List<string>());
        userConfigRepo.Setup(r => r.LoadMaxParallelFileSizeKb()).Returns(0);

        var backupJobRepo = new Mock<IBackupJobRepository>(MockBehavior.Strict);
        backupJobRepo.Setup(r => r.GetAll()).Returns(new List<BackupJob> { job });

        var progressWriter = new Mock<IProgressWriter>(MockBehavior.Strict);
        progressWriter.Setup(w => w.Clear());

        var businessMonitor = new Mock<IBusinessSoftwareMonitor>(MockBehavior.Strict);

        var languageService = new Mock<ILanguageService>(MockBehavior.Strict);
        languageService.Setup(l => l.GetString("GuiErrorJobAlreadyRunning")).Returns("Already running: {0}");

        var firstRunStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var firstRunCompletion = new TaskCompletionSource<BackupState>(TaskCreationOptions.RunContinuationsAsynchronously);

        var backupService = new Mock<IBackupService>(MockBehavior.Strict);
        backupService
            .Setup(s => s.ExecuteBackupAsync(
                It.IsAny<BackupJob>(),
                It.IsAny<LogFormat>(),
                It.IsAny<List<string>>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<ManualResetEventSlim>(),
                It.IsAny<SharedExecutionContext>(),
                It.IsAny<LogStorageMode>()))
            .Returns(() =>
            {
                firstRunStarted.TrySetResult();
                return firstRunCompletion.Task;
            });

        var service = new JobExecutionService(
            userConfigRepo.Object,
            backupJobRepo.Object,
            backupService.Object,
            progressWriter.Object,
            businessMonitor.Object,
            languageService.Object);

        var firstRunTask = service.ExecuteBackupJobsAsync("1");

        await firstRunStarted.Task.WaitAsync(TimeSpan.FromSeconds(2));

        var secondRunResult = await service.ExecuteBackupJobsAsync("1");

        Assert.False(secondRunResult.success);
        Assert.Contains("Job1", secondRunResult.errorMessage, StringComparison.Ordinal);

        progressWriter.Verify(w => w.Clear(), Times.Once);

        firstRunCompletion.TrySetResult(new BackupState(job) { Status = BackupStatus.Completed });
        var firstRunResult = await firstRunTask;

        Assert.True(firstRunResult.success);
    }
}
