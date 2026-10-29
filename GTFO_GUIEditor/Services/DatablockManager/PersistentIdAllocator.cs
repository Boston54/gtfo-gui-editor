using System.Collections.Generic;

namespace GTFO_GUIEditor.Services;

/// <summary>
/// Allocates and manages unique persistent IDs for datablocks and synthesized items.
/// </summary>
public class PersistentIdAllocator
{
    private readonly HashSet<uint> _usedIds = new();
    private uint _nextOffset = 100000;

    public PersistentIdAllocator(uint startingOffset = 100000)
    {
        _nextOffset = startingOffset;
    }

    public void RegisterExisting(uint id)
    {
        if (id > 0)
        {
            _usedIds.Add(id);
        }
    }

    public void RegisterExisting(IEnumerable<uint> ids)
    {
        foreach (var id in ids)
        {
            RegisterExisting(id);
        }
    }

    public uint Allocate(uint? requestedId = null)
    {
        if (requestedId.HasValue && requestedId.Value > 0 && !_usedIds.Contains(requestedId.Value))
        {
            _usedIds.Add(requestedId.Value);
            return requestedId.Value;
        }

        while (_usedIds.Contains(_nextOffset) || _nextOffset == 0)
        {
            _nextOffset++;
        }

        uint allocated = _nextOffset++;
        _usedIds.Add(allocated);
        return allocated;
    }
}
