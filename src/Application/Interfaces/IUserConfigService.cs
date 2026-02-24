using Core.Enums;
using Log.Enums;
using System.Collections.Generic;

namespace EasySave.Application.Interfaces;

public interface IUserConfigService
{
    string? LoadLanguage();
    bool SaveLanguage(string cultureCode);
    LogFormat? LoadLogFormat();
    bool SaveLogFormat(LogFormat format);
    string? LoadBusinessSoftware();
    bool SaveBusinessSoftware(string software);
    List<string>? LoadCryptoSoftExtensions();
    bool SaveCryptoSoftExtensions(List<string> extensions);

    /// <summary>
    /// Extensions declared as priority (e.g. [".txt", ".pdf"]).
    /// Jobs process these files before any non-priority file across all parallel jobs.
    /// Returns null or empty list when the priority rule is not configured.
    /// </summary>
    List<string>? LoadPriorityExtensions();
    bool SavePriorityExtensions(List<string> extensions);

    /// <summary>
    /// Maximum file size (in KB) that can be transferred in parallel.
    /// Files strictly larger than this value require the large-file semaphore
    /// and only one such transfer is allowed at a time.
    /// Returns 0 when the rule is disabled.
    /// </summary>
    long LoadMaxParallelFileSizeKb();
    bool SaveMaxParallelFileSizeKb(long sizeKb);

    public bool SaveStorageMode(LogStorageMode mode);
    public LogStorageMode? LoadStorageMode();

}
