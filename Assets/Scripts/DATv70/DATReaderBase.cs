using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LithFAQ
{
    public interface IDATReader
    {
        void Load(BinaryReader b);
        void ClearLevel();
    }
}
