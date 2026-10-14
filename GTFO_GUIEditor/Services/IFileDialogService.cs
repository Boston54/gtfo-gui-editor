using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;

namespace GTFO_GUIEditor.Services;

public interface IFileDialogService
{
    string DefaultRundownsDirectory { get; }
    Task<string?> ShowOpenFileDialogAsync(string? defaultDirectory = null, string? filter = null);
    Task<string?> ShowSaveFileDialogAsync(string? defaultFileName = null, string? defaultDirectory = null, string? defaultExtension = "rundown");
}

public class AvaloniaFileDialogService : IFileDialogService
{
    public string DefaultRundownsDirectory
    {
        get
        {
            string docFolder = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            if (string.IsNullOrWhiteSpace(docFolder))
            {
                docFolder = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            }
            string rundownsFolder = Path.Combine(docFolder, "GTFO_GUIEditor", "Rundowns");
            try
            {
                if (!Directory.Exists(rundownsFolder))
                {
                    Directory.CreateDirectory(rundownsFolder);
                }
            }
            catch
            {
                rundownsFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Rundowns");
                if (!Directory.Exists(rundownsFolder))
                {
                    Directory.CreateDirectory(rundownsFolder);
                }
            }
            return rundownsFolder;
        }
    }

    public async Task<string?> ShowOpenFileDialogAsync(string? defaultDirectory = null, string? filter = null)
    {
        var storage = GetStorageProvider();
        if (storage == null)
        {
            return null;
        }

        string initialDir = !string.IsNullOrWhiteSpace(defaultDirectory) && Directory.Exists(defaultDirectory)
            ? defaultDirectory
            : DefaultRundownsDirectory;

        IStorageFolder? startFolder = null;
        try
        {
            startFolder = await storage.TryGetFolderFromPathAsync(initialDir);
        }
        catch
        {
            // Ignore error retrieving folder
        }

        var filterList = new List<FilePickerFileType>
        {
            new("GTFO Rundown Files (*.rundown)")
            {
                Patterns = new[] { "*.rundown" }
            },
            new("All Files (*.*)")
            {
                Patterns = new[] { "*.*" }
            }
        };

        var options = new FilePickerOpenOptions
        {
            Title = "Open Rundown File",
            AllowMultiple = false,
            FileTypeFilter = filterList,
            SuggestedStartLocation = startFolder
        };

        var result = await storage.OpenFilePickerAsync(options);
        if (result != null && result.Count > 0)
        {
            return result[0].Path.LocalPath;
        }

        return null;
    }

    public async Task<string?> ShowSaveFileDialogAsync(string? defaultFileName = null, string? defaultDirectory = null, string? defaultExtension = "rundown")
    {
        var storage = GetStorageProvider();
        if (storage == null)
        {
            return null;
        }

        string initialDir = !string.IsNullOrWhiteSpace(defaultDirectory) && Directory.Exists(defaultDirectory)
            ? defaultDirectory
            : DefaultRundownsDirectory;

        IStorageFolder? startFolder = null;
        try
        {
            startFolder = await storage.TryGetFolderFromPathAsync(initialDir);
        }
        catch
        {
            // Ignore error retrieving folder
        }

        string suggestedName = defaultFileName ?? "NewRundown.rundown";
        if (!suggestedName.EndsWith(".rundown", StringComparison.OrdinalIgnoreCase))
        {
            suggestedName += ".rundown";
        }

        var filterList = new List<FilePickerFileType>
        {
            new("GTFO Rundown Files (*.rundown)")
            {
                Patterns = new[] { "*.rundown" }
            },
            new("All Files (*.*)")
            {
                Patterns = new[] { "*.*" }
            }
        };

        var options = new FilePickerSaveOptions
        {
            Title = "Save Rundown File",
            DefaultExtension = defaultExtension ?? "rundown",
            SuggestedFileName = suggestedName,
            FileTypeChoices = filterList,
            SuggestedStartLocation = startFolder
        };

        var result = await storage.SaveFilePickerAsync(options);
        if (result != null)
        {
            return result.Path.LocalPath;
        }

        return null;
    }

    private static IStorageProvider? GetStorageProvider()
    {
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop &&
            desktop.MainWindow != null)
        {
            return TopLevel.GetTopLevel(desktop.MainWindow)?.StorageProvider;
        }
        return null;
    }
}
