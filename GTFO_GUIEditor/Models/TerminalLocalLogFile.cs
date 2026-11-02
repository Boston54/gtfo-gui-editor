using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace GTFO_GUIEditor.Models;

public class TerminalLocalLogFile : INotifyPropertyChanged
{
    private string _fileName = string.Empty;
    private string _fileContent = string.Empty;

    [JsonPropertyName("fileName")]
    public string FileName
    {
        get => _fileName;
        set => SetField(ref _fileName, value ?? string.Empty);
    }

    [JsonPropertyName("fileContent")]
    public string FileContent
    {
        get => _fileContent;
        set => SetField(ref _fileContent, value ?? string.Empty);
    }

    public TerminalLocalLogFile Clone()
    {
        return new TerminalLocalLogFile
        {
            FileName = FileName,
            FileContent = FileContent
        };
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    protected bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }
}
