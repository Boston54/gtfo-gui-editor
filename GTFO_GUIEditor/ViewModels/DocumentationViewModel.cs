using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows.Input;
using GTFO_GUIEditor.Models;
using GTFO_GUIEditor.Services;

namespace GTFO_GUIEditor.ViewModels;

public class DocumentationViewModel : ViewModelBase
{
    private ObservableCollection<DocSection> _sections = new();
    private ObservableCollection<DocSection> _displayTabs = new();
    private DocSection? _selectedSection;
    private string _sourceFilePath = string.Empty;
    private string _searchQuery = string.Empty;
    private ObservableCollection<DocSearchResult> _searchResults = new();
    private string _searchStatusMessage = string.Empty;
    private bool _hasSearched;

    public DocSection SearchSection { get; } = new()
    {
        Title = "Search",
        IsSearchTab = true,
        Level = 1
    };

    public ObservableCollection<DocSection> Sections
    {
        get => _sections;
        set
        {
            if (SetField(ref _sections, value))
            {
                UpdateDisplayTabs();
                OnPropertyChanged(nameof(HasSections));
            }
        }
    }

    public ObservableCollection<DocSection> DisplayTabs
    {
        get => _displayTabs;
        private set => SetField(ref _displayTabs, value);
    }

    public bool HasSections => _sections != null && _sections.Count > 0;

    public DocSection? SelectedSection
    {
        get => _selectedSection ?? GetDefaultSection();
        set => SetField(ref _selectedSection, value);
    }

    private DocSection? GetDefaultSection()
    {
        return _sections?.FirstOrDefault(s => string.Equals(s.Title, "Welcome", StringComparison.OrdinalIgnoreCase))
               ?? _sections?.FirstOrDefault(s => string.Equals(s.Title, "Home", StringComparison.OrdinalIgnoreCase))
               ?? _sections?.FirstOrDefault()
               ?? SearchSection;
    }

    public string SourceFilePath
    {
        get => _sourceFilePath;
        set => SetField(ref _sourceFilePath, value);
    }

    public string SearchQuery
    {
        get => _searchQuery;
        set
        {
            if (SetField(ref _searchQuery, value))
            {
                OnPropertyChanged(nameof(HasSearchQuery));
                PerformSearch();
            }
        }
    }

    public bool HasSearchQuery => !string.IsNullOrWhiteSpace(_searchQuery);

    public ObservableCollection<DocSearchResult> SearchResults
    {
        get => _searchResults;
        set
        {
            if (SetField(ref _searchResults, value))
            {
                OnPropertyChanged(nameof(HasSearchResults));
            }
        }
    }

    public bool HasSearchResults => _searchResults != null && _searchResults.Count > 0;

    public bool HasSearched
    {
        get => _hasSearched;
        set
        {
            if (SetField(ref _hasSearched, value))
            {
                OnPropertyChanged(nameof(ShowSearchPrompt));
            }
        }
    }

    public bool ShowSearchPrompt => !HasSearched && !HasSearchResults;

    public string SearchStatusMessage
    {
        get => _searchStatusMessage;
        set
        {
            if (SetField(ref _searchStatusMessage, value))
            {
                OnPropertyChanged(nameof(HasSearchStatus));
            }
        }
    }

    public bool HasSearchStatus => !string.IsNullOrWhiteSpace(_searchStatusMessage);

    public ICommand SearchCommand => new RelayCommand(PerformSearch);
    public ICommand ClearSearchCommand => new RelayCommand(ClearSearch);
    public ICommand NavigateToResultCommand => new RelayCommand<DocSearchResult>(NavigateToSearchResult);

    public DocumentationViewModel()
    {
        UpdateDisplayTabs();
        LoadDocumentation();
        SelectedSection = GetDefaultSection();
    }

    private void UpdateDisplayTabs()
    {
        var list = new List<DocSection>();
        if (_sections != null)
        {
            list.AddRange(_sections);
        }
        list.Add(SearchSection);
        DisplayTabs = new ObservableCollection<DocSection>(list);
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
                SelectedSection = GetDefaultSection();
            }
        }
        catch (Exception ex)
        {
            SourceFilePath = "Error loading documentation";
            string errorMd = $"# Error\n\nFailed to load documentation file: {ex.Message}";
            Sections = DocumentationService.ParseDocument(errorMd);
            SelectedSection = GetDefaultSection();
        }

        if (HasSearchQuery)
        {
            PerformSearch();
        }
    }

    public void PerformSearch()
    {
        if (string.IsNullOrWhiteSpace(SearchQuery))
        {
            SearchResults = new ObservableCollection<DocSearchResult>();
            SearchStatusMessage = string.Empty;
            HasSearched = false;
            return;
        }

        HasSearched = true;
        var results = DocumentationService.Search(Sections, SearchQuery, NavigateToSearchResult);
        SearchResults = new ObservableCollection<DocSearchResult>(results);

        if (results.Count == 0)
        {
            SearchStatusMessage = $"No matching documentation found for \"{SearchQuery.Trim()}\".";
        }
        else
        {
            SearchStatusMessage =
                $"Found {results.Count} matching page{(results.Count == 1 ? "" : "s")} for \"{SearchQuery.Trim()}\":";
        }
    }

    public void ClearSearch()
    {
        SearchQuery = string.Empty;
        SearchResults = new ObservableCollection<DocSearchResult>();
        SearchStatusMessage = string.Empty;
        HasSearched = false;
    }

    public void NavigateToSearchResult(DocSearchResult? result)
    {
        if (result == null) return;

        if (result.H1Section != null)
        {
            SelectedSection = result.H1Section;
            if (result.H2Section != null)
            {
                result.H1Section.SelectedSubSection = result.H2Section;
                if (result.H3Section != null)
                {
                    result.H2Section.SelectedSubSection = result.H3Section;
                }
            }
        }
    }

    public void Reload()
    {
        LoadDocumentation(string.IsNullOrWhiteSpace(SourceFilePath) || SourceFilePath.StartsWith("Built-in")
            ? null
            : SourceFilePath);
    }
}