using System;
namespace GTFO_GUIEditor.Services
{
    public class DatablockAllocator : PersistentIdAllocator
    {
        public DatablockAllocator(uint startingOffset = 100000) : base(startingOffset) { }
    }
}
