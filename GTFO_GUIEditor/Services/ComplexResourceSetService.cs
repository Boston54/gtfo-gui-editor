using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
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

    public static string GetDatablocksDirectory()
    {
        string outputDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "vanilla", "datablocks");
        if (Directory.Exists(outputDir))
        {
            return outputDir;
        }

        string cwdDir = Path.Combine(Directory.GetCurrentDirectory(), "vanilla", "datablocks");
        if (Directory.Exists(cwdDir))
        {
            return cwdDir;
        }

        string cwdProjectDir = Path.Combine(Directory.GetCurrentDirectory(), "GTFO_GUIEditor", "vanilla", "datablocks");
        if (Directory.Exists(cwdProjectDir))
        {
            return cwdProjectDir;
        }

        var dir = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);
        while (dir != null)
        {
            string candidateProject = Path.Combine(dir.FullName, "GTFO_GUIEditor", "vanilla", "datablocks");
            if (Directory.Exists(candidateProject))
            {
                return candidateProject;
            }

            string candidateVanilla = Path.Combine(dir.FullName, "vanilla", "datablocks");
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
        string directPath = Path.Combine(dir, "GameData_ComplexResourceSetDataBlock_bin.json");
        if (File.Exists(directPath))
        {
            return directPath;
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
