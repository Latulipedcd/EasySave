using Core.Enums;
using Core.Interfaces;
using Core.Models;
using Core.Services;
using Log.Enums;
using Moq;

namespace EasySave.Tests;

/// <summary>
/// Unit tests for <see cref="BackupStateService"/>.
/// </summary>
public class BackupStateServiceTests
{
    private static BackupJob MakeJob() =>
        new BackupJob("Job", "/src", "/dst", BackupType.Full);

    // ── Initialize ────────────────────────────────────────────────────────────

    [Fact]
    public void Initialize_ReturnsActiveStateWithCorrectFileCounts()
    {
        string tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);
        File.WriteAllText(Path.Combine(tempDir, "a.txt"), "hello");
        File.WriteAllText(Path.Combine(tempDir, "b.txt"), "world");

        var job = new BackupJob("Job", tempDir, "/dst", BackupType.Full);

        var fileServiceMock = new Mock<IFileService>();
        fileServiceMock
            .Setup(f => f.GetFiles(tempDir))
            .Returns(Directory.GetFiles(tempDir));

        var progressWriterMock = new Mock<IProgressWriter>();

        var service = new BackupStateService(fileServiceMock.Object, progressWriterMock.Object);

        var (state, files) = service.Initialize(job);

        Directory.Delete(tempDir, true);

        Assert.Equal(BackupStatus.Active, state.Status);
        Assert.Equal(2, state.TotalFiles);
        Assert.Equal(2, state.FilesRemaining);
        Assert.Equal(2, files.Length);
    }

    // ── CreateError ───────────────────────────────────────────────────────────

    [Fact]
    public void CreateError_ReturnsErrorStateAndWritesProgress()
    {
        var job = MakeJob();
        var fileServiceMock = new Mock<IFileService>();
        var progressWriterMock = new Mock<IProgressWriter>();

        var service = new BackupStateService(fileServiceMock.Object, progressWriterMock.Object);

        var state = service.CreateError(job, "Something went wrong");

        Assert.Equal(BackupStatus.Error, state.Status);
        Assert.Equal("Something went wrong", state.ErrorMessage);
        progressWriterMock.Verify(p => p.Write(It.Is<BackupState>(s => s.Status == BackupStatus.Error)), Times.Once);
    }

    // ── Finalize ──────────────────────────────────────────────────────────────

    [Fact]
    public void Finalize_ActiveState_SetsStatusToCompleted()
    {
        var job = MakeJob();
        var fileServiceMock = new Mock<IFileService>();
        var progressWriterMock = new Mock<IProgressWriter>();

        var service = new BackupStateService(fileServiceMock.Object, progressWriterMock.Object);
        var state = new BackupState(job) { Status = BackupStatus.Active };

        service.Finalize(job, state);

        Assert.Equal(BackupStatus.Completed, state.Status);
    }

    [Fact]
    public void Finalize_ErrorState_DoesNotChangeStatus()
    {
        var job = MakeJob();
        var fileServiceMock = new Mock<IFileService>();
        var progressWriterMock = new Mock<IProgressWriter>();

        var service = new BackupStateService(fileServiceMock.Object, progressWriterMock.Object);
        var state = new BackupState(job) { Status = BackupStatus.Error };

        service.Finalize(job, state);

        Assert.Equal(BackupStatus.Error, state.Status);
    }

    [Fact]
    public void Finalize_CancelledState_DoesNotChangeStatus()
    {
        var job = MakeJob();
        var fileServiceMock = new Mock<IFileService>();
        var progressWriterMock = new Mock<IProgressWriter>();

        var service = new BackupStateService(fileServiceMock.Object, progressWriterMock.Object);
        var state = new BackupState(job) { Status = BackupStatus.Cancelled };

        service.Finalize(job, state);

        Assert.Equal(BackupStatus.Cancelled, state.Status);
    }

    [Fact]
    public void Finalize_WritesProgressWithZeroedCounters()
    {
        var job = MakeJob();
        var fileServiceMock = new Mock<IFileService>();
        var progressWriterMock = new Mock<IProgressWriter>();

        var service = new BackupStateService(fileServiceMock.Object, progressWriterMock.Object);
        var state = new BackupState(job) { Status = BackupStatus.Active, TotalFiles = 5, FilesRemaining = 1 };

        service.Finalize(job, state);

        progressWriterMock.Verify(p => p.Write(It.Is<BackupState>(
            s => s.TotalFiles == 0 && s.FilesRemaining == 0 && s.TotalBytes == 0 && s.BytesRemaining == 0)),
            Times.Once);
    }

    // ── WriteProgress ─────────────────────────────────────────────────────────

    [Fact]
    public void WriteProgress_InvokesProgressWriterWithCurrentCounters()
    {
        var job = MakeJob();
        var fileServiceMock = new Mock<IFileService>();
        var progressWriterMock = new Mock<IProgressWriter>();

        var service = new BackupStateService(fileServiceMock.Object, progressWriterMock.Object);
        var state = new BackupState(job)
        {
            Status = BackupStatus.Active,
            TotalFiles = 10,
            FilesRemaining = 4,
            TotalBytes = 1000,
            BytesRemaining = 400
        };

        service.WriteProgress(job, state);

        progressWriterMock.Verify(p => p.Write(It.Is<BackupState>(
            s => s.TotalFiles == 10 && s.FilesRemaining == 4 && s.TotalBytes == 1000 && s.BytesRemaining == 400)),
            Times.Once);
    }
}
