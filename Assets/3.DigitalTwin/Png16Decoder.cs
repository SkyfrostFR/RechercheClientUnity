using System;
using System.IO;
using System.IO.Compression;

/// <summary>
/// Minimal decoder for 16-bit greyscale, non-interlaced PNG — the exact shape that
/// ROS compressed_depth_image_transport emits for a 16UC1 depth image.
///
/// Unity's ImageConversion.LoadImage cannot be used here: it downsamples 16-bit PNG to
/// 8 bits, which would collapse a 0..65535 mm range onto 256 levels and destroy the
/// depth. This decoder keeps the full ushort.
///
/// Deliberately narrow. It handles colour type 0 at bit depth 16 with no interlacing,
/// verified against a live frame from the robot, and refuses anything else rather than
/// silently producing wrong numbers.
///
/// No Unity API is touched, so this is safe to run on the rosbridge socket thread.
/// </summary>
public static class Png16Decoder
{
    private static readonly byte[] Signature = { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };

    /// <summary>
    /// Decode a 16-bit greyscale PNG into one ushort per pixel, row-major from the top.
    /// Returns null and sets <paramref name="error"/> if the data is not the expected shape.
    /// </summary>
    public static ushort[] Decode(byte[] png, int offset, out int width, out int height,
                                  out string error)
    {
        width = height = 0;
        error = null;

        if (png == null || png.Length - offset < 8 + 25)
        {
            error = "payload too short";
            return null;
        }
        for (int i = 0; i < 8; i++)
        {
            if (png[offset + i] != Signature[i]) { error = "not a PNG"; return null; }
        }

        int p = offset + 8;
        int w = 0, h = 0, bitDepth = 0, colorType = -1, interlace = 0;
        var idat = new MemoryStream();

        while (p + 8 <= png.Length)
        {
            int len = BE32(png, p);
            string type = "" + (char)png[p + 4] + (char)png[p + 5] +
                               (char)png[p + 6] + (char)png[p + 7];
            int dataStart = p + 8;
            if (dataStart + len > png.Length) { error = "truncated chunk " + type; return null; }

            if (type == "IHDR")
            {
                w = BE32(png, dataStart);
                h = BE32(png, dataStart + 4);
                bitDepth = png[dataStart + 8];
                colorType = png[dataStart + 9];
                interlace = png[dataStart + 12];
            }
            else if (type == "IDAT")
            {
                idat.Write(png, dataStart, len);
            }
            else if (type == "IEND") break;

            p = dataStart + len + 4;     // + CRC
        }

        if (bitDepth != 16 || colorType != 0 || interlace != 0)
        {
            error = $"unsupported PNG (bitDepth={bitDepth} colorType={colorType} " +
                    $"interlace={interlace}); expected 16/0/0";
            return null;
        }
        if (w <= 0 || h <= 0) { error = "bad IHDR size"; return null; }

        byte[] comp = idat.ToArray();
        if (comp.Length < 3) { error = "no IDAT"; return null; }

        // IDAT is zlib: 2-byte header, deflate stream, 4-byte Adler-32. DeflateStream
        // wants the bare deflate stream, so skip the header; the trailing checksum is
        // simply never read. (ZLibStream would do this for us but is .NET 6+, which the
        // .NET Standard 2.1 profile does not expose.)
        byte[] flat;
        try
        {
            using (var msIn = new MemoryStream(comp, 2, comp.Length - 2))
            using (var inflate = new DeflateStream(msIn, CompressionMode.Decompress))
            using (var msOut = new MemoryStream(h * (w * 2 + 1)))
            {
                inflate.CopyTo(msOut);
                flat = msOut.ToArray();
            }
        }
        catch (Exception e)
        {
            error = "inflate failed: " + e.Message;
            return null;
        }

        const int bpp = 2;                 // one 16-bit channel
        int stride = w * bpp;
        if (flat.Length < h * (stride + 1)) { error = "short scanline data"; return null; }

        var line = new byte[stride];
        var prev = new byte[stride];
        var outPix = new ushort[w * h];

        int src = 0;
        for (int y = 0; y < h; y++)
        {
            byte filter = flat[src++];
            Buffer.BlockCopy(flat, src, line, 0, stride);
            src += stride;

            switch (filter)
            {
                case 0: break;
                case 1:
                    for (int x = bpp; x < stride; x++)
                        line[x] = (byte)(line[x] + line[x - bpp]);
                    break;
                case 2:
                    for (int x = 0; x < stride; x++)
                        line[x] = (byte)(line[x] + prev[x]);
                    break;
                case 3:
                    for (int x = 0; x < stride; x++)
                    {
                        int a = x >= bpp ? line[x - bpp] : 0;
                        line[x] = (byte)(line[x] + ((a + prev[x]) >> 1));
                    }
                    break;
                case 4:
                    for (int x = 0; x < stride; x++)
                    {
                        int a = x >= bpp ? line[x - bpp] : 0;
                        int b = prev[x];
                        int c = x >= bpp ? prev[x - bpp] : 0;
                        int pp = a + b - c;
                        int pa = Math.Abs(pp - a), pb = Math.Abs(pp - b), pc = Math.Abs(pp - c);
                        int pr = (pa <= pb && pa <= pc) ? a : (pb <= pc ? b : c);
                        line[x] = (byte)(line[x] + pr);
                    }
                    break;
                default:
                    error = "unknown PNG filter " + filter;
                    return null;
            }

            // PNG samples are big-endian.
            int row = y * w;
            for (int x = 0; x < w; x++)
                outPix[row + x] = (ushort)((line[x * 2] << 8) | line[x * 2 + 1]);

            var swap = prev; prev = line; line = swap;
        }

        width = w;
        height = h;
        return outPix;
    }

    private static int BE32(byte[] b, int i)
    {
        return (b[i] << 24) | (b[i + 1] << 16) | (b[i + 2] << 8) | b[i + 3];
    }
}
