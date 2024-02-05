using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;


namespace LithFAQ
{
    public interface IDATReader
    {
        void Load(BinaryReader b);
        void ClearLevel();
        WorldObjects GetWorldObjects();

        UInt32 GetVersion();
    }
}
