using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using GTFO_GUIEditor.Models;

namespace GTFO_GUIEditor.Services;

public sealed class ImportContext
{
    public string FolderPath { get; }

    public JsonSerializerOptions JsonOptions { get; } = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public ImportContext(string folderPath)
    {
        if (string.IsNullOrWhiteSpace(folderPath))
        {
            throw new ArgumentException("Datablock folder cannot be null or whitespace.", nameof(folderPath));
        }
        FolderPath = folderPath;
    }

    public string? FindFile(params string[] names)
    {
        foreach (var name in names)
        {
            var direct = Path.Combine(FolderPath, name);
            if (File.Exists(direct))
            {
                return direct;
            }

            var match = Directory.EnumerateFiles(FolderPath, name, SearchOption.AllDirectories)
                .FirstOrDefault();
            if (match != null)
            {
                return match;
            }
        }
        return null;
    }

    public bool TryDeserializeFile<T>(string? filePath, out T? value)
    {
        value = default;
        if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
        {
            return false;
        }

        try
        {
            value = JsonSerializer.Deserialize<T>(File.ReadAllText(filePath), JsonOptions);
            return value != null;
        }
        catch
        {
            return false;
        }
    }
}
