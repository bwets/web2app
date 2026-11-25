using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using Web2App.Core;

namespace Web2App;

/// <summary>Decodes downloaded icons and favicons into square bitmaps of at most 256 pixels.</summary>
internal static class IconImages
{
    public const int MaxSize = 256;

    /// <summary>Decodes ICO, PNG, JPEG, GIF or BMP data; returns null for anything else (SVG included).</summary>
    public static Bitmap? Decode(byte[] data)
    {
        try
        {
            if (IconFile.IsIco(data))
            {
                var png = IconFile.ExtractLargestPng(data);
                if (png is not null)
                {
                    return Decode(png);
                }

                using var icoStream = new MemoryStream(data);
                using var icon = new Icon(icoStream, MaxSize, MaxSize);
                return icon.ToBitmap();
            }

            using var stream = new MemoryStream(data);
            using var image = Image.FromStream(stream);
            return new Bitmap(image);
        }
        catch (Exception ex) when (ex is ArgumentException or ExternalException)
        {
            return null;
        }
    }

    /// <summary>Scales down to fit 256x256 and pads to a square with transparency.</summary>
    public static Bitmap Normalize(Bitmap source)
    {
        var side = Math.Min(MaxSize, Math.Max(source.Width, source.Height));
        var scale = Math.Min((double)side / source.Width, (double)side / source.Height);
        var width = Math.Max(1, (int)Math.Round(source.Width * scale));
        var height = Math.Max(1, (int)Math.Round(source.Height * scale));

        var result = new Bitmap(side, side, PixelFormat.Format32bppArgb);
        using var graphics = Graphics.FromImage(result);
        graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
        graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
        graphics.DrawImage(source, (side - width) / 2, (side - height) / 2, width, height);
        return result;
    }

    public static byte[] ToPng(Bitmap bitmap)
    {
        using var stream = new MemoryStream();
        bitmap.Save(stream, ImageFormat.Png);
        return stream.ToArray();
    }
}
