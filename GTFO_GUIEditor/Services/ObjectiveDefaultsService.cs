using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using GTFO_GUIEditor.Models;

namespace GTFO_GUIEditor.Services;

public static class ObjectiveDefaultsService
{
    private static readonly object _lock = new();
    private static Dictionary<ObjectiveType, ObjectiveTextDefaults>? _cache;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
        WriteIndented = true,
        Converters =
        {
            new JsonStringEnumConverter()
        }
    };

    public static string GetDataDirectory()
    {
        string outputDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data");
        if (Directory.Exists(outputDir))
        {
            return outputDir;
        }

        string cwdDir = Path.Combine(Directory.GetCurrentDirectory(), "Data");
        if (Directory.Exists(cwdDir))
        {
            return cwdDir;
        }

        string cwdProjectDir = Path.Combine(Directory.GetCurrentDirectory(), "GTFO_GUIEditor", "Data");
        if (Directory.Exists(cwdProjectDir))
        {
            return cwdProjectDir;
        }

        var dir = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);
        while (dir != null)
        {
            string candidateProject = Path.Combine(dir.FullName, "GTFO_GUIEditor", "Data");
            if (Directory.Exists(candidateProject))
            {
                return candidateProject;
            }

            string candidateVanilla = Path.Combine(dir.FullName, "Data");
            if (Directory.Exists(candidateVanilla))
            {
                return candidateVanilla;
            }

            dir = dir.Parent;
        }

        dir = new DirectoryInfo(Directory.GetCurrentDirectory());
        while (dir != null)
        {
            string candidateProject = Path.Combine(dir.FullName, "GTFO_GUIEditor", "Data");
            if (Directory.Exists(candidateProject))
            {
                return candidateProject;
            }

            string candidateVanilla = Path.Combine(dir.FullName, "Data");
            if (Directory.Exists(candidateVanilla))
            {
                return candidateVanilla;
            }

            dir = dir.Parent;
        }

        return outputDir;
    }

    public static string GetAssetsDirectory() => GetDataDirectory();

    public static string? FindDefaultsFile()
    {
        string dir = GetDataDirectory();
        string directPath = Path.Combine(dir, "ObjectiveDefaults.json");
        if (File.Exists(directPath)) return directPath;

        string cwdVanilla = Path.Combine(Directory.GetCurrentDirectory(), "Data", "ObjectiveDefaults.json");
        if (File.Exists(cwdVanilla)) return cwdVanilla;

        string cwdProjectData = Path.Combine(Directory.GetCurrentDirectory(), "GTFO_GUIEditor", "Data", "ObjectiveDefaults.json");
        if (File.Exists(cwdProjectData)) return cwdProjectData;

        return null;
    }

    public static void Initialize(string? filePath = null)
    {
        lock (_lock)
        {
            _cache = new Dictionary<ObjectiveType, ObjectiveTextDefaults>();

            foreach (var type in Enum.GetValues<ObjectiveType>())
            {
                _cache[type] = new ObjectiveTextDefaults();
            }

            filePath ??= FindDefaultsFile();
            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath)) return;

            try
            {
                string json = File.ReadAllText(filePath);
                var rawDict = JsonSerializer.Deserialize<Dictionary<string, ObjectiveTextDefaults>>(json, JsonOptions);
                if (rawDict != null)
                {
                    foreach (var (key, value) in rawDict)
                    {
                        if (Enum.TryParse<ObjectiveType>(key, true, out var objType))
                        {
                            _cache[objType] = value ?? new ObjectiveTextDefaults();
                        }
                    }
                }
            }
            catch
            {
                // Graceful fallback if JSON is invalid or missing
            }
        }
    }

    public static ObjectiveTextDefaults GetDefaults(ObjectiveType type)
    {
        lock (_lock)
        {
            if (_cache == null)
            {
                Initialize();
            }

            if (_cache != null && _cache.TryGetValue(type, out var found))
            {
                return found.Clone();
            }

            return new ObjectiveTextDefaults();
        }
    }

    public static void SetDefaults(ObjectiveType type, ObjectiveTextDefaults defaults)
    {
        lock (_lock)
        {
            if (_cache == null)
            {
                Initialize();
            }

            _cache![type] = defaults.Clone();
        }
    }

    public static void ResetCache()
    {
        lock (_lock)
        {
            _cache = null;
        }
    }
}
