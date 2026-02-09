using System;
using System.IO;
using System.Management; // You may need to add this reference

public static class PathHelper
{
    public static string ToUncPath(string path)
    {
        if (string.IsNullOrWhiteSpace(path)) return path;

        // 1. If it's already a UNC path, return it.
        if (path.StartsWith(@"\\")) return path;

        // 2. Get the drive letter (e.g., "Z:")
        string root = Path.GetPathRoot(path);
        string driveLetter = root.TrimEnd('\\');

        // 3. Query the system to see if this is a network drive
        using (ManagementObject mgmt = new ManagementObject($"Win32_LogicalDisk.DeviceID='{driveLetter}'"))
        {
            mgmt.Get();
            uint driveType = (uint)mgmt["DriveType"];

            if (driveType == 4) // 4 = Network Drive
            {
                string remotePath = mgmt["ProviderName"].ToString(); // e.g. \\Server\Share
                return path.Replace(root, remotePath + @"\");
            }
        }

        // 4. If it's a local drive (Type 3), use the MachineName convention
        string machineName = Environment.MachineName;
        string adminShare = root.Replace(":", "$").TrimEnd('\\');
        return $@"\\{machineName}\{adminShare}\{path.Substring(root.Length)}";
    }
}