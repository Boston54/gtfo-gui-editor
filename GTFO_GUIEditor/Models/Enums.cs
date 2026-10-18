using System.Text.Json.Serialization;

namespace GTFO_GUIEditor.Models;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ComplexType
{
    Mining,
    Service,
    Tech
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum SubComplexType
{
    DigSite,
    Refinery,
    Storage,
    DataCenter,
    Lab,
    All,
    Floodways,
    Mining_Reactor,
    Plug_SubComplex_Transition,
    Tech_Reactor,
    Tech_Portal,
    Gardens,
    Mining_Portal
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum BundleNameType
{
    None,
    Complex_Shared,
    Complex_Mining,
    Complex_Tech,
    Complex_Service
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum TransitionDirection
{
    FloorUp,
    FloorDown
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum LevelProgression
{
    StartLevel,
    MidLevel,
    EndLevel
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ShardType
{
    S1,
    S2,
    S3,
    S4,
    S5,
    S6,
    S7,
    S8,
    S9,
    S10,
    S11,
    S12,
    S13,
    S14,
    S15,
    S16,
    S17,
    S18,
    S19,
    S20
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum SurvivalWaveSpawnType
{
    InRelationToClosestAlivePlayer,
    InSuppliedCourseNodeZone,
    InSuppliedCourseNode,
    InSuppliedCourseNode_OnPosition,
    ClosestToSuppliedNodeButNoBetweenPlayers,
    OnSpawnPoints,
    FromElevatorDirection
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum WaveFilterType
{
    Include,
    Exclude
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum WaveEnemyType
{
    Weakling,
    Standard,
    Special,
    MiniBoss,
    Boss
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ExpeditionAccessibility
{
    Normal,
    AlwayBlock,
    AlwaysAllow,
    BlockedAndScrambled,
    UseCustomProgressionLock,
    UnlockedByExpedition
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum DoorType
{
    RoomDoor,
    SecurityDoor,
    MainDoor,
    SecondaryDoor,
    OverloadDoor
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum GeomorphAltitude
{
    Low,
    Mid,
    High
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum LightCategory
{
    General,
    Special,
    Emergency,
    Independent,
    Door,
    Sign,
    DoorImportant
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum SectorType
{
    Main,
    Secondary,
    Overload
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum WinCondition
{
    GoToElevator,
    GoToExitGeo
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ObjectiveType
{
    HSU_FindTakeSample,
    Reactor_Startup,
    Reactor_Shutdown,
    GatherSmallItems,
    ClearAPath,
    SpecialTerminalCommand,
    RetrieveBigItems,
    DistributePowerCells,
    TerminalUplink,
    CentralGeneratorCluster,
    ActivateSmallHSU,
    Survival,
    GatherTerminal,
    CorruptedTerminalUplink,
    TimedTerminalSequence
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum SpecialCommandRule
{
    Normal,
    OnlyOnce,
    OnlyOnceDelete
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum RetrieveBigItemType
{
    PowerCell = 131,
    FogTurbine = 133,
    HSUNeonateStage1 = 137,
    HSUNeonateStage2 = 141,
    HSUNeonateStage3 = 143,
    HSUNeonateStage4 = 145,
    HSUNeonateStage5 = 170,
    MWP = 164,
    DataSphere = 151,
    PortalKey = 166,
    CargoCrate = 138,
    CryoHardcase = 148,
    CargoCrateHighSecurity = 154,
    CargoCrateHighSecurityOpen = 155,
    SedativesCase = 173
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum GatherSmallItemType
{
    PersonnelID = 128,
    PartialDecoder = 129,
    HardDrive = 147,
    Microdrive = 183,
    [JsonPropertyName("GLP-1")] GLP_1 = 149,
    OSIP = 150,
    [JsonPropertyName("GLP-2")] GLP_2 = 169,
    DataCubeTampered = 168,
    DataCube = 165,
    DataCubes = 179,
    PlantSample = 153,
    MemoryStick = 171
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum WaveOnGotoWinTrigger
{
    OnObjectiveCompleted,
    WhenExitScanMakesProgress
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum EventTrigger
{
    None = 0,
    OnStart = 1,
    OnMid = 2,
    OnEnd = 3
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum EventType
{
    None = 0,
    OpenSecurityDoor = 1,
    UnlockSecurityDoor = 2,
    AllLightsOff = 3,
    AllLightsOn = 4,
    PlaySound = 5,
    SetFogSetting = 6,
    DimensionFlashTeam = 7,
    DimensionWarpTeam = 8,
    SpawnEnemyWave = 9,
    StopEnemyWaves = 10,
    UpdateCustomSubObjective = 11,
    ForceCompleteObjective = 12,
    LightsInZone = 13,
    LightsInZoneToggle = 14,
    AnimationTrigger = 15,
    SpawnEnemyOnPoint = 16,
    SetNavMarker = 17,
    StepProgressionObjective = 18,
    SetWorldEventCondition = 19,
    LockSecurityDoor = 20,
    SetTerminalCommand = 21,
    ActivateChainedPuzzle = 22,
    LightOnWorldEventObject = 23,
    AddToTimer = 24,
    ResetTimer = 25,
    WinOnDeath = 26,
    ForceInstantWin = 27,
    DialogueOnClosest = 28,
    GetAchievement = 29,
    ClearDimension = 30,
    StartRepeatingFog = 31,
    StopSustainedEvent = 32
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ZoneDistributionAmount
{
    None_0,
    Pair_2,
    Few_5,
    Some_10,
    SomeMore_15,
    Many_20,
    Alot_30,
    Tons_50
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum TerminalLineType
{
    Normal,
    Fail,
    SpinningWaitDone,
    SpinningWaitNoDone,
    ProgressWait,
    Warning
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum TerminalStartingState
{
    Sleeping,
    Awake,
    PlayerInteracting,
    DataMining,
    Hacked,
    CodePuzzle,
    InputTest,
    ReactorError,
    AskToPlayLogFile,
    DoPlayAudioFile,
    AudioLoopError,
    Ping,
    PasswordProtected,
    EnterPassword
}