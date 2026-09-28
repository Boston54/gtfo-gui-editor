using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using GTFO_GUIEditor.Models;

namespace GTFO_GUIEditor.Services;

public static class SoundService
{
    public static string GetSoundsDirectory()
    {
        string outputDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "vanilla", "sounds");
        if (Directory.Exists(outputDir))
        {
            return outputDir;
        }

        string cwdDir = Path.Combine(Directory.GetCurrentDirectory(), "vanilla", "sounds");
        if (Directory.Exists(cwdDir))
        {
            return cwdDir;
        }

        string cwdProjectDir = Path.Combine(Directory.GetCurrentDirectory(), "GTFO_GUIEditor", "vanilla", "sounds");
        if (Directory.Exists(cwdProjectDir))
        {
            return cwdProjectDir;
        }

        var dir = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);
        while (dir != null)
        {
            string candidateProject = Path.Combine(dir.FullName, "GTFO_GUIEditor", "vanilla", "sounds");
            if (Directory.Exists(candidateProject))
            {
                return candidateProject;
            }

            string candidateVanilla = Path.Combine(dir.FullName, "vanilla", "sounds");
            if (Directory.Exists(candidateVanilla))
            {
                return candidateVanilla;
            }

            dir = dir.Parent;
        }

        return outputDir;
    }

    public static List<SoundEntry> LoadSounds()
    {
        string? filePath = FindSoundsFile();
        if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
        {
            return new List<SoundEntry>();
        }

        return ParseSoundsFile(filePath);
    }

    public static string? FindSoundsFile()
    {
        string soundsDir = GetSoundsDirectory();

        string directCsv = Path.Combine(soundsDir, "sounds.csv");
        if (File.Exists(directCsv))
        {
            return directCsv;
        }

        string directTxt = Path.Combine(soundsDir, "simon_sfx.txt");
        if (File.Exists(directTxt))
        {
            return directTxt;
        }

        return null;
    }

    public static List<SoundEntry> ParseSoundsFile(string filePath)
    {
        var lines = File.ReadAllLines(filePath);
        return ParseLines(lines);
    }

    public static List<SoundEntry> ParseLines(IEnumerable<string> lines)
    {
        var entries = new List<SoundEntry>();
        bool isFirstLine = true;
        char delimiter = '\t';
        int idCol = -1;
        int nameCol = -1;
        int wwiseCol = -1;
        int eventCol = -1;
        int notesCol = -1;
        bool hasMappedHeaders = false;

        foreach (var rawLine in lines)
        {
            if (string.IsNullOrWhiteSpace(rawLine))
                continue;

            string line = rawLine.TrimStart('\uFEFF');

            if (isFirstLine)
            {
                isFirstLine = false;

                if (line.Contains('\t'))
                {
                    delimiter = '\t';
                }
                else if (line.Contains(','))
                {
                    delimiter = ',';
                }

                var headerParts = SplitLine(line, delimiter);
                bool isHeader = headerParts.Any(p =>
                    p.Equals("ID", StringComparison.OrdinalIgnoreCase) ||
                    p.Equals("Name", StringComparison.OrdinalIgnoreCase) ||
                    p.Equals("Event", StringComparison.OrdinalIgnoreCase) ||
                    p.IndexOf("Wwise", StringComparison.OrdinalIgnoreCase) >= 0);

                if (isHeader)
                {
                    for (int i = 0; i < headerParts.Length; i++)
                    {
                        var h = headerParts[i].Trim().Trim('\"');
                        if (h.Equals("ID", StringComparison.OrdinalIgnoreCase) || h.Equals("SoundID", StringComparison.OrdinalIgnoreCase))
                            idCol = i;
                        else if (h.Equals("Name", StringComparison.OrdinalIgnoreCase) || h.Equals("SoundName", StringComparison.OrdinalIgnoreCase))
                            nameCol = i;
                        else if (h.IndexOf("Wwise", StringComparison.OrdinalIgnoreCase) >= 0 || h.IndexOf("Path", StringComparison.OrdinalIgnoreCase) >= 0)
                            wwiseCol = i;
                        else if (h.Equals("Event", StringComparison.OrdinalIgnoreCase))
                            eventCol = i;
                        else if (h.Equals("Notes", StringComparison.OrdinalIgnoreCase) || h.Equals("Note", StringComparison.OrdinalIgnoreCase))
                            notesCol = i;
                    }

                    hasMappedHeaders = true;
                    continue;
                }
            }

            var parts = SplitLine(line, delimiter);

            string eventName = string.Empty;
            string idStr = string.Empty;
            string name = string.Empty;
            string wwisePath = string.Empty;
            string notes = string.Empty;

            if (hasMappedHeaders)
            {
                if (idCol >= 0 && idCol < parts.Length) idStr = parts[idCol].Trim().Trim('\"');
                if (nameCol >= 0 && nameCol < parts.Length) name = parts[nameCol].Trim().Trim('\"');
                if (wwiseCol >= 0 && wwiseCol < parts.Length) wwisePath = parts[wwiseCol].Trim().Trim('\"');
                if (eventCol >= 0 && eventCol < parts.Length) eventName = parts[eventCol].Trim().Trim('\"');
                if (notesCol >= 0 && notesCol < parts.Length) notes = parts[notesCol].Trim().Trim('\"');
            }
            else
            {
                if (delimiter == ',')
                {
                    idStr = parts.Length > 0 ? parts[0].Trim().Trim('\"') : string.Empty;
                    name = parts.Length > 1 ? parts[1].Trim().Trim('\"') : string.Empty;
                    wwisePath = parts.Length > 2 ? parts[2].Trim().Trim('\"') : string.Empty;
                }
                else
                {
                    eventName = parts.Length > 0 ? parts[0].Trim() : string.Empty;
                    idStr = parts.Length > 1 ? parts[1].Trim() : string.Empty;
                    name = parts.Length > 2 ? parts[2].Trim() : string.Empty;

                    if (parts.Length > 5 && !string.IsNullOrWhiteSpace(parts[5]))
                    {
                        wwisePath = parts[5].Trim();
                        if (parts.Length > 6)
                        {
                            notes = parts[6].Trim();
                        }
                    }
                    else
                    {
                        for (int i = 3; i < parts.Length; i++)
                        {
                            var p = parts[i].Trim();
                            if (!string.IsNullOrEmpty(p))
                            {
                                if (string.IsNullOrEmpty(wwisePath))
                                    wwisePath = p;
                                else if (string.IsNullOrEmpty(notes))
                                    notes = p;
                            }
                        }
                    }
                }
            }

            uint.TryParse(idStr, out uint id);

            if (id == 0 && string.IsNullOrWhiteSpace(name))
                continue;

            entries.Add(new SoundEntry
            {
                Event = eventName,
                Id = id,
                IdString = idStr,
                Name = name,
                WwisePath = wwisePath,
                Notes = notes,
                RawLine = rawLine
            });
        }

        return entries;
    }

    private static string[] SplitLine(string line, char delimiter)
    {
        if (delimiter == '\t')
        {
            return line.Split('\t');
        }

        var result = new List<string>();
        bool inQuotes = false;
        var current = new System.Text.StringBuilder();

        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];
            if (c == '\"')
            {
                if (inQuotes && i + 1 < line.Length && line[i + 1] == '\"')
                {
                    current.Append('\"');
                    i++;
                }
                else
                {
                    inQuotes = !inQuotes;
                }
            }
            else if (c == delimiter && !inQuotes)
            {
                result.Add(current.ToString());
                current.Clear();
            }
            else
            {
                current.Append(c);
            }
        }

        result.Add(current.ToString());
        return result.ToArray();
    }
}
