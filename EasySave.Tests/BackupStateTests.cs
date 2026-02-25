using Core.Enums;
using Core.Models;

namespace EasySave.Tests;

/// <summary>
/// Unit tests for <see cref="BackupState"/> progress calculation and status transitions.
/// </summary>
public class BackupStateTests
{
    private static BackupJob MakeJob() =>
        new BackupJob("TestJob", "/src", "/dst", BackupType.Full);

    [Fact]
    public void Constructor_SetsDefaultValues()
    {
        var state = new BackupState(MakeJob());

        Assert.Equal(BackupStatus.Inactive, state.Status);
        Assert.Equal(0, state.TotalFiles);
        Assert.Equal(0, state.FilesRemaining);
        Assert.Equal(0, state.TotalBytes);
        Assert.Equal(0, state.BytesRemaining);
    }

    [Fact]
    public void Constructor_NullJob_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new BackupState(null!));
    }

    [Fact]
    public void ProgressPercentage_WhenTotalBytesIsZero_ReturnsZero()
    {
        var state = new BackupState(MakeJob()) { TotalBytes = 0, BytesRemaining = 0 };

        Assert.Equal(0.0, state.ProgressPercentage);
    }

    [Fact]
    public void ProgressPercentage_WhenHalfBytesTransferred_ReturnsFiftyPercent()
    {
        var state = new BackupState(MakeJob())
        {
            TotalBytes = 1000,
            BytesRemaining = 500
        };

        Assert.Equal(50.0, state.ProgressPercentage);
    }

    [Fact]
    public void ProgressPercentage_WhenAllBytesTransferred_ReturnsOneHundredPercent()
    {
        var state = new BackupState(MakeJob())
        {
            TotalBytes = 1000,
            BytesRemaining = 0
        };

        Assert.Equal(100.0, state.ProgressPercentage);
    }

    [Fact]
    public void UpdateProgress_WhenFilesAndBytesAreZero_SetsStatusToCompleted()
    {
        var state = new BackupState(MakeJob())
        {
            Status = BackupStatus.Active,
            FilesRemaining = 0,
            BytesRemaining = 0
        };

        state.UpdateProgress();

        Assert.Equal(BackupStatus.Completed, state.Status);
    }

    [Fact]
    public void UpdateProgress_WhenStatusInactiveAndFilesRemaining_SetsStatusToActive()
    {
        var state = new BackupState(MakeJob())
        {
            Status = BackupStatus.Inactive,
            FilesRemaining = 5,
            BytesRemaining = 500
        };

        state.UpdateProgress();

        Assert.Equal(BackupStatus.Active, state.Status);
    }
}
