using System;
using System.Collections.Generic;
using System.Threading;

namespace GTFO_GUIEditor.Services;

/// <summary>
/// Thread-safe queue of datablock files that still need to be saved.
/// Files edited while a save is running are deliberately retained in this queue.
/// </summary>
public sealed class DatablockSaveQueue
{
    public static readonly string[] AllDatablockFiles =
    {
        "GameData_ChainedPuzzleDataBlock_bin.json",
        "GameData_ChainedPuzzleTypeDataBlock_bin.json",
        "GameData_ExpeditionBalanceDataBlock_bin.json",
        "GameData_ComplexResourceSetDataBlock_bin.json",
        "GameData_DimensionsDataBlock_bin.json",
        "GameData_AtmosphereDataBlock_bin.json",
        "GameData_WardenObjectiveDataBlock_bin.json",
        "GameData_LevelLayoutDataBlock_bin.json",
        "GameData_LightSettingsDataBlock_bin.json",
        "GameData_FogSettingsDataBlock_bin.json",
        "GameData_ConsumableDistributionDataBlock_bin.json",
        "GameData_SurvivalWavePopulationDataBlock_bin.json",
        "GameData_SurvivalWaveSettingsDataBlock_bin.json",
        "GameData_EnemyDataBlock_bin.json",
        "GameData_EnemyGroupDataBlock_bin.json",
        "GameData_RundownDataBlock_bin.json",
        "GameData_GameSetupDataBlock_bin.json",
        "GameData_TextDataBlock_bin.json"
    };
    private readonly object _mutex = new();
    private readonly HashSet<string> _filesToSave = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, long> _revisions = new(StringComparer.OrdinalIgnoreCase);

    public static DatablockSaveQueue Instance { get; } = new();

    public int Count
    {
        get
        {
            lock (_mutex)
            {
                return _filesToSave.Count;
            }
        }
    }

    public IReadOnlyCollection<string> Files
    {
        get
        {
            lock (_mutex)
            {
                return new List<string>(_filesToSave);
            }
        }
    }

    public void MarkDirty(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            throw new ArgumentException("File name cannot be empty.", nameof(fileName));
        }
        lock (_mutex)
        {
            _filesToSave.Add(fileName);
            _revisions.TryGetValue(fileName, out long revision);
            _revisions[fileName] = revision + 1;
        }
    }

    public void MarkDirty(IEnumerable<string> fileNames)
    {
        lock (_mutex)
        {
            foreach (string fileName in fileNames)
            {
                if (!string.IsNullOrWhiteSpace(fileName))
                {
                    _filesToSave.Add(fileName);
                    _revisions.TryGetValue(fileName, out long revision);
                    _revisions[fileName] = revision + 1;
                }
            }
        }
    }

    public IReadOnlyList<string> Snapshot()
    {
        lock (_mutex)
        {
            return new List<string>(_filesToSave);
        }
    }

    public void MarkSaved(IEnumerable<string> fileNames)
    {
        lock (_mutex)
        {
            foreach (string fileName in fileNames)
            {
                _filesToSave.Remove(fileName);
            }
        }
    }

    public void Clear()
    {
        lock (_mutex)
        {
            _filesToSave.Clear();
            _revisions.Clear();
        }
    }

    public Dictionary<string, long> TakeRevisionSnapshot()
    {
        lock (_mutex)
        {
            var snapshot = new Dictionary<string, long>(StringComparer.OrdinalIgnoreCase);
            foreach (string fileName in _filesToSave)
            {
                snapshot[fileName] = _revisions[fileName];
            }
            return snapshot;
        }
    }

    public void MarkSaved(string fileName, long revision)
    {
        lock (_mutex)
        {
            if (_revisions.TryGetValue(fileName, out long current) && current == revision)
            {
                _filesToSave.Remove(fileName);
                _revisions.Remove(fileName);
            }
        }
    }
}
