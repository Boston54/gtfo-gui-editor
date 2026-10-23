using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using GTFO_GUIEditor.Models;

namespace GTFO_GUIEditor.Services;

public static class LightSettingsService
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
        string directPath = Path.Combine(dir, "LightSettingsDataBlock.json");
        if (File.Exists(directPath))
        {
            return directPath;
        }

        string altPath = Path.Combine(dir, "GameData_LightSettingsDataBlock_bin.json");
        if (File.Exists(altPath))
        {
            return altPath;
        }

        return null;
    }

    public static LightSettingsDataBlockFile LoadDataBlockFile(string? filePath = null)
    {
        filePath ??= FindDataBlockFile();

        if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
        {
            return new LightSettingsDataBlockFile();
        }

        try
        {
            string json = File.ReadAllText(filePath);
            return JsonSerializer.Deserialize<LightSettingsDataBlockFile>(json, JsonOptions) ?? new LightSettingsDataBlockFile();
        }
        catch
        {
            return new LightSettingsDataBlockFile();
        }
    }

    public static List<LightSettingsBlock> LoadVanillaBlocks(string? filePath = null)
    {
        var file = LoadDataBlockFile(filePath);
        return file.Blocks;
    }

    public static LightSettingsBlock CreateFreshBlock(uint id, string name)
    {
        return EditorDefaultsService.CreateDefaultLightBlock(id, name);
    }

    public static LightSettingsBlock CreateBlockFromTemplate(LightSettingsBlock template, uint newId, string newName)
    {
        var clone = template.Clone();
        clone.PersistentId = newId;
        clone.Name = newName;
        return clone;
    }
}
