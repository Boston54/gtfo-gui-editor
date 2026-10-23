using System.IO;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
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
}
