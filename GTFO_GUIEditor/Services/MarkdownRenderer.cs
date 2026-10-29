using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Controls.Shapes;
using Avalonia.Layout;
using Avalonia.Media;

namespace GTFO_GUIEditor.Services;

public static class MarkdownRenderer
{
    private static readonly FontFamily MonospaceFont = new("Consolas, Cascadia Code, Menlo, Courier New, monospace");
    private static readonly IBrush TextBrush = new SolidColorBrush(Color.Parse("#CCCCCC"));
    private static readonly IBrush HeaderBrush = new SolidColorBrush(Color.Parse("#FFFFFF"));
    private static readonly IBrush AccentBrush = new SolidColorBrush(Color.Parse("#FFA200"));
    private static readonly IBrush LinkBrush = new SolidColorBrush(Color.Parse("#4EC9B0"));
    private static readonly IBrush CodeBgBrush = new SolidColorBrush(Color.Parse("#181818"));
    private static readonly IBrush InlineCodeBgBrush = new SolidColorBrush(Color.Parse("#28282B"));
    private static readonly IBrush BorderSubtleBrush = new SolidColorBrush(Color.Parse("#3E3E42"));
    private static readonly IBrush QuoteBorderBrush = new SolidColorBrush(Color.Parse("#007ACC"));
    private static readonly IBrush QuoteBgBrush = new SolidColorBrush(Color.Parse("#1E2228"));
    private static readonly IBrush TableHeaderBgBrush = new SolidColorBrush(Color.Parse("#252526"));
    private static readonly IBrush TableAltRowBgBrush = new SolidColorBrush(Color.Parse("#1D1D20"));
    private static readonly IBrush HighlightBgBrush = new SolidColorBrush(Color.Parse("#FFE066"));
    private static readonly IBrush HighlightFgBrush = new SolidColorBrush(Color.Parse("#111111"));

    public static Control RenderToControl(string markdown, string? highlightPhrase = null)
    {
        var mainStack = new StackPanel
        {
            Spacing = 10,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            Margin = new Thickness(0, 0, 8, 0)
        };

        if (string.IsNullOrWhiteSpace(markdown))
        {
            return mainStack;
        }

        string[] lines = markdown.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
        int i = 0;

        while (i < lines.Length)
        {
            string line = lines[i];
            string trimmed = line.Trim();

            // 1. Fenced Code Block
            if (trimmed.StartsWith("```", StringComparison.Ordinal))
            {
                string lang = trimmed.Substring(3).Trim();
                var codeSb = new StringBuilder();
                i++;
                while (i < lines.Length && !lines[i].Trim().StartsWith("```", StringComparison.Ordinal))
                {
                    codeSb.AppendLine(lines[i]);
                    i++;
                }

                if (i < lines.Length) i++; // consume closing ```

                var codeBorder = CreateCodeBlock(codeSb.ToString().TrimEnd(), lang, highlightPhrase);
                mainStack.Children.Add(codeBorder);
                continue;
            }

            // 2. Horizontal Rule (---, ***, ___)
            if (IsHorizontalRule(trimmed))
            {
                var hr = new Border
                {
                    Height = 1,
                    Background = BorderSubtleBrush,
                    Margin = new Thickness(0, 8, 0, 8)
                };
                mainStack.Children.Add(hr);
                i++;
                continue;
            }

            // 3. Headings (#, ##, ###, ####, #####, ######)
            if (trimmed.StartsWith('#'))
            {
                int hashCount = 0;
                while (hashCount < trimmed.Length && trimmed[hashCount] == '#')
                {
                    hashCount++;
                }

                if (hashCount <= 6 && hashCount < trimmed.Length && trimmed[hashCount] == ' ')
                {
                    string headingText = trimmed.Substring(hashCount + 1).Trim();
                    var headerControl = CreateHeading(headingText, hashCount, highlightPhrase);
                    mainStack.Children.Add(headerControl);
                    i++;
                    continue;
                }
            }

            // 4. Blockquote (> ...)
            if (trimmed.StartsWith('>'))
            {
                var quoteLines = new List<string>();
                while (i < lines.Length && (lines[i].Trim().StartsWith('>') || (!string.IsNullOrWhiteSpace(lines[i]) &&
                           quoteLines.Count > 0 && !lines[i].Trim().StartsWith('#'))))
                {
                    string qLine = lines[i].Trim();
                    if (qLine.StartsWith('>'))
                    {
                        qLine = qLine.Substring(1).TrimStart();
                    }

                    quoteLines.Add(qLine);
                    i++;
                }

                var quoteBorder = CreateBlockquote(string.Join("\n", quoteLines), highlightPhrase);
                mainStack.Children.Add(quoteBorder);
                continue;
            }

            // 5. Tables (| col | col |)
            if (trimmed.StartsWith('|') && trimmed.EndsWith('|') && trimmed.Contains('|'))
            {
                var tableLines = new List<string>();
                while (i < lines.Length && lines[i].Trim().StartsWith('|') && lines[i].Trim().EndsWith('|'))
                {
                    tableLines.Add(lines[i].Trim());
                    i++;
                }

                if (tableLines.Count >= 2 && tableLines[1].Contains("---"))
                {
                    var tableControl = CreateTable(tableLines, highlightPhrase);
                    mainStack.Children.Add(tableControl);
                    continue;
                }
                else
                {
                    // Not a valid table, rewind and treat as paragraph
                    i -= tableLines.Count;
                }
            }

            // 6. Lists (unordered: -, *, + or ordered: 1., 2., etc.)
            if (IsListItem(line))
            {
                var listContainer = new StackPanel { Spacing = 4, Margin = new Thickness(0, 2, 8, 6) };
                while (i < lines.Length && (IsListItem(lines[i]) || (string.IsNullOrWhiteSpace(lines[i]) &&
                                                                     i + 1 < lines.Length && IsListItem(lines[i + 1]))))
                {
                    if (string.IsNullOrWhiteSpace(lines[i]))
                    {
                        i++;
                        continue;
                    }

                    string itemLine = lines[i];
                    int indent = GetIndentLevel(itemLine);
                    string itemTrimmed = itemLine.TrimStart();
                    bool isOrdered = char.IsDigit(itemTrimmed[0]);

                    string bulletText = "•";
                    string itemContent = itemTrimmed;

                    if (isOrdered)
                    {
                        int dotIdx = itemTrimmed.IndexOf('.');
                        if (dotIdx > 0)
                        {
                            bulletText = itemTrimmed.Substring(0, dotIdx + 1);
                            itemContent = itemTrimmed.Substring(dotIdx + 1).TrimStart();
                        }
                    }
                    else if (itemTrimmed.Length >= 2 &&
                             (itemTrimmed[0] == '-' || itemTrimmed[0] == '*' || itemTrimmed[0] == '+'))
                    {
                        itemContent = itemTrimmed.Substring(1).TrimStart();
                    }

                    var itemGrid = new Grid
                    {
                        ColumnDefinitions = new ColumnDefinitions("Auto,*"),
                        Margin = new Thickness(indent * 16, 1, 0, 1)
                    };

                    var bulletTb = new TextBlock
                    {
                        Text = bulletText,
                        Foreground = AccentBrush,
                        FontWeight = isOrdered ? FontWeight.Normal : FontWeight.Bold,
                        Margin = new Thickness(0, 0, 8, 0),
                        VerticalAlignment = VerticalAlignment.Top
                    };
                    Grid.SetColumn(bulletTb, 0);
                    itemGrid.Children.Add(bulletTb);

                    var contentTb = CreateInlineTextBlock(itemContent, highlightPhrase);
                    Grid.SetColumn(contentTb, 1);
                    itemGrid.Children.Add(contentTb);

                    listContainer.Children.Add(itemGrid);
                    i++;
                }

                mainStack.Children.Add(listContainer);
                continue;
            }

            // 7. Regular paragraph / text
            if (string.IsNullOrWhiteSpace(trimmed))
            {
                i++;
                continue;
            }

            var paraLines = new List<string>();
            while (i < lines.Length &&
                   !string.IsNullOrWhiteSpace(lines[i]) &&
                   !lines[i].Trim().StartsWith('#') &&
                   !lines[i].Trim().StartsWith("```", StringComparison.Ordinal) &&
                   !lines[i].Trim().StartsWith('>') &&
                   !IsHorizontalRule(lines[i].Trim()) &&
                   !IsListItem(lines[i]) &&
                   !(lines[i].Trim().StartsWith('|') && lines[i].Trim().EndsWith('|')))
            {
                paraLines.Add(lines[i]);
                i++;
            }

            if (paraLines.Count > 0)
            {
                string paragraphText = string.Join(" ", paraLines);
                var tb = CreateInlineTextBlock(paragraphText, highlightPhrase);
                tb.Margin = new Thickness(0, 0, 8, 4);
                mainStack.Children.Add(tb);
            }
        }

        return mainStack;
    }

    private static bool IsHorizontalRule(string trimmed)
    {
        if (trimmed.Length < 3) return false;
        if (trimmed == "---" || trimmed == "***" || trimmed == "___") return true;
        bool allDash = true, allStar = true, allUnderscore = true;
        foreach (char c in trimmed)
        {
            if (c != '-') allDash = false;
            if (c != '*') allStar = false;
            if (c != '_') allUnderscore = false;
        }

        return allDash || allStar || allUnderscore;
    }

    private static bool IsListItem(string line)
    {
        string t = line.TrimStart();
        if (string.IsNullOrEmpty(t)) return false;
        if ((t.StartsWith("- ") || t.StartsWith("* ") || t.StartsWith("+ ")) && t.Length >= 2) return true;
        if (char.IsDigit(t[0]))
        {
            int dot = t.IndexOf('.');
            if (dot > 0 && dot < 5 && t.Length > dot + 1 && t[dot + 1] == ' ') return true;
        }

        return false;
    }

    private static int GetIndentLevel(string line)
    {
        int spaces = 0;
        foreach (char c in line)
        {
            if (c == ' ') spaces++;
            else if (c == '\t') spaces += 4;
            else break;
        }

        return spaces / 2;
    }

    private static Control CreateHeading(string text, int level, string? highlightPhrase = null)
    {
        double fontSize = level switch
        {
            1 => 20,
            2 => 17,
            3 => 15,
            4 => 13.5,
            5 => 12.5,
            _ => 11.5
        };

        IBrush fg = level switch
        {
            1 => AccentBrush,
            2 => HeaderBrush,
            3 => HeaderBrush,
            _ => TextBrush
        };

        var tb = CreateInlineTextBlock(text, highlightPhrase, fg);
        tb.FontSize = fontSize;
        tb.FontWeight = FontWeight.Bold;
        tb.Margin = new Thickness(0, level == 1 ? 8 : 4, 8, 4);
        return tb;
    }

    private static Control CreateCodeBlock(string code, string? lang, string? highlightPhrase = null)
    {
        var border = new Border
        {
            Background = CodeBgBrush,
            BorderBrush = BorderSubtleBrush,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(5),
            Padding = new Thickness(12, 8),
            Margin = new Thickness(0, 4, 8, 8)
        };

        var stack = new StackPanel { Spacing = 6 };

        if (!string.IsNullOrWhiteSpace(lang))
        {
            var langTb = new TextBlock
            {
                Text = lang.ToUpperInvariant(),
                FontSize = 10,
                FontWeight = FontWeight.SemiBold,
                Foreground = AccentBrush,
                Opacity = 0.8,
                HorizontalAlignment = HorizontalAlignment.Right
            };
            stack.Children.Add(langTb);
        }

        var codeFg = new SolidColorBrush(Color.Parse("#DCDCDC"));
        var codeTb = new TextBlock
        {
            FontFamily = MonospaceFont,
            Foreground = codeFg,
            FontSize = 12.5,
            TextWrapping = TextWrapping.Wrap
        };
        if (codeTb.Inlines != null)
        {
            AddHighlightedRuns(codeTb.Inlines, code, highlightPhrase, codeFg, null, null, MonospaceFont, 12.5);
        }
        else
        {
            codeTb.Text = code;
        }

        stack.Children.Add(codeTb);

        border.Child = stack;
        return border;
    }

    private static Control CreateBlockquote(string text, string? highlightPhrase = null)
    {
        var border = new Border
        {
            Background = QuoteBgBrush,
            BorderBrush = QuoteBorderBrush,
            BorderThickness = new Thickness(3, 0, 0, 0),
            CornerRadius = new CornerRadius(3),
            Padding = new Thickness(12, 8),
            Margin = new Thickness(0, 4, 8, 8)
        };

        var content = RenderToControl(text, highlightPhrase);
        border.Child = content;
        return border;
    }

    private static Control CreateTable(List<string> tableLines, string? highlightPhrase = null)
    {
        var border = new Border
        {
            Background = CodeBgBrush,
            BorderBrush = BorderSubtleBrush,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(4),
            Margin = new Thickness(0, 4, 8, 8)
        };

        var headers = ParseTableRow(tableLines[0]);
        int colCount = headers.Count;
        if (colCount == 0) return border;

        var grid = new Grid();
        for (int c = 0; c < colCount; c++)
        {
            grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
        }

        int rowCount = 0;
        // Header row
        grid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
        for (int c = 0; c < colCount; c++)
        {
            var cellBorder = new Border
            {
                Background = TableHeaderBgBrush,
                BorderBrush = BorderSubtleBrush,
                BorderThickness = new Thickness(0, 0, c < colCount - 1 ? 1 : 0, 1),
                Padding = new Thickness(8, 6)
            };
            var hTb = CreateInlineTextBlock(headers[c], highlightPhrase, HeaderBrush);
            hTb.FontWeight = FontWeight.Bold;
            cellBorder.Child = hTb;

            Grid.SetRow(cellBorder, 0);
            Grid.SetColumn(cellBorder, c);
            grid.Children.Add(cellBorder);
        }

        rowCount++;

        // Data rows (skip index 1 which is separator ---)
        for (int r = 2; r < tableLines.Count; r++)
        {
            var cells = ParseTableRow(tableLines[r]);
            grid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
            var rowBg = (r % 2 == 1) ? TableAltRowBgBrush : Brushes.Transparent;

            for (int c = 0; c < colCount; c++)
            {
                string cellText = c < cells.Count ? cells[c] : string.Empty;
                var cellBorder = new Border
                {
                    Background = rowBg,
                    BorderBrush = BorderSubtleBrush,
                    BorderThickness = new Thickness(0, 0, c < colCount - 1 ? 1 : 0, r < tableLines.Count - 1 ? 1 : 0),
                    Padding = new Thickness(8, 5)
                };
                var cTb = CreateInlineTextBlock(cellText, highlightPhrase, TextBrush);
                cellBorder.Child = cTb;

                Grid.SetRow(cellBorder, rowCount);
                Grid.SetColumn(cellBorder, c);
                grid.Children.Add(cellBorder);
            }

            rowCount++;
        }

        border.Child = grid;
        return border;
    }

    private static List<string> ParseTableRow(string line)
    {
        string trimmed = line.Trim();
        if (trimmed.StartsWith('|')) trimmed = trimmed.Substring(1);
        if (trimmed.EndsWith('|')) trimmed = trimmed.Substring(0, trimmed.Length - 1);

        string[] parts = trimmed.Split('|');
        var list = new List<string>();
        foreach (var p in parts)
        {
            list.Add(p.Trim());
        }

        return list;
    }

    public static TextBlock CreateInlineTextBlock(string rawText, string? highlightPhrase = null,
        IBrush? defaultForeground = null)
    {
        var tb = new TextBlock
        {
            TextWrapping = TextWrapping.Wrap,
            Foreground = defaultForeground ?? TextBrush,
            FontSize = 13
        };

        if (string.IsNullOrEmpty(rawText))
        {
            return tb;
        }

        if (tb.Inlines != null)
        {
            ParseInlines(rawText, tb.Inlines, highlightPhrase, defaultForeground ?? TextBrush);
        }
        else
        {
            tb.Text = rawText;
        }

        return tb;
    }

    private static readonly Regex InlinePattern = new(
        @"(?<Code>`[^`]+`)|(?<BoldItalic>(\*\*\*|___)[^*_]+(\*\*\*|___))|(?<Bold>(\*\*|__)[^*_]+(\*\*|__))|(?<Italic>(\*|_)[^*_]+(\*|_))|(?<Strike>~~[^~]+~~)|(?<Link>\[(?<LinkText>[^\]]+)\]\((?<LinkUrl>[^)]+)\))",
        RegexOptions.Compiled);

    public static void ParseInlines(string text, InlineCollection inlines, string? highlightPhrase = null,
        IBrush? defaultForeground = null)
    {
        if (string.IsNullOrEmpty(text)) return;

        IBrush fg = defaultForeground ?? TextBrush;

        int lastIndex = 0;
        var matches = InlinePattern.Matches(text);

        foreach (Match match in matches)
        {
            if (match.Index > lastIndex)
            {
                string plainText = text.Substring(lastIndex, match.Index - lastIndex);
                AddHighlightedRuns(inlines, plainText, highlightPhrase, fg);
            }

            if (match.Groups["Code"].Success)
            {
                string codeVal = match.Value.Substring(1, match.Value.Length - 2);
                AddHighlightedRuns(inlines, codeVal, highlightPhrase, AccentBrush, InlineCodeBgBrush, null,
                    MonospaceFont, 12);
            }
            else if (match.Groups["BoldItalic"].Success)
            {
                string biVal = match.Value.Substring(3, match.Value.Length - 6);
                var boldSpan = new Bold();
                var italicSpan = new Italic();
                AddHighlightedRuns(italicSpan.Inlines, biVal, highlightPhrase, HeaderBrush);
                boldSpan.Inlines.Add(italicSpan);
                inlines.Add(boldSpan);
            }
            else if (match.Groups["Bold"].Success)
            {
                string bVal = match.Value.Substring(2, match.Value.Length - 4);
                var boldSpan = new Bold();
                AddHighlightedRuns(boldSpan.Inlines, bVal, highlightPhrase, HeaderBrush);
                inlines.Add(boldSpan);
            }
            else if (match.Groups["Italic"].Success)
            {
                string iVal = match.Value.Substring(1, match.Value.Length - 2);
                var italicSpan = new Italic();
                AddHighlightedRuns(italicSpan.Inlines, iVal, highlightPhrase, fg);
                inlines.Add(italicSpan);
            }
            else if (match.Groups["Strike"].Success)
            {
                string sVal = match.Value.Substring(2, match.Value.Length - 4);
                AddHighlightedRuns(inlines, sVal, highlightPhrase, fg, null, null, null, null,
                    TextDecorations.Strikethrough);
            }
            else if (match.Groups["Link"].Success)
            {
                string linkText = match.Groups["LinkText"].Value;
                string linkUrl = match.Groups["LinkUrl"].Value;
                var linkSpan = new Underline();
                AddHighlightedRuns(linkSpan.Inlines, linkText, highlightPhrase, LinkBrush);
                inlines.Add(linkSpan);
            }

            lastIndex = match.Index + match.Length;
        }

        if (lastIndex < text.Length)
        {
            string remaining = text.Substring(lastIndex);
            AddHighlightedRuns(inlines, remaining, highlightPhrase, fg);
        }
    }

    public static void AddHighlightedRuns(
        InlineCollection inlines,
        string text,
        string? highlightPhrase,
        IBrush defaultForeground,
        IBrush? defaultBackground = null,
        FontWeight? defaultFontWeight = null,
        FontFamily? defaultFontFamily = null,
        double? fontSize = null,
        TextDecorationCollection? textDecorations = null,
        FontStyle? fontStyle = null)
    {
        if (string.IsNullOrEmpty(text)) return;

        if (string.IsNullOrWhiteSpace(highlightPhrase))
        {
            var run = new Run(text)
            {
                Foreground = defaultForeground,
                Background = defaultBackground,
                FontWeight = defaultFontWeight ?? FontWeight.Normal,
                FontFamily = defaultFontFamily ?? FontFamily.Default,
                FontStyle = fontStyle ?? FontStyle.Normal,
                TextDecorations = textDecorations
            };
            if (fontSize.HasValue) run.FontSize = fontSize.Value;
            inlines.Add(run);
            return;
        }

        var terms = GetHighlightTerms(highlightPhrase);
        if (terms.Count == 0)
        {
            var run = new Run(text)
            {
                Foreground = defaultForeground,
                Background = defaultBackground,
                FontWeight = defaultFontWeight ?? FontWeight.Normal,
                FontFamily = defaultFontFamily ?? FontFamily.Default,
                FontStyle = fontStyle ?? FontStyle.Normal,
                TextDecorations = textDecorations
            };
            if (fontSize.HasValue) run.FontSize = fontSize.Value;
            inlines.Add(run);
            return;
        }

        string pattern = string.Join("|", terms.Select(BuildTermPattern));
        var matches = Regex.Matches(text, pattern, RegexOptions.IgnoreCase);

        int lastIndex = 0;
        foreach (Match match in matches)
        {
            if (match.Index > lastIndex)
            {
                string unhighlighted = text.Substring(lastIndex, match.Index - lastIndex);
                var run = new Run(unhighlighted)
                {
                    Foreground = defaultForeground,
                    Background = defaultBackground,
                    FontWeight = defaultFontWeight ?? FontWeight.Normal,
                    FontFamily = defaultFontFamily ?? FontFamily.Default,
                    FontStyle = fontStyle ?? FontStyle.Normal,
                    TextDecorations = textDecorations
                };
                if (fontSize.HasValue) run.FontSize = fontSize.Value;
                inlines.Add(run);
            }

            string highlightedText = match.Value;
            var hRun = new Run(highlightedText)
            {
                Foreground = HighlightFgBrush,
                Background = HighlightBgBrush,
                FontWeight = FontWeight.Bold,
                FontFamily = defaultFontFamily ?? FontFamily.Default,
                FontStyle = fontStyle ?? FontStyle.Normal,
                TextDecorations = textDecorations
            };
            if (fontSize.HasValue) hRun.FontSize = fontSize.Value;
            inlines.Add(hRun);

            lastIndex = match.Index + match.Length;
        }

        if (lastIndex < text.Length)
        {
            string remaining = text.Substring(lastIndex);
            var run = new Run(remaining)
            {
                Foreground = defaultForeground,
                Background = defaultBackground,
                FontWeight = defaultFontWeight ?? FontWeight.Normal,
                FontFamily = defaultFontFamily ?? FontFamily.Default,
                FontStyle = fontStyle ?? FontStyle.Normal,
                TextDecorations = textDecorations
            };
            if (fontSize.HasValue) run.FontSize = fontSize.Value;
            inlines.Add(run);
        }
    }

    private static List<string> GetHighlightTerms(string highlightPhrase)
    {
        var terms = new List<string>();
        string trimmed = highlightPhrase.Trim();
        if (string.IsNullOrEmpty(trimmed)) return terms;

        terms.Add(trimmed);

        string[] words = trimmed.Split(new[] { ' ', '\t', ',', ';', '-', '_' },
            StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        foreach (var w in words)
        {
            if (w.Length > 0 && !terms.Contains(w, StringComparer.OrdinalIgnoreCase))
            {
                terms.Add(w);
            }
        }

        return terms.OrderByDescending(t => t.Length).ToList();
    }

    private static string BuildTermPattern(string term)
    {
        return Regex.Escape(term);
    }
}