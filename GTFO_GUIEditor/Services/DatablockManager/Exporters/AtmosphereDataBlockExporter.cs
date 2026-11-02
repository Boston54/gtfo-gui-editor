using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Nodes;
using GTFO_GUIEditor.Models;

namespace GTFO_GUIEditor.Services;

public class AtmosphereDataBlockExporter : IDatablockExporter
{
    public void Export(ExportContext context)
    {
        var atmosphereBlocks = new List<JsonObject>(context.AtmosphereBlocks);

        foreach (var level in context.MainVm.LevelLayout.Levels)
        {
            if (level == null) continue;

            foreach (var dim in level.Dimensions)
            {
                if (dim == null) continue;

                uint atmoId = context.AtmosphereAlloc.Allocate();
                context.DimensionAtmosphereIds[dim] = atmoId;

                var lightColorObj = new JsonObject
                {
                    ["a"] = dim.AtmosphereLightColor.A,
                    ["r"] = dim.AtmosphereLightColor.R,
                    ["g"] = dim.AtmosphereLightColor.G,
                    ["b"] = dim.AtmosphereLightColor.B
                };

                var planetAlbedoObj = new JsonObject
                {
                    ["a"] = dim.AtmospherePlanetAlbedo.A,
                    ["r"] = dim.AtmospherePlanetAlbedo.R,
                    ["g"] = dim.AtmospherePlanetAlbedo.G,
                    ["b"] = dim.AtmospherePlanetAlbedo.B
                };

                string atmoName = string.IsNullOrWhiteSpace(dim.Name)
                    ? $"Atmosphere_{atmoId}"
                    : $"Atmosphere_Dimension_{dim.Name}";

                var atmoObj = new JsonObject
                {
                    ["LightColor"] = lightColorObj,
                    ["PlanetAlbedo"] = planetAlbedoObj,
                    ["name"] = atmoName,
                    ["internalEnabled"] = true,
                    ["persistentID"] = atmoId
                };

                atmosphereBlocks.Add(atmoObj);
            }
        }

        context.AtmosphereBlocks.Clear();
        context.AtmosphereBlocks.AddRange(atmosphereBlocks);

        ExportContext.WriteDatablockFile(
            context.ExportRootPath,
            "GameData_AtmosphereDataBlock_bin.json",
            context.AtmosphereBlocks);

        context.DatablocksExportedCount++;
    }
}
