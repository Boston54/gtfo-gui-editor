using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using GTFO_GUIEditor.Models;

namespace GTFO_GUIEditor.Services;

public static class DocumentationService
{
    public static string? FindDocumentationFile()
    {
        string dataDir = EditorDefaultsService.GetDataDirectory();
        
        string[] candidates = { "Documentation.md", "UserDocumentation.md", "Home.md", "documentation.md", "userdocumentation.md" };
        foreach (var name in candidates)
        {
            string p = Path.Combine(dataDir, name);
            if (File.Exists(p)) return p;
        }

        if (Directory.Exists(dataDir))
        {
            var firstMd = Directory.GetFiles(dataDir, "*.md", SearchOption.TopDirectoryOnly).FirstOrDefault();
            if (firstMd != null) return firstMd;
        }

        // Check relative to cwd
        foreach (var name in candidates)
        {
            string p = Path.Combine(Directory.GetCurrentDirectory(), "Data", name);
            if (File.Exists(p)) return p;

            string p2 = Path.Combine(Directory.GetCurrentDirectory(), "GTFO_GUIEditor", "Data", name);
            if (File.Exists(p2)) return p2;
        }

        return null;
    }

    public static ObservableCollection<DocSection> ParseDocument(string markdown)
    {
        var sections = new ObservableCollection<DocSection>();
        if (string.IsNullOrWhiteSpace(markdown))
        {
            return sections;
        }

        string[] lines = markdown.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
        
        // We will build a tree of parsed sections:
        // Level 1: H1
        // Level 2: H2
        // Level 3: H3

        var rawH1List = new List<RawSection>();
        RawSection? currentH1 = null;
        RawSection? currentH2 = null;
        RawSection? currentH3 = null;
        var introSb = new StringBuilder();

        bool inCodeBlock = false;

        foreach (var line in lines)
        {
            string trimmed = line.Trim();
            if (trimmed.StartsWith("```", StringComparison.Ordinal))
            {
                inCodeBlock = !inCodeBlock;
            }

            if (!inCodeBlock)
            {
                if (trimmed.StartsWith("# ") && trimmed.Length >= 3)
                {
                    // New H1
                    string title = trimmed.Substring(2).Trim();
                    currentH1 = new RawSection { Level = 1, Title = title };
                    rawH1List.Add(currentH1);
                    currentH2 = null;
                    currentH3 = null;
                    continue;
                }
                else if (trimmed.StartsWith("## ") && trimmed.Length >= 4)
                {
                    // New H2
                    if (currentH1 == null)
                    {
                        currentH1 = new RawSection { Level = 1, Title = "Documentation" };
                        rawH1List.Add(currentH1);
                    }
                    string title = trimmed.Substring(3).Trim();
                    currentH2 = new RawSection { Level = 2, Title = title };
                    currentH1.Children.Add(currentH2);
                    currentH3 = null;
                    continue;
                }
                else if (trimmed.StartsWith("### ") && trimmed.Length >= 5)
                {
                    // New H3
                    if (currentH1 == null)
                    {
                        currentH1 = new RawSection { Level = 1, Title = "Documentation" };
                        rawH1List.Add(currentH1);
                    }
                    if (currentH2 == null)
                    {
                        currentH2 = new RawSection { Level = 2, Title = "Overview" };
                        currentH1.Children.Add(currentH2);
                    }
                    string title = trimmed.Substring(4).Trim();
                    currentH3 = new RawSection { Level = 3, Title = title };
                    currentH2.Children.Add(currentH3);
                    continue;
                }
            }

            // Regular content line
            if (currentH3 != null)
            {
                currentH3.ContentBuilder.AppendLine(line);
            }
            else if (currentH2 != null)
            {
                currentH2.ContentBuilder.AppendLine(line);
            }
            else if (currentH1 != null)
            {
                currentH1.ContentBuilder.AppendLine(line);
            }
            else
            {
                introSb.AppendLine(line);
            }
        }

        // If there was intro content before any H1
        string introContent = introSb.ToString().Trim();
        if (!string.IsNullOrWhiteSpace(introContent))
        {
            var introH1 = new RawSection { Level = 1, Title = "Overview" };
            introH1.ContentBuilder.Append(introContent);
            rawH1List.Insert(0, introH1);
        }

        // If document had no headers at all
        if (rawH1List.Count == 0)
        {
            var fallbackH1 = new RawSection { Level = 1, Title = "Documentation" };
            fallbackH1.ContentBuilder.Append(markdown.Trim());
            rawH1List.Add(fallbackH1);
        }

        // Convert RawSections to DocSection tree
        foreach (var rawH1 in rawH1List)
        {
            var h1Sec = ConvertToDocSection(rawH1);
            sections.Add(h1Sec);
        }

        return sections;
    }

    private static DocSection ConvertToDocSection(RawSection raw)
    {
        var docSec = new DocSection
        {
            Title = raw.Title,
            Level = raw.Level,
            Content = raw.ContentBuilder.ToString().Trim()
        };

        if (raw.Children.Count > 0)
        {
            // If this section has intro content before its first child, create an Overview sub-section
            if (!string.IsNullOrWhiteSpace(docSec.Content))
            {
                var overviewChild = new DocSection
                {
                    Title = "Overview",
                    Level = raw.Level + 1,
                    Content = docSec.Content
                };
                docSec.SubSections.Add(overviewChild);
                // Clear parent content since it's now accessible in the Overview subtab
                docSec.Content = string.Empty;
            }

            foreach (var child in raw.Children)
            {
                docSec.SubSections.Add(ConvertToDocSection(child));
            }

            docSec.SelectedSubSection = docSec.SubSections.FirstOrDefault();
        }

        return docSec;
    }

    private class RawSection
    {
        public int Level { get; set; } = 1;
        public string Title { get; set; } = string.Empty;
        public StringBuilder ContentBuilder { get; } = new();
        public List<RawSection> Children { get; } = new();
    }

    public static string GetDefaultDocumentationMarkdown()
    {
        return @"# Home
## Overview
### Welcome
Welcome to the **GTFO GUI Level Editor**, developed for building custom rundowns and level layouts.

This documentation is dynamically loaded from markdown files located in your `Data` folder (`Data/Documentation.md`). You can customize and expand this guide at any time.

### Editor Highlights
- **Dynamic Rundown Design**: Create tiers, expeditions, bulkheads, and custom objectives.
- **Interactive Visual Grid**: Place and rotate 64x64, 32x32, and custom geomorphs in real time.
- **Smart Door & Bulkhead Routing**: Link security doors, Main bulkheads, bulkhead controllers (DCs), and candidate key placements.
- **Full Datablock Export**: Quickly export full vanilla-compliant datablocks (`Ctrl+E` or `Export -> Quick Export`).

## Shortcuts & Navigation
### Keyboard Shortcuts
| Shortcut | Action | Description |
| --- | --- | --- |
| `Ctrl+N` | New Rundown | Creates a fresh rundown project with default templates. |
| `Ctrl+S` | Save Rundown | Saves the current rundown project to disk. |
| `Ctrl+O` | Open Rundown | Opens an existing `.gtfo` project file. |
| `Ctrl+E` | Quick Export | Exports all datablocks directly to the last export directory. |

### Canvas Navigation
- **Left Click & Drag**: Pan across the 40x40 level layout grid.
- **Mouse Wheel**: Zoom in and out.
- **Right Click Room**: Rotate placed geomorph 90 degrees clockwise.
- **Left Click Door**: Open door configuration dialog (Security Door, Main Door, Secondary Door, Overload Door).

# Level Design
## Sectors & Zones
### Sector Hierarchy
Levels are organized into three primary sectors:
1. **Main Sector**: The primary expedition path. Zone indexing starts at `Zone 0` and increments sequentially.
2. **Secondary Sector**: Optional secondary objective zone. Zone indexing starts at `Zone 0` (ZoneAlias default `100`).
3. **Overload Sector**: Optional high-threat overload objective zone. Zone indexing starts at `Zone 0` (ZoneAlias default `200`).

> **Zone Indexing Rule**: Zone indexes are maintained incrementally per-sector. When a zone is deleted, subsequent zones automatically shift downward with no gaps.

### Bulkheads & Controllers (DCs)
- **Main Security Doors**: Security doors marked as `Main Door` lead into Bulkhead zones (`ZonesWithBulkheadEntrance`).
- **Bulkhead Door Controllers (DC)**: Placed inside candidate zones to allow unlocking bulkhead doors originating from that zone.
- **Bulkhead Keys**: Defined per-sector with candidate zones where keys can spawn.

## Geomorphs & Placement
### Placing Geomorphs
To place a geomorph:
1. Select the geomorph category and type from the **Geomorphs** browser.
2. Click **Place Geomorph** and position it onto the grid canvas.
3. Align door connections with neighboring tiles.

### Room Connections & Doors
- Rooms connect at matching edge portals.
- Internal doors connect areas within multi-area geomorphs.
- External doors connect adjacent geomorphs across the grid.

# Security & Alarms
## Alarms Configuration
### Scan Types & Puzzles
Alarms utilize chained puzzles (`ChainedPuzzleType`) to determine the scanning requirements:
- **Class I - VIII Alarms**: Progressive bioscan requirements.
- **S1 / S2 Scans**: Sustained team and solo bioscan zones.
- **Cluster Scans**: Multi-point rapid puzzle bioscans.

### Wave Spawning
- **Roles**: Weakling (0.75 pts), Standard (1 pt), Special (1 pt), Mini-Boss (2 pts), Boss (2 pts).
- **Spawn Controls**: Direction randomization, group timing, population ramp over time, and custom survival spawn types.

# Environment
## Lighting & Fog
### Light Settings
- Configure zone lighting palettes, ambient colors, intensity, and flicker animations.
- Link light settings to individual zones or override dynamically via Warden Events.

### Fog Settings
- Configure fog altitude, density, color grading, and infection multipliers.
- Trigger dynamic fog transitions using `SetFogSetting` warden events.

## Audio & Sounds
### Sound IDs
- Look up vanilla GTFO sound event IDs in the **Sounds** tab.
- Attach audio cues to level events, alarm starts, and warden broadcasts.
";
    }
}
