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

        string[] candidates =
            { "Documentation.md", "UserDocumentation.md", "Home.md", "documentation.md", "userdocumentation.md" };
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

    public static List<string> ExtractParagraphs(string content)
    {
        var list = new List<string>();
        if (string.IsNullOrWhiteSpace(content))
        {
            return list;
        }

        string normalized = content.Replace("\r\n", "\n").Replace('\r', '\n');
        string[] blocks = System.Text.RegularExpressions.Regex.Split(normalized, @"\n\s*\n+");

        foreach (var b in blocks)
        {
            string trimmed = b.Trim();
            if (!string.IsNullOrWhiteSpace(trimmed))
            {
                list.Add(trimmed);
            }
        }

        return list;
    }

    public static List<DocSearchResult> Search(
        IEnumerable<DocSection>? sections,
        string query,
        Action<DocSearchResult>? onNavigate = null)
    {
        var results = new List<DocSearchResult>();
        if (sections == null || string.IsNullOrWhiteSpace(query))
        {
            return results;
        }

        string trimmedQuery = query.Trim();
        string[] keywords = trimmedQuery.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (keywords.Length == 0)
        {
            return results;
        }

        foreach (var h1 in sections)
        {
            if (h1.IsSearchTab) continue;

            if (!h1.HasSubSections)
            {
                CheckAndAddResult(results, h1, null, null, h1, h1.Title, trimmedQuery, keywords, onNavigate);
            }
            else
            {
                foreach (var h2 in h1.SubSections)
                {
                    if (!h2.HasSubSections)
                    {
                        string breadcrumb = $"{h1.Title} > {h2.Title}";
                        CheckAndAddResult(results, h1, h2, null, h2, breadcrumb, trimmedQuery, keywords, onNavigate);
                    }
                    else
                    {
                        foreach (var h3 in h2.SubSections)
                        {
                            string breadcrumb = $"{h1.Title} > {h2.Title} > {h3.Title}";
                            CheckAndAddResult(results, h1, h2, h3, h3, breadcrumb, trimmedQuery, keywords, onNavigate);
                        }
                    }
                }
            }
        }

        return results;
    }

    private static void CheckAndAddResult(
        List<DocSearchResult> results,
        DocSection h1,
        DocSection? h2,
        DocSection? h3,
        DocSection target,
        string breadcrumb,
        string fullQuery,
        string[] keywords,
        Action<DocSearchResult>? onNavigate)
    {
        string content = target.Content ?? string.Empty;
        var paragraphs = ExtractParagraphs(content);

        var matchingParagraphs = new List<string>();

        bool titleMatches = target.Title.IndexOf(fullQuery, StringComparison.OrdinalIgnoreCase) >= 0 ||
                            breadcrumb.IndexOf(fullQuery, StringComparison.OrdinalIgnoreCase) >= 0 ||
                            keywords.All(k => target.Title.IndexOf(k, StringComparison.OrdinalIgnoreCase) >= 0 ||
                                              breadcrumb.IndexOf(k, StringComparison.OrdinalIgnoreCase) >= 0);

        foreach (var p in paragraphs)
        {
            bool paragraphMatches = p.IndexOf(fullQuery, StringComparison.OrdinalIgnoreCase) >= 0 ||
                                    keywords.All(k => p.IndexOf(k, StringComparison.OrdinalIgnoreCase) >= 0);

            if (paragraphMatches)
            {
                matchingParagraphs.Add(p);
            }
        }

        if (matchingParagraphs.Count > 0)
        {
            var result = new DocSearchResult(onNavigate)
            {
                Title = target.Title,
                Breadcrumb = breadcrumb,
                SearchQuery = fullQuery,
                H1Section = h1,
                H2Section = h2,
                H3Section = h3,
                TargetSection = target
            };
            foreach (var p in matchingParagraphs)
            {
                result.MatchingParagraphs.Add(new DocSearchParagraph
                {
                    Text = p,
                    HighlightQuery = fullQuery
                });
            }

            results.Add(result);
        }
        else if (titleMatches && paragraphs.Count > 0)
        {
            var result = new DocSearchResult(onNavigate)
            {
                Title = target.Title,
                Breadcrumb = breadcrumb,
                SearchQuery = fullQuery,
                H1Section = h1,
                H2Section = h2,
                H3Section = h3,
                TargetSection = target
            };
            result.MatchingParagraphs.Add(new DocSearchParagraph
            {
                Text = paragraphs[0],
                HighlightQuery = fullQuery
            });
            results.Add(result);
        }
    }
}