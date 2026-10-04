using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using GTFO_GUIEditor.Models;

namespace GTFO_GUIEditor.Services;

public class FlexibleUIntConverter : JsonConverter<uint>
{
    public override uint Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Number)
        {
            if (reader.TryGetUInt32(out uint u)) return u;
            if (reader.TryGetInt64(out long l)) return (uint)l;
            if (reader.TryGetDouble(out double d)) return (uint)d;
        }
        else if (reader.TokenType == JsonTokenType.String)
        {
            string? str = reader.GetString();
            if (uint.TryParse(str, out uint val)) return val;
            return 0;
        }
        return 0;
    }

    public override void Write(Utf8JsonWriter writer, uint value, JsonSerializerOptions options)
    {
        writer.WriteNumberValue(value);
    }
}

public class FlexibleIntConverter : JsonConverter<int>
{
    public override int Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Number)
        {
            if (reader.TryGetInt32(out int i)) return i;
            if (reader.TryGetInt64(out long l)) return (int)l;
            if (reader.TryGetDouble(out double d)) return (int)d;
        }
        else if (reader.TokenType == JsonTokenType.String)
        {
            string? str = reader.GetString();
            if (int.TryParse(str, out int val)) return val;
            return 0;
        }
        return 0;
    }

    public override void Write(Utf8JsonWriter writer, int value, JsonSerializerOptions options)
    {
        writer.WriteNumberValue(value);
    }
}

public class FlexibleDoubleConverter : JsonConverter<double>
{
    public override double Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Number)
        {
            if (reader.TryGetDouble(out double d)) return d;
        }
        else if (reader.TokenType == JsonTokenType.String)
        {
            string? str = reader.GetString();
            if (double.TryParse(str, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double val)) return val;
            return 0.0;
        }
        return 0.0;
    }

    public override void Write(Utf8JsonWriter writer, double value, JsonSerializerOptions options)
    {
        writer.WriteNumberValue(value);
    }
}

public class FlexibleTextConverter : JsonConverter<string>
{
    public override string Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String)
        {
            string? str = reader.GetString();
            if (string.IsNullOrEmpty(str)) return string.Empty;

            if (uint.TryParse(str, out uint id) && id > 0)
            {
                if (TextService.TryGetEnglishText(id, out string text))
                {
                    return text;
                }
            }

            return str;
        }
        else if (reader.TokenType == JsonTokenType.Number)
        {
            uint id = 0;
            if (reader.TryGetUInt32(out uint u))
            {
                id = u;
            }
            else if (reader.TryGetInt64(out long l) && l > 0)
            {
                id = (uint)l;
            }

            if (id > 0)
            {
                if (TextService.TryGetEnglishText(id, out string text))
                {
                    return text;
                }
            }

            return string.Empty;
        }

        return string.Empty;
    }

    public override void Write(Utf8JsonWriter writer, string value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value ?? string.Empty);
    }
}

public static class RundownService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
        WriteIndented = true,
        Converters =
        {
            new JsonStringEnumConverter(),
            new FlexibleUIntConverter(),
            new FlexibleIntConverter(),
            new FlexibleDoubleConverter(),
            new FlexibleTextConverter()
        }
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
        string directPath = Path.Combine(dir, "RundownDataBlock.json");
        if (File.Exists(directPath))
        {
            return directPath;
        }

        string altPath = Path.Combine(dir, "GameData_RundownDataBlock_bin.json");
        if (File.Exists(altPath))
        {
            return altPath;
        }

        return null;
    }

    public static RundownDataBlockFile LoadDataBlockFile(string? filePath = null)
    {
        filePath ??= FindDataBlockFile();

        if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
        {
            return new RundownDataBlockFile();
        }

        string json = File.ReadAllText(filePath);
        return JsonSerializer.Deserialize<RundownDataBlockFile>(json, JsonOptions) ?? new RundownDataBlockFile();
    }

    public static List<RundownBlock> LoadVanillaBlocks(string? filePath = null)
    {
        var file = LoadDataBlockFile(filePath);
        return file.Blocks;
    }

    public static RundownBlock CreateFreshBlock(uint id, string name)
    {
        return new RundownBlock
        {
            PersistentId = id,
            Name = name,
            InternalEnabled = true,
            NeverShowRundownTree = false,
            UseTierUnlockRequirements = false,
            ReqToReachTierA = new TierRequirements { MainSectors = 0, SecondarySectors = 0, ThirdSectors = 0, AllClearedSectors = 0 },
            ReqToReachTierB = new TierRequirements { MainSectors = 0, SecondarySectors = 0, ThirdSectors = 0, AllClearedSectors = 0 },
            ReqToReachTierC = new TierRequirements { MainSectors = 0, SecondarySectors = 0, ThirdSectors = 0, AllClearedSectors = 0 },
            ReqToReachTierD = new TierRequirements { MainSectors = 0, SecondarySectors = 0, ThirdSectors = 0, AllClearedSectors = 0 },
            ReqToReachTierE = new TierRequirements { MainSectors = 0, SecondarySectors = 0, ThirdSectors = 0, AllClearedSectors = 0 },
            StorytellingData = new StorytellingData
            {
                Title = string.Empty,
                TextLog = 0,
                TextLogPos = new Vector2Data { X = 0.0, Y = 0.0 },
                Visuals = new RundownVisuals
                {
                    ColorBackground = new ColorData(1.0, 0.0, 0.0, 0.0),
                    TierAVisuals = new TierVisuals { Color = new ColorData(1.0, 0.97, 0.88, 0.79), Scale = 0.65, ScaleYModifier = 0.3 },
                    TierBVisuals = new TierVisuals { Color = new ColorData(1.0, 0.91, 0.77, 0.66), Scale = 0.65, ScaleYModifier = 0.3 },
                    TierCVisuals = new TierVisuals { Color = new ColorData(1.0, 0.81, 0.66, 0.54), Scale = 0.65, ScaleYModifier = 0.3 },
                    TierDVisuals = new TierVisuals { Color = new ColorData(1.0, 0.68, 0.49, 0.32), Scale = 0.65, ScaleYModifier = 0.3 },
                    TierEVisuals = new TierVisuals { Color = new ColorData(1.0, 0.68, 0.41, 0.18), Scale = 0.65, ScaleYModifier = 0.3 }
                },
                SurfaceIconPosition = new Vector2Data { X = 0.0, Y = 0.0 },
                SurfaceDescription = string.Empty,
                ExternalExpTitle = 0
            },
            VanityItemLayerDropDataBlock = 0,
            TierA = new List<ExpeditionBlock>(),
            TierB = new List<ExpeditionBlock>(),
            TierC = new List<ExpeditionBlock>(),
            TierD = new List<ExpeditionBlock>(),
            TierE = new List<ExpeditionBlock>()
        };
    }

    public static RundownBlock CreateBlockFromTemplate(RundownBlock template, uint newId, string newName)
    {
        var clone = template.Clone();
        clone.PersistentId = newId;
        clone.Name = newName;
        return clone;
    }
}
