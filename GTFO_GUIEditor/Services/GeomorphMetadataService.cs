using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using GTFO_GUIEditor.Models;

namespace GTFO_GUIEditor.Services;

public static class GeomorphMetadataService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
        Converters = { new JsonStringEnumConverter() }
    };

    private static readonly ConcurrentDictionary<string, GeomorphMetadata?> Cache = new(StringComparer.OrdinalIgnoreCase);
    private static bool _initialized;
    private static readonly object LockObj = new();

    public static string GetMetadataDirectory()
    {
        string outputDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "vanilla", "geomorphs", "metadata");
        if (Directory.Exists(outputDir)) return outputDir;

        string cwdDir = Path.Combine(Directory.GetCurrentDirectory(), "vanilla", "geomorphs", "metadata");
        if (Directory.Exists(cwdDir)) return cwdDir;

        string cwdProjectDir = Path.Combine(Directory.GetCurrentDirectory(), "GTFO_GUIEditor", "vanilla", "geomorphs", "metadata");
        if (Directory.Exists(cwdProjectDir)) return cwdProjectDir;

        var dir = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);
        while (dir != null)
        {
            string candidateProject = Path.Combine(dir.FullName, "GTFO_GUIEditor", "vanilla", "geomorphs", "metadata");
            if (Directory.Exists(candidateProject)) return candidateProject;

            string candidateVanilla = Path.Combine(dir.FullName, "vanilla", "geomorphs", "metadata");
            if (Directory.Exists(candidateVanilla)) return candidateVanilla;

            dir = dir.Parent;
        }

        return outputDir;
    }

    public static void Initialize()
    {
        if (_initialized) return;

        lock (LockObj)
        {
            if (_initialized) return;

            string dir = GetMetadataDirectory();
            if (Directory.Exists(dir))
            {
                var files = Directory.GetFiles(dir, "*.prefab.json", SearchOption.AllDirectories);
                foreach (var file in files)
                {
                    try
                    {
                        string json = File.ReadAllText(file);
                        var metadata = JsonSerializer.Deserialize<GeomorphMetadata>(json, JsonOptions);
                        if (metadata != null)
                        {
                            if (!string.IsNullOrWhiteSpace(metadata.GeoAssetLink))
                            {
                                string normLink = NormalizePath(metadata.GeoAssetLink);
                                Cache[normLink] = metadata;

                                string fileWithoutExt = Path.GetFileNameWithoutExtension(metadata.GeoAssetLink);
                                Cache[fileWithoutExt] = metadata;
                            }

                            string jsonFileName = Path.GetFileNameWithoutExtension(file); // Removes .json
                            Cache[jsonFileName] = metadata;
                        }
                    }
                    catch
                    {
                        // Ignore malformed files
                    }
                }
            }

            _initialized = true;
        }
    }

    public static GeomorphMetadata? GetMetadata(string? prefabPath)
    {
        if (string.IsNullOrWhiteSpace(prefabPath)) return null;

        if (!_initialized)
        {
            Initialize();
        }

        string normPath = NormalizePath(prefabPath);
        if (Cache.TryGetValue(normPath, out var metadata) && metadata != null)
        {
            return metadata;
        }

        string fileName = Path.GetFileNameWithoutExtension(prefabPath);
        if (Cache.TryGetValue(fileName, out metadata) && metadata != null)
        {
            return metadata;
        }

        // Direct file search fallback
        string dir = GetMetadataDirectory();
        if (Directory.Exists(dir))
        {
            string cleanName = fileName.Replace('/', '_').Replace('\\', '_');
            string candidate1 = Path.Combine(dir, $"{cleanName}.prefab.json");
            string candidate2 = Path.Combine(dir, $"{normPath.Replace('/', '_')}.json");

            foreach (var cand in new[] { candidate1, candidate2 })
            {
                if (File.Exists(cand))
                {
                    try
                    {
                        string json = File.ReadAllText(cand);
                        var loaded = JsonSerializer.Deserialize<GeomorphMetadata>(json, JsonOptions);
                        if (loaded != null)
                        {
                            Cache[normPath] = loaded;
                            return loaded;
                        }
                    }
                    catch { }
                }
            }
        }

        return null;
    }

    private static string NormalizePath(string path)
    {
        return path.Replace('\\', '/').Trim();
    }
}
