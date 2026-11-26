using System;
using System.IO;

namespace GTFO_GUIEditor.Services;

public static class DatablockPathResolver
{
    public static string GetCustomDirectory(string rootPath)
    {
        if (string.IsNullOrWhiteSpace(rootPath))
        {
            throw new ArgumentException("Path cannot be empty.", nameof(rootPath));
        }

        string fullRootPath = Path.GetFullPath(rootPath);
        DirectoryInfo? directory = File.Exists(fullRootPath)
            ? new FileInfo(fullRootPath).Directory
            : new DirectoryInfo(fullRootPath);

        while (directory != null)
        {
            if (directory.Name.Equals("plugins", StringComparison.OrdinalIgnoreCase))
            {
                return Path.Combine(directory.FullName, "Custom");
            }

            directory = directory.Parent;
        }

        return Path.Combine(fullRootPath, "Custom");
    }
}
