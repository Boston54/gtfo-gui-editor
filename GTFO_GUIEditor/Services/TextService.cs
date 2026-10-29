using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace GTFO_GUIEditor.Services;

public static class TextService
{
    private static readonly object _lock = new();
    private static Dictionary<uint, string>? _textCache;

    public static void Initialize()
    {
        lock (_lock)
        {
            if (_textCache != null && _textCache.Count > 0) return;
            _textCache = new Dictionary<uint, string>();

            foreach (var block in DatablockManager.Instance.GetRawBlocks(
                         "TextDataBlock.json",
                         "GameData_TextDataBlock_bin.json"))
            {
                uint id = 0;
                if (block.TryGetPropertyValue("persistentID", out var idNode))
                {
                    uint.TryParse(idNode?.ToString(), out id);
                }
                if (id != 0 && block.TryGetPropertyValue("English", out var englishNode))
                {
                    var english = englishNode?.GetValue<string>();
                    if (!string.IsNullOrEmpty(english)) _textCache[id] = english;
                }
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
