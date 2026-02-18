using Log.Enums;
using System.Collections.Generic;

namespace Core.Interfaces;

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
}
