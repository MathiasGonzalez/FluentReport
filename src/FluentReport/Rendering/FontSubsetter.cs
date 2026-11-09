using System.Buffers.Binary;
using SkiaSharp;

namespace FluentReport.Rendering;

/// <summary>
/// Builds a copy of a TrueType font that keeps only the outlines of the glyphs a document uses.
/// SkiaSharp embeds fonts whole, which adds several hundred KB per face to every PDF. Glyph ids,
/// metrics and the character map are kept, so text measures, draws and extracts exactly as with
/// the full font; unused glyphs become empty. Fonts that are not plain TrueType (CFF outlines,
/// variable fonts) or that do not parse cleanly are left alone: the caller embeds them whole.
/// </summary>
internal static class FontSubsetter
{
    // Layout, kerning and device-metric tables are not used to draw positioned glyphs in a PDF, and a
    // digital signature no longer matches the rewritten font.
    private static readonly HashSet<uint> Dropped =
        [Tag("GPOS"), Tag("GSUB"), Tag("GDEF"), Tag("kern"), Tag("DSIG"), Tag("hdmx"), Tag("LTSH"), Tag("VDMX")];

    private static readonly uint[] Unsupported = [Tag("fvar"), Tag("gvar"), Tag("CFF "), Tag("CFF2")];

    private static readonly uint Glyf = Tag("glyf"), Loca = Tag("loca"), Head = Tag("head"), Maxp = Tag("maxp"), Post = Tag("post");

    private const int HeadLength = 54, PostHeaderLength = 32, GlyphHeaderLength = 10;

    /// <summary>The subset font file, or <c>null</c> when <paramref name="typeface"/> cannot be subset safely.</summary>
    public static byte[]? Subset(SKTypeface typeface, IReadOnlyCollection<ushort> glyphs)
    {
        try
        {
            return SubsetCore(typeface, glyphs);
        }
        catch (Exception e) when (e is InvalidDataException or ArgumentException or IndexOutOfRangeException or OverflowException)
        {
            return null;
        }
    }

    private static byte[]? SubsetCore(SKTypeface typeface, IReadOnlyCollection<ushort> glyphs)
    {
        var tags = typeface.GetTableTags();
        if (tags == null || Unsupported.Any(tags.Contains)) return null;
        if (!tags.Contains(Glyf) || !tags.Contains(Loca) || !tags.Contains(Head) || !tags.Contains(Maxp)) return null;

        var tables = new SortedDictionary<uint, byte[]>();
        foreach (uint tag in tags)
            if (!Dropped.Contains(tag))
                tables[tag] = typeface.GetTableData(tag);

        byte[] head = (byte[])tables[Head].Clone(), glyf = tables[Glyf], loca = tables[Loca];
        if (head.Length < HeadLength || tables[Maxp].Length < 6) return null;

        int glyphCount = BinaryPrimitives.ReadUInt16BigEndian(tables[Maxp].AsSpan(4));
        bool longOffsets = BinaryPrimitives.ReadInt16BigEndian(head.AsSpan(50)) != 0;
        var offsets = ReadOffsets(loca, glyphCount, longOffsets, glyf.Length);

        var keep = Closure(glyphs, glyf, offsets, glyphCount);
        var (newGlyf, newLoca) = Rebuild(glyf, offsets, keep, glyphCount);

        BinaryPrimitives.WriteInt16BigEndian(head.AsSpan(50), 1); // long loca offsets
        BinaryPrimitives.WriteUInt32BigEndian(head.AsSpan(8), 0); // checkSumAdjustment, filled in by Assemble
        tables[Head] = head;
        tables[Glyf] = newGlyf;
        tables[Loca] = newLoca;
        if (tables.TryGetValue(Post, out var post) && post.Length > PostHeaderLength)
            tables[Post] = WithoutGlyphNames(post);

        return Assemble(tables);
    }

    private static int[] ReadOffsets(byte[] loca, int glyphCount, bool longOffsets, int glyfLength)
    {
        if (loca.Length < (glyphCount + 1) * (longOffsets ? 4 : 2)) throw new InvalidDataException("loca is too short.");
        var offsets = new int[glyphCount + 1];
        for (int i = 0; i <= glyphCount; i++)
        {
            long offset = longOffsets
                ? BinaryPrimitives.ReadUInt32BigEndian(loca.AsSpan(i * 4))
                : BinaryPrimitives.ReadUInt16BigEndian(loca.AsSpan(i * 2)) * 2L;
            if (offset > glyfLength || (i > 0 && offset < offsets[i - 1])) throw new InvalidDataException("loca is not monotonic.");
            offsets[i] = (int)offset;
        }
        return offsets;
    }

    /// <summary>The requested glyphs, glyph 0 and every glyph that composite glyphs among them reference.</summary>
    private static bool[] Closure(IReadOnlyCollection<ushort> glyphs, byte[] glyf, int[] offsets, int glyphCount)
    {
        var keep = new bool[glyphCount];
        var pending = new Stack<int>(glyphs.Select(g => (int)g));
        pending.Push(0);
        while (pending.Count > 0)
        {
            int glyph = pending.Pop();
            if (glyph >= glyphCount || keep[glyph]) continue;
            keep[glyph] = true;

            int pos = offsets[glyph], end = offsets[glyph + 1];
            if (end - pos < GlyphHeaderLength || BinaryPrimitives.ReadInt16BigEndian(glyf.AsSpan(pos)) >= 0) continue;

            // Composite glyph: a list of component records after the header.
            pos += GlyphHeaderLength;
            ushort flags;
            do
            {
                if (pos + 4 > end) throw new InvalidDataException("Truncated composite glyph.");
                flags = BinaryPrimitives.ReadUInt16BigEndian(glyf.AsSpan(pos));
                pending.Push(BinaryPrimitives.ReadUInt16BigEndian(glyf.AsSpan(pos + 2)));
                pos += 4 + ((flags & 0x0001) != 0 ? 4 : 2);       // arguments are words or bytes
                if ((flags & 0x0008) != 0) pos += 2;              // one scale
                else if ((flags & 0x0040) != 0) pos += 4;         // x and y scale
                else if ((flags & 0x0080) != 0) pos += 8;         // 2x2 transform
            } while ((flags & 0x0020) != 0);                      // more components
        }
        return keep;
    }

    private static (byte[] Glyf, byte[] Loca) Rebuild(byte[] glyf, int[] offsets, bool[] keep, int glyphCount)
    {
        using var data = new MemoryStream();
        var loca = new byte[(glyphCount + 1) * 4];
        for (int i = 0; i < glyphCount; i++)
        {
            BinaryPrimitives.WriteUInt32BigEndian(loca.AsSpan(i * 4), (uint)data.Length);
            if (!keep[i]) continue;
            data.Write(glyf, offsets[i], offsets[i + 1] - offsets[i]);
            while (data.Length % 4 != 0) data.WriteByte(0);
        }
        BinaryPrimitives.WriteUInt32BigEndian(loca.AsSpan(glyphCount * 4), (uint)data.Length);
        return (data.ToArray(), loca);
    }

    /// <summary>The post table as version 3.0: same metrics, no per-glyph names.</summary>
    private static byte[] WithoutGlyphNames(byte[] post)
    {
        var header = post.AsSpan(0, PostHeaderLength).ToArray();
        BinaryPrimitives.WriteUInt32BigEndian(header, 0x00030000);
        return header;
    }

    private static byte[] Assemble(SortedDictionary<uint, byte[]> tables)
    {
        int count = tables.Count;
        int offset = 12 + count * 16;
        int total = offset + tables.Values.Sum(t => Padded(t.Length));
        var file = new byte[total];

        int entrySelector = (int)Math.Floor(Math.Log2(count));
        int searchRange = (1 << entrySelector) * 16;
        BinaryPrimitives.WriteUInt32BigEndian(file, 0x00010000);
        BinaryPrimitives.WriteUInt16BigEndian(file.AsSpan(4), (ushort)count);
        BinaryPrimitives.WriteUInt16BigEndian(file.AsSpan(6), (ushort)searchRange);
        BinaryPrimitives.WriteUInt16BigEndian(file.AsSpan(8), (ushort)entrySelector);
        BinaryPrimitives.WriteUInt16BigEndian(file.AsSpan(10), (ushort)(count * 16 - searchRange));

        int record = 12, headOffset = 0;
        foreach (var (tag, table) in tables) // sorted by tag, as the format requires
        {
            table.CopyTo(file, offset);
            if (tag == Head) headOffset = offset;
            BinaryPrimitives.WriteUInt32BigEndian(file.AsSpan(record), tag);
            BinaryPrimitives.WriteUInt32BigEndian(file.AsSpan(record + 4), Checksum(file.AsSpan(offset, Padded(table.Length))));
            BinaryPrimitives.WriteUInt32BigEndian(file.AsSpan(record + 8), (uint)offset);
            BinaryPrimitives.WriteUInt32BigEndian(file.AsSpan(record + 12), (uint)table.Length);
            record += 16;
            offset += Padded(table.Length);
        }

        BinaryPrimitives.WriteUInt32BigEndian(file.AsSpan(headOffset + 8), unchecked(0xB1B0AFBA - Checksum(file)));
        return file;
    }

    private static int Padded(int length) => (length + 3) & ~3;

    private static uint Checksum(ReadOnlySpan<byte> data)
    {
        uint sum = 0;
        for (int i = 0; i + 4 <= data.Length; i += 4)
            sum = unchecked(sum + BinaryPrimitives.ReadUInt32BigEndian(data[i..]));
        return sum;
    }

    private static uint Tag(string tag) => (uint)(tag[0] << 24 | tag[1] << 16 | tag[2] << 8 | tag[3]);
}
