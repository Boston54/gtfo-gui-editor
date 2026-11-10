using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.Json.Nodes;
using GTFO_GUIEditor.Models;

namespace GTFO_GUIEditor.Services;

public static class AraLevelLayoutService
{
    public static JsonArray ExportEventsOnTriggerForZone(ExportContext context, LevelLayoutGridState? gridState,
        SectorType sector, int zoneNumber)
    {
        var array = new JsonArray();
        if (gridState == null) return array;

        var triggersInZone = gridState.PlacedTriggers.Where(t =>
        {
            if (t.IsWorldEventObject) return false;
            var (sec, zone, _) = gridState.GetZoneForPoint(t.Center.X, t.Center.Y);
            return sec == sector && zone == zoneNumber;
        }).ToList();

        foreach (var trig in triggersInZone)
        {
            string filter = string.IsNullOrWhiteSpace(trig.Name) ? $"WE_Trigger_{trig.Id}" : trig.Name;
            var serializedEvents =
                WardenObjectiveDataBlockExporter.SerializeWardenEvents(trig.Events,
                    (t, c) => context.SynthesizeText(t, c));
            foreach (var node in serializedEvents)
            {
                if (node is JsonObject evObj)
                {
                    var cloneObj = JsonNode.Parse(evObj.ToJsonString()) as JsonObject;
                    if (cloneObj != null)
                    {
                        cloneObj["WorldEventTriggerObjectFilter"] = filter;
                        array.Add(cloneObj);
                    }
                }
            }
        }

        return array;
    }

    public static JsonArray ExportSpecificPickupSpawningDatasForZone(ExportContext context,
        LevelLayoutGridState? gridState, SectorType sector, int zoneNumber)
    {
        var array = new JsonArray();
        if (gridState == null) return array;

        var pickupsInZone = gridState.PlacedTriggers.Where(t =>
        {
            if (!t.IsWorldEventObject) return false;
            var (sec, zone, _) = gridState.GetZoneForPoint(t.Center.X, t.Center.Y);
            return sec == sector && zone == zoneNumber;
        }).ToList();

        foreach (var pickup in pickupsInZone)
        {
            string filter = string.IsNullOrWhiteSpace(pickup.Name) ? $"BigPickup_{pickup.PickupToSpawn}" : pickup.Name;
            array.Add(new JsonObject
            {
                ["PickupToSpawn"] = (int)pickup.PickupToSpawn,
                ["WorldEventObjectFilter"] = filter
            });
        }

        return array;
    }

    public static JsonObject? ExportAraLevelLayout(ExportContext context, LevelLayoutBlock level, ExpeditionBlock? exp)
    {
        JsonObject? mainRoot = null;

        var sectorsToExport = new List<(SectorType Sector, uint LayoutId, string Suffix)>();

        uint mainLayoutId = context.LayoutIds.TryGetValue(level, out uint lid) ? lid : level.PersistentId;
        sectorsToExport.Add((SectorType.Main, mainLayoutId, string.Empty));

        bool secExists = (level.GridState?.PlacedRooms.Any(r => r.Sector == SectorType.Secondary) == true) ||
                         (level.GridState?.PlacedTriggers.Any(t =>
                              level.GridState.GetZoneForPoint(t.Center.X, t.Center.Y).Sector == SectorType.Secondary) ==
                          true) ||
                         (exp?.SecondaryLayerEnabled == true && exp.SecondaryLayout > 0) ||
                         context.SecondaryLayoutIds.ContainsKey(level);

        if (secExists)
        {
            uint secLayoutId = context.SecondaryLayoutIds.TryGetValue(level, out uint secId)
                ? secId
                : (exp?.SecondaryLayout ?? 0);
            if (secLayoutId > 0)
            {
                sectorsToExport.Add((SectorType.Secondary, secLayoutId, "_Secondary"));
            }
        }

        bool thirdExists = (level.GridState?.PlacedRooms.Any(r => r.Sector == SectorType.Overload) == true) ||
                           (level.GridState?.PlacedTriggers.Any(t =>
                                level.GridState.GetZoneForPoint(t.Center.X, t.Center.Y).Sector ==
                                SectorType.Overload) ==
                            true) ||
                           (exp?.ThirdLayerEnabled == true && exp.ThirdLayout > 0) ||
                           context.ThirdLayoutIds.ContainsKey(level);

        if (thirdExists)
        {
            uint thirdLayoutId = context.ThirdLayoutIds.TryGetValue(level, out uint thirdId)
                ? thirdId
                : (exp?.ThirdLayout ?? 0);
            if (thirdLayoutId > 0)
            {
                sectorsToExport.Add((SectorType.Overload, thirdLayoutId, "_Third"));
            }
        }

        Directory.CreateDirectory(context.AraLevelLayoutDirectory);

        foreach (var (sector, layoutId, suffix) in sectorsToExport)
        {
            var root = ExportAraLevelLayoutForSector(context, level, exp, sector, layoutId, suffix);
            if (sector == SectorType.Main)
            {
                mainRoot = root;
            }
        }

        return mainRoot;
    }

    private static JsonObject ExportAraLevelLayoutForSector(ExportContext context, LevelLayoutBlock level,
        ExpeditionBlock? exp, SectorType targetSector, uint layoutId, string fileSuffix)
    {
        var root = new JsonObject
        {
            ["MainLevelLayout"] = layoutId,
            ["Elevator"] = new JsonObject
            {
                ["ForceDisableElevatorCargo"] = false,
                ["OverrideDefaultElevatorCargoItems"] = false,
                ["CustomElevatorCargoItems"] = new JsonArray()
            },
            ["AutoWorldEventObjectPrefix"] = "WE_ARA_{0}_",
            ["SetupWorldEventObjectOnAllTerminals"] = false
        };

        var zonesArray = new JsonArray();
        var realityItems = level.GridState?.PlacedTriggers ?? new ObservableCollection<PlacedTriggerNode>();

        var zoneNumbers = new HashSet<int>();
        if (level.GridState != null)
        {
            foreach (var room in level.GridState.PlacedRooms.Where(r => r.Sector == targetSector))
            {
                zoneNumbers.Add(room.Zone);
            }

            foreach (var item in realityItems)
            {
                var (sec, zone, _) = level.GridState.GetZoneForPoint(item.Center.X, item.Center.Y);
                if (sec == targetSector)
                {
                    zoneNumbers.Add(zone);
                }
            }
        }

        if (zoneNumbers.Count == 0)
        {
            zoneNumbers.Add(0);
        }

        string layerName = targetSector switch
        {
            SectorType.Secondary => "SecondaryLayer",
            SectorType.Overload => "ThirdLayer",
            _ => "MainLayer"
        };

        foreach (int zone in zoneNumbers.OrderBy(z => z))
        {
            var zoneCustomData = new JsonObject
            {
                ["DimensionIndex"] = "Reality",
                ["Layer"] = layerName,
                ["LocalIndex"] = $"Zone_{zone}",
                ["HibernateSpawnAligns"] = new JsonArray(),
                ["EnemySpawnPoints"] = new JsonArray(),
                ["BioscanSpawnPoints"] = new JsonArray(),
                ["InvisibleWalls"] = new JsonArray(),
                ["ForceGeneratorClusterMarkers"] = false,
                ["SetupWorldEventObjectOnLightsInZone"] = false
            };

            var weObjectsArray = new JsonArray();

            var matchingItems = realityItems.Where(t =>
            {
                if (level.GridState == null) return false;
                var (sec, z, _) = level.GridState.GetZoneForPoint(t.Center.X, t.Center.Y);
                return sec == targetSector && z == zone;
            }).ToList();

            var filterInstanceCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

            foreach (var item in matchingItems)
            {
                int areaIndex = item.AreaIndex;
                if (level.GridState != null)
                {
                    var (_, _, area) = level.GridState.GetZoneForPoint(item.Center.X, item.Center.Y);
                    if (areaIndex <= 0) areaIndex = area;
                }

                if (item.IsWorldEventObject)
                {
                    string filter = string.IsNullOrWhiteSpace(item.Name)
                        ? $"BigPickup_{item.PickupToSpawn}"
                        : item.Name;
                    int instanceIndex = filterInstanceCounts.TryGetValue(filter, out int instCount) ? instCount : 0;
                    filterInstanceCounts[filter] = instanceIndex + 1;
                    item.InstanceIndex = instanceIndex;

                    weObjectsArray.Add(SerializeBigPickupToWorldEventObject(item, instanceIndex, context));
                }
                else
                {
                    weObjectsArray.Add(SerializeTriggerToWorldEventObject(item, areaIndex, context));
                }
            }

            zoneCustomData["WorldEventObjects"] = weObjectsArray;
            zoneCustomData["StaticEventsOnTrigger"] = new JsonArray();
            zoneCustomData["StaticWorldEventChainedPuzzleDatas"] = new JsonArray();
            zoneCustomData["StaticSpecificPickupSpawnDatas"] = new JsonArray();
            zoneCustomData["StaticSpecificTerminalSpawnDatas"] = new JsonArray();

            zonesArray.Add(zoneCustomData);
        }

        // Include dimension zones in Main sector file (or if targetSector == SectorType.Main)
        if (targetSector == SectorType.Main)
        {
            foreach (var dimension in level.Dimensions)
            {
                if (dimension?.GridState == null) continue;
                string dimName = !string.IsNullOrWhiteSpace(dimension.Name) ? dimension.Name : "Dimension_1";
                var dimItems = dimension.GridState.PlacedTriggers;
                if (dimItems.Count == 0 && dimension.GridState.PlacedRooms.Count == 0) continue;

                var dimZoneKeys = new HashSet<int>();
                foreach (var room in dimension.GridState.PlacedRooms) dimZoneKeys.Add(room.Zone);
                foreach (var item in dimItems)
                {
                    var (_, z, _) = dimension.GridState.GetZoneForPoint(item.Center.X, item.Center.Y);
                    dimZoneKeys.Add(z);
                }

                if (dimZoneKeys.Count == 0) dimZoneKeys.Add(0);

                foreach (int zone in dimZoneKeys.OrderBy(z => z))
                {
                    var dimZoneObj = new JsonObject
                    {
                        ["DimensionIndex"] = dimName,
                        ["Layer"] = "MainLayer",
                        ["LocalIndex"] = $"Zone_{zone}",
                        ["HibernateSpawnAligns"] = new JsonArray(),
                        ["EnemySpawnPoints"] = new JsonArray(),
                        ["BioscanSpawnPoints"] = new JsonArray(),
                        ["InvisibleWalls"] = new JsonArray(),
                        ["ForceGeneratorClusterMarkers"] = false,
                        ["SetupWorldEventObjectOnLightsInZone"] = false
                    };

                    var weObjectsArray = new JsonArray();
                    var staticEventsArray = new JsonArray();
                    var staticPickupsArray = new JsonArray();

                    var matchingDimItems = dimItems.Where(t =>
                    {
                        var (_, z, _) = dimension.GridState.GetZoneForPoint(t.Center.X, t.Center.Y);
                        return z == zone;
                    }).ToList();

                    var dimFilterInstanceCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

                    foreach (var item in matchingDimItems)
                    {
                        var (_, _, area) = dimension.GridState.GetZoneForPoint(item.Center.X, item.Center.Y);
                        int areaIndex = item.AreaIndex > 0 ? item.AreaIndex : area;

                        if (item.IsWorldEventObject)
                        {
                            string filter = string.IsNullOrWhiteSpace(item.Name)
                                ? $"BigPickup_{item.PickupToSpawn}"
                                : item.Name;
                            int instanceIndex = dimFilterInstanceCounts.TryGetValue(filter, out int instCount)
                                ? instCount
                                : 0;
                            dimFilterInstanceCounts[filter] = instanceIndex + 1;
                            item.InstanceIndex = instanceIndex;

                            weObjectsArray.Add(SerializeBigPickupToWorldEventObject(item, instanceIndex, context));

                            staticPickupsArray.Add(new JsonObject
                            {
                                ["PickupToSpawn"] = (int)item.PickupToSpawn,
                                ["WorldEventObjectFilter"] = filter
                            });
                        }
                        else
                        {
                            weObjectsArray.Add(SerializeTriggerToWorldEventObject(item, areaIndex, context));

                            string filter = string.IsNullOrWhiteSpace(item.Name) ? $"WE_Trigger_{item.Id}" : item.Name;
                            var serializedEvents =
                                WardenObjectiveDataBlockExporter.SerializeWardenEvents(item.Events,
                                    (t, c) => context.SynthesizeText(t, c));
                            foreach (var node in serializedEvents)
                            {
                                if (node is JsonObject evObj)
                                {
                                    var cloneObj = JsonNode.Parse(evObj.ToJsonString()) as JsonObject;
                                    if (cloneObj != null)
                                    {
                                        cloneObj["WorldEventTriggerObjectFilter"] = filter;
                                        staticEventsArray.Add(cloneObj);
                                    }
                                }
                            }
                        }
                    }

                    dimZoneObj["WorldEventObjects"] = weObjectsArray;
                    dimZoneObj["StaticEventsOnTrigger"] = staticEventsArray;
                    dimZoneObj["StaticWorldEventChainedPuzzleDatas"] = new JsonArray();
                    dimZoneObj["StaticSpecificPickupSpawnDatas"] = staticPickupsArray;
                    dimZoneObj["StaticSpecificTerminalSpawnDatas"] = new JsonArray();

                    zonesArray.Add(dimZoneObj);
                }
            }
        }

        root["Zones"] = zonesArray;

        string fileName = GetAraFileName(level, exp, layoutId, targetSector);
        string fullPath = Path.Combine(context.AraLevelLayoutDirectory, fileName);
        File.WriteAllText(fullPath, root.ToJsonString(ExportContext.JsonOptions));
        context.AraLevelLayoutFilesExportedCount++;

        return root;
    }

    public static string GetAraFileName(LevelLayoutBlock level, ExpeditionBlock? exp, uint layoutId,
        SectorType sector = SectorType.Main)
    {
        string suffix = sector switch
        {
            SectorType.Secondary => "_Secondary",
            SectorType.Overload => "_Third",
            _ => string.Empty
        };

        if (exp != null && !string.IsNullOrWhiteSpace(exp.Descriptive.Prefix))
        {
            return $"{SanitizeFileName(exp.Descriptive.Prefix)}{suffix}.json";
        }

        if (exp != null && !string.IsNullOrWhiteSpace(exp.Descriptive.PublicName))
        {
            return $"{SanitizeFileName(exp.Descriptive.PublicName)}{suffix}.json";
        }

        if (!string.IsNullOrWhiteSpace(level.Name))
        {
            return $"{SanitizeFileName(level.Name)}{suffix}.json";
        }

        return $"LevelLayout_{layoutId}{suffix}.json";
    }

    private static string SanitizeFileName(string name)
    {
        var invalid = Path.GetInvalidFileNameChars();
        return string.Concat(name.Select(c => invalid.Contains(c) ? '_' : c)).Trim();
    }

    private static JsonObject SerializeBigPickupToWorldEventObject(PlacedTriggerNode pickup, int instanceIndex,
        ExportContext context)
    {
        string filter = string.IsNullOrWhiteSpace(pickup.Name) ? $"BigPickup_{pickup.PickupToSpawn}" : pickup.Name;

        var weObj = new JsonObject
        {
            ["WorldEventObjectFilter"] = filter,
            ["InstanceIndex"] = instanceIndex,
            ["AreaIndex"] = -1,
            ["UseExistingFilterInArea"] = false,
            ["UseRandomPosition"] = pickup.UseRandomPosition,
            ["RandomPositions"] = new JsonArray(),
            ["Position"] = new JsonObject
            {
                ["x"] = Math.Round(pickup.Center.X, 4),
                ["y"] = Math.Round(pickup.Center.Y, 4),
                ["z"] = Math.Round(pickup.Center.Z, 4)
            },
            ["Rotation"] = new JsonObject
            {
                ["x"] = Math.Round(pickup.Rotation.X, 4),
                ["y"] = Math.Round(pickup.Rotation.Y, 4),
                ["z"] = Math.Round(pickup.Rotation.Z, 4)
            },
            ["Scale"] = new JsonObject
            {
                ["x"] = Math.Round(pickup.Scale.X, 4),
                ["y"] = Math.Round(pickup.Scale.Y, 4),
                ["z"] = Math.Round(pickup.Scale.Z, 4)
            }
        };

        var eventsArray = new JsonArray();
        var serializedEvents =
            WardenObjectiveDataBlockExporter.SerializeWardenEvents(pickup.Events,
                (t, c) => context.SynthesizeText(t, c));
        foreach (var evNode in serializedEvents)
        {
            if (evNode is JsonObject evObj)
            {
                var cloneObj = JsonNode.Parse(evObj.ToJsonString()) as JsonObject;
                if (cloneObj != null)
                {
                    eventsArray.Add(cloneObj);
                }
            }
        }

        weObj["Components"] = new JsonObject
        {
            ["WE_SpecificPickup"] = new JsonObject
            {
                ["EventsOnPickup"] = eventsArray
            }
        };

        return weObj;
    }

    private static JsonObject SerializeTriggerToWorldEventObject(PlacedTriggerNode trigger, int areaIndex,
        ExportContext context)
    {
        var weObj = new JsonObject
        {
            ["WorldEventObjectFilter"] =
                string.IsNullOrWhiteSpace(trigger.Name) ? $"WE_Trigger_{trigger.Id}" : trigger.Name,
            ["InstanceIndex"] = trigger.InstanceIndex,
            ["AreaIndex"] = areaIndex,
            ["UseExistingFilterInArea"] = trigger.UseExistingFilterInArea,
            ["UseRandomPosition"] = trigger.UseRandomPosition,
            ["RandomPositions"] = new JsonArray(),
            ["Position"] = new JsonObject
            {
                ["x"] = Math.Round(trigger.Center.X, 4),
                ["y"] = Math.Round(trigger.Center.Y, 4),
                ["z"] = Math.Round(trigger.Center.Z, 4)
            },
            ["Rotation"] = new JsonObject
            {
                ["x"] = Math.Round(trigger.Rotation.X, 4),
                ["y"] = Math.Round(trigger.Rotation.Y, 4),
                ["z"] = Math.Round(trigger.Rotation.Z, 4)
            },
            ["Scale"] = new JsonObject
            {
                ["x"] = Math.Round(trigger.Scale.X, 4),
                ["y"] = Math.Round(trigger.Scale.Y, 4),
                ["z"] = Math.Round(trigger.Scale.Z, 4)
            }
        };

        var componentsObj = new JsonObject();
        string compKey = trigger.Type switch
        {
            TriggerType.Interaction => "WE_InteractTrigger",
            TriggerType.LookAt => "WE_LookatTrigger",
            _ => "WE_CollisionTrigger"
        };

        var compObj = new JsonObject
        {
            ["ColliderType"] = trigger.Shape.ToString(),
            ["Center"] = new JsonObject
            {
                ["x"] = 0.0,
                ["y"] = 0.0,
                ["z"] = 0.0
            }
        };

        switch (trigger.Shape)
        {
            case TriggerShape.Box:
                compObj["Size"] = new JsonObject
                {
                    ["x"] = Math.Round(trigger.Size.X, 4),
                    ["y"] = Math.Round(trigger.Size.Y, 4),
                    ["z"] = Math.Round(trigger.Size.Z, 4)
                };
                break;

            case TriggerShape.Sphere:
                compObj["Radius"] = Math.Round(trigger.Radius, 4);
                break;

            case TriggerShape.Capsule:
                compObj["Radius"] = Math.Round(trigger.Radius, 4);
                compObj["Height"] = Math.Round(trigger.Height, 4);
                break;
        }

        compObj["IsToggle"] = trigger.IsToggle;

        if (trigger.Type == TriggerType.LookAt)
        {
            compObj["LookatMaxDistance"] = 0.0;
        }
        else if (trigger.Type == TriggerType.Interaction)
        {
            uint intTextId = 0;
            if (!string.IsNullOrWhiteSpace(trigger.InteractionText))
            {
                intTextId = context.SynthesizeText(trigger.InteractionText, "Trigger Interaction Text");
            }

            compObj["InteractionText"] = intTextId;
            compObj["CarryItemInsertType"] = trigger.CarryItemInsertType.ToString();
            compObj["CarryItemTransform"] = new JsonObject
            {
                ["Position"] = new JsonObject
                {
                    ["x"] = Math.Round(trigger.CarryItemTransform.Position.X, 4),
                    ["y"] = Math.Round(trigger.CarryItemTransform.Position.Y, 4),
                    ["z"] = Math.Round(trigger.CarryItemTransform.Position.Z, 4)
                },
                ["Rotation"] = new JsonObject
                {
                    ["x"] = Math.Round(trigger.CarryItemTransform.Rotation.X, 4),
                    ["y"] = Math.Round(trigger.CarryItemTransform.Rotation.Y, 4),
                    ["z"] = Math.Round(trigger.CarryItemTransform.Rotation.Z, 4)
                },
                ["Scale"] = new JsonObject
                {
                    ["x"] = Math.Round(trigger.CarryItemTransform.Scale.X, 4),
                    ["y"] = Math.Round(trigger.CarryItemTransform.Scale.Y, 4),
                    ["z"] = Math.Round(trigger.CarryItemTransform.Scale.Z, 4)
                }
            };
            compObj["RemoveItemOnInsert"] = trigger.RemoveItemOnInsert;
            compObj["ItemStateAfterInsert"] = trigger.ItemStateAfterInsert.ToString();
        }

        componentsObj[compKey] = compObj;
        weObj["Components"] = componentsObj;

        return weObj;
    }

    public static void ImportAraLevelLayout(
        ImportContext context,
        LevelLayoutBlock level,
        ExpeditionBlock? exp,
        Dictionary<string, List<WardenEvent>>? zoneEventsByFilter = null,
        Dictionary<string, ARA_CustomWorldEventObjectOptions>? specificPickupsByFilter = null)
    {
        var layoutIds = new List<uint>();
        if (level.PersistentId > 0) layoutIds.Add(level.PersistentId);
        if (exp != null)
        {
            if (exp.LevelLayoutData > 0 && !layoutIds.Contains(exp.LevelLayoutData)) layoutIds.Add(exp.LevelLayoutData);
            if (exp.SecondaryLayout > 0 && !layoutIds.Contains(exp.SecondaryLayout)) layoutIds.Add(exp.SecondaryLayout);
            if (exp.ThirdLayout > 0 && !layoutIds.Contains(exp.ThirdLayout)) layoutIds.Add(exp.ThirdLayout);
        }

        var araFiles =
            context.FindAllAraLevelLayoutConfigFiles(layoutIds, exp?.Descriptive?.Prefix, exp?.Descriptive?.PublicName);

        foreach (var araFile in araFiles)
        {
            if (!File.Exists(araFile)) continue;

            try
            {
                string content = File.ReadAllText(araFile);
                var rootNode = JsonNode.Parse(content);
                if (rootNode is not JsonObject araRoot || araRoot["Zones"] is not JsonArray zonesArray) continue;

                foreach (var zoneNode in zonesArray)
                {
                    if (zoneNode is not JsonObject zoneObj) continue;

                    string dimIndexStr = zoneObj["DimensionIndex"]?.ToString() ?? "Reality";
                    bool isDimension = !dimIndexStr.Equals("Reality", StringComparison.OrdinalIgnoreCase) && dimIndexStr != "0";
                    LevelLayoutGridState? targetGrid = null;
                    DimensionLayoutBlock? dim = null;

                    if (!isDimension)
                    {
                        targetGrid = level.GridState;
                    }
                    else
                    {
                        dim = level.Dimensions.FirstOrDefault(d =>
                            d.Name.Equals(dimIndexStr, StringComparison.OrdinalIgnoreCase));
                        if (dim == null)
                        {
                            dim = new DimensionLayoutBlock { Name = dimIndexStr };
                            level.Dimensions.Add(dim);
                        }

                        targetGrid = dim.GridState;
                    }

                    if (targetGrid == null) continue;

                    string layerStr = zoneObj["Layer"]?.ToString() ?? "MainLayer";
                    SectorType targetSector = layerStr switch
                    {
                        "SecondaryLayer" => SectorType.Secondary,
                        "ThirdLayer" => SectorType.Overload,
                        _ => SectorType.Main
                    };

                    string localIdxStr = zoneObj["LocalIndex"]?.ToString() ?? "Zone_0";
                    int zoneNumber = 0;
                    if (localIdxStr.StartsWith("Zone_", StringComparison.OrdinalIgnoreCase) &&
                        int.TryParse(localIdxStr.Substring(5), out int parsedZone))
                    {
                        zoneNumber = parsedZone;
                    }
                    else if (int.TryParse(localIdxStr, out int parsedNumZone))
                    {
                        zoneNumber = parsedNumZone;
                    }

                    var staticEventsMap = new Dictionary<string, List<WardenEvent>>(StringComparer.OrdinalIgnoreCase);
                    if (zoneObj["StaticEventsOnTrigger"] is JsonArray staticEventsArr)
                    {
                        var importedStaticEvents = new ObservableCollection<WardenEvent>();
                        LevelLayoutDataBlockImporter.ImportWardenEvents(staticEventsArr, importedStaticEvents, context);
                        for (int i = 0; i < staticEventsArr.Count && i < importedStaticEvents.Count; i++)
                        {
                            string filter = staticEventsArr[i]?["WorldEventTriggerObjectFilter"]?.ToString()
                                            ?? staticEventsArr[i]?["WorldEventObjectFilter"]?.ToString()
                                            ?? string.Empty;
                            if (!string.IsNullOrWhiteSpace(filter))
                            {
                                if (!staticEventsMap.TryGetValue(filter, out var list))
                                {
                                    list = new List<WardenEvent>();
                                    staticEventsMap[filter] = list;
                                }

                                list.Add(importedStaticEvents[i]);
                            }
                        }
                    }

                    var staticPickupsMap =
                        new Dictionary<string, ARA_CustomWorldEventObjectOptions>(StringComparer.OrdinalIgnoreCase);
                    if (zoneObj["StaticSpecificPickupSpawnDatas"] is JsonArray staticPickupsArr)
                    {
                        foreach (var spNode in staticPickupsArr)
                        {
                            if (spNode is not JsonObject spObj) continue;
                            string f = spObj["WorldEventObjectFilter"]?.ToString() ?? string.Empty;
                            int pId = spObj["PickupToSpawn"]?.GetValue<int>() ?? 0;
                            if (!string.IsNullOrWhiteSpace(f) && pId > 0 &&
                                Enum.IsDefined(typeof(ARA_CustomWorldEventObjectOptions), pId))
                            {
                                staticPickupsMap[f] = (ARA_CustomWorldEventObjectOptions)pId;
                            }
                        }
                    }

                    if (zoneObj["WorldEventObjects"] is not JsonArray weArray) continue;

                    foreach (var weNode in weArray)
                    {
                        if (weNode is not JsonObject weObj) continue;
                        if (weObj["Components"] is not JsonObject compObj) continue;

                        string filterName = weObj["WorldEventObjectFilter"]?.ToString() ?? string.Empty;

                        double x = 0, y = 0, z = 0;
                        if (weObj["Position"] is JsonObject posObj)
                        {
                            x = posObj["x"]?.GetValue<double>() ?? 0.0;
                            y = posObj["y"]?.GetValue<double>() ?? 0.0;
                            z = posObj["z"]?.GetValue<double>() ?? 0.0;
                        }

                        double rx = 0, ry = 0, rz = 0;
                        if (weObj["Rotation"] is JsonObject rotObj)
                        {
                            rx = rotObj["x"]?.GetValue<double>() ?? 0.0;
                            ry = rotObj["y"]?.GetValue<double>() ?? 0.0;
                            rz = rotObj["z"]?.GetValue<double>() ?? 0.0;
                        }

                        double sx = 1, sy = 1, sz = 1;
                        if (weObj["Scale"] is JsonObject scObj)
                        {
                            sx = scObj["x"]?.GetValue<double>() ?? 1.0;
                            sy = scObj["y"]?.GetValue<double>() ?? 1.0;
                            sz = scObj["z"]?.GetValue<double>() ?? 1.0;
                        }

                        int instanceIndex = weObj["InstanceIndex"]?.GetValue<int>() ?? 0;
                        int areaIndex = weObj["AreaIndex"]?.GetValue<int>() ?? 0;
                        bool useExistingFilter = weObj["UseExistingFilterInArea"]?.GetValue<bool>() ?? false;
                        bool useRandomPos = weObj["UseRandomPosition"]?.GetValue<bool>() ?? false;

                        // WE_SpecificPickup
                        if (compObj["WE_SpecificPickup"] is JsonObject spComp)
                        {
                            var pickupType = ARA_CustomWorldEventObjectOptions.DataSphere;
                            if (!string.IsNullOrWhiteSpace(filterName))
                            {
                                if (specificPickupsByFilter != null &&
                                    specificPickupsByFilter.TryGetValue(filterName, out var foundType))
                                {
                                    pickupType = foundType;
                                }
                                else if (staticPickupsMap.TryGetValue(filterName, out var foundStaticType))
                                {
                                    pickupType = foundStaticType;
                                }
                            }

                            var pickup = new PlacedTriggerNode
                            {
                                Id = Guid.NewGuid().ToString(),
                                Name = !string.IsNullOrWhiteSpace(filterName) ? filterName : $"BigPickup_{pickupType}",
                                IsWorldEventObject = true,
                                ItemType = PaletteItemType.BigPickup,
                                PickupToSpawn = pickupType,
                                Center = new Vector3Data(x, y, z),
                                Rotation = new Vector3Data(rx, ry, rz),
                                Scale = new Vector3Data(sx, sy, sz),
                                InstanceIndex = instanceIndex,
                                AreaIndex = -1,
                                UseExistingFilterInArea = false,
                                UseRandomPosition = useRandomPos
                            };

                            if (spComp["EventsOnPickup"] is JsonArray pickupEventsArr)
                            {
                                var importedPickupEvents = new ObservableCollection<WardenEvent>();
                                LevelLayoutDataBlockImporter.ImportWardenEvents(pickupEventsArr, importedPickupEvents,
                                    context);
                                foreach (var ev in importedPickupEvents)
                                {
                                    pickup.Events.Add(ev);
                                }
                            }

                            if (!targetGrid.PlacedTriggers.Any(t =>
                                    t.IsWorldEventObject && t.Name == pickup.Name && Math.Abs(t.Center.X - x) < 0.001 &&
                                    Math.Abs(t.Center.Y - y) < 0.001))
                            {
                                targetGrid.PlacedTriggers.Add(pickup);
                            }

                            continue;
                        }

                        // Trigger types
                        JsonObject? trigComp = null;
                        TriggerType trigType = TriggerType.Collision;

                        if (compObj["WE_CollisionTrigger"] is JsonObject colObj)
                        {
                            trigComp = colObj;
                            trigType = TriggerType.Collision;
                        }
                        else if (compObj["WE_InteractTrigger"] is JsonObject intObj)
                        {
                            trigComp = intObj;
                            trigType = TriggerType.Interaction;
                        }
                        else if (compObj["WE_LookatTrigger"] is JsonObject lookObj)
                        {
                            trigComp = lookObj;
                            trigType = TriggerType.LookAt;
                        }

                        if (trigComp == null) continue;

                        if (string.IsNullOrWhiteSpace(filterName))
                        {
                            filterName = $"Trigger {targetGrid.PlacedTriggers.Count + 1}";
                        }

                        if (trigComp["Center"] is JsonObject centerObj)
                        {
                            x += centerObj["x"]?.GetValue<double>() ?? 0.0;
                            y += centerObj["y"]?.GetValue<double>() ?? 0.0;
                            z += centerObj["z"]?.GetValue<double>() ?? 0.0;
                        }

                        string colliderStr = trigComp["ColliderType"]?.ToString() ?? "Box";
                        Enum.TryParse<TriggerShape>(colliderStr, true, out var shape);

                        var trigger = new PlacedTriggerNode
                        {
                            Id = Guid.NewGuid().ToString(),
                            Name = filterName,
                            Shape = shape,
                            Type = trigType,
                            Center = new Vector3Data(x, y, z),
                            Rotation = new Vector3Data(rx, ry, rz),
                            Scale = new Vector3Data(sx, sy, sz),
                            InstanceIndex = instanceIndex,
                            AreaIndex = areaIndex,
                            UseExistingFilterInArea = useExistingFilter,
                            UseRandomPosition = useRandomPos
                        };

                        switch (shape)
                        {
                            case TriggerShape.Box:
                                if (trigComp["Size"] is JsonObject sizeObj)
                                {
                                    trigger.Size = new Vector3Data(
                                        sizeObj["x"]?.GetValue<double>() ?? 10.0,
                                        sizeObj["y"]?.GetValue<double>() ?? 10.0,
                                        sizeObj["z"]?.GetValue<double>() ?? 10.0
                                    );
                                }

                                break;

                            case TriggerShape.Sphere:
                                trigger.Radius = trigComp["Radius"]?.GetValue<double>() ?? 5.0;
                                break;

                            case TriggerShape.Capsule:
                                trigger.Radius = trigComp["Radius"]?.GetValue<double>() ?? 5.0;
                                trigger.Height = trigComp["Height"]?.GetValue<double>() ?? 10.0;
                                break;
                        }

                        trigger.IsToggle = trigComp["IsToggle"]?.GetValue<bool>() ?? false;

                        if (trigType == TriggerType.Interaction)
                        {
                            if (trigComp.TryGetPropertyValue("InteractionText", out var intTextNode) &&
                                intTextNode != null)
                            {
                                if (uint.TryParse(intTextNode.ToString(), out uint textId) && textId > 0)
                                {
                                    string resolved = context.ResolveText(textId);
                                    trigger.InteractionText = !string.IsNullOrWhiteSpace(resolved)
                                        ? resolved
                                        : textId.ToString();
                                }
                                else
                                {
                                    trigger.InteractionText = intTextNode.ToString();
                                }
                            }

                            if (trigComp["CarryItemInsertType"]?.ToString() is string ciStr &&
                                Enum.TryParse<CarryItemInsertType>(ciStr, true, out var ciType))
                            {
                                trigger.CarryItemInsertType = ciType;
                            }

                            if (trigComp["CarryItemTransform"] is JsonObject citObj)
                            {
                                var cit = new CarryItemTransformData();
                                if (citObj["Position"] is JsonObject cp)
                                {
                                    cit.Position = new Vector3Data(
                                        cp["x"]?.GetValue<double>() ?? 0.0,
                                        cp["y"]?.GetValue<double>() ?? 0.0,
                                        cp["z"]?.GetValue<double>() ?? 0.0
                                    );
                                }

                                if (citObj["Rotation"] is JsonObject cr)
                                {
                                    cit.Rotation = new Vector3Data(
                                        cr["x"]?.GetValue<double>() ?? 0.0,
                                        cr["y"]?.GetValue<double>() ?? 0.0,
                                        cr["z"]?.GetValue<double>() ?? 0.0
                                    );
                                }

                                if (citObj["Scale"] is JsonObject cs)
                                {
                                    cit.Scale = new Vector3Data(
                                        cs["x"]?.GetValue<double>() ?? 1.0,
                                        cs["y"]?.GetValue<double>() ?? 1.0,
                                        cs["z"]?.GetValue<double>() ?? 1.0
                                    );
                                }

                                trigger.CarryItemTransform = cit;
                            }

                            trigger.RemoveItemOnInsert = trigComp["RemoveItemOnInsert"]?.GetValue<bool>() ?? false;

                            if (trigComp["ItemStateAfterInsert"]?.ToString() is string isStr &&
                                Enum.TryParse<ItemStateAfterInsert>(isStr, true, out var isState))
                            {
                                trigger.ItemStateAfterInsert = isState;
                            }
                        }

                        if (zoneEventsByFilter != null &&
                            zoneEventsByFilter.TryGetValue(filterName, out var matchedEvs))
                        {
                            foreach (var ev in matchedEvs)
                            {
                                trigger.Events.Add(ev);
                            }
                        }

                        if (staticEventsMap.TryGetValue(filterName, out var matchedStaticEvs))
                        {
                            foreach (var ev in matchedStaticEvs)
                            {
                                trigger.Events.Add(ev);
                            }
                        }

                        if (!targetGrid.PlacedTriggers.Any(t =>
                                !t.IsWorldEventObject && t.Name == trigger.Name && Math.Abs(t.Center.X - x) < 0.001 &&
                                Math.Abs(t.Center.Y - y) < 0.001))
                        {
                            targetGrid.PlacedTriggers.Add(trigger);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                context.Warnings.Add($"Failed to import ARA LevelLayout config '{araFile}': {ex.Message}");
            }
        }
    }
}