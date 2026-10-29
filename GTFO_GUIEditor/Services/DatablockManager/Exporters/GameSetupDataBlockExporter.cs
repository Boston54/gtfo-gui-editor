using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Nodes;

namespace GTFO_GUIEditor.Services;

public class GameSetupDataBlockExporter : IDatablockExporter
{
    public void Export(ExportContext context)
    {
        var gameSetupBlocks = new List<JsonObject>();
        uint exportedRundownId = context.MainVm.Rundown.SelectedRundown?.PersistentId > 0
            ? context.MainVm.Rundown.SelectedRundown.PersistentId
            : 1;

        var vanillaGameSetup = ExportContext.LoadVanillaBlocksArray("GameSetupDataBlock.json");
        if (vanillaGameSetup.Count > 0)
        {
            foreach (var setupBlock in vanillaGameSetup)
            {
                setupBlock["RundownIdsToLoad"] = new JsonArray { exportedRundownId };
                gameSetupBlocks.Add(setupBlock);
            }
        }
        else
        {
            var setupBlock = new JsonObject
            {
                ["RundownIdsToLoad"] = new JsonArray { exportedRundownId },
                ["StartupScreenToLoad"] = "StartupScreenData_3",
                ["name"] = "Default",
                ["internalEnabled"] = true,
                ["persistentID"] = 1
            };
            gameSetupBlocks.Add(setupBlock);
        }

        context.GameSetupBlocks.Clear();
        context.GameSetupBlocks.AddRange(gameSetupBlocks);

        ExportContext.WriteDatablockFile(
            context.ExportRootPath,
            "GameData_GameSetupDataBlock_bin.json",
            context.GameSetupBlocks);

        context.DatablocksExportedCount++;
    }
}
