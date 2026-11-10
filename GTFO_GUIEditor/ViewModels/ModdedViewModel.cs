using System;
using System.ComponentModel;
using GTFO_GUIEditor.Models;
using GTFO_GUIEditor.Services;

namespace GTFO_GUIEditor.ViewModels;

public class ModdedViewModel : ViewModelBase
{
    private string _errorMessage = string.Empty;

    public ModManager ModManager => ModManager.Instance;

    public bool AdvancedWardenObjective
    {
        get => ModManager.Instance.AdvancedWardenObjective;
        set
        {
            if (ModManager.Instance.AdvancedWardenObjective == value)
            {
                return;
            }

            if (!ModManager.Instance.TrySetModEnabled(ModManager.AdvancedWardenObjectiveModId, value, out var error))
            {
                ErrorMessage = error ?? "Cannot disable mod because one or more associated features are actively in use.";
                OnPropertyChanged(nameof(AdvancedWardenObjective));
                OnPropertyChanged(nameof(IsAdvancedWardenObjectiveInUse));
                OnPropertyChanged(nameof(CanDisableAdvancedWardenObjective));
                return;
            }

            ErrorMessage = string.Empty;
            OnPropertyChanged(nameof(AdvancedWardenObjective));
            OnPropertyChanged(nameof(IsAdvancedWardenObjectiveInUse));
            OnPropertyChanged(nameof(CanDisableAdvancedWardenObjective));
        }
    }

    public bool IsAdvancedWardenObjectiveInUse => ModManager.Instance.IsModInUse(ModManager.AdvancedWardenObjectiveModId);

    public bool CanDisableAdvancedWardenObjective => ModManager.Instance.CanDisableMod(ModManager.AdvancedWardenObjectiveModId);

    public bool AdditionalRundownAdvancements
    {
        get => ModManager.Instance.AdditionalRundownAdvancements;
        set
        {
            if (ModManager.Instance.AdditionalRundownAdvancements == value)
            {
                return;
            }

            if (!ModManager.Instance.TrySetModEnabled(ModManager.AdditionalRundownAdvancementsModId, value, out var error))
            {
                ErrorMessage = error ?? "Cannot disable mod because one or more associated features are actively in use.";
                OnPropertyChanged(nameof(AdditionalRundownAdvancements));
                OnPropertyChanged(nameof(IsAdditionalRundownAdvancementsInUse));
                OnPropertyChanged(nameof(CanDisableAdditionalRundownAdvancements));
                return;
            }

            ErrorMessage = string.Empty;
            OnPropertyChanged(nameof(AdditionalRundownAdvancements));
            OnPropertyChanged(nameof(IsAdditionalRundownAdvancementsInUse));
            OnPropertyChanged(nameof(CanDisableAdditionalRundownAdvancements));
        }
    }

    public bool IsAdditionalRundownAdvancementsInUse => ModManager.Instance.IsModInUse(ModManager.AdditionalRundownAdvancementsModId);

    public bool CanDisableAdditionalRundownAdvancements => ModManager.Instance.CanDisableMod(ModManager.AdditionalRundownAdvancementsModId);

    public string ErrorMessage
    {
        get => _errorMessage;
        set
        {
            if (SetField(ref _errorMessage, value))
            {
                OnPropertyChanged(nameof(HasErrorMessage));
            }
        }
    }

    public bool HasErrorMessage => !string.IsNullOrEmpty(ErrorMessage);

    public ModdedViewModel()
    {
        ModManager.Instance.PropertyChanged += OnModManagerPropertyChanged;
    }

    private void OnModManagerPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (string.IsNullOrEmpty(e.PropertyName) ||
            e.PropertyName == nameof(ModManager.AdvancedWardenObjective) ||
            e.PropertyName == ModManager.AdvancedWardenObjectiveModId)
        {
            OnPropertyChanged(nameof(AdvancedWardenObjective));
            OnPropertyChanged(nameof(IsAdvancedWardenObjectiveInUse));
            OnPropertyChanged(nameof(CanDisableAdvancedWardenObjective));
        }

        if (string.IsNullOrEmpty(e.PropertyName) ||
            e.PropertyName == nameof(ModManager.AdditionalRundownAdvancements) ||
            e.PropertyName == ModManager.AdditionalRundownAdvancementsModId)
        {
            OnPropertyChanged(nameof(AdditionalRundownAdvancements));
            OnPropertyChanged(nameof(IsAdditionalRundownAdvancementsInUse));
            OnPropertyChanged(nameof(CanDisableAdditionalRundownAdvancements));
        }
    }

    public void Reset()
    {
        ModManager.Instance.Reset();
        ErrorMessage = string.Empty;
        OnPropertyChanged(nameof(AdvancedWardenObjective));
        OnPropertyChanged(nameof(IsAdvancedWardenObjectiveInUse));
        OnPropertyChanged(nameof(CanDisableAdvancedWardenObjective));
        OnPropertyChanged(nameof(AdditionalRundownAdvancements));
        OnPropertyChanged(nameof(IsAdditionalRundownAdvancementsInUse));
        OnPropertyChanged(nameof(CanDisableAdditionalRundownAdvancements));
    }

    public void ApplyData(ModdedSettingsData? data)
    {
        ModManager.Instance.LoadFrom(data);
        ErrorMessage = string.Empty;
        OnPropertyChanged(nameof(AdvancedWardenObjective));
        OnPropertyChanged(nameof(IsAdvancedWardenObjectiveInUse));
        OnPropertyChanged(nameof(CanDisableAdvancedWardenObjective));
        OnPropertyChanged(nameof(AdditionalRundownAdvancements));
        OnPropertyChanged(nameof(IsAdditionalRundownAdvancementsInUse));
        OnPropertyChanged(nameof(CanDisableAdditionalRundownAdvancements));
    }

    public ModdedSettingsData ExportData()
    {
        return ModManager.Instance.ExportData();
    }
}
