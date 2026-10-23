using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using GTFO_GUIEditor.Models;

namespace GTFO_GUIEditor.Services;

public static class ComplexResourceSetService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public static string? FindTemplateFile()
    {
        string[] candidates =
        {
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "Templates", "ComplexResourceSet.json"),
            Path.Combine(Directory.GetCurrentDirectory(), "Data", "Templates", "ComplexResourceSet.json"),
            Path.Combine(Directory.GetCurrentDirectory(), "GTFO_GUIEditor", "Data", "Templates", "ComplexResourceSet.json")
        };

        foreach (var c in candidates)
        {
            if (File.Exists(c)) return c;
        }

        var dir = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);
        while (dir != null)
        {
            string candidateProject = Path.Combine(dir.FullName, "GTFO_GUIEditor", "Data", "Templates", "ComplexResourceSet.json");
            if (File.Exists(candidateProject)) return candidateProject;

            string candidateData = Path.Combine(dir.FullName, "Data", "Templates", "ComplexResourceSet.json");
            if (File.Exists(candidateData)) return candidateData;

            dir = dir.Parent;
        }

        return null;
    }

    public static JsonObject? LoadTemplateJsonObject(ComplexType complexType)
    {
        string? path = FindTemplateFile();
        if (string.IsNullOrEmpty(path) || !File.Exists(path)) return null;

        try
        {
            string json = File.ReadAllText(path);
            var node = JsonNode.Parse(json)?.AsObject();
            if (node == null) return null;

            string key = complexType.ToString().ToLowerInvariant();
            if (node.TryGetPropertyValue(key, out var val) && val is JsonObject obj)
            {
                return JsonNode.Parse(obj.ToJsonString())?.AsObject();
            }

            var match = node.FirstOrDefault(kv => string.Equals(kv.Key, complexType.ToString(), StringComparison.OrdinalIgnoreCase));
            if (match.Value is JsonObject matchObj)
            {
                return JsonNode.Parse(matchObj.ToJsonString())?.AsObject();
            }

            return null;
        }
        catch
        {
            return null;
        }
    }

    public static ComplexResourceSetBlock? LoadTemplateBlock(ComplexType complexType)
    {
        var obj = LoadTemplateJsonObject(complexType);
        if (obj == null) return null;

        try
        {
            return JsonSerializer.Deserialize<ComplexResourceSetBlock>(obj.ToJsonString(), JsonOptions);
        }
        catch
        {
            return null;
        }
    }

    public static string GetDatablocksDirectory()
    {
        string outputDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "Vanilla", "Datablocks");
        if (Directory.Exists(outputDir))
        {
            return outputDir;
        }

        string cwdDir = Path.Combine(Directory.GetCurrentDirectory(), "Data", "Vanilla", "Datablocks");
        if (Directory.Exists(cwdDir))
        {
            return cwdDir;
        }

        string cwdProjectDir = Path.Combine(Directory.GetCurrentDirectory(), "GTFO_GUIEditor", "Data", "Vanilla", "Datablocks");
        if (Directory.Exists(cwdProjectDir))
        {
            return cwdProjectDir;
        }

        var dir = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);
        while (dir != null)
        {
            string candidateProject = Path.Combine(dir.FullName, "GTFO_GUIEditor", "Data", "Vanilla", "Datablocks");
            if (Directory.Exists(candidateProject))
            {
                return candidateProject;
            }

            string candidateVanilla = Path.Combine(dir.FullName, "Data", "Vanilla", "Datablocks");
            if (Directory.Exists(candidateVanilla))
            {
                return candidateVanilla;
            }

            dir = dir.Parent;
        }

        return outputDir;
    }

    public static string? FindDataBlockFile()
    {
        string dir = GetDatablocksDirectory();
        string directPath = Path.Combine(dir, "ComplexResourceSetDataBlock.json");
        if (File.Exists(directPath))
        {
            return directPath;
        }

        string altPath = Path.Combine(dir, "GameData_ComplexResourceSetDataBlock_bin.json");
        if (File.Exists(altPath))
        {
            return altPath;
        }

        return null;
    }

    public static ComplexResourceSetDataBlockFile LoadDataBlockFile(string? filePath = null)
    {
        filePath ??= FindDataBlockFile();

        if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
        {
            return new ComplexResourceSetDataBlockFile();
        }

        try
        {
            string json = File.ReadAllText(filePath);
            return JsonSerializer.Deserialize<ComplexResourceSetDataBlockFile>(json, JsonOptions) ?? new ComplexResourceSetDataBlockFile();
        }
        catch
        {
            return new ComplexResourceSetDataBlockFile();
        }
    }

    public static List<ComplexResourceSetBlock> LoadVanillaBlocks(string? filePath = null)
    {
        var file = LoadDataBlockFile(filePath);
        return file.Blocks;
    }

    public static ComplexResourceSetBlock CreateFreshBlock(uint id, string name, ComplexType complexType = ComplexType.Mining, SubComplexType primarySubComplex = SubComplexType.DigSite)
    {
        var templateBlock = LoadTemplateBlock(complexType);
        if (templateBlock != null)
        {
            templateBlock.PersistentId = id;
            templateBlock.Name = name;
            templateBlock.InternalEnabled = true;
            templateBlock.ComplexType = complexType;
            templateBlock.PrimareSubComplexUsed = primarySubComplex;
            return templateBlock;
        }

        BundleNameType bundle = complexType switch
        {
            ComplexType.Mining => BundleNameType.Complex_Mining,
            ComplexType.Tech => BundleNameType.Complex_Tech,
            ComplexType.Service => BundleNameType.Complex_Service,
            _ => BundleNameType.Complex_Mining
        };

        return new ComplexResourceSetBlock
        {
            PersistentId = id,
            Name = name,
            ComplexType = complexType,
            PrimareSubComplexUsed = primarySubComplex,
            BundleName = bundle,
            InternalEnabled = true,
            RandomizeGeomorphOrder = false,
            LevelGenConfig = new LevelGenConfig
            {
                GridSize = 40,
                CellDimension = 64.0,
                AltitudeOffset = 6.0,
                TransitionDirection = TransitionDirection.FloorUp,
                LevelProgression = LevelProgression.StartLevel
            }
        };
    }

    public static ComplexResourceSetBlock CreateBlockFromTemplate(ComplexResourceSetBlock template, uint newId, string newName)
    {
        var clone = template.Clone();
        clone.PersistentId = newId;
        clone.Name = newName;
        return clone;
    }
}
