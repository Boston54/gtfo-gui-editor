using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using GTFO_GUIEditor.Models;

namespace GTFO_GUIEditor.Services;

/// <summary>
/// Central modding manager coordinating mod support throughout the application.
/// Allows querying whether mods are enabled in the active rundown, controlling feature visibility,
/// adjusting export behaviors, and enforcing locking when mod features are actively in use.
/// </summary>
public class ModManager : INotifyPropertyChanged
{
    private static readonly Lazy<ModManager> _lazyInstance = new(() => new ModManager());
    public static ModManager Instance => _lazyInstance.Value;

    public const string AdvancedWardenObjectiveModId = "AdvancedWardenObjective";

    private readonly HashSet<string> _enabledMods = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, List<ModFeatureUsageCheck>> _featureUsageChecks = new(StringComparer.OrdinalIgnoreCase);

    public event PropertyChangedEventHandler? PropertyChanged;
    public event Action<string, bool>? ModStateChanged;

    /// <summary>
    /// Gets or sets whether Advanced Warden Objective support is enabled.
    /// Throws InvalidOperationException if set to false while associated features are actively in use.
    /// </summary>
    public bool AdvancedWardenObjective
    {
        get => IsModEnabled(AdvancedWardenObjectiveModId);
        set => SetModEnabled(AdvancedWardenObjectiveModId, value);
    }

    /// <summary>
    /// Returns true if the specified mod ID is currently enabled in the active rundown.
    /// </summary>
    public bool IsModEnabled(string modId)
    {
        if (string.IsNullOrWhiteSpace(modId))
        {
            return false;
        }

        return _enabledMods.Contains(modId);
    }

    /// <summary>
    /// Checks whether the specified mod can be safely disabled without breaking features actively in use.
    /// </summary>
    public bool CanDisableMod(string modId, out string? reason)
    {
        if (string.IsNullOrWhiteSpace(modId) || !_enabledMods.Contains(modId))
        {
            reason = null;
            return true;
        }

        if (_featureUsageChecks.TryGetValue(modId, out var checks))
        {
            foreach (var check in checks)
            {
                if (check.Predicate())
                {
                    reason = !string.IsNullOrWhiteSpace(check.FeatureName)
                        ? $"Cannot disable '{modId}' because feature '{check.FeatureName}' is actively in use."
                        : $"Cannot disable '{modId}' because one or more associated features are actively in use.";
                    return false;
                }
            }
        }

        reason = null;
        return true;
    }

    /// <summary>
    /// Checks whether the specified mod can be safely disabled.
    /// </summary>
    public bool CanDisableMod(string modId) => CanDisableMod(modId, out _);

    /// <summary>
    /// Returns true if any registered feature check reports that the mod is actively in use.
    /// </summary>
    public bool IsModInUse(string modId) => !CanDisableMod(modId, out _);

    /// <summary>
    /// Attempts to enable or disable a mod, returning false and an error message if disabling is blocked.
    /// </summary>
    public bool TrySetModEnabled(string modId, bool enabled, out string? errorMessage)
    {
        if (string.IsNullOrWhiteSpace(modId))
        {
            throw new ArgumentException("Mod ID cannot be null or whitespace.", nameof(modId));
        }

        if (!enabled && !CanDisableMod(modId, out errorMessage))
        {
            return false;
        }

        errorMessage = null;
        bool changed;
        if (enabled)
        {
            changed = _enabledMods.Add(modId);
        }
        else
        {
            changed = _enabledMods.Remove(modId);
        }

        if (changed)
        {
            OnPropertyChanged(modId);
            if (string.Equals(modId, AdvancedWardenObjectiveModId, StringComparison.OrdinalIgnoreCase))
            {
                OnPropertyChanged(nameof(AdvancedWardenObjective));
            }
            ModStateChanged?.Invoke(modId, enabled);
        }

        return true;
    }

    /// <summary>
    /// Enables or disables a mod, throwing an exception if disabling is blocked.
    /// </summary>
    public void SetModEnabled(string modId, bool enabled)
    {
        if (!TrySetModEnabled(modId, enabled, out var errorMessage))
        {
            throw new InvalidOperationException(errorMessage ?? $"Cannot disable mod '{modId}' because it is in use.");
        }
    }

    /// <summary>
    /// Registers a feature usage checker predicate for a given mod.
    /// </summary>
    public void RegisterFeature(string modId, string featureName, Func<bool> isInUsePredicate)
    {
        if (string.IsNullOrWhiteSpace(modId))
        {
            throw new ArgumentException("Mod ID cannot be null or whitespace.", nameof(modId));
        }
        ArgumentNullException.ThrowIfNull(isInUsePredicate);

        if (!_featureUsageChecks.TryGetValue(modId, out var checks))
        {
            checks = new List<ModFeatureUsageCheck>();
            _featureUsageChecks[modId] = checks;
        }

        checks.Add(new ModFeatureUsageCheck(featureName, isInUsePredicate));
    }

    /// <summary>
    /// Unregisters all feature usage checks for the specified mod.
    /// </summary>
    public void UnregisterFeatures(string modId)
    {
        if (!string.IsNullOrWhiteSpace(modId))
        {
            _featureUsageChecks.Remove(modId);
        }
    }

    /// <summary>
    /// Clears all registered feature usage checks.
    /// </summary>
    public void ClearAllFeatureRegistrations()
    {
        _featureUsageChecks.Clear();
    }

    /// <summary>
    /// Resets all enabled mods to their default state.
    /// </summary>
    public void Reset()
    {
        _enabledMods.Clear();
        OnPropertyChanged(nameof(AdvancedWardenObjective));
        OnPropertyChanged(string.Empty);
    }

    /// <summary>
    /// Loads enabled mods from serialized rundown mod settings.
    /// </summary>
    public void LoadFrom(ModdedSettingsData? data)
    {
        _enabledMods.Clear();
        if (data != null)
        {
            if (data.AdvancedWardenObjective)
            {
                _enabledMods.Add(AdvancedWardenObjectiveModId);
            }

            if (data.EnabledMods != null)
            {
                foreach (var mod in data.EnabledMods)
                {
                    if (!string.IsNullOrWhiteSpace(mod))
                    {
                        _enabledMods.Add(mod);
                    }
                }
            }
        }

        OnPropertyChanged(nameof(AdvancedWardenObjective));
        OnPropertyChanged(string.Empty);
    }

    /// <summary>
    /// Exports the current mod configurations into a serializable model.
    /// </summary>
    public ModdedSettingsData ExportData()
    {
        return new ModdedSettingsData
        {
            AdvancedWardenObjective = IsModEnabled(AdvancedWardenObjectiveModId),
            EnabledMods = new HashSet<string>(_enabledMods, StringComparer.OrdinalIgnoreCase)
        };
    }

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    private sealed record ModFeatureUsageCheck(string? FeatureName, Func<bool> Predicate);
}

/// <summary>
/// Alias for ModManager to provide consistency across naming conventions.
/// </summary>
public static class ModdingManager
{
    public static ModManager Instance => ModManager.Instance;
}
