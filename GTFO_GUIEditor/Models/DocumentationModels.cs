using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using Avalonia.Media;
using GTFO_GUIEditor.Services;
using GTFO_GUIEditor.ViewModels;

namespace GTFO_GUIEditor.Models;

public class DocSection : ViewModelBase
{
    private string _title = string.Empty;
    private string _content = string.Empty;
    private int _level = 1;
    private ObservableCollection<DocSection> _subSections = new();
    private DocSection? _selectedSubSection;
    private object? _renderedContent;
    private bool _isSearchTab;

    public string Title
    {
        get => _title;
        set => SetField(ref _title, value);
    }

    public string Content
    {
        get => _content;
        set
        {
            if (SetField(ref _content, value))
            {
                _renderedContent = null;
                OnPropertyChanged(nameof(RenderedContent));
            }
        }
    }

    public int Level
    {
        get => _level;
        set => SetField(ref _level, value);
    }

    public bool IsSearchTab
    {
        get => _isSearchTab;
        set => SetField(ref _isSearchTab, value);
    }

    public ObservableCollection<DocSection> SubSections
    {
        get => _subSections;
        set
        {
            if (SetField(ref _subSections, value))
            {
                OnPropertyChanged(nameof(HasSubSections));
            }
        }
    }

    public bool HasSubSections => _subSections != null && _subSections.Count > 0;

    public DocSection? SelectedSubSection
    {
        get => _selectedSubSection ?? _subSections?.FirstOrDefault();
        set => SetField(ref _selectedSubSection, value);
    }

    public object? RenderedContent
    {
        get
        {
            if (_renderedContent == null && !string.IsNullOrWhiteSpace(_content))
            {
                _renderedContent = MarkdownRenderer.RenderToControl(_content);
            }

            return _renderedContent;
        }
    }

    public override string ToString() => $"[H{Level}] {Title} ({SubSections.Count} sub-sections)";
}

public class DocSearchParagraph : ViewModelBase
{
    private string _text = string.Empty;
    private string? _highlightQuery;
    private object? _renderedContent;

    public string Text
    {
        get => _text;
        set
        {
            if (SetField(ref _text, value))
            {
                _renderedContent = null;
                OnPropertyChanged(nameof(RenderedContent));
            }
        }
    }

    public string? HighlightQuery
    {
        get => _highlightQuery;
        set
        {
            if (SetField(ref _highlightQuery, value))
            {
                _renderedContent = null;
                OnPropertyChanged(nameof(RenderedContent));
            }
        }
    }

    public object? RenderedContent
    {
        get
        {
            if (_renderedContent == null && !string.IsNullOrWhiteSpace(_text))
            {
                _renderedContent = MarkdownRenderer.RenderToControl(_text, _highlightQuery);
            }

            return _renderedContent;
        }
    }
}

public class DocSearchResult : ViewModelBase
{
    private readonly Action<DocSearchResult>? _onNavigate;
    private string _title = string.Empty;
    private string _breadcrumb = string.Empty;
    private string? _searchQuery;
    private object? _renderedTitle;

    public string Title
    {
        get => _title;
        set
        {
            if (SetField(ref _title, value))
            {
                _renderedTitle = null;
                OnPropertyChanged(nameof(DisplayTitle));
                OnPropertyChanged(nameof(RenderedTitle));
            }
        }
    }

    public string Breadcrumb
    {
        get => _breadcrumb;
        set
        {
            if (SetField(ref _breadcrumb, value))
            {
                _renderedTitle = null;
                OnPropertyChanged(nameof(DisplayTitle));
                OnPropertyChanged(nameof(RenderedTitle));
            }
        }
    }

    public string? SearchQuery
    {
        get => _searchQuery;
        set
        {
            if (SetField(ref _searchQuery, value))
            {
                _renderedTitle = null;
                OnPropertyChanged(nameof(RenderedTitle));
            }
        }
    }

    public string DisplayTitle => string.IsNullOrWhiteSpace(Breadcrumb) ? Title : Breadcrumb;

    public object? RenderedTitle
    {
        get
        {
            if (_renderedTitle == null)
            {
                var tb = MarkdownRenderer.CreateInlineTextBlock(DisplayTitle, _searchQuery, new SolidColorBrush(Color.Parse("#FFA200")));
                tb.FontSize = 14;
                tb.FontWeight = FontWeight.Bold;
                tb.TextDecorations = TextDecorations.Underline;
                tb.VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center;
                _renderedTitle = tb;
            }

            return _renderedTitle;
        }
    }

    public DocSection H1Section { get; set; } = null!;
    public DocSection? H2Section { get; set; }
    public DocSection? H3Section { get; set; }
    public DocSection TargetSection { get; set; } = null!;
    public ObservableCollection<DocSearchParagraph> MatchingParagraphs { get; set; } = new();

    public ICommand NavigateCommand => new RelayCommand(() => _onNavigate?.Invoke(this));

    public DocSearchResult(Action<DocSearchResult>? onNavigate = null)
    {
        _onNavigate = onNavigate;
    }

    public void Navigate()
    {
        _onNavigate?.Invoke(this);
    }
}