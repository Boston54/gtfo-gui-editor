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
