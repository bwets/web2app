using System.Buffers.Binary;

namespace Web2App.Core;

/// <summary>Minimal .ico handling: wrapping a PNG into an icon file and reading PNG frames back.</summary>
public static class IconFile
{
    private const int HeaderSize = 6;
    private const int EntrySize = 16;

    private static ReadOnlySpan<byte> PngSignature => [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];

    public static bool IsPng(ReadOnlySpan<byte> data) => data.StartsWith(PngSignature);

    public static bool IsIco(ReadOnlySpan<byte> data) =>
        data.Length >= HeaderSize && data[0] == 0 && data[1] == 0 && data[2] == 1 && data[3] == 0;

    /// <summary>Builds a single-frame .ico whose frame is the given PNG (supported since Windows Vista).</summary>
    public static byte[] FromPng(byte[] png)
    {
        if (!IsPng(png) || png.Length < 24)
        {
            throw new ArgumentException("Not a PNG image.", nameof(png));
        }

        var width = BinaryPrimitives.ReadInt32BigEndian(png.AsSpan(16, 4));
        var height = BinaryPrimitives.ReadInt32BigEndian(png.AsSpan(20, 4));

        var ico = new byte[HeaderSize + EntrySize + png.Length];
        var span = ico.AsSpan();
        BinaryPrimitives.WriteUInt16LittleEndian(span[2..], 1); // type: icon
        BinaryPrimitives.WriteUInt16LittleEndian(span[4..], 1); // one image

        var entry = span.Slice(HeaderSize, EntrySize);
        entry[0] = width >= 256 ? (byte)0 : (byte)width;
        entry[1] = height >= 256 ? (byte)0 : (byte)height;
        BinaryPrimitives.WriteUInt16LittleEndian(entry[4..], 1);  // colour planes
        BinaryPrimitives.WriteUInt16LittleEndian(entry[6..], 32); // bits per pixel
        BinaryPrimitives.WriteUInt32LittleEndian(entry[8..], (uint)png.Length);
        BinaryPrimitives.WriteUInt32LittleEndian(entry[12..], HeaderSize + EntrySize);

        png.CopyTo(span[(HeaderSize + EntrySize)..]);
        return ico;
    }

    /// <summary>Returns the largest frame of an .ico when that frame is a PNG, otherwise null.</summary>
    public static byte[]? ExtractLargestPng(byte[] ico)
    {
        if (!IsIco(ico))
        {
            return null;
        }

        int count = BinaryPrimitives.ReadUInt16LittleEndian(ico.AsSpan(4, 2));
        var bestSize = -1;
        ReadOnlySpan<byte> best = default;

        for (var i = 0; i < count; i++)
        {
            var entryOffset = HeaderSize + (i * EntrySize);
            if (entryOffset + EntrySize > ico.Length)
            {
                break;
            }

            var entry = ico.AsSpan(entryOffset, EntrySize);
            var size = entry[0] == 0 ? 256 : entry[0];
            var length = BinaryPrimitives.ReadUInt32LittleEndian(entry[8..]);
            var offset = BinaryPrimitives.ReadUInt32LittleEndian(entry[12..]);
            if (offset > (uint)ico.Length || length > (uint)ico.Length - offset)
            {
                continue;
            }

            if (size > bestSize)
            {
                bestSize = size;
                best = ico.AsSpan((int)offset, (int)length);
            }
        }

        return bestSize >= 0 && IsPng(best) ? best.ToArray() : null;
    }
}
