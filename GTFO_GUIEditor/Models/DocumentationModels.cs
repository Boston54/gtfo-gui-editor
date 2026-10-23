using System.Collections.ObjectModel;
using System.Linq;
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
