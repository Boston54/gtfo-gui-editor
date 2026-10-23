using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using GTFO_GUIEditor.Models;
using GTFO_GUIEditor.Services;

namespace GTFO_GUIEditor.ViewModels;

public class DocumentationViewModel : ViewModelBase
{
    private ObservableCollection<DocSection> _sections = new();
    private DocSection? _selectedSection;
    private string _sourceFilePath = string.Empty;

    public ObservableCollection<DocSection> Sections
    {
        get => _sections;
        set
        {
            if (SetField(ref _sections, value))
            {
                OnPropertyChanged(nameof(HasSections));
            }
        }
    }

    public bool HasSections => _sections != null && _sections.Count > 0;

    public DocSection? SelectedSection
    {
        get => _selectedSection ?? _sections?.FirstOrDefault();
        set => SetField(ref _selectedSection, value);
    }

    public string SourceFilePath
    {
        get => _sourceFilePath;
        set => SetField(ref _sourceFilePath, value);
    }

    public DocumentationViewModel()
    {
        LoadDocumentation();
    }

    public void LoadDocumentation(string? customFilePath = null)
    {
        try
        {
            string? filePath = customFilePath ?? DocumentationService.FindDocumentationFile();
            if (!string.IsNullOrEmpty(filePath) && File.Exists(filePath))
            {
                SourceFilePath = filePath;
                string markdown = File.ReadAllText(filePath);
                Sections = DocumentationService.ParseDocument(markdown);
                SelectedSection = Sections.FirstOrDefault();
            }
            else
            {
                SourceFilePath = "Built-in Defaults";
                string fallback = DocumentationService.GetDefaultDocumentationMarkdown();
                Sections = DocumentationService.ParseDocument(fallback);
                SelectedSection = Sections.FirstOrDefault();
            }
        }
        catch (Exception ex)
        {
            SourceFilePath = "Error loading documentation";
            string errorMd = $"# Error\n\nFailed to load documentation file: {ex.Message}";
            Sections = DocumentationService.ParseDocument(errorMd);
            SelectedSection = Sections.FirstOrDefault();
        }
    }

    public void Reload()
    {
        LoadDocumentation(string.IsNullOrWhiteSpace(SourceFilePath) || SourceFilePath.StartsWith("Built-in") ? null : SourceFilePath);
    }
}
