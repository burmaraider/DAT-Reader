using System;

[Flags]
public enum DTXFlags : uint
{
    FULLBRIGHT = (1U << 0),     // 1        Use full bright.
    PREFER16BIT = (1U << 1),    // 2        Prefer 16-bit mode.
    UNK1 = (1U << 2),           // 4        Each TextureMipData has its texture data allocated.
    UNK2 = (1U << 3),           // 8        Set to indicate this has a "fixed" section count. Originally the sections count was wrong.
    UNK3 = (1U << 4),           // 16
    UNK4 = (1U << 5),           // 32
    NOSYSCACHE = (1U << 6),     // 64       Tells the engine  to not keep a system memory copy of the texture.
    PREFER4444 = (1U << 7),     // 128      If in 16-bit mode, use a 4444 texture for this.
    PREFER5551 = (1U << 8),     // 256      Use 5551 if 16-bit.
    _32BITSYSCOPY = (1 << 9),   // 512      If there is a sys copy - don't convert it to device specific format (keep it 32 bit).
    DTX_CUBEMAP = (1 << 10),    // 1024     Cube environment map.  +x is stored in the normal data area, -x,+y,-y,+z,-z are stored in their own sections
    DTX_BUMPMAP = (1 << 11),    // 2048     Bump mapped texture, this has 8 bit U and V components for the bump normal
	DTX_LUMBUMPMAP = (1 << 12), // 4096     Bump mapped texture with luminance, this has 8 bits for luminance, U and V
}
