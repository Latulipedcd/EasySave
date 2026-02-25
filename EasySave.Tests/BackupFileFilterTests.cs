using Core.Enums;
using Core.Models;
using Core.Services;

namespace EasySave.Tests;

/// <summary>
/// Unit tests for <see cref="BackupFileFilter"/>.
/// </summary>
public class BackupFileFilterTests
{
    private readonly BackupFileFilter _filter = new BackupFileFilter();

    // ── ShouldProcess ────────────────────────────────────────────────────────

    [Fact]
    public void ShouldProcess_FullBackup_AlwaysReturnsTrue()
    {
        var job = new BackupJob("Job", "/src", "/dst", BackupType.Full);

        // Target doesn't exist on disk; for a Full backup this still returns true.
        bool result = _filter.ShouldProcess(job, "/src/file.txt", "/dst/nonexistent.txt");

        Assert.True(result);
    }

    [Fact]
    public void ShouldProcess_DifferentialBackup_TargetMissing_ReturnsTrue()
    {
        var job = new BackupJob("Job", "/src", "/dst", BackupType.Differencial);
        string targetPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString(), "missing.txt");

        bool result = _filter.ShouldProcess(job, "/src/file.txt", targetPath);

        Assert.True(result);
    }

    [Fact]
    public void ShouldProcess_DifferentialBackup_TargetNewer_ReturnsFalse()
    {
        var job = new BackupJob("Job", "/src", "/dst", BackupType.Differencial);

        string tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);

        string sourceFile = Path.Combine(tempDir, "source.txt");
        string targetFile = Path.Combine(tempDir, "target.txt");

        File.WriteAllText(sourceFile, "source");
        File.WriteAllText(targetFile, "target");

        // Make target newer than source.
        File.SetLastWriteTime(sourceFile, DateTime.Now.AddHours(-1));
        File.SetLastWriteTime(targetFile, DateTime.Now);

        bool result = _filter.ShouldProcess(job, sourceFile, targetFile);

        // Cleanup.
        Directory.Delete(tempDir, true);

        Assert.False(result);
    }

    [Fact]
    public void ShouldProcess_DifferentialBackup_SourceNewer_ReturnsTrue()
    {
        var job = new BackupJob("Job", "/src", "/dst", BackupType.Differencial);

        string tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);

        string sourceFile = Path.Combine(tempDir, "source.txt");
        string targetFile = Path.Combine(tempDir, "target.txt");

        File.WriteAllText(sourceFile, "source");
        File.WriteAllText(targetFile, "target");

        // Make source newer than target.
        File.SetLastWriteTime(targetFile, DateTime.Now.AddHours(-1));
        File.SetLastWriteTime(sourceFile, DateTime.Now);

        bool result = _filter.ShouldProcess(job, sourceFile, targetFile);

        // Cleanup.
        Directory.Delete(tempDir, true);

        Assert.True(result);
    }

    // ── IsPriorityFile ────────────────────────────────────────────────────────

    [Fact]
    public void IsPriorityFile_NullExtensions_ReturnsFalse()
    {
        bool result = _filter.IsPriorityFile("file.txt", null!);

        Assert.False(result);
    }

    [Fact]
    public void IsPriorityFile_EmptyExtensions_ReturnsFalse()
    {
        bool result = _filter.IsPriorityFile("file.txt", new List<string>());

        Assert.False(result);
    }

    [Fact]
    public void IsPriorityFile_MatchingExtension_ReturnsTrue()
    {
        var extensions = new List<string> { ".txt", ".pdf" };

        bool result = _filter.IsPriorityFile("document.txt", extensions);

        Assert.True(result);
    }

    [Fact]
    public void IsPriorityFile_NonMatchingExtension_ReturnsFalse()
    {
        var extensions = new List<string> { ".pdf", ".docx" };

        bool result = _filter.IsPriorityFile("image.png", extensions);

        Assert.False(result);
    }

    [Fact]
    public void IsPriorityFile_ExtensionMatchIsCaseInsensitive()
    {
        var extensions = new List<string> { ".txt" };

        bool result = _filter.IsPriorityFile("DOCUMENT.TXT", extensions);

        Assert.True(result);
    }
}
