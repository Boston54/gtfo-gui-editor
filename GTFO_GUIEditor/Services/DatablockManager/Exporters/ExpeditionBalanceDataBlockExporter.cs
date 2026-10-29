using System.Collections.Generic;
using System.Text.Json.Nodes;

namespace GTFO_GUIEditor.Services;

public class ExpeditionBalanceDataBlockExporter : IDatablockExporter
{
    public void Export(ExportContext context)
    {
        var expeditionBalanceBlocks = new List<JsonObject>();

        foreach (var level in context.MainVm.LevelLayout.Levels)
        {
            if (level == null || level.ExpeditionBalance == null) continue;

            uint expBalId = context.ExpeditionBalanceAlloc.Allocate();
            context.LevelExpeditionBalanceIds[level] = expBalId;

            var expBalObj = level.ExpeditionBalance.ToExportJsonObject(
                expBalId,
                $"Generated_ExpeditionBalance_{level.Name}_{expBalId}",
                level.InternalEnabled
            );
            expeditionBalanceBlocks.Add(expBalObj);
        }

        context.ExpeditionBalanceBlocks.Clear();
        context.ExpeditionBalanceBlocks.AddRange(expeditionBalanceBlocks);

        var vanillaExpeditionBalanceHeaders = ExportContext.LoadVanillaHeadersArray("ExpeditionBalanceDataBlock.json");

        ExportContext.WriteDatablockFile(
            context.ExportRootPath,
            "GameData_ExpeditionBalanceDataBlock_bin.json",
            context.ExpeditionBalanceBlocks.Count > 0 ? context.ExpeditionBalanceBlocks : context.VanillaExpeditionBalanceBlocks,
            vanillaExpeditionBalanceHeaders);

        context.DatablocksExportedCount++;
    }
}
