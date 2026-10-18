using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;
using GTFO_GUIEditor.Services;

namespace GTFO_GUIEditor.Models;

public class SectorSettings : INotifyPropertyChanged
{
    private double _artifactAmountMultiplier = 1.0;
    private int _zoneAliasStart = 0;
    private WinCondition _winCondition = WinCondition.GoToElevator;
    private int _exitZone = 0;
    private ObjectiveType _objectiveType = ObjectiveType.HSU_FindTakeSample;
    private int _hsuPlacementZone = 0;
    private double _hsuPlacementStartWeight = 0.0;
    private double _hsuPlacementMiddleWeight = 0.0;
    private double _hsuPlacementEndWeight = 0.0;
    private int _reactorPlacementZone = 0;
    private ObservableCollection<ReactorStartupStage> _reactorStartupStages = new();
    private uint _chainedPuzzleToActive = 0;
    private uint _chainedPuzzleMidObjective = 0;
    private bool _doNotSolveObjectiveOnReactorComplete = false;
    private int _specialTerminalCommandZone = 0;
    private string _specialTerminalCommand = string.Empty;
    private string _specialTerminalCommandDesc = string.Empty;
    private string _postCommandOutput = string.Empty;
    private SpecialCommandRule _specialCommandRule = SpecialCommandRule.Normal;
    private GatherSmallItemType _gatherItem = GatherSmallItemType.PersonnelID;
    private RetrieveBigItemType _retrieveItem = RetrieveBigItemType.PowerCell;
    private string _header = string.Empty;
    private string _mainObjective = string.Empty;
    private string _findLocationInfo = string.Empty;
    private string _findLocationInfoHelp = string.Empty;
    private string _goToZone = string.Empty;
    private string _inZoneFindItem = string.Empty;
    private string _solveItem = string.Empty;
    private GenericWaveSettings _wavesOnGotoWin = new();
    private WaveOnGotoWinTrigger _waveOnGotoWinTrigger = WaveOnGotoWinTrigger.OnObjectiveCompleted;
    private bool _stopAllWavesBeforeGotoWin = false;
    private GenericWaveSettings _wavesOnElevatorLand = new();
    private uint _fogTransitionDataOnGotoWin = 0;
    private double _fogTransitionDataOnGotoWinDuration = 0.0;

    private ObservableCollection<GatherItemPlacement> _gatherItemPlacements = new()
    {
        new GatherItemPlacement
        {
            ItemNumber = 1
        }
    };

    private ObservableCollection<GatherItemPlacement> _retrieveItemPlacements = new()
    {
        new GatherItemPlacement
        {
            ItemNumber = 1
        }
    };

    private int _gatherMaxPerZone = 0;
    private int _retrieveMaxPerZone = 0;

    private ObservableCollection<DistributeGeneratorPlacement> _distributeGeneratorPlacements = new()
    {
        new DistributeGeneratorPlacement
        {
            GeneratorNumber = 1,
            Zone = 0
        }
    };

    private int _uplinkNumberOfVerificationRounds = 1;

    private ObservableCollection<GatherItemPlacement> _terminalUplinkPlacements = new()
    {
        new GatherItemPlacement
        {
            ItemNumber = 1,
            ItemPrefix = "Terminal"
        }
    };

    private int _centralGeneratorClusterZone = 0;

    private ObservableCollection<GatherItemPlacement> _centralGeneratorClusterPowerCellPlacements = new()
    {
        new GatherItemPlacement { ItemNumber = 1, ItemPrefix = "Power Cell" },
        new GatherItemPlacement { ItemNumber = 2, ItemPrefix = "Power Cell" },
        new GatherItemPlacement { ItemNumber = 3, ItemPrefix = "Power Cell" },
        new GatherItemPlacement { ItemNumber = 4, ItemPrefix = "Power Cell" }
    };

    private ObservableCollection<CentralGeneratorFogTransitionStage> _centralGeneratorClusterFogTransitions = new()
    {
        new CentralGeneratorFogTransitionStage { StageNumber = 1, FogSetting = 0, TransitionDuration = 0.0 },
        new CentralGeneratorFogTransitionStage { StageNumber = 2, FogSetting = 0, TransitionDuration = 0.0 },
        new CentralGeneratorFogTransitionStage { StageNumber = 3, FogSetting = 0, TransitionDuration = 0.0 },
        new CentralGeneratorFogTransitionStage { StageNumber = 4, FogSetting = 0, TransitionDuration = 0.0 }
    };

    private int _activateSmallHsuZone = 0;
    private RetrieveBigItemType _itemFromStart = RetrieveBigItemType.PowerCell;
    private RetrieveBigItemType _itemAfterActivation = RetrieveBigItemType.PowerCell;
    private bool _bringItemInElevator = false;
    private bool _stopEnemyWavesOnActivation = false;
    private bool _objectiveCompleteAfterInsertion = false;
    private bool _requireItemAfterActivationInExitScan = false;
    private int _timeToActivate = 0;
    private int _timeToSurvive = 0;
    private string _timerTitle = string.Empty;
    private string _timerToActivateTitle = string.Empty;

    private int _gatherTerminalRequiredCount = 1;
    private string _gatherTerminalCommand = string.Empty;
    private string _gatherTerminalCommandHelp = string.Empty;
    private string _gatherTerminalDownloadingText = string.Empty;
    private string _gatherTerminalDownloadCompleteText = string.Empty;
    private double _gatherTerminalDownloadTime = 0.0;
    private ObservableCollection<GatherItemPlacement> _gatherTerminalPlacements = new()
    {
        new GatherItemPlacement
        {
            ItemNumber = 1,
            ItemPrefix = "Terminal"
        }
    };

    private CandidateZonePlacement _timedTerminalSequenceMainLocation = new()
    {
        DimensionIndex = "Reality",
        Zone = 0,
        PlacementStartWeight = 0.0,
        PlacementMiddleWeight = 0.0,
        PlacementEndWeight = 0.0
    };
    private int _timedTerminalSequenceNumberOfRounds = 1;
    private double _timedTerminalSequenceTimePerRound = 0.0;
    private double _timedTerminalSequenceTimeForConfirmation = 0.0;
    private string _timedTerminalSequenceSourceTerminalWorldEventObjectFilter = "MainTerminal";
    private ObservableCollection<GatherItemPlacement> _timedTerminalSequencePlacements = new()
    {
        new GatherItemPlacement
        {
            ItemNumber = 1,
            ItemPrefix = "Terminal"
        }
    };

    private ObservableCollection<WardenEvent> _eventsOnActivation = new();
    private ObservableCollection<WardenEvent> _eventsOnSequenceStart = new();
    private ObservableCollection<WardenEvent> _eventsOnSequenceDone = new();
    private ObservableCollection<WardenEvent> _eventsOnSequenceFail = new();
    private ObservableCollection<WardenEvent> _eventsOnElevatorLand = new();
    private ObservableCollection<WardenEvent> _eventsOnGotoWin = new();
    private ObservableCollection<WardenEvent> _eventsOnActivate = new();

    public event PropertyChangedEventHandler? PropertyChanged;

    public SectorSettings()
    {
        ApplyDefaultsForObjectiveType(_objectiveType);
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

    [JsonPropertyName("WinCondition")]
    public WinCondition WinCondition
    {
        get => _winCondition;
        set => SetField(ref _winCondition, value);
    }

    [JsonPropertyName("ExitZone")]
    public int ExitZone
    {
        get => _exitZone;
        set => SetField(ref _exitZone, value);
    }

    [JsonPropertyName("ObjectiveType")]
    public ObjectiveType ObjectiveType
    {
        get => _objectiveType;
        set => SetField(ref _objectiveType, value);
    }

    [JsonPropertyName("HsuPlacementZone")]
    public int HsuPlacementZone
    {
        get => _hsuPlacementZone;
        set => SetField(ref _hsuPlacementZone, value);
    }

    [JsonPropertyName("HsuPlacementStartWeight")]
    public double HsuPlacementStartWeight
    {
        get => _hsuPlacementStartWeight;
        set => SetField(ref _hsuPlacementStartWeight, value);
    }

    [JsonPropertyName("HsuPlacementMiddleWeight")]
    public double HsuPlacementMiddleWeight
    {
        get => _hsuPlacementMiddleWeight;
        set => SetField(ref _hsuPlacementMiddleWeight, value);
    }

    [JsonPropertyName("HsuPlacementEndWeight")]
    public double HsuPlacementEndWeight
    {
        get => _hsuPlacementEndWeight;
        set => SetField(ref _hsuPlacementEndWeight, value);
    }

    [JsonPropertyName("ReactorPlacementZone")]
    public int ReactorPlacementZone
    {
        get => _reactorPlacementZone;
        set => SetField(ref _reactorPlacementZone, value);
    }

    [JsonPropertyName("ReactorStartupStages")]
    public ObservableCollection<ReactorStartupStage> ReactorStartupStages
    {
        get => _reactorStartupStages;
        set => SetField(ref _reactorStartupStages, value);
    }

    [JsonPropertyName("ChainedPuzzleToActive")]
    public uint ChainedPuzzleToActive
    {
        get => _chainedPuzzleToActive;
        set => SetField(ref _chainedPuzzleToActive, value);
    }

    [JsonPropertyName("ChainedPuzzleMidObjective")]
    public uint ChainedPuzzleMidObjective
    {
        get => _chainedPuzzleMidObjective;
        set => SetField(ref _chainedPuzzleMidObjective, value);
    }

    [JsonPropertyName("DoNotSolveObjectiveOnReactorComplete")]
    public bool DoNotSolveObjectiveOnReactorComplete
    {
        get => _doNotSolveObjectiveOnReactorComplete;
        set => SetField(ref _doNotSolveObjectiveOnReactorComplete, value);
    }

    [JsonPropertyName("SpecialTerminalCommandZone")]
    public int SpecialTerminalCommandZone
    {
        get => _specialTerminalCommandZone;
        set => SetField(ref _specialTerminalCommandZone, value);
    }

    [JsonPropertyName("SpecialTerminalCommand")]
    public string SpecialTerminalCommand
    {
        get => _specialTerminalCommand;
        set => SetField(ref _specialTerminalCommand, value);
    }

    [JsonPropertyName("SpecialTerminalCommandDesc")]
    public string SpecialTerminalCommandDesc
    {
        get => _specialTerminalCommandDesc;
        set => SetField(ref _specialTerminalCommandDesc, value);
    }

    [JsonPropertyName("PostCommandOutput")]
    public string PostCommandOutput
    {
        get => _postCommandOutput;
        set => SetField(ref _postCommandOutput, value);
    }

    [JsonPropertyName("SpecialCommandRule")]
    public SpecialCommandRule SpecialCommandRule
    {
        get => _specialCommandRule;
        set => SetField(ref _specialCommandRule, value);
    }

    [JsonPropertyName("GatherItem")]
    public GatherSmallItemType GatherItem
    {
        get => _gatherItem;
        set => SetField(ref _gatherItem, value);
    }

    [JsonPropertyName("RetrieveItem")]
    public RetrieveBigItemType RetrieveItem
    {
        get => _retrieveItem;
        set => SetField(ref _retrieveItem, value);
    }

    [JsonPropertyName("Header")]
    public string Header
    {
        get => _header;
        set => SetField(ref _header, value);
    }

    [JsonPropertyName("MainObjective")]
    public string MainObjective
    {
        get => _mainObjective;
        set => SetField(ref _mainObjective, value);
    }

    [JsonPropertyName("FindLocationInfo")]
    public string FindLocationInfo
    {
        get => _findLocationInfo;
        set => SetField(ref _findLocationInfo, value);
    }

    [JsonPropertyName("FindLocationInfoHelp")]
    public string FindLocationInfoHelp
    {
        get => _findLocationInfoHelp;
        set => SetField(ref _findLocationInfoHelp, value);
    }

    [JsonPropertyName("GoToZone")]
    public string GoToZone
    {
        get => _goToZone;
        set => SetField(ref _goToZone, value);
    }

    [JsonPropertyName("InZoneFindItem")]
    public string InZoneFindItem
    {
        get => _inZoneFindItem;
        set => SetField(ref _inZoneFindItem, value);
    }

    [JsonPropertyName("SolveItem")]
    public string SolveItem
    {
        get => _solveItem;
        set => SetField(ref _solveItem, value);
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
    }

    [JsonPropertyName("WavesOnGotoWin")]
    public GenericWaveSettings WavesOnGotoWin
    {
        get => _wavesOnGotoWin;
        set => SetField(ref _wavesOnGotoWin, value);
    }

    [JsonPropertyName("WaveOnGotoWinTrigger")]
    public WaveOnGotoWinTrigger WaveOnGotoWinTrigger
    {
        get => _waveOnGotoWinTrigger;
        set => SetField(ref _waveOnGotoWinTrigger, value);
    }

    [JsonPropertyName("StopAllWavesBeforeGotoWin")]
    public bool StopAllWavesBeforeGotoWin
    {
        get => _stopAllWavesBeforeGotoWin;
        set => SetField(ref _stopAllWavesBeforeGotoWin, value);
    }

    [JsonPropertyName("WavesOnElevatorLand")]
    public GenericWaveSettings WavesOnElevatorLand
    {
        get => _wavesOnElevatorLand;
        set => SetField(ref _wavesOnElevatorLand, value);
    }

    [JsonPropertyName("FogTransitionDataOnGotoWin")]
    public uint FogTransitionDataOnGotoWin
    {
        get => _fogTransitionDataOnGotoWin;
        set => SetField(ref _fogTransitionDataOnGotoWin, value);
    }

    [JsonPropertyName("FogTransitionDataOnGotoWinDuration")]
    public double FogTransitionDataOnGotoWinDuration
    {
        get => _fogTransitionDataOnGotoWinDuration;
        set => SetField(ref _fogTransitionDataOnGotoWinDuration, value);
    }

    [JsonPropertyName("GatherItemPlacements")]
    public ObservableCollection<GatherItemPlacement> GatherItemPlacements
    {
        get => _gatherItemPlacements;
        set => SetField(ref _gatherItemPlacements, value);
    }

    [JsonIgnore]
    public int GatherSpawnCount
    {
        get => _gatherItemPlacements.Count;
        set
        {
            int target = Math.Max(1, value);
            while (_gatherItemPlacements.Count < target)
            {
                _gatherItemPlacements.Add(new GatherItemPlacement
                {
                    ItemNumber = _gatherItemPlacements.Count + 1,
                    DimensionIndex = "Reality",
                    Zone = 0,
                    PlacementStartWeight = 0.0,
                    PlacementMiddleWeight = 0.0,
                    PlacementEndWeight = 0.0
                });
            }

            while (_gatherItemPlacements.Count > target)
            {
                _gatherItemPlacements.RemoveAt(_gatherItemPlacements.Count - 1);
            }

            OnPropertyChanged(nameof(GatherSpawnCount));
        }
    }

    [JsonPropertyName("RetrieveItemPlacements")]
    public ObservableCollection<GatherItemPlacement> RetrieveItemPlacements
    {
        get => _retrieveItemPlacements;
        set => SetField(ref _retrieveItemPlacements, value);
    }

    [JsonIgnore]
    public int RetrieveSpawnCount
    {
        get => _retrieveItemPlacements.Count;
        set
        {
            int target = Math.Max(1, value);
            while (_retrieveItemPlacements.Count < target)
            {
                _retrieveItemPlacements.Add(new GatherItemPlacement
                {
                    ItemNumber = _retrieveItemPlacements.Count + 1,
                    DimensionIndex = "Reality",
                    Zone = 0,
                    PlacementStartWeight = 0.0,
                    PlacementMiddleWeight = 0.0,
                    PlacementEndWeight = 0.0
                });
            }

            while (_retrieveItemPlacements.Count > target)
            {
                _retrieveItemPlacements.RemoveAt(_retrieveItemPlacements.Count - 1);
            }

            OnPropertyChanged(nameof(RetrieveSpawnCount));
        }
    }

    [JsonPropertyName("GatherMaxPerZone")]
    public int GatherMaxPerZone
    {
        get => _gatherMaxPerZone;
        set => SetField(ref _gatherMaxPerZone, value);
    }

    [JsonPropertyName("RetrieveMaxPerZone")]
    public int RetrieveMaxPerZone
    {
        get => _retrieveMaxPerZone;
        set => SetField(ref _retrieveMaxPerZone, value);
    }

    [JsonPropertyName("DistributeGeneratorPlacements")]
    public ObservableCollection<DistributeGeneratorPlacement> DistributeGeneratorPlacements
    {
        get => _distributeGeneratorPlacements;
        set => SetField(ref _distributeGeneratorPlacements, value);
    }

    [JsonIgnore]
    public int DistributeGeneratorCount
    {
        get => _distributeGeneratorPlacements.Count;
        set
        {
            int target = Math.Max(1, value);
            while (_distributeGeneratorPlacements.Count < target)
            {
                _distributeGeneratorPlacements.Add(new DistributeGeneratorPlacement
                {
                    GeneratorNumber = _distributeGeneratorPlacements.Count + 1,
                    Zone = 0
                });
            }

            while (_distributeGeneratorPlacements.Count > target)
            {
                _distributeGeneratorPlacements.RemoveAt(_distributeGeneratorPlacements.Count - 1);
            }

            OnPropertyChanged(nameof(DistributeGeneratorCount));
        }
    }

    [JsonPropertyName("UplinkNumberOfVerificationRounds")]
    public int UplinkNumberOfVerificationRounds
    {
        get => _uplinkNumberOfVerificationRounds;
        set => SetField(ref _uplinkNumberOfVerificationRounds, value);
    }

    [JsonPropertyName("TerminalUplinkPlacements")]
    public ObservableCollection<GatherItemPlacement> TerminalUplinkPlacements
    {
        get => _terminalUplinkPlacements;
        set => SetField(ref _terminalUplinkPlacements, value);
    }

    [JsonIgnore]
    public int UplinkNumberOfTerminals
    {
        get => _terminalUplinkPlacements.Count;
        set
        {
            int target = Math.Max(1, value);
            while (_terminalUplinkPlacements.Count < target)
            {
                _terminalUplinkPlacements.Add(new GatherItemPlacement
                {
                    ItemNumber = _terminalUplinkPlacements.Count + 1,
                    ItemPrefix = "Terminal"
                });
            }

            while (_terminalUplinkPlacements.Count > target)
            {
                _terminalUplinkPlacements.RemoveAt(_terminalUplinkPlacements.Count - 1);
            }

            OnPropertyChanged(nameof(UplinkNumberOfTerminals));
        }
    }

    [JsonPropertyName("CentralGeneratorClusterZone")]
    public int CentralGeneratorClusterZone
    {
        get => _centralGeneratorClusterZone;
        set => SetField(ref _centralGeneratorClusterZone, value);
    }

    [JsonPropertyName("CentralGeneratorClusterFogTransitions")]
    public ObservableCollection<CentralGeneratorFogTransitionStage> CentralGeneratorClusterFogTransitions
    {
        get => _centralGeneratorClusterFogTransitions;
        set => SetField(ref _centralGeneratorClusterFogTransitions, value);
    }

    [JsonIgnore]
    public int CentralGeneratorClusterGeneratorCount
    {
        get => _centralGeneratorClusterFogTransitions.Count;
        set
        {
            int target = Math.Max(1, value);
            while (_centralGeneratorClusterFogTransitions.Count < target)
            {
                _centralGeneratorClusterFogTransitions.Add(new CentralGeneratorFogTransitionStage
                {
                    StageNumber = _centralGeneratorClusterFogTransitions.Count + 1,
                    FogSetting = 0,
                    TransitionDuration = 0.0
                });
            }

            while (_centralGeneratorClusterFogTransitions.Count > target)
            {
                _centralGeneratorClusterFogTransitions.RemoveAt(_centralGeneratorClusterFogTransitions.Count - 1);
            }

            OnPropertyChanged(nameof(CentralGeneratorClusterGeneratorCount));
        }
    }

    [JsonPropertyName("CentralGeneratorClusterPowerCellPlacements")]
    public ObservableCollection<GatherItemPlacement> CentralGeneratorClusterPowerCellPlacements
    {
        get => _centralGeneratorClusterPowerCellPlacements;
        set => SetField(ref _centralGeneratorClusterPowerCellPlacements, value);
    }

    [JsonIgnore]
    public int CentralGeneratorClusterPowerCellCount
    {
        get => _centralGeneratorClusterPowerCellPlacements.Count;
        set
        {
            int target = Math.Max(1, value);
            while (_centralGeneratorClusterPowerCellPlacements.Count < target)
            {
                _centralGeneratorClusterPowerCellPlacements.Add(new GatherItemPlacement
                {
                    ItemNumber = _centralGeneratorClusterPowerCellPlacements.Count + 1,
                    ItemPrefix = "Power Cell"
                });
            }

            while (_centralGeneratorClusterPowerCellPlacements.Count > target)
            {
                _centralGeneratorClusterPowerCellPlacements.RemoveAt(_centralGeneratorClusterPowerCellPlacements.Count -
                                                                     1);
            }

            OnPropertyChanged(nameof(CentralGeneratorClusterPowerCellCount));
        }
    }

    [JsonPropertyName("ActivateSmallHsuZone")]
    public int ActivateSmallHsuZone
    {
        get => _activateSmallHsuZone;
        set => SetField(ref _activateSmallHsuZone, value);
    }

    [JsonPropertyName("ItemFromStart")]
    public RetrieveBigItemType ItemFromStart
    {
        get => _itemFromStart;
        set => SetField(ref _itemFromStart, value);
    }

    [JsonPropertyName("ItemAfterActivation")]
    public RetrieveBigItemType ItemAfterActivation
    {
        get => _itemAfterActivation;
        set => SetField(ref _itemAfterActivation, value);
    }

    [JsonPropertyName("BringItemInElevator")]
    public bool BringItemInElevator
    {
        get => _bringItemInElevator;
        set => SetField(ref _bringItemInElevator, value);
    }

    [JsonPropertyName("StopEnemyWavesOnActivation")]
    public bool StopEnemyWavesOnActivation
    {
        get => _stopEnemyWavesOnActivation;
        set => SetField(ref _stopEnemyWavesOnActivation, value);
    }

    [JsonPropertyName("ObjectiveCompleteAfterInsertion")]
    public bool ObjectiveCompleteAfterInsertion
    {
        get => _objectiveCompleteAfterInsertion;
        set => SetField(ref _objectiveCompleteAfterInsertion, value);
    }

    [JsonPropertyName("RequireItemAfterActivationInExitScan")]
    public bool RequireItemAfterActivationInExitScan
    {
        get => _requireItemAfterActivationInExitScan;
        set => SetField(ref _requireItemAfterActivationInExitScan, value);
    }

    [JsonPropertyName("TimeToActivate")]
    public int TimeToActivate
    {
        get => _timeToActivate;
        set => SetField(ref _timeToActivate, value);
    }

    [JsonPropertyName("TimeToSurvive")]
    public int TimeToSurvive
    {
        get => _timeToSurvive;
        set => SetField(ref _timeToSurvive, value);
    }

    [JsonPropertyName("TimerTitle")]
    public string TimerTitle
    {
        get => _timerTitle;
        set => SetField(ref _timerTitle, value);
    }

    [JsonPropertyName("TimerToActivateTitle")]
    public string TimerToActivateTitle
    {
        get => _timerToActivateTitle;
        set => SetField(ref _timerToActivateTitle, value);
    }

    [JsonPropertyName("GatherTerminalRequiredCount")]
    public int GatherTerminalRequiredCount
    {
        get => _gatherTerminalRequiredCount;
        set => SetField(ref _gatherTerminalRequiredCount, value);
    }

    [JsonPropertyName("GatherTerminalCommand")]
    public string GatherTerminalCommand
    {
        get => _gatherTerminalCommand;
        set => SetField(ref _gatherTerminalCommand, value);
    }

    [JsonPropertyName("GatherTerminalCommandHelp")]
    public string GatherTerminalCommandHelp
    {
        get => _gatherTerminalCommandHelp;
        set => SetField(ref _gatherTerminalCommandHelp, value);
    }

    [JsonPropertyName("GatherTerminalDownloadingText")]
    public string GatherTerminalDownloadingText
    {
        get => _gatherTerminalDownloadingText;
        set => SetField(ref _gatherTerminalDownloadingText, value);
    }

    [JsonPropertyName("GatherTerminalDownloadCompleteText")]
    public string GatherTerminalDownloadCompleteText
    {
        get => _gatherTerminalDownloadCompleteText;
        set => SetField(ref _gatherTerminalDownloadCompleteText, value);
    }

    [JsonPropertyName("GatherTerminalDownloadTime")]
    public double GatherTerminalDownloadTime
    {
        get => _gatherTerminalDownloadTime;
        set => SetField(ref _gatherTerminalDownloadTime, value);
    }

    [JsonPropertyName("GatherTerminalPlacements")]
    public ObservableCollection<GatherItemPlacement> GatherTerminalPlacements
    {
        get => _gatherTerminalPlacements;
        set => SetField(ref _gatherTerminalPlacements, value);
    }

    [JsonIgnore]
    public int GatherTerminalSpawnCount
    {
        get => _gatherTerminalPlacements.Count;
        set
        {
            int target = Math.Max(1, value);
            while (_gatherTerminalPlacements.Count < target)
            {
                _gatherTerminalPlacements.Add(new GatherItemPlacement
                {
                    ItemNumber = _gatherTerminalPlacements.Count + 1,
                    ItemPrefix = "Terminal"
                });
            }

            while (_gatherTerminalPlacements.Count > target)
            {
                _gatherTerminalPlacements.RemoveAt(_gatherTerminalPlacements.Count - 1);
            }

            OnPropertyChanged(nameof(GatherTerminalSpawnCount));
        }
    }

    [JsonPropertyName("TimedTerminalSequenceMainLocation")]
    public CandidateZonePlacement TimedTerminalSequenceMainLocation
    {
        get => _timedTerminalSequenceMainLocation;
        set => SetField(ref _timedTerminalSequenceMainLocation, value ?? new CandidateZonePlacement());
    }

    [JsonPropertyName("TimedTerminalSequenceNumberOfRounds")]
    public int TimedTerminalSequenceNumberOfRounds
    {
        get => _timedTerminalSequenceNumberOfRounds;
        set => SetField(ref _timedTerminalSequenceNumberOfRounds, value);
    }

    [JsonPropertyName("TimedTerminalSequenceTimePerRound")]
    public double TimedTerminalSequenceTimePerRound
    {
        get => _timedTerminalSequenceTimePerRound;
        set => SetField(ref _timedTerminalSequenceTimePerRound, value);
    }

    [JsonPropertyName("TimedTerminalSequenceTimeForConfirmation")]
    public double TimedTerminalSequenceTimeForConfirmation
    {
        get => _timedTerminalSequenceTimeForConfirmation;
        set => SetField(ref _timedTerminalSequenceTimeForConfirmation, value);
    }

    [JsonPropertyName("TimedTerminalSequenceSourceTerminalWorldEventObjectFilter")]
    public string TimedTerminalSequenceSourceTerminalWorldEventObjectFilter
    {
        get => _timedTerminalSequenceSourceTerminalWorldEventObjectFilter;
        set => SetField(ref _timedTerminalSequenceSourceTerminalWorldEventObjectFilter, value);
    }

    [JsonPropertyName("TimedTerminalSequencePlacements")]
    public ObservableCollection<GatherItemPlacement> TimedTerminalSequencePlacements
    {
        get => _timedTerminalSequencePlacements;
        set => SetField(ref _timedTerminalSequencePlacements, value);
    }

    [JsonIgnore]
    public int TimedTerminalSequenceNumberOfTerminals
    {
        get => _timedTerminalSequencePlacements.Count;
        set
        {
            int target = Math.Max(1, value);
            while (_timedTerminalSequencePlacements.Count < target)
            {
                _timedTerminalSequencePlacements.Add(new GatherItemPlacement
                {
                    ItemNumber = _timedTerminalSequencePlacements.Count + 1,
                    ItemPrefix = "Terminal"
                });
            }

            while (_timedTerminalSequencePlacements.Count > target)
            {
                _timedTerminalSequencePlacements.RemoveAt(_timedTerminalSequencePlacements.Count - 1);
            }

            OnPropertyChanged(nameof(TimedTerminalSequenceNumberOfTerminals));
        }
    }

    [JsonPropertyName("EventsOnActivation")]
    public ObservableCollection<WardenEvent> EventsOnActivation
    {
        get => _eventsOnActivation;
        set => SetField(ref _eventsOnActivation, value);
    }

    [JsonPropertyName("EventsOnSequenceStart")]
    public ObservableCollection<WardenEvent> EventsOnSequenceStart
    {
        get => _eventsOnSequenceStart;
        set => SetField(ref _eventsOnSequenceStart, value);
    }

    [JsonPropertyName("EventsOnSequenceDone")]
    public ObservableCollection<WardenEvent> EventsOnSequenceDone
    {
        get => _eventsOnSequenceDone;
        set => SetField(ref _eventsOnSequenceDone, value);
    }

    [JsonPropertyName("EventsOnSequenceFail")]
    public ObservableCollection<WardenEvent> EventsOnSequenceFail
    {
        get => _eventsOnSequenceFail;
        set => SetField(ref _eventsOnSequenceFail, value);
    }

    [JsonPropertyName("EventsOnElevatorLand")]
    public ObservableCollection<WardenEvent> EventsOnElevatorLand
    {
        get => _eventsOnElevatorLand;
        set => SetField(ref _eventsOnElevatorLand, value);
    }

    [JsonPropertyName("EventsOnGotoWin")]
    public ObservableCollection<WardenEvent> EventsOnGotoWin
    {
        get => _eventsOnGotoWin;
        set => SetField(ref _eventsOnGotoWin, value);
    }

    [JsonPropertyName("EventsOnActivate")]
    public ObservableCollection<WardenEvent> EventsOnActivate
    {
        get => _eventsOnActivate;
        set => SetField(ref _eventsOnActivate, value);
    }

    public string GetZoneDisplayName(int zoneIndex) => $"Zone {ZoneAliasStart + zoneIndex}";

    public SectorSettings Clone()
    {
        var clone = new SectorSettings
        {
            ArtifactAmountMultiplier = ArtifactAmountMultiplier,
            ZoneAliasStart = ZoneAliasStart,
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
            TimedTerminalSequenceSourceTerminalWorldEventObjectFilter = TimedTerminalSequenceSourceTerminalWorldEventObjectFilter
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