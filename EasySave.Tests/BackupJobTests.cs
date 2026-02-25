using Core.Enums;
using Core.Models;

namespace EasySave.Tests;

/// <summary>
/// Unit tests for <see cref="BackupJob"/> constructor validation.
/// </summary>
public class BackupJobTests
{
    [Fact]
    public void Constructor_ValidArguments_CreatesJob()
    {
        var job = new BackupJob("MyBackup", "/src", "/dst", BackupType.Full);

        Assert.Equal("MyBackup", job.Name);
        Assert.Equal("/src", job.SourceDirectory);
        Assert.Equal("/dst", job.TargetDirectory);
        Assert.Equal(BackupType.Full, job.Type);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_EmptyOrWhitespaceName_ThrowsArgumentException(string name)
    {
        Assert.Throws<ArgumentException>(() =>
            new BackupJob(name, "/src", "/dst", BackupType.Full));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_EmptyOrWhitespaceSource_ThrowsArgumentException(string source)
    {
        Assert.Throws<ArgumentException>(() =>
            new BackupJob("Job", source, "/dst", BackupType.Full));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_EmptyOrWhitespaceTarget_ThrowsArgumentException(string target)
    {
        Assert.Throws<ArgumentException>(() =>
            new BackupJob("Job", "/src", target, BackupType.Full));
    }

    [Fact]
    public void Constructor_DifferentialType_SetsTypeCorrectly()
    {
        var job = new BackupJob("Job", "/src", "/dst", BackupType.Differencial);

        Assert.Equal(BackupType.Differencial, job.Type);
    }
}
