using Core.Enums;
using Log.Enums;

namespace EasySave.Application.Interfaces;

/// <summary>
/// Repository interface for persisting and retrieving user application settings and configuration.
/// </summary>
public interface IUserConfigRepository
{
    /// <summary>
    /// Loads the saved language culture code.
    /// </summary>
    /// <returns> The culture code string (e.g., "en-US", "fr-FR") if configured; 
    /// otherwise, <c>null</c>.
    /// </returns>
    string? LoadLanguage();

    /// <summary>
    /// Saves the selected language culture code.
    /// </summary>
    /// <param name="cultureCode">The culture code to save.</param>
    /// <returns><c>true</c> if the setting was saved successfully; 
    /// otherwise, <c>false</c>.</returns>
    bool SaveLanguage(string cultureCode);

    /// <summary>
    /// Loads the saved log format preference.
    /// </summary>
    /// <returns>The configured <see cref="LogFormat"/> if found; otherwise, <c>null</c>.</returns>
    LogFormat? LoadLogFormat();

    /// <summary>
    /// Saves the selected log format preference.
    /// </summary>
    /// <param name="format">The log format to save (e.g., JSON or XML).</param>
    /// <returns><c>true</c> if the setting was saved successfully; otherwise, <c>false</c>.</returns>
    bool SaveLogFormat(LogFormat format);

    /// <summary>
    /// Loads the name or process identifier of the business software that should pause backups when running.
    /// </summary>
    /// <returns>The name of the business software if configured; otherwise, <c>null</c>.</returns>
    string? LoadBusinessSoftware();

    /// <summary>
    /// Saves the name or process identifier of the business software to monitor.
    /// </summary>
    /// <param name="software">The name of the software executable or process.</param>
    /// <returns><c>true</c> if the setting was saved successfully; otherwise, <c>false</c>.</returns>
    bool SaveBusinessSoftware(string software);

    /// <summary>
    /// Loads the list of file extensions that require encryption via CryptoSoft.
    /// </summary>
    /// <returns>A list of file extensions (e.g., ".txt") if configured; otherwise, <c>null</c>.</returns>
    List<string>? LoadCryptoSoftExtensions();

    /// <summary>
    /// Saves the list of file extensions that should be encrypted.
    /// </summary>
    /// <param name="extensions">The list of file extensions to encrypt.</param>
    /// <returns><c>true</c> if the settings were saved successfully; otherwise, <c>false</c>.</returns>
    bool SaveCryptoSoftExtensions(List<string> extensions);

    /// <summary>
    /// Extensions declared as priority (e.g. [".txt", ".pdf"]).
    /// Jobs process these files before any non-priority file across all parallel jobs.
    /// </summary>
    /// <returns>A list of priority extensions, or <c>null</c> (or empty) when the priority rule is not configured.</returns>
    List<string>? LoadPriorityExtensions();

    /// <summary>
    /// Saves the list of priority file extensions.
    /// </summary>
    /// <param name="extensions">The list of file extensions to prioritize.</param>
    /// <returns><c>true</c> if the settings were saved successfully; otherwise, <c>false</c>.</returns>
    bool SavePriorityExtensions(List<string> extensions);

    /// <summary>
    /// Maximum file size (in KB) that can be transferred in parallel.
    /// Files strictly larger than this value require the large-file semaphore
    /// and only one such transfer is allowed at a time.
    /// Returns 0 when the rule is disabled.
    /// </summary>
    long LoadMaxParallelFileSizeKb();

    /// <summary>
    /// Saves the maximum file size threshold for parallel file transfers.
    /// </summary>
    /// <param name="sizeKb">The file size threshold in KB.</param>
    /// <returns><c>true</c> if the setting was saved successfully; otherwise, <c>false</c>.</returns>
    bool SaveMaxParallelFileSizeKb(long sizeKb);

    /// <summary>
    /// Saves the storage mode preference for logs (e.g., local file, database).
    /// </summary>
    /// <param name="mode">The storage mode to apply.</param>
    /// <returns><c>true</c> if the setting was saved successfully; otherwise, <c>false</c>.</returns>
    public bool SaveStorageMode(LogStorageMode mode);

    /// <summary>
    /// Loads the current storage mode preference for logs.
    /// </summary>
    /// <returns>The configured <see cref="LogStorageMode"/> if found; otherwise, <c>null</c>.</returns>
    public LogStorageMode? LoadStorageMode();

}
