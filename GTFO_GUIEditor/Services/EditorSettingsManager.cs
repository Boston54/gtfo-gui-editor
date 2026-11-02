using System;
using System.ComponentModel;
using System.IO;
using System.Text.Json;
using GTFO_GUIEditor.Models;

namespace GTFO_GUIEditor.Services;

public class EditorSettingsManager
{
    private static readonly Lazy<EditorSettingsManager> _lazyInstance = new(() => new EditorSettingsManager());
    public static EditorSettingsManager Instance => _lazyInstance.Value;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    public static string? CustomSettingsConfigPath { get; set; }

    public static string SettingsConfigPath
    {
        get
        {
            if (!string.IsNullOrWhiteSpace(CustomSettingsConfigPath))
            {
                return CustomSettingsConfigPath;
            }

            string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            if (string.IsNullOrWhiteSpace(appData))
            {
                appData = AppDomain.CurrentDomain.BaseDirectory;
            }

            string dir = Path.Combine(appData, "GTFO_GUIEditor");
            if (!Directory.Exists(dir))
            {
                try
                {
                    Directory.CreateDirectory(dir);
                }
                catch
                {
                    // Ignore directory creation failure
                }
            }

            return Path.Combine(dir, "settings.json");
        }
    }

    private EditorSettings _settings;

    public EditorSettings Settings => _settings;

    public EditorSettingsManager()
    {
        _settings = LoadSettingsInternal();
        _settings.Appearance.PropertyChanged += OnAppearancePropertyChanged;
    }

    public void ReloadSettings()
    {
        _settings.Appearance.PropertyChanged -= OnAppearancePropertyChanged;
        _settings = LoadSettingsInternal();
        _settings.Appearance.PropertyChanged += OnAppearancePropertyChanged;
    }

    private EditorSettings LoadSettingsInternal()
    {
        try
        {
            string path = SettingsConfigPath;
            if (File.Exists(path))
            {
                string json = File.ReadAllText(path);
                var loaded = JsonSerializer.Deserialize<EditorSettings>(json, JsonOptions);
                if (loaded != null)
                {
                    return loaded;
                }
            }
        }
        catch
        {
            // Ignore failure and fallback to defaults
        }

        return new EditorSettings();
    }

    public void SaveSettings()
    {
        try
        {
            string path = SettingsConfigPath;
            string? dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrWhiteSpace(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            string json = JsonSerializer.Serialize(_settings, JsonOptions);
            File.WriteAllText(path, json);
        }
        catch
        {
            // Ignore save failure
        }
    }

    private void OnAppearancePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        SaveSettings();
    }
}
