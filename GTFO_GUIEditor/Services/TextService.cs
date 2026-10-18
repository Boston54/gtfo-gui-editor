using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace GTFO_GUIEditor.Services;

public static class TextService
{
    private static readonly object _lock = new();
    private static Dictionary<uint, string>? _textCache;

    public static string? FindTextDataBlockFile()
    {
        string dir = RundownService.GetDatablocksDirectory();
        string directPath = Path.Combine(dir, "TextDataBlock.json");
        if (File.Exists(directPath)) return directPath;

        string altPath = Path.Combine(dir, "GameData_TextDataBlock_bin.json");
        if (File.Exists(altPath)) return altPath;

        return null;
    }

    public static void Initialize(string? filePath = null)
    {
        lock (_lock)
        {
            if (_textCache != null && _textCache.Count > 0) return;
            _textCache = new Dictionary<uint, string>();

            filePath ??= FindTextDataBlockFile();
            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath)) return;

            try
            {
                using var fs = File.OpenRead(filePath);
                using var doc = JsonDocument.Parse(fs);
                if (doc.RootElement.TryGetProperty("Blocks", out var blocksElement) &&
                    blocksElement.ValueKind == JsonValueKind.Array)
                {
                    foreach (var block in blocksElement.EnumerateArray())
                    {
                        uint id = 0;
                        if (block.TryGetProperty("persistentID", out var idProp) ||
                            block.TryGetProperty("PersistentID", out idProp) ||
                            block.TryGetProperty("persistentId", out idProp))
                        {
                            if (idProp.ValueKind == JsonValueKind.Number)
                            {
                                id = idProp.GetUInt32();
                            }
                            else if (idProp.ValueKind == JsonValueKind.String && uint.TryParse(idProp.GetString(), out uint parsedId))
                            {
                                id = parsedId;
                            }
                        }

                        if (id != 0 && block.TryGetProperty("English", out var engProp))
                        {
                            string? eng = engProp.GetString();
                            if (eng != null)
                            {
                                _textCache[id] = eng;
                            }
                        }
                    }
                }
            }
            catch
            {
                // Graceful fallback if text data block cannot be parsed
            }
        }
    }

    public static void SetCache(Dictionary<uint, string> cache)
    {
        lock (_lock)
        {
            _textCache = new Dictionary<uint, string>(cache);
        }
    }

    public static void ResetCache()
    {
        lock (_lock)
        {
            _textCache = null;
        }
    }

    public static bool TryGetEnglishText(uint id, out string text)
    {
        if (id == 0)
        {
            text = string.Empty;
            return false;
        }

        if (_textCache == null)
        {
            Initialize();
        }

        if (_textCache != null && _textCache.TryGetValue(id, out var found))
        {
            text = found;
            return true;
        }

        text = string.Empty;
        return false;
    }

    public static string GetEnglishText(uint id)
    {
        if (TryGetEnglishText(id, out var text))
        {
            return text;
        }
        return string.Empty;
    }
}
