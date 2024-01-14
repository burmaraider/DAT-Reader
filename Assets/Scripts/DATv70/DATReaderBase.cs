using System.Drawing;
using System.IO;


namespace LithFAQ
{
    public interface IDATReader
    {
        void Load(BinaryReader b);
        void ClearLevel();
    }
}
