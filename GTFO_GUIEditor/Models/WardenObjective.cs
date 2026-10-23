using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;
using GTFO_GUIEditor.Services;

namespace GTFO_GUIEditor.Models;

public class WardenObjective : INotifyPropertyChanged
{
    private int _objectiveNumber = 1;
    private WinCondition _winCondition = WinCondition.GoToElevator;
    private int _exitZone;
    private ObjectiveType _objectiveType = ObjectiveType.HSU_FindTakeSample;

    // HSU
    private int _hsuPlacementZone;
    private double _hsuPlacementStartWeight;
    private double _hsuPlacementMiddleWeight;
    private double _hsuPlacementEndWeight;

    // Reactor
    private int _reactorPlacementZone;
    private ObservableCollection<ReactorStartupStage> _reactorStartupStages = new();
    private uint _chainedPuzzleToActive;
    private uint _chainedPuzzleMidObjective;
    private bool _doNotSolveObjectiveOnReactorComplete;

    // Special Terminal Command
    private int _specialTerminalCommandZone;
    private string _specialTerminalCommand = string.Empty;
    private string _specialTerminalCommandDesc = string.Empty;
    private string _postCommandOutput = string.Empty;
    private SpecialCommandRule _specialCommandRule = SpecialCommandRule.Normal;

    // Items
    private GatherSmallItemType _gatherItem = GatherSmallItemType.PersonnelID;
    private RetrieveBigItemType _retrieveItem = RetrieveBigItemType.PowerCell;
    private ObservableCollection<GatherItemPlacement> _gatherItemPlacements = new()
    {
        new GatherItemPlacement()
    };
    private ObservableCollection<GatherItemPlacement> _retrieveItemPlacements = new()
    {
        new GatherItemPlacement()
    };
    private int _gatherMaxPerZone;
    private int _retrieveMaxPerZone;

    // Distribute Power Cells
    private ObservableCollection<DistributeGeneratorPlacement> _distributeGeneratorPlacements = new()
    {
        new DistributeGeneratorPlacement()
    };

    // Terminal Uplink
    private int _uplinkNumberOfVerificationRounds = 1;
    private ObservableCollection<GatherItemPlacement> _terminalUplinkPlacements = new()
    {
        new GatherItemPlacement { ItemNumber = 1, ItemPrefix = "Terminal" }
    };

    // Central Generator Cluster
    private int _centralGeneratorClusterZone;
    private ObservableCollection<GatherItemPlacement> _centralGeneratorClusterPowerCellPlacements = new()
    {
        new GatherItemPlacement { ItemNumber = 1, ItemPrefix = "Power Cell" },
        new GatherItemPlacement { ItemNumber = 2, ItemPrefix = "Power Cell" },
        new GatherItemPlacement { ItemNumber = 3, ItemPrefix = "Power Cell" },
        new GatherItemPlacement { ItemNumber = 4, ItemPrefix = "Power Cell" }
    };
    private ObservableCollection<CentralGeneratorFogTransitionStage> _centralGeneratorClusterFogTransitions = new()
    {
        new CentralGeneratorFogTransitionStage { StageNumber = 1 },
        new CentralGeneratorFogTransitionStage { StageNumber = 2 },
        new CentralGeneratorFogTransitionStage { StageNumber = 3 },
        new CentralGeneratorFogTransitionStage { StageNumber = 4 }
    };

    // Small HSU
    private int _activateSmallHsuZone;
    private RetrieveBigItemType _itemFromStart = RetrieveBigItemType.PowerCell;
    private RetrieveBigItemType _itemAfterActivation = RetrieveBigItemType.PowerCell;
    private bool _bringItemInElevator;
    private bool _stopEnemyWavesOnActivation;
    private bool _objectiveCompleteAfterInsertion;
    private bool _requireItemAfterActivationInExitScan;

    // Survival
    private int _timeToActivate;
    private int _timeToSurvive;
    private string _timerTitle = string.Empty;
    private string _timerToActivateTitle = string.Empty;

    // Gather Terminal
    private int _gatherTerminalSpawnCount = 1;
    private int _gatherTerminalRequiredCount = 1;
    private string _gatherTerminalCommand = string.Empty;
    private string _gatherTerminalCommandHelp = string.Empty;
    private string _gatherTerminalDownloadingText = string.Empty;
    private string _gatherTerminalDownloadCompleteText = string.Empty;
    private double _gatherTerminalDownloadTime;
    private ObservableCollection<GatherItemPlacement> _gatherTerminalPlacements = new()
    {
        new GatherItemPlacement { ItemNumber = 1, ItemPrefix = "Terminal" }
    };

    // Timed Terminal Sequence
    private CandidateZonePlacement _timedTerminalSequenceMainLocation = new();
    private int _timedTerminalSequenceNumberOfRounds = 1;
    private double _timedTerminalSequenceTimePerRound;
    private double _timedTerminalSequenceTimeForConfirmation;
    private string _timedTerminalSequenceSourceTerminalWorldEventObjectFilter = "MainTerminal";
    private int _timedTerminalSequenceNumberOfTerminals = 1;
    private ObservableCollection<GatherItemPlacement> _timedTerminalSequencePlacements = new()
    {
        new GatherItemPlacement { ItemNumber = 1, ItemPrefix = "Terminal" }
    };

    // Objective Texts
    private string _header = string.Empty;
    private string _mainObjective = string.Empty;
    private string _findLocationInfo = string.Empty;
    private string _findLocationInfoHelp = string.Empty;
    private string _goToZone = string.Empty;
    private string _inZoneFindItem = string.Empty;
    private string _solveItem = string.Empty;

    // Waves / Triggers
    private GenericWaveSettings _wavesOnGotoWin = new();
    private WaveOnGotoWinTrigger _waveOnGotoWinTrigger = WaveOnGotoWinTrigger.OnObjectiveCompleted;
    private bool _stopAllWavesBeforeGotoWin;
    private GenericWaveSettings _wavesOnElevatorLand = new();
    private uint _fogTransitionDataOnGotoWin;
    private double _fogTransitionDataOnGotoWinDuration;

    // Events
    private ObservableCollection<WardenEvent> _eventsOnActivation = new();
    private ObservableCollection<WardenEvent> _eventsOnSequenceStart = new();
    private ObservableCollection<WardenEvent> _eventsOnSequenceDone = new();
    private ObservableCollection<WardenEvent> _eventsOnSequenceFail = new();
    private ObservableCollection<WardenEvent> _eventsOnElevatorLand = new();
    private ObservableCollection<WardenEvent> _eventsOnGotoWin = new();
    private ObservableCollection<WardenEvent> _eventsOnActivate = new();

    public event PropertyChangedEventHandler? PropertyChanged;

    public WardenObjective()
    {
        ApplyDefaultsForObjectiveType(_objectiveType);
    }

    public int ObjectiveNumber
    {
        get => _objectiveNumber;
        set
        {
            if (SetField(ref _objectiveNumber, value))
            {
                OnPropertyChanged(nameof(DisplayTitle));
            }
        }
    }

    public string DisplayTitle
    {
        get
        {
            string friendlyName = ObjectiveType switch
            {
                ObjectiveType.HSU_FindTakeSample => "HSU - Find & Take Sample",
                ObjectiveType.Reactor_Startup => "Reactor Startup",
                ObjectiveType.Reactor_Shutdown => "Reactor Shutdown",
                ObjectiveType.GatherSmallItems => "Gather Small Items",
                ObjectiveType.ClearAPath => "Clear a Path",
                ObjectiveType.SpecialTerminalCommand => "Special Terminal Command",
                ObjectiveType.RetrieveBigItems => "Retrieve Big Items",
                ObjectiveType.DistributePowerCells => "Distribute Power Cells",
                ObjectiveType.TerminalUplink => "Terminal Uplink",
                ObjectiveType.CentralGeneratorCluster => "Central Generator Cluster",
                ObjectiveType.ActivateSmallHSU => "Activate Item",
                ObjectiveType.Survival => "Survival",
                ObjectiveType.GatherTerminal => "Gather Terminal",
                ObjectiveType.CorruptedTerminalUplink => "Corrupted Terminal Uplink",
                ObjectiveType.TimedTerminalSequence => "Timed Terminal Sequence",
                _ => ObjectiveType.ToString()
            };
            return $"Objective {ObjectiveNumber}: {friendlyName}";
        }
    }

    public WinCondition WinCondition
    {
        get => _winCondition;
        set => SetField(ref _winCondition, value);
    }

    public int ExitZone
    {
        get => _exitZone;
        set => SetField(ref _exitZone, Math.Max(0, value));
    }

    public ObjectiveType ObjectiveType
    {
        get => _objectiveType;
        set
        {
            if (SetField(ref _objectiveType, value))
            {
                ApplyDefaultsForObjectiveType(value);
                OnPropertyChanged(nameof(DisplayTitle));
            }
        }
    }

    public int HsuPlacementZone
    {
        get => _hsuPlacementZone;
        set => SetField(ref _hsuPlacementZone, Math.Max(0, value));
    }

    public double HsuPlacementStartWeight
    {
        get => _hsuPlacementStartWeight;
        set => SetField(ref _hsuPlacementStartWeight, Math.Clamp(value, 0.0, 1.0));
    }

    public double HsuPlacementMiddleWeight
    {
        get => _hsuPlacementMiddleWeight;
        set => SetField(ref _hsuPlacementMiddleWeight, Math.Clamp(value, 0.0, 1.0));
    }

    public double HsuPlacementEndWeight
    {
        get => _hsuPlacementEndWeight;
        set => SetField(ref _hsuPlacementEndWeight, Math.Clamp(value, 0.0, 1.0));
    }

    public int ReactorPlacementZone
    {
        get => _reactorPlacementZone;
        set => SetField(ref _reactorPlacementZone, Math.Max(0, value));
    }

    public ObservableCollection<ReactorStartupStage> ReactorStartupStages
    {
        get => _reactorStartupStages;
        set => SetField(ref _reactorStartupStages, value ?? new ObservableCollection<ReactorStartupStage>());
    }

    public uint ChainedPuzzleToActive
    {
        get => _chainedPuzzleToActive;
        set => SetField(ref _chainedPuzzleToActive, value);
    }

    public uint ChainedPuzzleMidObjective
    {
        get => _chainedPuzzleMidObjective;
        set => SetField(ref _chainedPuzzleMidObjective, value);
    }

    public bool DoNotSolveObjectiveOnReactorComplete
    {
        get => _doNotSolveObjectiveOnReactorComplete;
        set => SetField(ref _doNotSolveObjectiveOnReactorComplete, value);
    }

    public int SpecialTerminalCommandZone
    {
        get => _specialTerminalCommandZone;
        set => SetField(ref _specialTerminalCommandZone, Math.Max(0, value));
    }

    public string SpecialTerminalCommand
    {
        get => _specialTerminalCommand;
        set => SetField(ref _specialTerminalCommand, value ?? string.Empty);
    }

    public string SpecialTerminalCommandDesc
    {
        get => _specialTerminalCommandDesc;
        set => SetField(ref _specialTerminalCommandDesc, value ?? string.Empty);
    }

    public string PostCommandOutput
    {
        get => _postCommandOutput;
        set => SetField(ref _postCommandOutput, value ?? string.Empty);
    }

    public SpecialCommandRule SpecialCommandRule
    {
        get => _specialCommandRule;
        set => SetField(ref _specialCommandRule, value);
    }

    public GatherSmallItemType GatherItem
    {
        get => _gatherItem;
        set => SetField(ref _gatherItem, value);
    }

    public RetrieveBigItemType RetrieveItem
    {
        get => _retrieveItem;
        set => SetField(ref _retrieveItem, value);
    }

    public ObservableCollection<GatherItemPlacement> GatherItemPlacements
    {
        get => _gatherItemPlacements;
        set => SetField(ref _gatherItemPlacements, value ?? new ObservableCollection<GatherItemPlacement>());
    }

    [JsonIgnore]
    public int GatherSpawnCount
    {
        get => GatherItemPlacements.Count;
        set
        {
            int target = Math.Max(1, value);
            while (GatherItemPlacements.Count < target)
            {
                GatherItemPlacements.Add(new GatherItemPlacement
                {
                    ItemNumber = GatherItemPlacements.Count + 1
                });
            }

            while (GatherItemPlacements.Count > target)
            {
                GatherItemPlacements.RemoveAt(GatherItemPlacements.Count - 1);
            }

            OnPropertyChanged(nameof(GatherSpawnCount));
        }
    }

    public ObservableCollection<GatherItemPlacement> RetrieveItemPlacements
    {
        get => _retrieveItemPlacements;
        set => SetField(ref _retrieveItemPlacements, value ?? new ObservableCollection<GatherItemPlacement>());
    }

    [JsonIgnore]
    public int RetrieveSpawnCount
    {
        get => RetrieveItemPlacements.Count;
        set
        {
            int target = Math.Max(1, value);
            while (RetrieveItemPlacements.Count < target)
            {
                RetrieveItemPlacements.Add(new GatherItemPlacement
                {
                    ItemNumber = RetrieveItemPlacements.Count + 1
                });
            }

            while (RetrieveItemPlacements.Count > target)
            {
                RetrieveItemPlacements.RemoveAt(RetrieveItemPlacements.Count - 1);
            }

            OnPropertyChanged(nameof(RetrieveSpawnCount));
        }
    }

    public int GatherMaxPerZone
    {
        get => _gatherMaxPerZone;
        set => SetField(ref _gatherMaxPerZone, Math.Max(0, value));
    }

    public int RetrieveMaxPerZone
    {
        get => _retrieveMaxPerZone;
        set => SetField(ref _retrieveMaxPerZone, Math.Max(0, value));
    }

    public ObservableCollection<DistributeGeneratorPlacement> DistributeGeneratorPlacements
    {
        get => _distributeGeneratorPlacements;
        set => SetField(ref _distributeGeneratorPlacements, value ?? new ObservableCollection<DistributeGeneratorPlacement>());
    }

    [JsonIgnore]
    public int DistributeGeneratorCount
    {
        get => DistributeGeneratorPlacements.Count;
        set
        {
            int target = Math.Max(0, value);
            while (DistributeGeneratorPlacements.Count < target)
            {
                DistributeGeneratorPlacements.Add(new DistributeGeneratorPlacement
                {
                    GeneratorNumber = DistributeGeneratorPlacements.Count + 1
                });
            }

            while (DistributeGeneratorPlacements.Count > target)
            {
                DistributeGeneratorPlacements.RemoveAt(DistributeGeneratorPlacements.Count - 1);
            }

            OnPropertyChanged(nameof(DistributeGeneratorCount));
        }
    }

    public int UplinkNumberOfVerificationRounds
    {
        get => _uplinkNumberOfVerificationRounds;
        set => SetField(ref _uplinkNumberOfVerificationRounds, Math.Max(1, value));
    }

    public ObservableCollection<GatherItemPlacement> TerminalUplinkPlacements
    {
        get => _terminalUplinkPlacements;
        set => SetField(ref _terminalUplinkPlacements, value ?? new ObservableCollection<GatherItemPlacement>());
    }

    [JsonIgnore]
    public int UplinkNumberOfTerminals
    {
        get => TerminalUplinkPlacements.Count;
        set
        {
            int target = Math.Max(1, value);
            while (TerminalUplinkPlacements.Count < target)
            {
                TerminalUplinkPlacements.Add(new GatherItemPlacement
                {
                    ItemNumber = TerminalUplinkPlacements.Count + 1,
                    ItemPrefix = "Terminal"
                });
            }

            while (TerminalUplinkPlacements.Count > target)
            {
                TerminalUplinkPlacements.RemoveAt(TerminalUplinkPlacements.Count - 1);
            }

            OnPropertyChanged(nameof(UplinkNumberOfTerminals));
        }
    }

    public int CentralGeneratorClusterZone
    {
        get => _centralGeneratorClusterZone;
        set => SetField(ref _centralGeneratorClusterZone, Math.Max(0, value));
    }

    [JsonIgnore]
    public int CentralGeneratorClusterGeneratorCount
    {
        get => CentralGeneratorClusterFogTransitions.Count;
        set
        {
            if (value >= 1)
            {
                while (CentralGeneratorClusterFogTransitions.Count < value)
                {
                    CentralGeneratorClusterFogTransitions.Add(new CentralGeneratorFogTransitionStage
                    {
                        StageNumber = CentralGeneratorClusterFogTransitions.Count + 1
                    });
                }
                while (CentralGeneratorClusterFogTransitions.Count > value)
                {
                    CentralGeneratorClusterFogTransitions.RemoveAt(CentralGeneratorClusterFogTransitions.Count - 1);
                }
                OnPropertyChanged();
            }
        }
    }

    [JsonIgnore]
    public int CentralGeneratorClusterPowerCellCount
    {
        get => CentralGeneratorClusterPowerCellPlacements.Count;
        set
        {
            if (value >= 1)
            {
                while (CentralGeneratorClusterPowerCellPlacements.Count < value)
                {
                    CentralGeneratorClusterPowerCellPlacements.Add(new GatherItemPlacement
                    {
                        ItemNumber = CentralGeneratorClusterPowerCellPlacements.Count + 1,
                        ItemPrefix = "Power Cell"
                    });
                }
                while (CentralGeneratorClusterPowerCellPlacements.Count > value)
                {
                    CentralGeneratorClusterPowerCellPlacements.RemoveAt(CentralGeneratorClusterPowerCellPlacements.Count - 1);
                }
                OnPropertyChanged();
            }
        }
    }

    public ObservableCollection<GatherItemPlacement> CentralGeneratorClusterPowerCellPlacements
    {
        get => _centralGeneratorClusterPowerCellPlacements;
        set => SetField(ref _centralGeneratorClusterPowerCellPlacements, value ?? new ObservableCollection<GatherItemPlacement>());
    }

    public ObservableCollection<CentralGeneratorFogTransitionStage> CentralGeneratorClusterFogTransitions
    {
        get => _centralGeneratorClusterFogTransitions;
        set => SetField(ref _centralGeneratorClusterFogTransitions, value ?? new ObservableCollection<CentralGeneratorFogTransitionStage>());
    }

    public int ActivateSmallHsuZone
    {
        get => _activateSmallHsuZone;
        set => SetField(ref _activateSmallHsuZone, Math.Max(0, value));
    }

    public RetrieveBigItemType ItemFromStart
    {
        get => _itemFromStart;
        set => SetField(ref _itemFromStart, value);
    }

    public RetrieveBigItemType ItemAfterActivation
    {
        get => _itemAfterActivation;
        set => SetField(ref _itemAfterActivation, value);
    }

    public bool BringItemInElevator
    {
        get => _bringItemInElevator;
        set => SetField(ref _bringItemInElevator, value);
    }

    public bool StopEnemyWavesOnActivation
    {
        get => _stopEnemyWavesOnActivation;
        set => SetField(ref _stopEnemyWavesOnActivation, value);
    }

    public bool ObjectiveCompleteAfterInsertion
    {
        get => _objectiveCompleteAfterInsertion;
        set => SetField(ref _objectiveCompleteAfterInsertion, value);
    }

    public bool RequireItemAfterActivationInExitScan
    {
        get => _requireItemAfterActivationInExitScan;
        set => SetField(ref _requireItemAfterActivationInExitScan, value);
    }

    public int TimeToActivate
    {
        get => _timeToActivate;
        set => SetField(ref _timeToActivate, Math.Max(0, value));
    }

    public int TimeToSurvive
    {
        get => _timeToSurvive;
        set => SetField(ref _timeToSurvive, Math.Max(0, value));
    }

    public string TimerTitle
    {
        get => _timerTitle;
        set => SetField(ref _timerTitle, value ?? string.Empty);
    }

    public string TimerToActivateTitle
    {
        get => _timerToActivateTitle;
        set => SetField(ref _timerToActivateTitle, value ?? string.Empty);
    }

    [JsonIgnore]
    public int GatherTerminalSpawnCount
    {
        get => GatherTerminalPlacements.Count;
        set
        {
            int target = Math.Max(1, value);
            while (GatherTerminalPlacements.Count < target)
            {
                GatherTerminalPlacements.Add(new GatherItemPlacement
                {
                    ItemNumber = GatherTerminalPlacements.Count + 1,
                    ItemPrefix = "Terminal"
                });
            }

            while (GatherTerminalPlacements.Count > target)
            {
                GatherTerminalPlacements.RemoveAt(GatherTerminalPlacements.Count - 1);
            }

            OnPropertyChanged(nameof(GatherTerminalSpawnCount));
        }
    }

    public int GatherTerminalRequiredCount
    {
        get => _gatherTerminalRequiredCount;
        set => SetField(ref _gatherTerminalRequiredCount, Math.Max(1, value));
    }

    public string GatherTerminalCommand
    {
        get => _gatherTerminalCommand;
        set => SetField(ref _gatherTerminalCommand, value ?? string.Empty);
    }

    public string GatherTerminalCommandHelp
    {
        get => _gatherTerminalCommandHelp;
        set => SetField(ref _gatherTerminalCommandHelp, value ?? string.Empty);
    }

    public string GatherTerminalDownloadingText
    {
        get => _gatherTerminalDownloadingText;
        set => SetField(ref _gatherTerminalDownloadingText, value ?? string.Empty);
    }

    public string GatherTerminalDownloadCompleteText
    {
        get => _gatherTerminalDownloadCompleteText;
        set => SetField(ref _gatherTerminalDownloadCompleteText, value ?? string.Empty);
    }

    public double GatherTerminalDownloadTime
    {
        get => _gatherTerminalDownloadTime;
        set => SetField(ref _gatherTerminalDownloadTime, Math.Max(0.0, value));
    }

    public ObservableCollection<GatherItemPlacement> GatherTerminalPlacements
    {
        get => _gatherTerminalPlacements;
        set => SetField(ref _gatherTerminalPlacements, value ?? new ObservableCollection<GatherItemPlacement>());
    }

    public CandidateZonePlacement TimedTerminalSequenceMainLocation
    {
        get => _timedTerminalSequenceMainLocation;
        set => SetField(ref _timedTerminalSequenceMainLocation, value ?? new CandidateZonePlacement());
    }

    public int TimedTerminalSequenceNumberOfRounds
    {
        get => _timedTerminalSequenceNumberOfRounds;
        set => SetField(ref _timedTerminalSequenceNumberOfRounds, Math.Max(1, value));
    }

    public double TimedTerminalSequenceTimePerRound
    {
        get => _timedTerminalSequenceTimePerRound;
        set => SetField(ref _timedTerminalSequenceTimePerRound, Math.Max(0.0, value));
    }

    public double TimedTerminalSequenceTimeForConfirmation
    {
        get => _timedTerminalSequenceTimeForConfirmation;
        set => SetField(ref _timedTerminalSequenceTimeForConfirmation, Math.Max(0.0, value));
    }

    public string TimedTerminalSequenceSourceTerminalWorldEventObjectFilter
    {
        get => _timedTerminalSequenceSourceTerminalWorldEventObjectFilter;
        set => SetField(ref _timedTerminalSequenceSourceTerminalWorldEventObjectFilter, value ?? string.Empty);
    }

    [JsonIgnore]
    public int TimedTerminalSequenceNumberOfTerminals
    {
        get => TimedTerminalSequencePlacements.Count;
        set
        {
            int target = Math.Max(1, value);
            while (TimedTerminalSequencePlacements.Count < target)
            {
                TimedTerminalSequencePlacements.Add(new GatherItemPlacement
                {
                    ItemNumber = TimedTerminalSequencePlacements.Count + 1,
                    ItemPrefix = "Terminal"
                });
            }

            while (TimedTerminalSequencePlacements.Count > target)
            {
                TimedTerminalSequencePlacements.RemoveAt(TimedTerminalSequencePlacements.Count - 1);
            }

            OnPropertyChanged(nameof(TimedTerminalSequenceNumberOfTerminals));
        }
    }

    public ObservableCollection<GatherItemPlacement> TimedTerminalSequencePlacements
    {
        get => _timedTerminalSequencePlacements;
        set => SetField(ref _timedTerminalSequencePlacements, value ?? new ObservableCollection<GatherItemPlacement>());
    }

    public string Header
    {
        get => _header;
        set => SetField(ref _header, value ?? string.Empty);
    }

    public string MainObjective
    {
        get => _mainObjective;
        set => SetField(ref _mainObjective, value ?? string.Empty);
    }

    public string FindLocationInfo
    {
        get => _findLocationInfo;
        set => SetField(ref _findLocationInfo, value ?? string.Empty);
    }

    public string FindLocationInfoHelp
    {
        get => _findLocationInfoHelp;
        set => SetField(ref _findLocationInfoHelp, value ?? string.Empty);
    }

    public string GoToZone
    {
        get => _goToZone;
        set => SetField(ref _goToZone, value ?? string.Empty);
    }

    public string InZoneFindItem
    {
        get => _inZoneFindItem;
        set => SetField(ref _inZoneFindItem, value ?? string.Empty);
    }

    public string SolveItem
    {
        get => _solveItem;
        set => SetField(ref _solveItem, value ?? string.Empty);
    }

    public GenericWaveSettings WavesOnGotoWin
    {
        get => _wavesOnGotoWin;
        set => SetField(ref _wavesOnGotoWin, value ?? new GenericWaveSettings());
    }

    public WaveOnGotoWinTrigger WaveOnGotoWinTrigger
    {
        get => _waveOnGotoWinTrigger;
        set => SetField(ref _waveOnGotoWinTrigger, value);
    }

    public bool StopAllWavesBeforeGotoWin
    {
        get => _stopAllWavesBeforeGotoWin;
        set => SetField(ref _stopAllWavesBeforeGotoWin, value);
    }

    public GenericWaveSettings WavesOnElevatorLand
    {
        get => _wavesOnElevatorLand;
        set => SetField(ref _wavesOnElevatorLand, value ?? new GenericWaveSettings());
    }

    public uint FogTransitionDataOnGotoWin
    {
        get => _fogTransitionDataOnGotoWin;
        set => SetField(ref _fogTransitionDataOnGotoWin, value);
    }

    public double FogTransitionDataOnGotoWinDuration
    {
        get => _fogTransitionDataOnGotoWinDuration;
        set => SetField(ref _fogTransitionDataOnGotoWinDuration, Math.Max(0.0, value));
    }

    public ObservableCollection<WardenEvent> EventsOnActivation
    {
        get => _eventsOnActivation;
        set => SetField(ref _eventsOnActivation, value ?? new ObservableCollection<WardenEvent>());
    }

    public ObservableCollection<WardenEvent> EventsOnSequenceStart
    {
        get => _eventsOnSequenceStart;
        set => SetField(ref _eventsOnSequenceStart, value ?? new ObservableCollection<WardenEvent>());
    }

    public ObservableCollection<WardenEvent> EventsOnSequenceDone
    {
        get => _eventsOnSequenceDone;
        set => SetField(ref _eventsOnSequenceDone, value ?? new ObservableCollection<WardenEvent>());
    }

    public ObservableCollection<WardenEvent> EventsOnSequenceFail
    {
        get => _eventsOnSequenceFail;
        set => SetField(ref _eventsOnSequenceFail, value ?? new ObservableCollection<WardenEvent>());
    }

    public ObservableCollection<WardenEvent> EventsOnElevatorLand
    {
        get => _eventsOnElevatorLand;
        set => SetField(ref _eventsOnElevatorLand, value ?? new ObservableCollection<WardenEvent>());
    }

    public ObservableCollection<WardenEvent> EventsOnGotoWin
    {
        get => _eventsOnGotoWin;
        set => SetField(ref _eventsOnGotoWin, value ?? new ObservableCollection<WardenEvent>());
    }

    public ObservableCollection<WardenEvent> EventsOnActivate
    {
        get => _eventsOnActivate;
        set => SetField(ref _eventsOnActivate, value ?? new ObservableCollection<WardenEvent>());
    }

    public void ApplyDefaultsForObjectiveType(ObjectiveType type)
    {
        var defaults = ObjectiveDefaultsService.GetDefaults(type);
        Header = defaults.Header;
        MainObjective = defaults.MainObjective;
        FindLocationInfo = defaults.FindLocationInfo;
        FindLocationInfoHelp = defaults.FindLocationInfoHelp;
        GoToZone = defaults.GoToZone;
        InZoneFindItem = defaults.InZoneFindItem;
        SolveItem = defaults.SolveItem;

        if (type == ObjectiveType.Survival)
        {
            TimerTitle = "SURVIVAL TIMER";
            TimerToActivateTitle = "INITIATING EXTRACTION SEQUENCE";
            TimeToActivate = 0;
            TimeToSurvive = 120;
        }
        else if (type == ObjectiveType.GatherTerminal)
        {
            GatherTerminalCommand = "GET_DATA";
            GatherTerminalCommandHelp = "Downloads archived warden telemetry";
            GatherTerminalDownloadingText = "ARCHIVE DOWNLOAD IN PROGRESS...";
            GatherTerminalDownloadCompleteText = "DOWNLOAD COMPLETE";
            GatherTerminalDownloadTime = 20.0;
            GatherTerminalSpawnCount = 1;
            GatherTerminalRequiredCount = 1;
        }
        else if (type == ObjectiveType.TimedTerminalSequence)
        {
            TimedTerminalSequenceNumberOfRounds = 3;
            TimedTerminalSequenceTimePerRound = 90.0;
            TimedTerminalSequenceTimeForConfirmation = 10.0;
            TimedTerminalSequenceNumberOfTerminals = 1;
            TimedTerminalSequenceSourceTerminalWorldEventObjectFilter = "MainTerminal";
        }
    }

    public WardenObjective Clone()
    {
        var clone = new WardenObjective
        {
            ObjectiveNumber = ObjectiveNumber,
            WinCondition = WinCondition,
            ExitZone = ExitZone,
            ObjectiveType = ObjectiveType,
            HsuPlacementZone = HsuPlacementZone,
            HsuPlacementStartWeight = HsuPlacementStartWeight,
            HsuPlacementMiddleWeight = HsuPlacementMiddleWeight,
            HsuPlacementEndWeight = HsuPlacementEndWeight,
            ReactorPlacementZone = ReactorPlacementZone,
            ChainedPuzzleToActive = ChainedPuzzleToActive,
            ChainedPuzzleMidObjective = ChainedPuzzleMidObjective,
            DoNotSolveObjectiveOnReactorComplete = DoNotSolveObjectiveOnReactorComplete,
            SpecialTerminalCommandZone = SpecialTerminalCommandZone,
            SpecialTerminalCommand = SpecialTerminalCommand,
            SpecialTerminalCommandDesc = SpecialTerminalCommandDesc,
            PostCommandOutput = PostCommandOutput,
            SpecialCommandRule = SpecialCommandRule,
            GatherItem = GatherItem,
            GatherMaxPerZone = GatherMaxPerZone,
            RetrieveItem = RetrieveItem,
            RetrieveMaxPerZone = RetrieveMaxPerZone,
            Header = Header,
            MainObjective = MainObjective,
            FindLocationInfo = FindLocationInfo,
            FindLocationInfoHelp = FindLocationInfoHelp,
            GoToZone = GoToZone,
            InZoneFindItem = InZoneFindItem,
            SolveItem = SolveItem,
            WaveOnGotoWinTrigger = WaveOnGotoWinTrigger,
            StopAllWavesBeforeGotoWin = StopAllWavesBeforeGotoWin,
            WavesOnGotoWin = WavesOnGotoWin.Clone(),
            WavesOnElevatorLand = WavesOnElevatorLand.Clone(),
            FogTransitionDataOnGotoWin = FogTransitionDataOnGotoWin,
            FogTransitionDataOnGotoWinDuration = FogTransitionDataOnGotoWinDuration,
            UplinkNumberOfVerificationRounds = UplinkNumberOfVerificationRounds,
            CentralGeneratorClusterZone = CentralGeneratorClusterZone,
            ActivateSmallHsuZone = ActivateSmallHsuZone,
            ItemFromStart = ItemFromStart,
            ItemAfterActivation = ItemAfterActivation,
            BringItemInElevator = BringItemInElevator,
            StopEnemyWavesOnActivation = StopEnemyWavesOnActivation,
            ObjectiveCompleteAfterInsertion = ObjectiveCompleteAfterInsertion,
            RequireItemAfterActivationInExitScan = RequireItemAfterActivationInExitScan,
            TimeToActivate = TimeToActivate,
            TimeToSurvive = TimeToSurvive,
            TimerTitle = TimerTitle,
            TimerToActivateTitle = TimerToActivateTitle,
            GatherTerminalSpawnCount = GatherTerminalSpawnCount,
            GatherTerminalRequiredCount = GatherTerminalRequiredCount,
            GatherTerminalCommand = GatherTerminalCommand,
            GatherTerminalCommandHelp = GatherTerminalCommandHelp,
            GatherTerminalDownloadingText = GatherTerminalDownloadingText,
            GatherTerminalDownloadCompleteText = GatherTerminalDownloadCompleteText,
            GatherTerminalDownloadTime = GatherTerminalDownloadTime,
            TimedTerminalSequenceMainLocation = TimedTerminalSequenceMainLocation.Clone(),
            TimedTerminalSequenceNumberOfRounds = TimedTerminalSequenceNumberOfRounds,
            TimedTerminalSequenceTimePerRound = TimedTerminalSequenceTimePerRound,
            TimedTerminalSequenceTimeForConfirmation = TimedTerminalSequenceTimeForConfirmation,
            TimedTerminalSequenceSourceTerminalWorldEventObjectFilter = TimedTerminalSequenceSourceTerminalWorldEventObjectFilter,
            TimedTerminalSequenceNumberOfTerminals = TimedTerminalSequenceNumberOfTerminals
        };

        clone.ReactorStartupStages.Clear();
        foreach (var stage in ReactorStartupStages)
        {
            clone.ReactorStartupStages.Add(stage.Clone());
        }

        clone.GatherItemPlacements.Clear();
        foreach (var placement in GatherItemPlacements)
        {
            clone.GatherItemPlacements.Add(placement.Clone());
        }

        clone.RetrieveItemPlacements.Clear();
        foreach (var placement in RetrieveItemPlacements)
        {
            clone.RetrieveItemPlacements.Add(placement.Clone());
        }

        clone.DistributeGeneratorPlacements.Clear();
        foreach (var placement in DistributeGeneratorPlacements)
        {
            clone.DistributeGeneratorPlacements.Add(placement.Clone());
        }

        clone.TerminalUplinkPlacements.Clear();
        foreach (var placement in TerminalUplinkPlacements)
        {
            clone.TerminalUplinkPlacements.Add(placement.Clone());
        }

        clone.CentralGeneratorClusterPowerCellPlacements.Clear();
        foreach (var placement in CentralGeneratorClusterPowerCellPlacements)
        {
            clone.CentralGeneratorClusterPowerCellPlacements.Add(placement.Clone());
        }

        clone.CentralGeneratorClusterFogTransitions.Clear();
        foreach (var stage in CentralGeneratorClusterFogTransitions)
        {
            clone.CentralGeneratorClusterFogTransitions.Add(stage.Clone());
        }

        clone.GatherTerminalPlacements.Clear();
        foreach (var placement in GatherTerminalPlacements)
        {
            clone.GatherTerminalPlacements.Add(placement.Clone());
        }

        clone.TimedTerminalSequencePlacements.Clear();
        foreach (var placement in TimedTerminalSequencePlacements)
        {
            clone.TimedTerminalSequencePlacements.Add(placement.Clone());
        }

        clone.EventsOnActivation.Clear();
        foreach (var ev in EventsOnActivation)
        {
            clone.EventsOnActivation.Add(ev.Clone());
        }

        clone.EventsOnSequenceStart.Clear();
        foreach (var ev in EventsOnSequenceStart)
        {
            clone.EventsOnSequenceStart.Add(ev.Clone());
        }

        clone.EventsOnSequenceDone.Clear();
        foreach (var ev in EventsOnSequenceDone)
        {
            clone.EventsOnSequenceDone.Add(ev.Clone());
        }

        clone.EventsOnSequenceFail.Clear();
        foreach (var ev in EventsOnSequenceFail)
        {
            clone.EventsOnSequenceFail.Add(ev.Clone());
        }

        clone.EventsOnElevatorLand.Clear();
        foreach (var ev in EventsOnElevatorLand)
        {
            clone.EventsOnElevatorLand.Add(ev.Clone());
        }

        clone.EventsOnGotoWin.Clear();
        foreach (var ev in EventsOnGotoWin)
        {
            clone.EventsOnGotoWin.Add(ev.Clone());
        }

        clone.EventsOnActivate.Clear();
        foreach (var ev in EventsOnActivate)
        {
            clone.EventsOnActivate.Add(ev.Clone());
        }

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
