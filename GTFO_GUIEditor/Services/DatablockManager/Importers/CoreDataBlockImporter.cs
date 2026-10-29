using System.Collections.ObjectModel;
using System.Text.Json.Serialization;
using GTFO_GUIEditor.Models;

namespace GTFO_GUIEditor.Services;

public class CoreDataBlockImporter : IDatablockImporter
{
    public void Import(ImportContext context)
    {
        Import(context, "ComplexResourceSetDataBlock.json", "GameData_ComplexResourceSetDataBlock_bin.json",
            DatablockManager.Instance.Get<ComplexResourceSetBlock>(),
            (ComplexResourceSetDataBlockFile f) => f.Blocks);

        Import(context, "ConsumableDistributionDataBlock.json", "ConsumableDistributionDatablock.json",
            "GameData_ConsumableDistributionDataBlock_bin.json",
            DatablockManager.Instance.Get<ConsumableDistributionBlock>(),
            (ConsumableDistributionDataBlockFile f) => f.Blocks);

        Import(context, "FogSettingsDataBlock.json", "GameData_FogSettingsDataBlock_bin.json",
            DatablockManager.Instance.Get<FogSettingsBlock>(),
            (FogSettingsDataBlockFile f) => f.Blocks);

        Import(context, "LightSettingsDataBlock.json", "GameData_LightSettingsDataBlock_bin.json",
            DatablockManager.Instance.Get<LightSettingsBlock>(),
            (LightSettingsDataBlockFile f) => f.Blocks);

        Import(context, "RundownDataBlock.json", "GameData_RundownDataBlock_bin.json",
            DatablockManager.Instance.Get<RundownBlock>(),
            (RundownDataBlockFile f) => f.Blocks);
    }

    private static void Import<TFile, TBlock>(
        ImportContext context, string name, string alternate, ObservableCollection<TBlock> target,
        System.Func<TFile, System.Collections.Generic.List<TBlock>> getBlocks)
        where TBlock : class
        where TFile : class
    {
        var path = context.FindFile(name, alternate);
        if (context.TryDeserializeFile<TFile>(path, out var file) && file != null)
        {
            target.Clear();
            foreach (var block in getBlocks(file))
            {
                target.Add(block);
            }
        }
    }

    private static void Import<TFile, TBlock>(
        ImportContext context, string name, string alternate1, string alternate2, ObservableCollection<TBlock> target,
        System.Func<TFile, System.Collections.Generic.List<TBlock>> getBlocks)
        where TBlock : class
        where TFile : class
    {
        var path = context.FindFile(name, alternate1, alternate2);
        if (context.TryDeserializeFile<TFile>(path, out var file) && file != null)
        {
            target.Clear();
            foreach (var block in getBlocks(file))
            {
                target.Add(block);
            }
        }
    }
}
