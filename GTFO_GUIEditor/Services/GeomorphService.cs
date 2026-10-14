using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Avalonia.Media.Imaging;
using GTFO_GUIEditor.Models;
using GTFO_GUIEditor.ViewModels;

namespace GTFO_GUIEditor.Services;

public static class GeomorphService
{
    private static readonly ConcurrentDictionary<string, string?> ImagePathCache = new(StringComparer.OrdinalIgnoreCase);
    private static readonly ConcurrentDictionary<string, Bitmap?> BitmapCache = new(StringComparer.OrdinalIgnoreCase);
    public static string GetGeomorphsDirectory()
    {
        string[] candidates =
        {
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "Vanilla", "Geomorphs"),
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "Vanilla", "Datablocks", "Geomorphs"),
            Path.Combine(Directory.GetCurrentDirectory(), "Data", "Vanilla", "Geomorphs"),
            Path.Combine(Directory.GetCurrentDirectory(), "Data", "Vanilla", "Datablocks", "Geomorphs"),
            Path.Combine(Directory.GetCurrentDirectory(), "GTFO_GUIEditor", "Data", "Vanilla", "Geomorphs"),
            Path.Combine(Directory.GetCurrentDirectory(), "GTFO_GUIEditor", "Data", "Vanilla", "Datablocks", "Geomorphs")
        };

        foreach (var candidate in candidates)
        {
            if (Directory.Exists(candidate) && (File.Exists(Path.Combine(candidate, "layout.json")) || Directory.Exists(Path.Combine(candidate, "images"))))
            {
                return candidate;
            }
        }

        foreach (var candidate in candidates)
        {
            if (Directory.Exists(candidate))
            {
                return candidate;
            }
        }

        var dir = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);
        while (dir != null)
        {
            string candidateProject = Path.Combine(dir.FullName, "GTFO_GUIEditor", "Data", "Vanilla", "Geomorphs");
            if (Directory.Exists(candidateProject)) return candidateProject;

            string candidateProjectDb = Path.Combine(dir.FullName, "GTFO_GUIEditor", "Data", "Vanilla", "Datablocks", "Geomorphs");
            if (Directory.Exists(candidateProjectDb)) return candidateProjectDb;

            string candidateVanilla = Path.Combine(dir.FullName, "Data", "Vanilla", "Geomorphs");
            if (Directory.Exists(candidateVanilla)) return candidateVanilla;

            string candidateVanillaDb = Path.Combine(dir.FullName, "Data", "Vanilla", "Datablocks", "Geomorphs");
            if (Directory.Exists(candidateVanillaDb)) return candidateVanillaDb;

            dir = dir.Parent;
        }

        return candidates[0];
    }

    public static string? FindLayoutFile()
    {
        string geoDir = GetGeomorphsDirectory();
        string direct = Path.Combine(geoDir, "layout.json");
        if (File.Exists(direct)) return direct;

        string[] candidates =
        {
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "Vanilla", "Geomorphs", "layout.json"),
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "Vanilla", "Datablocks", "Geomorphs", "layout.json"),
            Path.Combine(Directory.GetCurrentDirectory(), "Data", "Vanilla", "Geomorphs", "layout.json"),
            Path.Combine(Directory.GetCurrentDirectory(), "Data", "Vanilla", "Datablocks", "Geomorphs", "layout.json"),
            Path.Combine(Directory.GetCurrentDirectory(), "GTFO_GUIEditor", "Data", "Vanilla", "Geomorphs", "layout.json"),
            Path.Combine(Directory.GetCurrentDirectory(), "GTFO_GUIEditor", "Data", "Vanilla", "Datablocks", "Geomorphs", "layout.json")
        };

        return candidates.FirstOrDefault(File.Exists);
    }

    public static string? FindMappingsFile()
    {
        string geoDir = GetGeomorphsDirectory();
        string direct = Path.Combine(geoDir, "mappings.json");
        if (File.Exists(direct)) return direct;

        string[] candidates =
        {
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "Vanilla", "Geomorphs", "mappings.json"),
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "Vanilla", "Datablocks", "Geomorphs", "mappings.json"),
            Path.Combine(Directory.GetCurrentDirectory(), "Data", "Vanilla", "Geomorphs", "mappings.json"),
            Path.Combine(Directory.GetCurrentDirectory(), "Data", "Vanilla", "Datablocks", "Geomorphs", "mappings.json"),
            Path.Combine(Directory.GetCurrentDirectory(), "GTFO_GUIEditor", "Data", "Vanilla", "Geomorphs", "mappings.json"),
            Path.Combine(Directory.GetCurrentDirectory(), "GTFO_GUIEditor", "Data", "Vanilla", "Datablocks", "Geomorphs", "mappings.json")
        };

        return candidates.FirstOrDefault(File.Exists);
    }

    public static string? FindTilesetsFile()
    {
        string geoDir = GetGeomorphsDirectory();
        string direct = Path.Combine(geoDir, "tilesets.json");
        if (File.Exists(direct)) return direct;

        string[] candidates =
        {
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "Vanilla", "Geomorphs", "tilesets.json"),
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "Vanilla", "Datablocks", "Geomorphs", "tilesets.json"),
            Path.Combine(Directory.GetCurrentDirectory(), "Data", "Vanilla", "Geomorphs", "tilesets.json"),
            Path.Combine(Directory.GetCurrentDirectory(), "Data", "Vanilla", "Datablocks", "Geomorphs", "tilesets.json"),
            Path.Combine(Directory.GetCurrentDirectory(), "GTFO_GUIEditor", "Data", "Vanilla", "Geomorphs", "tilesets.json"),
            Path.Combine(Directory.GetCurrentDirectory(), "GTFO_GUIEditor", "Data", "Vanilla", "Datablocks", "Geomorphs", "tilesets.json")
        };

        return candidates.FirstOrDefault(File.Exists);
    }

    public static string? FindImageMappingsFile()
    {
        string geoDir = GetGeomorphsDirectory();
        string direct = Path.Combine(geoDir, "imagemappings.json");
        if (File.Exists(direct)) return direct;

        string[] candidates =
        {
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "Vanilla", "Geomorphs", "imagemappings.json"),
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "Vanilla", "Datablocks", "Geomorphs", "imagemappings.json"),
            Path.Combine(Directory.GetCurrentDirectory(), "Data", "Vanilla", "Geomorphs", "imagemappings.json"),
            Path.Combine(Directory.GetCurrentDirectory(), "Data", "Vanilla", "Datablocks", "Geomorphs", "imagemappings.json"),
            Path.Combine(Directory.GetCurrentDirectory(), "GTFO_GUIEditor", "Data", "Vanilla", "Geomorphs", "imagemappings.json"),
            Path.Combine(Directory.GetCurrentDirectory(), "GTFO_GUIEditor", "Data", "Vanilla", "Datablocks", "Geomorphs", "imagemappings.json")
        };

        return candidates.FirstOrDefault(File.Exists);
    }

    public static Dictionary<string, string> LoadMappings(string? filePath = null)
    {
        filePath ??= FindMappingsFile();
        if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
        {
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }

        try
        {
            string json = File.ReadAllText(filePath);
            var map = JsonSerializer.Deserialize<Dictionary<string, string>>(json);
            return map != null
                ? new Dictionary<string, string>(map, StringComparer.OrdinalIgnoreCase)
                : new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }
        catch
        {
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }
    }

    public static Dictionary<string, List<string>> LoadTilesets(string? filePath = null)
    {
        filePath ??= FindTilesetsFile();
        if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
        {
            return new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        }

        try
        {
            string json = File.ReadAllText(filePath);
            var map = JsonSerializer.Deserialize<Dictionary<string, List<string>>>(json);
            return map != null
                ? new Dictionary<string, List<string>>(map, StringComparer.OrdinalIgnoreCase)
                : new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        }
        catch
        {
            return new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        }
    }

    public static Dictionary<string, string> LoadImageMappings(string? filePath = null)
    {
        filePath ??= FindImageMappingsFile();
        if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
        {
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }

        try
        {
            string json = File.ReadAllText(filePath);
            var map = JsonSerializer.Deserialize<Dictionary<string, string>>(json);
            return map != null
                ? new Dictionary<string, string>(map, StringComparer.OrdinalIgnoreCase)
                : new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }
        catch
        {
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }
    }

    public static string? FindImageFile(string imageName)
    {
        if (string.IsNullOrWhiteSpace(imageName)) return null;

        if (ImagePathCache.TryGetValue(imageName, out var cachedPath))
        {
            return cachedPath;
        }

        string geoDir = GetGeomorphsDirectory();
        if (!string.IsNullOrEmpty(geoDir) && Directory.Exists(geoDir))
        {
            string inImages = Path.Combine(geoDir, "images", imageName);
            if (File.Exists(inImages))
            {
                ImagePathCache[imageName] = inImages;
                return inImages;
            }

            string inGeoDir = Path.Combine(geoDir, imageName);
            if (File.Exists(inGeoDir))
            {
                ImagePathCache[imageName] = inGeoDir;
                return inGeoDir;
            }
        }

        var searchDirs = new List<string>();

        void AddDir(string? d)
        {
            if (!string.IsNullOrEmpty(d) && Directory.Exists(d) && !searchDirs.Contains(d))
            {
                searchDirs.Add(d);
            }
        }

        string baseDir = AppDomain.CurrentDomain.BaseDirectory;
        string curDir = Directory.GetCurrentDirectory();

        AddDir(Path.Combine(baseDir, "Data", "Vanilla", "Geomorphs", "images"));
        AddDir(Path.Combine(baseDir, "Data", "Vanilla", "Geomorphs"));
        AddDir(Path.Combine(baseDir, "Data", "Vanilla", "Datablocks", "Geomorphs", "images"));
        AddDir(Path.Combine(baseDir, "Data", "Vanilla", "Datablocks", "Geomorphs"));

        AddDir(Path.Combine(curDir, "Data", "Vanilla", "Geomorphs", "images"));
        AddDir(Path.Combine(curDir, "Data", "Vanilla", "Geomorphs"));
        AddDir(Path.Combine(curDir, "Data", "Vanilla", "Datablocks", "Geomorphs", "images"));
        AddDir(Path.Combine(curDir, "Data", "Vanilla", "Datablocks", "Geomorphs"));

        AddDir(Path.Combine(curDir, "GTFO_GUIEditor", "Data", "Vanilla", "Geomorphs", "images"));
        AddDir(Path.Combine(curDir, "GTFO_GUIEditor", "Data", "Vanilla", "Geomorphs"));
        AddDir(Path.Combine(curDir, "GTFO_GUIEditor", "Data", "Vanilla", "Datablocks", "Geomorphs", "images"));
        AddDir(Path.Combine(curDir, "GTFO_GUIEditor", "Data", "Vanilla", "Datablocks", "Geomorphs"));

        var dir = new DirectoryInfo(baseDir);
        while (dir != null)
        {
            AddDir(Path.Combine(dir.FullName, "GTFO_GUIEditor", "Data", "Vanilla", "Geomorphs", "images"));
            AddDir(Path.Combine(dir.FullName, "GTFO_GUIEditor", "Data", "Vanilla", "Geomorphs"));
            AddDir(Path.Combine(dir.FullName, "GTFO_GUIEditor", "Data", "Vanilla", "Datablocks", "Geomorphs", "images"));
            AddDir(Path.Combine(dir.FullName, "GTFO_GUIEditor", "Data", "Vanilla", "Datablocks", "Geomorphs"));

            AddDir(Path.Combine(dir.FullName, "Data", "Vanilla", "Geomorphs", "images"));
            AddDir(Path.Combine(dir.FullName, "Data", "Vanilla", "Geomorphs"));
            AddDir(Path.Combine(dir.FullName, "Data", "Vanilla", "Datablocks", "Geomorphs", "images"));
            AddDir(Path.Combine(dir.FullName, "Data", "Vanilla", "Datablocks", "Geomorphs"));

            dir = dir.Parent;
        }

        dir = new DirectoryInfo(curDir);
        while (dir != null)
        {
            AddDir(Path.Combine(dir.FullName, "GTFO_GUIEditor", "Data", "Vanilla", "Geomorphs", "images"));
            AddDir(Path.Combine(dir.FullName, "GTFO_GUIEditor", "Data", "Vanilla", "Geomorphs"));
            AddDir(Path.Combine(dir.FullName, "GTFO_GUIEditor", "Data", "Vanilla", "Datablocks", "Geomorphs", "images"));
            AddDir(Path.Combine(dir.FullName, "GTFO_GUIEditor", "Data", "Vanilla", "Datablocks", "Geomorphs"));

            AddDir(Path.Combine(dir.FullName, "Data", "Vanilla", "Geomorphs", "images"));
            AddDir(Path.Combine(dir.FullName, "Data", "Vanilla", "Geomorphs"));
            AddDir(Path.Combine(dir.FullName, "Data", "Vanilla", "Datablocks", "Geomorphs", "images"));
            AddDir(Path.Combine(dir.FullName, "Data", "Vanilla", "Datablocks", "Geomorphs"));

            dir = dir.Parent;
        }

        foreach (var d in searchDirs)
        {
            string candidate = Path.Combine(d, imageName);
            if (File.Exists(candidate))
            {
                ImagePathCache[imageName] = candidate;
                return candidate;
            }
        }

        ImagePathCache[imageName] = null;
        return null;
    }

    public static Bitmap? LoadImageBitmap(string? imageName)
    {
        if (string.IsNullOrWhiteSpace(imageName)) return null;

        if (BitmapCache.TryGetValue(imageName, out var cachedBitmap))
        {
            return cachedBitmap;
        }

        string? imagePath = FindImageFile(imageName);
        if (string.IsNullOrEmpty(imagePath) || !File.Exists(imagePath))
        {
            BitmapCache[imageName] = null;
            return null;
        }

        try
        {
            var bmp = new Bitmap(imagePath);
            BitmapCache[imageName] = bmp;
            return bmp;
        }
        catch
        {
            BitmapCache[imageName] = null;
            return null;
        }
    }

    public static string FormatDisplayName(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return string.Empty;

        var words = raw.Replace('_', ' ').Replace('-', ' ').Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return string.Join(" ", words.Select(w => char.ToUpperInvariant(w[0]) + (w.Length > 1 ? w[1..].ToLowerInvariant() : string.Empty)));
    }

    public static List<GeomorphEntry> LoadGeomorphs(string? layoutPath = null, string? mappingsPath = null)
    {
        layoutPath ??= FindLayoutFile();
        mappingsPath ??= FindMappingsFile();

        var mappings = LoadMappings(mappingsPath);
        var reverseMappings = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var kvp in mappings)
        {
            reverseMappings[kvp.Value] = kvp.Key;
        }

        if (string.IsNullOrEmpty(layoutPath) || !File.Exists(layoutPath))
        {
            return new List<GeomorphEntry>();
        }

        try
        {
            string json = File.ReadAllText(layoutPath);
            return ParseLayoutJson(json, mappings, reverseMappings);
        }
        catch
        {
            return new List<GeomorphEntry>();
        }
    }

    public static List<GeomorphEntry> ParseLayoutJson(string json, Dictionary<string, string>? mappings = null, Dictionary<string, string>? reverseMappings = null)
    {
        mappings ??= new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        reverseMappings ??= new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        var list = new List<GeomorphEntry>();
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        foreach (var complexProp in root.EnumerateObject())
        {
            string complexKey = complexProp.Name;
            string complexName = FormatDisplayName(complexKey);
            int complexId = 0;

            var complexObj = complexProp.Value;
            if (complexObj.ValueKind != JsonValueKind.Object) continue;

            if (complexObj.TryGetProperty("complexId", out var idElem) && idElem.TryGetInt32(out int parsedId))
            {
                complexId = parsedId;
            }

            foreach (var subComplexProp in complexObj.EnumerateObject())
            {
                if (subComplexProp.NameEquals("complexId")) continue;

                string subComplexKey = subComplexProp.Name;
                string subComplexName = FormatDisplayName(subComplexKey);
                var subComplexObj = subComplexProp.Value;
                if (subComplexObj.ValueKind != JsonValueKind.Object) continue;

                foreach (var categoryProp in subComplexObj.EnumerateObject())
                {
                    string categoryKey = categoryProp.Name;
                    string categoryName = FormatDisplayName(categoryKey);
                    var categoryArr = categoryProp.Value;
                    if (categoryArr.ValueKind != JsonValueKind.Array) continue;

                    foreach (var itemElem in categoryArr.EnumerateArray())
                    {
                        if (itemElem.ValueKind != JsonValueKind.Object) continue;

                        string itemName = itemElem.TryGetProperty("name", out var nameProp) ? (nameProp.GetString() ?? string.Empty) : string.Empty;
                        string itemDesc = itemElem.TryGetProperty("description", out var itemDescProp) ? (itemDescProp.GetString() ?? string.Empty) : string.Empty;

                        var variants = new List<JsonElement>();
                        if (itemElem.TryGetProperty("variants", out var variantsProp) && variantsProp.ValueKind == JsonValueKind.Array)
                        {
                            foreach (var v in variantsProp.EnumerateArray())
                            {
                                variants.Add(v);
                            }
                        }

                        if (variants.Count == 0)
                        {
                            // Single item without explicit variants array
                            var entry = CreateEntry(itemElem, itemName, itemDesc, complexName, complexId, subComplexName, subComplexKey, categoryName, 1, 1, mappings, reverseMappings);
                            list.Add(entry);
                        }
                        else
                        {
                            int varIdx = 1;
                            int totalVars = variants.Count;
                            foreach (var varElem in variants)
                            {
                                var entry = CreateEntry(varElem, itemName, itemDesc, complexName, complexId, subComplexName, subComplexKey, categoryName, varIdx, totalVars, mappings, reverseMappings);
                                list.Add(entry);
                                varIdx++;
                            }
                        }
                    }
                }
            }
        }

        return list;
    }

    private static GeomorphEntry CreateEntry(
        JsonElement elem,
        string baseName,
        string itemDesc,
        string complexName,
        int complexId,
        string subComplexName,
        string subComplexKey,
        string categoryName,
        int variantIndex,
        int variantCount,
        Dictionary<string, string> mappings,
        Dictionary<string, string> reverseMappings)
    {
        string variantDesc = elem.TryGetProperty("description", out var descProp) ? (descProp.GetString() ?? string.Empty) : string.Empty;
        string finalDesc = !string.IsNullOrWhiteSpace(variantDesc)
            ? variantDesc
            : itemDesc;

        string prefabPath = string.Empty;
        if (elem.TryGetProperty("prefabs", out var prefabsProp) && prefabsProp.ValueKind == JsonValueKind.Array)
        {
            var firstPrefab = prefabsProp.EnumerateArray().FirstOrDefault();
            if (firstPrefab.ValueKind == JsonValueKind.String)
            {
                prefabPath = firstPrefab.GetString() ?? string.Empty;
            }
        }
        else if (elem.TryGetProperty("prefab", out var singlePrefabProp) && singlePrefabProp.ValueKind == JsonValueKind.String)
        {
            prefabPath = singlePrefabProp.GetString() ?? string.Empty;
        }

        string prefabName = string.Empty;
        if (!string.IsNullOrEmpty(prefabPath))
        {
            if (reverseMappings.TryGetValue(prefabPath, out var mappedName))
            {
                prefabName = mappedName;
            }
            else
            {
                prefabName = Path.GetFileName(prefabPath);
            }

            if (mappings.TryGetValue(prefabName, out var fullMappedPath) && (string.IsNullOrEmpty(prefabPath) || !prefabPath.Contains('/')))
            {
                prefabPath = fullMappedPath;
            }
        }

        string imageName = string.Empty;
        if (elem.TryGetProperty("images", out var imagesProp) && imagesProp.ValueKind == JsonValueKind.Array)
        {
            var firstImg = imagesProp.EnumerateArray().FirstOrDefault();
            if (firstImg.ValueKind == JsonValueKind.String)
            {
                imageName = firstImg.GetString() ?? string.Empty;
            }
        }
        else if (elem.TryGetProperty("image", out var singleImgProp) && singleImgProp.ValueKind == JsonValueKind.String)
        {
            imageName = singleImgProp.GetString() ?? string.Empty;
        }

        if (string.IsNullOrEmpty(imageName))
        {
            var imageMappings = LoadImageMappings();
            if (imageMappings.TryGetValue(prefabPath, out var mappedImg))
            {
                imageName = mappedImg;
            }
            else if (!string.IsNullOrEmpty(prefabName) && imageMappings.TryGetValue(prefabName, out mappedImg))
            {
                imageName = mappedImg;
            }
        }

        string displayName = variantCount > 1
            ? $"{baseName} (Variant {variantIndex})"
            : baseName;

        return new GeomorphEntry
        {
            Name = baseName,
            DisplayName = displayName,
            PrefabName = prefabName,
            PrefabPath = prefabPath,
            Description = finalDesc,
            ImageName = imageName,
            Complex = complexName,
            ComplexId = complexId,
            SubComplex = subComplexName,
            SubComplexKey = subComplexKey,
            Category = categoryName,
            VariantIndex = variantIndex,
            VariantCount = variantCount
        };
    }

    public static string ExtractCategoryFromPath(string prefabPath)
    {
        if (string.IsNullOrWhiteSpace(prefabPath)) return "Geomorph";

        string norm = prefabPath.Replace('\\', '/');
        var parts = norm.Split('/', StringSplitOptions.RemoveEmptyEntries);

        int geoIdx = Array.FindIndex(parts, p => p.Equals("Geomorphs", StringComparison.OrdinalIgnoreCase));
        if (geoIdx >= 0 && geoIdx + 2 < parts.Length)
        {
            return FormatDisplayName(parts[geoIdx + 1]);
        }

        string fileName = Path.GetFileNameWithoutExtension(prefabPath).ToLowerInvariant();
        if (fileName.Contains("elevator")) return "Elevator Shaft";
        if (fileName.Contains("data_center") || fileName.Contains("datacenter")) return "Data Center";
        if (fileName.Contains("lab")) return "Lab";
        if (fileName.Contains("floodways")) return "Floodways";
        if (fileName.Contains("gardens")) return "Gardens";
        if (fileName.Contains("dig_site") || fileName.Contains("digsite")) return "Digsite";
        if (fileName.Contains("refinery")) return "Refinery";
        if (fileName.Contains("storage")) return "Storage";
        if (fileName.Contains("exit")) return "Exit";
        if (fileName.Contains("portal")) return "Portal";
        if (fileName.Contains("reactor")) return "Reactor";

        return "Geomorph";
    }

    public static List<GeomorphEntry> GetGeomorphsForTileset(ComplexType complexType, string? tilesetsPath = null, string? imageMappingsPath = null)
    {
        return GetGeomorphsForTileset(complexType.ToString(), tilesetsPath, imageMappingsPath);
    }

    public static List<GeomorphEntry> GetGeomorphsForTileset(string tilesetName, string? tilesetsPath = null, string? imageMappingsPath = null)
    {
        var tilesets = LoadTilesets(tilesetsPath);
        var imageMappings = LoadImageMappings(imageMappingsPath);

        string key = tilesetName.ToLowerInvariant();
        if (!tilesets.TryGetValue(key, out var prefabList) || prefabList == null)
        {
            var matched = tilesets.FirstOrDefault(kvp => kvp.Key.Equals(tilesetName, StringComparison.OrdinalIgnoreCase));
            prefabList = matched.Value ?? new List<string>();
        }

        var list = new List<GeomorphEntry>();
        var added = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        var layoutEntries = LoadGeomorphs();
        var layoutMap = layoutEntries
            .Where(e => !string.IsNullOrEmpty(e.PrefabPath))
            .GroupBy(e => e.PrefabPath, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

        foreach (var prefabPath in prefabList)
        {
            if (string.IsNullOrWhiteSpace(prefabPath) || !added.Add(prefabPath))
            {
                continue;
            }

            string imageName = string.Empty;
            if (imageMappings.TryGetValue(prefabPath, out var mappedImg))
            {
                imageName = mappedImg ?? string.Empty;
            }
            else if (imageMappings.TryGetValue(Path.GetFileName(prefabPath), out var fallbackImg))
            {
                imageName = fallbackImg ?? string.Empty;
            }

            if (layoutMap.TryGetValue(prefabPath, out var existing))
            {
                if (string.IsNullOrEmpty(existing.ImageName) && !string.IsNullOrEmpty(imageName))
                {
                    existing.ImageName = imageName;
                }
                list.Add(existing);
            }
            else
            {
                string fileName = Path.GetFileName(prefabPath);
                string baseName = Path.GetFileNameWithoutExtension(prefabPath);
                string friendlyName = LevelLayoutService.GetFriendlyElevatorName(prefabPath);

                string category = ExtractCategoryFromPath(prefabPath);

                list.Add(new GeomorphEntry
                {
                    Name = baseName,
                    DisplayName = friendlyName,
                    PrefabName = fileName,
                    PrefabPath = prefabPath,
                    Description = string.Empty,
                    ImageName = imageName ?? string.Empty,
                    Complex = FormatDisplayName(tilesetName),
                    SubComplex = category,
                    Category = category,
                    VariantIndex = 1,
                    VariantCount = 1
                });
            }
        }

        return list;
    }

    public static List<GeomorphComplexViewModel> BuildHierarchy(IEnumerable<GeomorphEntry> entries)
    {
        var result = new List<GeomorphComplexViewModel>();
        var entriesList = entries.ToList();

        var complexGroups = entriesList.GroupBy(e => e.Complex);
        foreach (var complexGroup in complexGroups)
        {
            string complexName = complexGroup.Key;
            int complexId = complexGroup.FirstOrDefault()?.ComplexId ?? 0;
            var complexVm = new GeomorphComplexViewModel(complexName, complexId);

            var subComplexGroups = complexGroup.GroupBy(e => e.SubComplex);
            foreach (var subComplexGroup in subComplexGroups)
            {
                string subComplexName = subComplexGroup.Key;
                string subComplexKey = subComplexGroup.FirstOrDefault()?.SubComplexKey ?? string.Empty;
                var subComplexVm = new GeomorphSubComplexViewModel(subComplexName, subComplexKey);

                var categoryGroups = subComplexGroup.GroupBy(e => e.Category);
                foreach (var catGroup in categoryGroups)
                {
                    string catName = catGroup.Key;
                    var catVm = new GeomorphCategoryViewModel(catName, catGroup);
                    subComplexVm.Categories.Add(catVm);
                }

                complexVm.SubComplexes.Add(subComplexVm);
            }

            result.Add(complexVm);
        }

        return result;
    }
}
