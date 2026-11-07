using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace GTFO_GUIEditor.Models;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ProgressionPuzzleType
{
    None,
    Keycard_SecurityBox,
    PowerGenerator_And_PowerCell,
    Locked_No_Key
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum EnemyZoneDistribution
{
    Rel_Value,
    Force_One
}

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
    LockSecurityDoor_Vanilla = 20,
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
    StopSustainedEvent = 32,
    EventBreak = 999,
    CloseSecurityDoor = 10000,
    LockSecurityDoor = 10001,
    SetDoorInteraction = 10002,
    TriggerSecurityDoorAlarm = 10003,
    SolveSecurityDoorAlarm = 10004,
    StartReactor = 10005,
    ModifyReactorWaveState = 10006,
    ForceCompleteReactor = 10007,
    ForceCompleteLevel = 10008,
    ForceFailLevel = 10009,
    Countdown = 10010,
    SetLevelFailCheckEnabled = 10011,
    SetLevelFailWhenAnyPlayerDowned = 10012,
    KillAllPlayers = 10013,
    KillPlayersInZone = 10014,
    SetLightDataInZone = 10015,
    AlertEnemiesInZone = 10017,
    CleanUpEnemiesInZone = 10018,
    SpawnHibernateInZone = 10019,
    SpawnScoutInZone = 10020,
    SaveCheckpoint = 10021,
    MoveExtractionWorldPosition = 10022,
    SetBlackoutEnabled = 10023,
    AddTerminalCommand = 10024,
    HideTerminalCommand = 10025,
    UnhideTerminalCommand = 10026,
    AddChainPuzzleToSecurityDoor = 10027,
    SetActiveEnemyWave = 10028,
    GiveResource = 10029,
    ForceResetChainPuzzle = 10030,
    NestedEvent = 20000,
    StartEventLoop = 20001,
    StopEventLoop = 20002,
    TeleportPlayer = 20003,
    InfectPlayer = 20004,
    DamagePlayer = 20005,
    RevivePlayer = 20006,
    AdjustAWOTimer = 20007,
    Countup = 20008,
    ForceCompleteChainPuzzle = 20009,
    SpawnNavMarker = 20010,
    ShakeScreen = 20011,
    StartPortalMachine = 20012,
    SetSuccessScreen = 20013,
    PlaySubtitles = 20014,
    MultiProgression = 20015,
    PlayWaveRoarSound = 20016,
    CustomHudText = 20017,
    SpecialHudTimer = 20018,
    ForcePlayPlayerDialogue = 20019,
    SetTerminalLog = 20020,
    SetPocketItem = 20021,
    DoInteractWeakDoorsInZone = 20022,
    ToggleInteractWeakDoorsInZone = 20023,
    PickupSentries = 20024,
    SetExpeditionEnvironment = 20026,
    ClearWardenIntelQueue = 20027,
    ResetOnDoorApproach = 20028,
    FadeScreenInOut = 20029
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

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum Consumables
{
    Flashlight = 30,
    Glowstick = 114,
    Glowstick_Christmas = 130,
    Glowstick_Halloween = 167,
    Glowstick_Yellow = 174,
    Syringe_Health = 140,
    Syringe_MeleeBuff = 142,
    CFoamGrenade = 115,
    LockMelter = 116,
    FogRepeller = 117,
    ExplosiveTripmine = 139,
    CFoamTripmine = 144
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum DimensionLinkedToLayer
{
    [JsonPropertyName("MainLayer")]
    Main,
    [JsonPropertyName("SecondaryLayer")]
    Secondary,
    [JsonPropertyName("ThirdLayer")]
    Overload
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum DimensionObjectiveType
{
    Ignore,
    LinkedToLayer,
    Independent
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum DimensionCloudsType
{
    None = 0,
    Anvils = 1,
    Stratocumulus = 2
}

[JsonConverter(typeof(AlarmSoundStartJsonConverter))]
public enum AlarmSoundStart : uint
{
    Standard = 3339129407,
    Error = 2200133294
}

[JsonConverter(typeof(AlarmSoundStopJsonConverter))]
public enum AlarmSoundStop : uint
{
    Standard = 42633153,
    Error = 1190355274,
    Error_Start = 2200133294
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum TERM_Command : uint
{
    None = 0,
    Help = 1,
    Commands = 2,
    Cls = 3,
    Exit = 4,
    Open = 5,
    Close = 6,
    Activate = 7,
    Deactivate = 8,
    EmptyLine = 9,
    InvalidCommand = 10,
    DownloadData = 11,
    ViewSecurityLog = 12,
    Override = 13,
    DisableAlarm = 14,
    Locate = 15,
    ActivateBeacon = 16,
    Find = 17,
    ShowList = 18,
    Query = 19,
    Ping = 20,
    ReactorStartup = 21,
    ReactorVerify = 22,
    ReactorShutdown = 23,
    WardenObjectiveSpecialCommand = 24,
    TerminalUplinkConnect = 25,
    TerminalUplinkVerify = 26,
    TerminalUplinkConfirm = 27,
    ListLogs = 28,
    ReadLog = 29,
    Start = 30,
    TryUnlockingTerminal = 31,
    WardenObjectiveGatherCommand = 32,
    TerminalCorruptedUplinkConnect = 33,
    TerminalCorruptedUplinkVerify = 34,
    TimedConnectionSend = 35,
    TimedConnectionVerify = 36,
    UsedCommand = 37,
    UniqueCommand1 = 38,
    UniqueCommand2 = 39,
    UniqueCommand3 = 40,
    UniqueCommand4 = 41,
    UniqueCommand5 = 42,
    Info = 43,
    MAX_COUNT = 44
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ScoutType
{
    Scout = 0,
    Charger_Scout = 1,
    Shadow_Scout = 2,
    Zoomer_Scout = 3,
    Nightmare_Scout = 4
}

public static class ScoutTypeExtensions
{
    public static (int GroupType, int Difficulty) ToGroupTypeAndDifficulty(this ScoutType type) => type switch
    {
        ScoutType.Scout => (3, 0),
        ScoutType.Charger_Scout => (3, 3),
        ScoutType.Shadow_Scout => (3, 4),
        ScoutType.Zoomer_Scout => (3, 5),
        ScoutType.Nightmare_Scout => (3, 6),
        _ => (3, 0)
    };

    public static ScoutType FromGroupTypeAndDifficulty(int groupType, int difficulty) => (groupType, difficulty) switch
    {
        (3, 0) => ScoutType.Scout,
        (3, 3) => ScoutType.Charger_Scout,
        (3, 4) => ScoutType.Shadow_Scout,
        (3, 5) => ScoutType.Zoomer_Scout,
        (3, 6) => ScoutType.Nightmare_Scout,
        (_, 3) => ScoutType.Charger_Scout,
        (_, 4) => ScoutType.Shadow_Scout,
        (_, 5) => ScoutType.Zoomer_Scout,
        (_, 6) => ScoutType.Nightmare_Scout,
        _ => ScoutType.Scout
    };
}

public class ScoutTypeOption
{
    public ScoutType Type { get; set; }
    public string DisplayName { get; set; } = string.Empty;

    public ScoutTypeOption() { }
    public ScoutTypeOption(ScoutType type, string displayName)
    {
        Type = type;
        DisplayName = displayName;
    }

    public override string ToString() => DisplayName;
}

public class AlarmSoundStartJsonConverter : JsonConverter<AlarmSoundStart>
{
    public override AlarmSoundStart Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Number)
        {
            if (reader.TryGetUInt32(out uint num) && Enum.IsDefined(typeof(AlarmSoundStart), num))
            {
                return (AlarmSoundStart)num;
            }
        }
        else if (reader.TokenType == JsonTokenType.String)
        {
            string? str = reader.GetString();
            if (!string.IsNullOrWhiteSpace(str))
            {
                if (Enum.TryParse<AlarmSoundStart>(str, true, out var result))
                {
                    return result;
                }
                if (uint.TryParse(str, out uint parsedNum) && Enum.IsDefined(typeof(AlarmSoundStart), parsedNum))
                {
                    return (AlarmSoundStart)parsedNum;
                }
            }
        }
        return AlarmSoundStart.Standard;
    }

    public override void Write(Utf8JsonWriter writer, AlarmSoundStart value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString());
    }
}

public class AlarmSoundStopJsonConverter : JsonConverter<AlarmSoundStop>
{
    public override AlarmSoundStop Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Number)
        {
            if (reader.TryGetUInt32(out uint num) && Enum.IsDefined(typeof(AlarmSoundStop), num))
            {
                return (AlarmSoundStop)num;
            }
        }
        else if (reader.TokenType == JsonTokenType.String)
        {
            string? str = reader.GetString();
            if (!string.IsNullOrWhiteSpace(str))
            {
                if (Enum.TryParse<AlarmSoundStop>(str, true, out var result))
                {
                    return result;
                }
                if (uint.TryParse(str, out uint parsedNum) && Enum.IsDefined(typeof(AlarmSoundStop), parsedNum))
                {
                    return (AlarmSoundStop)parsedNum;
                }
            }
        }
        return AlarmSoundStop.Standard;
    }

    public override void Write(Utf8JsonWriter writer, AlarmSoundStop value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString());
    }
}
