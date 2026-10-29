using System.IO;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Media;
using GTFO_GUIEditor.Models;
using GTFO_GUIEditor.Services;
using GTFO_GUIEditor.ViewModels;
using Xunit;

namespace GTFO_GUIEditor.Tests;

public class DocumentationTests
{
    [Fact]
    public void DocumentationService_FindDocumentationFile_LocatesFile()
    {
        string? file = DocumentationService.FindDocumentationFile();
        Assert.NotNull(file);
        Assert.True(File.Exists(file));
        Assert.EndsWith(".md", file);
    }

    [Fact]
    public void DocumentationService_ParseDocument_SegmentsH1H2H3Properly()
    {
        string markdown = @"# Top Level 1
Intro text for top level 1.

## Section 1A
Text for section 1A.

### Subsection 1A1
Text for 1A1.

### Subsection 1A2
Text for 1A2.

## Section 1B
Text for section 1B.

# Top Level 2
Text for top level 2 without sub-sections.";

        var sections = DocumentationService.ParseDocument(markdown);

        Assert.Equal(2, sections.Count);

        // Top Level 1
        var top1 = sections[0];
        Assert.Equal("Top Level 1", top1.Title);
        Assert.Equal(1, top1.Level);
        Assert.True(top1.HasSubSections);
        Assert.Equal(3, top1.SubSections.Count); // Overview, Section 1A, Section 1B

        // Overview sub-tab for top 1
        var top1Overview = top1.SubSections[0];
        Assert.Equal("Overview", top1Overview.Title);
        Assert.Equal("Intro text for top level 1.", top1Overview.Content);

        // Section 1A
        var sec1A = top1.SubSections[1];
        Assert.Equal("Section 1A", sec1A.Title);
        Assert.Equal(2, sec1A.Level);
        Assert.True(sec1A.HasSubSections);
        Assert.Equal(3, sec1A.SubSections.Count); // Overview, Subsection 1A1, Subsection 1A2

        var sec1AOverview = sec1A.SubSections[0];
        Assert.Equal("Overview", sec1AOverview.Title);
        Assert.Equal("Text for section 1A.", sec1AOverview.Content);

        var sub1A1 = sec1A.SubSections[1];
        Assert.Equal("Subsection 1A1", sub1A1.Title);
        Assert.Equal(3, sub1A1.Level);
        Assert.Equal("Text for 1A1.", sub1A1.Content);

        var sub1A2 = sec1A.SubSections[2];
        Assert.Equal("Subsection 1A2", sub1A2.Title);
        Assert.Equal(3, sub1A2.Level);
        Assert.Equal("Text for 1A2.", sub1A2.Content);

        // Section 1B
        var sec1B = top1.SubSections[2];
        Assert.Equal("Section 1B", sec1B.Title);
        Assert.Equal(2, sec1B.Level);
        Assert.False(sec1B.HasSubSections);
        Assert.Equal("Text for section 1B.", sec1B.Content);

        // Top Level 2
        var top2 = sections[1];
        Assert.Equal("Top Level 2", top2.Title);
        Assert.Equal(1, top2.Level);
        Assert.False(top2.HasSubSections);
        Assert.Equal("Text for top level 2 without sub-sections.", top2.Content);
    }

    [Fact]
    public void DocumentationService_ParseDocument_IgnoresHeadersInsideCodeBlocks()
    {
        string markdown = @"# Header One
Some text.

```csharp
# This is a comment or directive, not a header
## Not a subheader
### Not a sub-subheader
```

End of section.";

        var sections = DocumentationService.ParseDocument(markdown);

        Assert.Single(sections);
        var sec = sections[0];
        Assert.Equal("Header One", sec.Title);
        Assert.False(sec.HasSubSections);
        Assert.Contains("# This is a comment", sec.Content);
    }

    [Fact]
    public void DocumentationService_ParseDocument_HandlesNoHeadersOrIntroText()
    {
        string markdown = "Introductory text before anything else.\n\n# Main Tab\nMain content.";

        var sections = DocumentationService.ParseDocument(markdown);

        Assert.Equal(2, sections.Count);
        Assert.Equal("Overview", sections[0].Title);
        Assert.Equal("Introductory text before anything else.", sections[0].Content);
        Assert.Equal("Main Tab", sections[1].Title);
        Assert.Equal("Main content.", sections[1].Content);
    }

    [Fact]
    public void DocumentationService_ParseDocument_HandlesDocumentWithOnlyH2()
    {
        string markdown = @"## Subtopic 1
Content 1.

## Subtopic 2
Content 2.";

        var sections = DocumentationService.ParseDocument(markdown);

        Assert.Single(sections);
        var root = sections[0];
        Assert.Equal("Documentation", root.Title);
        Assert.Equal(2, root.SubSections.Count);
        Assert.Equal("Subtopic 1", root.SubSections[0].Title);
        Assert.Equal("Subtopic 2", root.SubSections[1].Title);
    }

    [Fact]
    public void MarkdownRenderer_RenderToControl_RendersBasicMarkdownElements()
    {
        string markdown = @"# Main Header
## Sub Header
### Smaller Header

This is a regular paragraph with **bold text**, *italic text*, ***bold italic***, `inline code`, ~~strikethrough~~, and [Link](https://example.com).

> This is a blockquote.

- Unordered item 1
- Unordered item 2
  - Nested item

1. Ordered item 1
2. Ordered item 2

---

```json
{
  ""Key"": ""Value""
}
```

| Header 1 | Header 2 |
| --- | --- |
| Row 1 Col 1 | Row 1 Col 2 |
| Row 2 Col 1 | Row 2 Col 2 |
";

        var control = MarkdownRenderer.RenderToControl(markdown);
        Assert.NotNull(control);
        Assert.IsType<StackPanel>(control);

        var stack = (StackPanel)control;
        Assert.NotEmpty(stack.Children);
    }

    [Fact]
    public void MarkdownRenderer_ParseInlines_CorrectlyParsesFormattedText()
    {
        var inlines = new InlineCollection();
        string text = "Plain **bold** and *italic* and `code` and ~~strike~~ and [link text](url)";

        MarkdownRenderer.ParseInlines(text, inlines);

        Assert.NotEmpty(inlines);
        Assert.Contains(inlines, i => i is Bold);
        Assert.Contains(inlines, i => i is Italic);
        Assert.Contains(inlines, i => i is Run r && r.Background != null); // code run
        Assert.Contains(inlines, i => i is Run r && r.TextDecorations != null); // strike run
        Assert.Contains(inlines, i => i is Underline); // link
    }

    [Fact]
    public void DocumentationViewModel_LoadsDefaultDocumentation()
    {
        var vm = new DocumentationViewModel();

        Assert.True(vm.HasSections);
        Assert.NotEmpty(vm.Sections);
        Assert.NotNull(vm.SelectedSection);
        Assert.False(string.IsNullOrWhiteSpace(vm.SourceFilePath));

        // SelectedSection should have rendered control
        var selected = vm.SelectedSection;
        if (selected.HasSubSections)
        {
            var sub = selected.SelectedSubSection;
            Assert.NotNull(sub);
            if (!string.IsNullOrWhiteSpace(sub.Content))
            {
                Assert.NotNull(sub.RenderedContent);
            }
        }
        else
        {
            Assert.NotNull(selected.RenderedContent);
        }
    }

    [Fact]
    public void MainWindowViewModel_InitializesDocumentationViewModel()
    {
        var mainVm = new MainWindowViewModel(autoReopenLastRundown: false);

        Assert.NotNull(mainVm.Documentation);
        Assert.True(mainVm.Documentation.HasSections);
    }

    [Fact]
    public void DocumentationService_ExtractParagraphs_SplitsBlocksProperly()
    {
        string content = @"First paragraph with some text.
Still first paragraph.

Second paragraph after blank line.

Third paragraph with **formatting** and table:
| A | B |
| - | - |
| 1 | 2 |";

        var paragraphs = DocumentationService.ExtractParagraphs(content);

        Assert.Equal(3, paragraphs.Count);
        Assert.StartsWith("First paragraph", paragraphs[0]);
        Assert.Equal("Second paragraph after blank line.", paragraphs[1]);
        Assert.StartsWith("Third paragraph", paragraphs[2]);
        Assert.Contains("| A | B |", paragraphs[2]);
    }

    [Fact]
    public void DocumentationService_Search_FindsMatchingPagesAndParagraphs()
    {
        string markdown = @"# Home
## Introduction
Welcome to the level editor. You can configure doors and bulkheads here.

## Navigation
Use Ctrl+N to create a new rundown. Use Ctrl+S to save.

# Security
## Alarms
Alarms are attached to security doors. They trigger waves of enemies.";

        var sections = DocumentationService.ParseDocument(markdown);
        var results = DocumentationService.Search(sections, "security doors");

        Assert.NotEmpty(results);
        var alarmResult = results.FirstOrDefault(r => r.Title == "Alarms");
        Assert.NotNull(alarmResult);
        Assert.Equal("Security > Alarms", alarmResult.Breadcrumb);
        Assert.NotEmpty(alarmResult.MatchingParagraphs);
        Assert.Contains("security doors", alarmResult.MatchingParagraphs[0].Text);
    }

    [Fact]
    public void DocumentationService_Search_IsCaseInsensitiveAndHandlesMultipleKeywords()
    {
        string markdown = @"# Level Layout
## Sectors
Main sector contains Zone 0. Bulkhead Keys can spawn here.

## Bulkhead Routing
Security doors link Bulkhead Controllers (DCs) to sectors.";

        var sections = DocumentationService.ParseDocument(markdown);
        var results = DocumentationService.Search(sections, "BULKHEAD keys");

        Assert.Single(results);
        Assert.Equal("Sectors", results[0].Title);
        Assert.Equal("Level Layout > Sectors", results[0].Breadcrumb);
        Assert.Single(results[0].MatchingParagraphs);
        Assert.Contains("Bulkhead Keys", results[0].MatchingParagraphs[0].Text);
    }

    [Fact]
    public void DocumentationService_Search_ReturnsEmptyForNonExistentKeywords()
    {
        string markdown = @"# Welcome
Some introductory text.";

        var sections = DocumentationService.ParseDocument(markdown);
        var results = DocumentationService.Search(sections, "nonexistentkeyword12345");

        Assert.Empty(results);
    }

    [Fact]
    public void DocumentationViewModel_SearchTab_IsPresentAndDisplayTabsIncludeSearch()
    {
        var vm = new DocumentationViewModel();

        Assert.NotNull(vm.SearchSection);
        Assert.True(vm.SearchSection.IsSearchTab);
        Assert.Equal("Search", vm.SearchSection.Title);
        Assert.Contains(vm.SearchSection, vm.DisplayTabs);
        Assert.True(vm.DisplayTabs.Count > vm.Sections.Count);
    }

    [Fact]
    public void DocumentationViewModel_PerformSearch_UpdatesSearchResultsAndStatus()
    {
        var vm = new DocumentationViewModel();

        vm.SearchQuery = "Shortcuts";

        Assert.True(vm.HasSearchQuery);
        Assert.True(vm.HasSearched);
        Assert.True(vm.HasSearchResults);
        Assert.NotEmpty(vm.SearchResults);
        Assert.False(string.IsNullOrWhiteSpace(vm.SearchStatusMessage));
        Assert.Contains("matching", vm.SearchStatusMessage);

        // Clear search
        vm.ClearSearch();
        Assert.False(vm.HasSearchQuery);
        Assert.False(vm.HasSearched);
        Assert.False(vm.HasSearchResults);
        Assert.Empty(vm.SearchResults);
        Assert.True(string.IsNullOrWhiteSpace(vm.SearchStatusMessage));
    }

    [Fact]
    public void DocumentationViewModel_DefaultSelectedSection_IsWelcomeOrHomeNotSearch()
    {
        var vm = new DocumentationViewModel();

        Assert.NotNull(vm.SelectedSection);
        Assert.False(vm.SelectedSection.IsSearchTab);
        Assert.True(vm.SelectedSection.Title == "Welcome" || vm.SelectedSection.Title == "Home");
        Assert.Contains(vm.DisplayTabs, t => t.IsSearchTab);
        Assert.Contains(vm.DisplayTabs, t => t.Title == "Welcome" || t.Title == "Home");
    }

    [Fact]
    public void DocumentationViewModel_NavigateToSearchResult_SelectsTargetTabAndSubTabs()
    {
        string markdown = @"# Welcome
Welcome content.

# Level Layout
## Introduction
Intro content.

## Sectors & Settings
### Bulkhead Setup
Configuring bulkhead doors and keys.";

        var sections = DocumentationService.ParseDocument(markdown);
        var vm = new DocumentationViewModel();
        vm.Sections = sections;

        // Search for bulkhead
        vm.SearchQuery = "bulkhead";
        Assert.NotEmpty(vm.SearchResults);

        var targetResult = vm.SearchResults.FirstOrDefault(r => r.Title == "Bulkhead Setup" || r.Breadcrumb.Contains("Bulkhead Setup"));
        Assert.NotNull(targetResult);

        // Trigger navigation via result
        targetResult.Navigate();

        // Verify active selection
        Assert.Equal("Level Layout", vm.SelectedSection?.Title);
        Assert.Equal("Sectors & Settings", vm.SelectedSection?.SelectedSubSection?.Title);
        Assert.Equal("Bulkhead Setup", vm.SelectedSection?.SelectedSubSection?.SelectedSubSection?.Title);
    }

    [Fact]
    public void MarkdownRenderer_HighlightPhrase_AppliesHighlightBrushToMatchingRuns()
    {
        var inlines = new InlineCollection();
        string text = "Configure Points Per Wave (Start) and Points Per Wave (End) correctly.";

        MarkdownRenderer.ParseInlines(text, inlines, "Points Per Wave");

        Assert.NotEmpty(inlines);
        var highlightedRuns = inlines.OfType<Run>().Where(r => r.Background != null && r.FontWeight == FontWeight.Bold).ToList();
        Assert.Equal(2, highlightedRuns.Count);
        Assert.All(highlightedRuns, r => Assert.Equal("Points Per Wave", r.Text));
    }

    [Fact]
    public void MarkdownRenderer_HighlightPhrase_InBoldAndItalic_HighlightsNestedRuns()
    {
        var inlines = new InlineCollection();
        string text = "Here is **important elevator** information and *elevator tiles*.";

        MarkdownRenderer.ParseInlines(text, inlines, "elevator");

        Assert.NotEmpty(inlines);
        var boldSpan = inlines.OfType<Bold>().FirstOrDefault();
        Assert.NotNull(boldSpan);
        var highlightedInBold = boldSpan.Inlines.OfType<Run>().FirstOrDefault(r => r.Text == "elevator");
        Assert.NotNull(highlightedInBold);
        Assert.NotNull(highlightedInBold.Background);

        var italicSpan = inlines.OfType<Italic>().FirstOrDefault();
        Assert.NotNull(italicSpan);
        var highlightedInItalic = italicSpan.Inlines.OfType<Run>().FirstOrDefault(r => r.Text == "elevator");
        Assert.NotNull(highlightedInItalic);
        Assert.NotNull(highlightedInItalic.Background);
    }

    [Fact]
    public void DocumentationService_Search_PopulatesSearchQueryAndHighlighting()
    {
        string markdown = @"# Alarms
## Settings
Configure Points Per Wave for surging alarms.";

        var sections = DocumentationService.ParseDocument(markdown);
        var results = DocumentationService.Search(sections, "Points Per Wave");

        Assert.Single(results);
        var result = results[0];
        Assert.Equal("Points Per Wave", result.SearchQuery);
        Assert.NotNull(result.RenderedTitle);

        Assert.Single(result.MatchingParagraphs);
        var para = result.MatchingParagraphs[0];
        Assert.Equal("Points Per Wave", para.HighlightQuery);

        var rendered = para.RenderedContent as StackPanel;
        Assert.NotNull(rendered);
        Assert.NotEmpty(rendered.Children);
        var tb = rendered.Children.OfType<TextBlock>().FirstOrDefault();
        Assert.NotNull(tb);
        Assert.NotNull(tb.Inlines);
        Assert.Contains(tb.Inlines, i => i is Run r && r.Text == "Points Per Wave" && r.Background != null);
    }

    [Fact]
    public void MarkdownRenderer_HighlightSubstring_HighlightsMatchesWithinLongerWords()
    {
        var inlines = new InlineCollection();
        string text = "Gather artifacts and find the ancient Artifact item.";

        MarkdownRenderer.ParseInlines(text, inlines, "Artifact");

        Assert.NotEmpty(inlines);
        var highlightedRuns = inlines.OfType<Run>().Where(r => r.Background != null && r.FontWeight == FontWeight.Bold).ToList();
        Assert.Equal(2, highlightedRuns.Count);
        Assert.Equal("artifact", highlightedRuns[0].Text, ignoreCase: true);
        Assert.Equal("Artifact", highlightedRuns[1].Text, ignoreCase: true);
    }
}
