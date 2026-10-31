using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;
using GTFO_GUIEditor.Services;

namespace GTFO_GUIEditor.Models;

public class SectorSettings : INotifyPropertyChanged
{
    private double _artifactAmountMultiplier = 1.0;
    private int _zoneAliasStart;
    private ObservableCollection<GatherItemPlacement> _bulkheadKeyPlacements = new();
    private ObservableCollection<WardenObjective> _objectives = new();
    private WardenObjective? _hookedPrimaryObjective;

    public event PropertyChangedEventHandler? PropertyChanged;

    public SectorSettings()
    {
        _bulkheadKeyPlacements.CollectionChanged += BulkheadKeyPlacements_CollectionChanged;
        var primary = new WardenObjective { ObjectiveNumber = 1 };
        _objectives.Add(primary);
        _objectives.CollectionChanged += Objectives_CollectionChanged;
        HookPrimaryObjective(primary);
        EditorDefaultsService.ApplySectorCommonDefaults(this);
    }

    private void HookPrimaryObjective(WardenObjective? obj)
    {
        if (_hookedPrimaryObjective != null)
        {
            _hookedPrimaryObjective.PropertyChanged -= PrimaryObjective_PropertyChanged;
        }

        _hookedPrimaryObjective = obj;
        if (_hookedPrimaryObjective != null)
        {
            _hookedPrimaryObjective.PropertyChanged += PrimaryObjective_PropertyChanged;
        }
    }

    private void PrimaryObjective_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        OnPropertyChanged(e.PropertyName);
    }

    private void Objectives_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        for (int i = 0; i < _objectives.Count; i++)
        {
            _objectives[i].ObjectiveNumber = i + 1;
        }

        HookPrimaryObjective(_objectives.Count > 0 ? _objectives[0] : null);
        OnPropertyChanged(nameof(Objectives));
        OnPropertyChanged(nameof(PrimaryObjective));
    }

    private void BulkheadKeyPlacements_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        OnPropertyChanged(nameof(BulkheadKeyPlacements));
    }

    [JsonPropertyName("Objectives")]
    public ObservableCollection<WardenObjective> Objectives
    {
        get => _objectives;
        set
        {
            if (_objectives != null)
            {
                _objectives.CollectionChanged -= Objectives_CollectionChanged;
            }

            var nonNull = value ?? new ObservableCollection<WardenObjective>();
            if (nonNull.Count == 0)
            {
                nonNull.Add(new WardenObjective { ObjectiveNumber = 1 });
            }

            for (int i = 0; i < nonNull.Count; i++)
            {
                nonNull[i].ObjectiveNumber = i + 1;
            }

            _objectives = nonNull;
            _objectives.CollectionChanged += Objectives_CollectionChanged;
            OnPropertyChanged();

            HookPrimaryObjective(PrimaryObjective);
            OnPropertyChanged(nameof(PrimaryObjective));
        }
    }

    [JsonIgnore]
    public WardenObjective PrimaryObjective
    {
        get
        {
            if (_objectives.Count == 0)
            {
                var newObj = new WardenObjective { ObjectiveNumber = 1 };
                _objectives.Add(newObj);
                return newObj;
            }

            return _objectives[0];
        }
    }

    [JsonPropertyName("BulkheadKeyPlacements")]
    public ObservableCollection<GatherItemPlacement> BulkheadKeyPlacements
    {
        get => _bulkheadKeyPlacements;
        set
        {
            var nonNull = value ?? new ObservableCollection<GatherItemPlacement>();
            if (_bulkheadKeyPlacements != null)
            {
                _bulkheadKeyPlacements.CollectionChanged -= BulkheadKeyPlacements_CollectionChanged;
            }

            _bulkheadKeyPlacements = nonNull;
            _bulkheadKeyPlacements.CollectionChanged += BulkheadKeyPlacements_CollectionChanged;
            OnPropertyChanged();
        }
    }

    [JsonPropertyName("ArtifactAmountMultiplier")]
    public double ArtifactAmountMultiplier
    {
        get => _artifactAmountMultiplier;
        set => SetField(ref _artifactAmountMultiplier, value);
    }

    [JsonPropertyName("ZoneAliasStart")]
    public int ZoneAliasStart
    {
        get => _zoneAliasStart;
        set => SetField(ref _zoneAliasStart, value);
    }

    // --- Forwarding Properties to PrimaryObjective ---

    [JsonPropertyName("WinCondition")]
    public WinCondition WinCondition
    {
        get => PrimaryObjective.WinCondition;
        set => PrimaryObjective.WinCondition = value;
    }

    [JsonPropertyName("ExitZone")]
    public int ExitZone
    {
        get => PrimaryObjective.ExitZone;
        set => PrimaryObjective.ExitZone = value;
    }

    [JsonPropertyName("ObjectiveType")]
    public ObjectiveType ObjectiveType
    {
        get => PrimaryObjective.ObjectiveType;
        set => PrimaryObjective.ObjectiveType = value;
    }

    [JsonPropertyName("HsuPlacementZone")]
    public int HsuPlacementZone
    {
        get => PrimaryObjective.HsuPlacementZone;
        set => PrimaryObjective.HsuPlacementZone = value;
    }

    [JsonPropertyName("HsuPlacementStartWeight")]
    public double HsuPlacementStartWeight
    {
        get => PrimaryObjective.HsuPlacementStartWeight;
        set => PrimaryObjective.HsuPlacementStartWeight = value;
    }

    [JsonPropertyName("HsuPlacementMiddleWeight")]
    public double HsuPlacementMiddleWeight
    {
        get => PrimaryObjective.HsuPlacementMiddleWeight;
        set => PrimaryObjective.HsuPlacementMiddleWeight = value;
    }

    [JsonPropertyName("HsuPlacementEndWeight")]
    public double HsuPlacementEndWeight
    {
        get => PrimaryObjective.HsuPlacementEndWeight;
        set => PrimaryObjective.HsuPlacementEndWeight = value;
    }

    [JsonPropertyName("ReactorPlacementZone")]
    public int ReactorPlacementZone
    {
        get => PrimaryObjective.ReactorPlacementZone;
        set => PrimaryObjective.ReactorPlacementZone = value;
    }

    [JsonPropertyName("ReactorStartupStages")]
    public ObservableCollection<ReactorStartupStage> ReactorStartupStages
    {
        get => PrimaryObjective.ReactorStartupStages;
        set => PrimaryObjective.ReactorStartupStages = value;
    }

    [JsonPropertyName("ChainedPuzzleToActive")]
    public uint ChainedPuzzleToActive
    {
        get => PrimaryObjective.ChainedPuzzleToActive;
        set => PrimaryObjective.ChainedPuzzleToActive = value;
    }

    [JsonPropertyName("ChainedPuzzleMidObjective")]
    public uint ChainedPuzzleMidObjective
    {
        get => PrimaryObjective.ChainedPuzzleMidObjective;
        set => PrimaryObjective.ChainedPuzzleMidObjective = value;
    }

    [JsonPropertyName("DoNotSolveObjectiveOnReactorComplete")]
    public bool DoNotSolveObjectiveOnReactorComplete
    {
        get => PrimaryObjective.DoNotSolveObjectiveOnReactorComplete;
        set => PrimaryObjective.DoNotSolveObjectiveOnReactorComplete = value;
    }

    [JsonPropertyName("SpecialTerminalCommandZone")]
    public int SpecialTerminalCommandZone
    {
        get => PrimaryObjective.SpecialTerminalCommandZone;
        set => PrimaryObjective.SpecialTerminalCommandZone = value;
    }

    [JsonPropertyName("SpecialTerminalCommand")]
    public string SpecialTerminalCommand
    {
        get => PrimaryObjective.SpecialTerminalCommand;
        set => PrimaryObjective.SpecialTerminalCommand = value;
    }

    [JsonPropertyName("SpecialTerminalCommandDesc")]
    public string SpecialTerminalCommandDesc
    {
        get => PrimaryObjective.SpecialTerminalCommandDesc;
        set => PrimaryObjective.SpecialTerminalCommandDesc = value;
    }

    [JsonPropertyName("PostCommandOutput")]
    public string PostCommandOutput
    {
        get => PrimaryObjective.PostCommandOutput;
        set => PrimaryObjective.PostCommandOutput = value;
    }

    [JsonPropertyName("SpecialCommandRule")]
    public SpecialCommandRule SpecialCommandRule
    {
        get => PrimaryObjective.SpecialCommandRule;
        set => PrimaryObjective.SpecialCommandRule = value;
    }

    [JsonPropertyName("GatherItem")]
    public GatherSmallItemType GatherItem
    {
        get => PrimaryObjective.GatherItem;
        set => PrimaryObjective.GatherItem = value;
    }

    [JsonPropertyName("RetrieveItem")]
    public RetrieveBigItemType RetrieveItem
    {
        get => PrimaryObjective.RetrieveItem;
        set => PrimaryObjective.RetrieveItem = value;
    }

    [JsonPropertyName("GatherItemPlacements")]
    public ObservableCollection<GatherItemPlacement> GatherItemPlacements
    {
        get => PrimaryObjective.GatherItemPlacements;
        set => PrimaryObjective.GatherItemPlacements = value;
    }

    [JsonPropertyName("RetrieveItemPlacements")]
    public ObservableCollection<GatherItemPlacement> RetrieveItemPlacements
    {
        get => PrimaryObjective.RetrieveItemPlacements;
        set => PrimaryObjective.RetrieveItemPlacements = value;
    }

    [JsonPropertyName("GatherMaxPerZone")]
    public int GatherMaxPerZone
    {
        get => PrimaryObjective.GatherMaxPerZone;
        set => PrimaryObjective.GatherMaxPerZone = value;
    }

    [JsonPropertyName("RetrieveMaxPerZone")]
    public int RetrieveMaxPerZone
    {
        get => PrimaryObjective.RetrieveMaxPerZone;
        set => PrimaryObjective.RetrieveMaxPerZone = value;
    }

    [JsonPropertyName("DistributeGeneratorPlacements")]
    public ObservableCollection<DistributeGeneratorPlacement> DistributeGeneratorPlacements
    {
        get => PrimaryObjective.DistributeGeneratorPlacements;
        set => PrimaryObjective.DistributeGeneratorPlacements = value;
    }

    [JsonPropertyName("DistributeGeneratorCount")]
    public int DistributeGeneratorCount
    {
        get => PrimaryObjective.DistributeGeneratorPlacements.Count;
        set
        {
            int current = PrimaryObjective.DistributeGeneratorPlacements.Count;
            if (value > current)
            {
                for (int i = current; i < value; i++)
                {
                    PrimaryObjective.DistributeGeneratorPlacements.Add(new DistributeGeneratorPlacement { GeneratorNumber = i + 1 });
                }
            }
            else if (value < current && value >= 0)
            {
                for (int i = current - 1; i >= value; i--)
                {
                    PrimaryObjective.DistributeGeneratorPlacements.RemoveAt(i);
                }
            }
            OnPropertyChanged(nameof(DistributeGeneratorCount));
        }
    }

    [JsonPropertyName("GatherSpawnCount")]
    public int GatherSpawnCount
    {
        get => PrimaryObjective.GatherItemPlacements.Count;
        set
        {
            int current = PrimaryObjective.GatherItemPlacements.Count;
            if (value > current)
            {
                for (int i = current; i < value; i++)
                {
                    PrimaryObjective.GatherItemPlacements.Add(new GatherItemPlacement { ItemNumber = i + 1 });
                }
            }
            else if (value < current && value >= 0)
            {
                for (int i = current - 1; i >= value; i--)
                {
                    PrimaryObjective.GatherItemPlacements.RemoveAt(i);
                }
            }
            OnPropertyChanged(nameof(GatherSpawnCount));
        }
    }

    [JsonPropertyName("RetrieveSpawnCount")]
    public int RetrieveSpawnCount
    {
        get => PrimaryObjective.RetrieveItemPlacements.Count;
        set
        {
            int current = PrimaryObjective.RetrieveItemPlacements.Count;
            if (value > current)
            {
                for (int i = current; i < value; i++)
                {
                    PrimaryObjective.RetrieveItemPlacements.Add(new GatherItemPlacement { ItemNumber = i + 1 });
                }
            }
            else if (value < current && value >= 0)
            {
                for (int i = current - 1; i >= value; i--)
                {
                    PrimaryObjective.RetrieveItemPlacements.RemoveAt(i);
                }
            }
            OnPropertyChanged(nameof(RetrieveSpawnCount));
        }
    }

    [JsonPropertyName("UplinkNumberOfTerminals")]
    public int UplinkNumberOfTerminals
    {
        get => PrimaryObjective.TerminalUplinkPlacements.Count;
        set
        {
            int current = PrimaryObjective.TerminalUplinkPlacements.Count;
            if (value > current)
            {
                for (int i = current; i < value; i++)
                {
                    PrimaryObjective.TerminalUplinkPlacements.Add(new GatherItemPlacement { ItemNumber = i + 1, ItemPrefix = "Terminal" });
                }
            }
            else if (value < current && value >= 0)
            {
                for (int i = current - 1; i >= value; i--)
                {
                    PrimaryObjective.TerminalUplinkPlacements.RemoveAt(i);
                }
            }
            OnPropertyChanged(nameof(UplinkNumberOfTerminals));
        }
    }

    [JsonPropertyName("CentralGeneratorClusterGeneratorCount")]
    public int CentralGeneratorClusterGeneratorCount
    {
        get => PrimaryObjective.CentralGeneratorClusterFogTransitions.Count;
        set
        {
            int current = PrimaryObjective.CentralGeneratorClusterFogTransitions.Count;
            if (value > current)
            {
                for (int i = current; i < value; i++)
                {
                    PrimaryObjective.CentralGeneratorClusterFogTransitions.Add(new CentralGeneratorFogTransitionStage { StageNumber = i + 1 });
                }
            }
            else if (value < current && value >= 0)
            {
                for (int i = current - 1; i >= value; i--)
                {
                    PrimaryObjective.CentralGeneratorClusterFogTransitions.RemoveAt(i);
                }
            }
            OnPropertyChanged(nameof(CentralGeneratorClusterGeneratorCount));
        }
    }

    [JsonPropertyName("CentralGeneratorClusterPowerCellCount")]
    public int CentralGeneratorClusterPowerCellCount
    {
        get => PrimaryObjective.CentralGeneratorClusterPowerCellPlacements.Count;
        set
        {
            int current = PrimaryObjective.CentralGeneratorClusterPowerCellPlacements.Count;
            if (value > current)
            {
                for (int i = current; i < value; i++)
                {
                    PrimaryObjective.CentralGeneratorClusterPowerCellPlacements.Add(new GatherItemPlacement { ItemNumber = i + 1, ItemPrefix = "Power Cell" });
                }
            }
            else if (value < current && value >= 0)
            {
                for (int i = current - 1; i >= value; i--)
                {
                    PrimaryObjective.CentralGeneratorClusterPowerCellPlacements.RemoveAt(i);
                }
            }
            OnPropertyChanged(nameof(CentralGeneratorClusterPowerCellCount));
        }
    }

    [JsonPropertyName("UplinkNumberOfVerificationRounds")]
    public int UplinkNumberOfVerificationRounds
    {
        get => PrimaryObjective.UplinkNumberOfVerificationRounds;
        set => PrimaryObjective.UplinkNumberOfVerificationRounds = value;
    }

    [JsonPropertyName("TerminalUplinkPlacements")]
    public ObservableCollection<GatherItemPlacement> TerminalUplinkPlacements
    {
        get => PrimaryObjective.TerminalUplinkPlacements;
        set => PrimaryObjective.TerminalUplinkPlacements = value;
    }

    [JsonPropertyName("CentralGeneratorClusterZone")]
    public int CentralGeneratorClusterZone
    {
        get => PrimaryObjective.CentralGeneratorClusterZone;
        set => PrimaryObjective.CentralGeneratorClusterZone = value;
    }

    [JsonPropertyName("CentralGeneratorClusterPowerCellPlacements")]
    public ObservableCollection<GatherItemPlacement> CentralGeneratorClusterPowerCellPlacements
    {
        get => PrimaryObjective.CentralGeneratorClusterPowerCellPlacements;
        set => PrimaryObjective.CentralGeneratorClusterPowerCellPlacements = value;
    }

    [JsonPropertyName("CentralGeneratorClusterFogTransitions")]
    public ObservableCollection<CentralGeneratorFogTransitionStage> CentralGeneratorClusterFogTransitions
    {
        get => PrimaryObjective.CentralGeneratorClusterFogTransitions;
        set => PrimaryObjective.CentralGeneratorClusterFogTransitions = value;
    }

    [JsonPropertyName("ActivateSmallHsuZone")]
    public int ActivateSmallHsuZone
    {
        get => PrimaryObjective.ActivateSmallHsuZone;
        set => PrimaryObjective.ActivateSmallHsuZone = value;
    }

    [JsonPropertyName("ItemFromStart")]
    public RetrieveBigItemType ItemFromStart
    {
        get => PrimaryObjective.ItemFromStart;
        set => PrimaryObjective.ItemFromStart = value;
    }

    [JsonPropertyName("ItemAfterActivation")]
    public RetrieveBigItemType ItemAfterActivation
    {
        get => PrimaryObjective.ItemAfterActivation;
        set => PrimaryObjective.ItemAfterActivation = value;
    }

    [JsonPropertyName("BringItemInElevator")]
    public bool BringItemInElevator
    {
        get => PrimaryObjective.BringItemInElevator;
        set => PrimaryObjective.BringItemInElevator = value;
    }

    [JsonPropertyName("StopEnemyWavesOnActivation")]
    public bool StopEnemyWavesOnActivation
    {
        get => PrimaryObjective.StopEnemyWavesOnActivation;
        set => PrimaryObjective.StopEnemyWavesOnActivation = value;
    }

    [JsonPropertyName("ObjectiveCompleteAfterInsertion")]
    public bool ObjectiveCompleteAfterInsertion
    {
        get => PrimaryObjective.ObjectiveCompleteAfterInsertion;
        set => PrimaryObjective.ObjectiveCompleteAfterInsertion = value;
    }

    [JsonPropertyName("RequireItemAfterActivationInExitScan")]
    public bool RequireItemAfterActivationInExitScan
    {
        get => PrimaryObjective.RequireItemAfterActivationInExitScan;
        set => PrimaryObjective.RequireItemAfterActivationInExitScan = value;
    }

    [JsonPropertyName("TimeToActivate")]
    public int TimeToActivate
    {
        get => PrimaryObjective.TimeToActivate;
        set => PrimaryObjective.TimeToActivate = value;
    }

    [JsonPropertyName("TimeToSurvive")]
    public int TimeToSurvive
    {
        get => PrimaryObjective.TimeToSurvive;
        set => PrimaryObjective.TimeToSurvive = value;
    }

    [JsonPropertyName("TimerTitle")]
    public string TimerTitle
    {
        get => PrimaryObjective.TimerTitle;
        set => PrimaryObjective.TimerTitle = value;
    }

    [JsonPropertyName("TimerToActivateTitle")]
    public string TimerToActivateTitle
    {
        get => PrimaryObjective.TimerToActivateTitle;
        set => PrimaryObjective.TimerToActivateTitle = value;
    }

    [JsonPropertyName("GatherTerminalSpawnCount")]
    public int GatherTerminalSpawnCount
    {
        get => PrimaryObjective.GatherTerminalSpawnCount;
        set => PrimaryObjective.GatherTerminalSpawnCount = value;
    }

    [JsonPropertyName("GatherTerminalRequiredCount")]
    public int GatherTerminalRequiredCount
    {
        get => PrimaryObjective.GatherTerminalRequiredCount;
        set => PrimaryObjective.GatherTerminalRequiredCount = value;
    }

    [JsonPropertyName("GatherTerminalCommand")]
    public string GatherTerminalCommand
    {
        get => PrimaryObjective.GatherTerminalCommand;
        set => PrimaryObjective.GatherTerminalCommand = value;
    }

    [JsonPropertyName("GatherTerminalCommandHelp")]
    public string GatherTerminalCommandHelp
    {
        get => PrimaryObjective.GatherTerminalCommandHelp;
        set => PrimaryObjective.GatherTerminalCommandHelp = value;
    }

    [JsonPropertyName("GatherTerminalDownloadingText")]
    public string GatherTerminalDownloadingText
    {
        get => PrimaryObjective.GatherTerminalDownloadingText;
        set => PrimaryObjective.GatherTerminalDownloadingText = value;
    }

    [JsonPropertyName("GatherTerminalDownloadCompleteText")]
    public string GatherTerminalDownloadCompleteText
    {
        get => PrimaryObjective.GatherTerminalDownloadCompleteText;
        set => PrimaryObjective.GatherTerminalDownloadCompleteText = value;
    }

    [JsonPropertyName("GatherTerminalDownloadTime")]
    public double GatherTerminalDownloadTime
    {
        get => PrimaryObjective.GatherTerminalDownloadTime;
        set => PrimaryObjective.GatherTerminalDownloadTime = value;
    }

    [JsonPropertyName("GatherTerminalPlacements")]
    public ObservableCollection<GatherItemPlacement> GatherTerminalPlacements
    {
        get => PrimaryObjective.GatherTerminalPlacements;
        set => PrimaryObjective.GatherTerminalPlacements = value;
    }

    [JsonPropertyName("TimedTerminalSequenceMainLocation")]
    public CandidateZonePlacement TimedTerminalSequenceMainLocation
    {
        get => PrimaryObjective.TimedTerminalSequenceMainLocation;
        set => PrimaryObjective.TimedTerminalSequenceMainLocation = value;
    }

    [JsonPropertyName("TimedTerminalSequenceNumberOfRounds")]
    public int TimedTerminalSequenceNumberOfRounds
    {
        get => PrimaryObjective.TimedTerminalSequenceNumberOfRounds;
        set => PrimaryObjective.TimedTerminalSequenceNumberOfRounds = value;
    }

    [JsonPropertyName("TimedTerminalSequenceTimePerRound")]
    public double TimedTerminalSequenceTimePerRound
    {
        get => PrimaryObjective.TimedTerminalSequenceTimePerRound;
        set => PrimaryObjective.TimedTerminalSequenceTimePerRound = value;
    }

    [JsonPropertyName("TimedTerminalSequenceTimeForConfirmation")]
    public double TimedTerminalSequenceTimeForConfirmation
    {
        get => PrimaryObjective.TimedTerminalSequenceTimeForConfirmation;
        set => PrimaryObjective.TimedTerminalSequenceTimeForConfirmation = value;
    }

    [JsonPropertyName("TimedTerminalSequenceSourceTerminalWorldEventObjectFilter")]
    public string TimedTerminalSequenceSourceTerminalWorldEventObjectFilter
    {
        get => PrimaryObjective.TimedTerminalSequenceSourceTerminalWorldEventObjectFilter;
        set => PrimaryObjective.TimedTerminalSequenceSourceTerminalWorldEventObjectFilter = value;
    }

    [JsonPropertyName("TimedTerminalSequenceNumberOfTerminals")]
    public int TimedTerminalSequenceNumberOfTerminals
    {
        get => PrimaryObjective.TimedTerminalSequenceNumberOfTerminals;
        set => PrimaryObjective.TimedTerminalSequenceNumberOfTerminals = value;
    }

    [JsonPropertyName("TimedTerminalSequencePlacements")]
    public ObservableCollection<GatherItemPlacement> TimedTerminalSequencePlacements
    {
        get => PrimaryObjective.TimedTerminalSequencePlacements;
        set => PrimaryObjective.TimedTerminalSequencePlacements = value;
    }

    [JsonPropertyName("Header")]
    public string Header
    {
        get => PrimaryObjective.Header;
        set => PrimaryObjective.Header = value;
    }

    [JsonPropertyName("MainObjective")]
    public string MainObjective
    {
        get => PrimaryObjective.MainObjective;
        set => PrimaryObjective.MainObjective = value;
    }

    [JsonPropertyName("FindLocationInfo")]
    public string FindLocationInfo
    {
        get => PrimaryObjective.FindLocationInfo;
        set => PrimaryObjective.FindLocationInfo = value;
    }

    [JsonPropertyName("FindLocationInfoHelp")]
    public string FindLocationInfoHelp
    {
        get => PrimaryObjective.FindLocationInfoHelp;
        set => PrimaryObjective.FindLocationInfoHelp = value;
    }

    [JsonPropertyName("GoToZone")]
    public string GoToZone
    {
        get => PrimaryObjective.GoToZone;
        set => PrimaryObjective.GoToZone = value;
    }

    [JsonPropertyName("InZoneFindItem")]
    public string InZoneFindItem
    {
        get => PrimaryObjective.InZoneFindItem;
        set => PrimaryObjective.InZoneFindItem = value;
    }

    [JsonPropertyName("SolveItem")]
    public string SolveItem
    {
        get => PrimaryObjective.SolveItem;
        set => PrimaryObjective.SolveItem = value;
    }

    [JsonPropertyName("WavesOnGotoWin")]
    public GenericWaveSettings WavesOnGotoWin
    {
        get => PrimaryObjective.WavesOnGotoWin;
        set => PrimaryObjective.WavesOnGotoWin = value;
    }

    [JsonPropertyName("WaveOnGotoWinTrigger")]
    public WaveOnGotoWinTrigger WaveOnGotoWinTrigger
    {
        get => PrimaryObjective.WaveOnGotoWinTrigger;
        set => PrimaryObjective.WaveOnGotoWinTrigger = value;
    }

    [JsonPropertyName("StopAllWavesBeforeGotoWin")]
    public bool StopAllWavesBeforeGotoWin
    {
        get => PrimaryObjective.StopAllWavesBeforeGotoWin;
        set => PrimaryObjective.StopAllWavesBeforeGotoWin = value;
    }

    [JsonPropertyName("WavesOnElevatorLand")]
    public GenericWaveSettings WavesOnElevatorLand
    {
        get => PrimaryObjective.WavesOnElevatorLand;
        set => PrimaryObjective.WavesOnElevatorLand = value;
    }

    [JsonPropertyName("WavesOnActivate")]
    public GenericWaveSettings WavesOnActivate
    {
        get => PrimaryObjective.WavesOnActivate;
        set => PrimaryObjective.WavesOnActivate = value;
    }

    [JsonPropertyName("FogTransitionDataOnGotoWin")]
    public uint FogTransitionDataOnGotoWin
    {
        get => PrimaryObjective.FogTransitionDataOnGotoWin;
        set => PrimaryObjective.FogTransitionDataOnGotoWin = value;
    }

    [JsonPropertyName("FogTransitionDataOnGotoWinDuration")]
    public double FogTransitionDataOnGotoWinDuration
    {
        get => PrimaryObjective.FogTransitionDataOnGotoWinDuration;
        set => PrimaryObjective.FogTransitionDataOnGotoWinDuration = value;
    }

    [JsonPropertyName("EventsOnActivation")]
    public ObservableCollection<WardenEvent> EventsOnActivation
    {
        get => PrimaryObjective.EventsOnActivation;
        set => PrimaryObjective.EventsOnActivation = value;
    }

    [JsonPropertyName("EventsOnSequenceStart")]
    public ObservableCollection<WardenEvent> EventsOnSequenceStart
    {
        get => PrimaryObjective.EventsOnSequenceStart;
        set => PrimaryObjective.EventsOnSequenceStart = value;
    }

    [JsonPropertyName("EventsOnSequenceDone")]
    public ObservableCollection<WardenEvent> EventsOnSequenceDone
    {
        get => PrimaryObjective.EventsOnSequenceDone;
        set => PrimaryObjective.EventsOnSequenceDone = value;
    }

    [JsonPropertyName("EventsOnSequenceFail")]
    public ObservableCollection<WardenEvent> EventsOnSequenceFail
    {
        get => PrimaryObjective.EventsOnSequenceFail;
        set => PrimaryObjective.EventsOnSequenceFail = value;
    }

    [JsonPropertyName("EventsOnElevatorLand")]
    public ObservableCollection<WardenEvent> EventsOnElevatorLand
    {
        get => PrimaryObjective.EventsOnElevatorLand;
        set => PrimaryObjective.EventsOnElevatorLand = value;
    }

    [JsonPropertyName("EventsOnGotoWin")]
    public ObservableCollection<WardenEvent> EventsOnGotoWin
    {
        get => PrimaryObjective.EventsOnGotoWin;
        set => PrimaryObjective.EventsOnGotoWin = value;
    }

    [JsonPropertyName("EventsOnActivate")]
    public ObservableCollection<WardenEvent> EventsOnActivate
    {
        get => PrimaryObjective.EventsOnActivate;
        set => PrimaryObjective.EventsOnActivate = value;
    }

    public string GetZoneDisplayName(int zoneIndex) => $"Zone {ZoneAliasStart + zoneIndex}";

    public void ApplyDefaultsForObjectiveType(ObjectiveType type)
    {
        PrimaryObjective.ApplyDefaultsForObjectiveType(type);
    }

    public SectorSettings Clone()
    {
        var clone = new SectorSettings
        {
            ArtifactAmountMultiplier = ArtifactAmountMultiplier,
            ZoneAliasStart = ZoneAliasStart,
            BulkheadKeyPlacements = new ObservableCollection<GatherItemPlacement>(BulkheadKeyPlacements.Select(p => p.Clone())),
            Objectives = new ObservableCollection<WardenObjective>(Objectives.Select(o => o.Clone()))
        };

        return clone;
    }

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
