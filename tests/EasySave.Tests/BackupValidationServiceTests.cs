using Core.Enums;
using Core.Interfaces;
using Core.Models;
using Core.Services;
using Log.Enums;
using Moq;

namespace EasySave.Tests;

/// <summary>
/// Unit tests for <see cref="BackupValidationService"/>.
/// </summary>
public class BackupValidationServiceTests
{
    private static BackupJob MakeJob(string source = "/src") =>
        new BackupJob("Job", source, "/dst", BackupType.Full);

    // ── IsSourceDirectoryMissing ──────────────────────────────────────────────

    [Fact]
    public void IsSourceDirectoryMissing_ExistingDirectory_ReturnsFalse()
    {
        string tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);

        var job = MakeJob(tempDir);
        var monitorMock = new Mock<IBusinessSoftwareMonitor>();
        var loggerMock = new Mock<IBackupLoggerService>();

        var service = new BackupValidationService(monitorMock.Object, loggerMock.Object);

        bool result = service.IsSourceDirectoryMissing(job, LogStorageMode.Local, LogFormat.Json);

        Directory.Delete(tempDir);

        Assert.False(result);
    }

    [Fact]
    public void IsSourceDirectoryMissing_NonExistentDirectory_ReturnsTrue()
    {
        var job = MakeJob("/nonexistent/path/that/does/not/exist");
        var monitorMock = new Mock<IBusinessSoftwareMonitor>();
        var loggerMock = new Mock<IBackupLoggerService>();

        var service = new BackupValidationService(monitorMock.Object, loggerMock.Object);

        bool result = service.IsSourceDirectoryMissing(job, LogStorageMode.Local, LogFormat.Json);

        Assert.True(result);
    }

    [Fact]
    public void IsSourceDirectoryMissing_NonExistentDirectory_LogsSourceNotFound()
    {
        var job = MakeJob("/nonexistent/path/that/does/not/exist");
        var monitorMock = new Mock<IBusinessSoftwareMonitor>();
        var loggerMock = new Mock<IBackupLoggerService>();

        var service = new BackupValidationService(monitorMock.Object, loggerMock.Object);

        service.IsSourceDirectoryMissing(job, LogStorageMode.Local, LogFormat.Json);

        loggerMock.Verify(l => l.LogSourceNotFound(
            LogFormat.Json, LogStorageMode.Local, "Job", job.SourceDirectory),
            Times.Once);
    }

    // ── IsBlockedByBusinessSoftware ───────────────────────────────────────────

    [Fact]
    public void IsBlockedByBusinessSoftware_NullSoftware_ReturnsFalse()
    {
        var job = MakeJob();
        var monitorMock = new Mock<IBusinessSoftwareMonitor>();
        var loggerMock = new Mock<IBackupLoggerService>();

        var service = new BackupValidationService(monitorMock.Object, loggerMock.Object);

        bool result = service.IsBlockedByBusinessSoftware(job, null, LogStorageMode.Local, LogFormat.Json);

        Assert.False(result);
        monitorMock.Verify(m => m.IsBusinessSoftwareRunning(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public void IsBlockedByBusinessSoftware_SoftwareNotRunning_ReturnsFalse()
    {
        var job = MakeJob();
        var monitorMock = new Mock<IBusinessSoftwareMonitor>();
        monitorMock.Setup(m => m.IsBusinessSoftwareRunning("MyApp")).Returns(false);
        var loggerMock = new Mock<IBackupLoggerService>();

        var service = new BackupValidationService(monitorMock.Object, loggerMock.Object);

        bool result = service.IsBlockedByBusinessSoftware(job, "MyApp", LogStorageMode.Local, LogFormat.Json);

        Assert.False(result);
    }

    [Fact]
    public void IsBlockedByBusinessSoftware_SoftwareRunning_ReturnsTrue()
    {
        var job = MakeJob();
        var monitorMock = new Mock<IBusinessSoftwareMonitor>();
        monitorMock.Setup(m => m.IsBusinessSoftwareRunning("MyApp")).Returns(true);
        var loggerMock = new Mock<IBackupLoggerService>();

        var service = new BackupValidationService(monitorMock.Object, loggerMock.Object);

        bool result = service.IsBlockedByBusinessSoftware(job, "MyApp", LogStorageMode.Local, LogFormat.Json);

        Assert.True(result);
    }

    [Fact]
    public void IsBlockedByBusinessSoftware_SoftwareRunning_LogsBlockEvent()
    {
        var job = MakeJob();
        var monitorMock = new Mock<IBusinessSoftwareMonitor>();
        monitorMock.Setup(m => m.IsBusinessSoftwareRunning("MyApp")).Returns(true);
        var loggerMock = new Mock<IBackupLoggerService>();

        var service = new BackupValidationService(monitorMock.Object, loggerMock.Object);

        service.IsBlockedByBusinessSoftware(job, "MyApp", LogStorageMode.Local, LogFormat.Json);

        loggerMock.Verify(l => l.LogBusinessSoftwareBlock(
            LogFormat.Json, LogStorageMode.Local, "Job", job.SourceDirectory, job.TargetDirectory),
            Times.Once);
    }
}
