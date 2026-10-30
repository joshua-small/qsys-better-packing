using System.Collections.ObjectModel;

namespace Designer.Modules.BetterPacking;

internal sealed record TransposeFamily(string Id, string Name, int Count);
internal sealed record TransposeItem(int Id, string FamilyId, int Index, double X, double Y, double Width, double Height)
{
    // Index zero is the family label; control indices start at one.
    internal double Right => X + Width;
    internal double Bottom => Y + Height;
}
internal sealed record TransposeMove(TransposeItem Before, double X, double Y);
internal sealed record TransposePlan(ReadOnlyCollection<TransposeFamily> Families,
    ReadOnlyCollection<TransposeMove> Moves, int Count, int Gap);

internal static class TransposeLayout
{
    internal const int MaximumItems = 256;
    private const double Tolerance = 1.000001;
    private static bool Coordinate(double v) => double.IsFinite(v) && v >= 0 && v < int.MaxValue && v == Math.Truncate(v);
    private static bool Size(double v) => double.IsFinite(v) && v > 0 && v < int.MaxValue;
    private static bool Aligned(IEnumerable<double> values)
    {
        var a = values.ToArray();
        return a.Max() - a.Min() <= Tolerance;
    }
    private static ArgumentException Invalid(string reason) => new(reason + " Nothing moved.");

    internal static TransposePlan Plan(IReadOnlyList<TransposeFamily> families, IReadOnlyList<TransposeItem> source, int gap)
    {
        if (gap < 0 || gap > 10000) throw Invalid("Gap must be between 0 and 10000 canvas units.");
        if (families.Count < 2 || source.Count > MaximumItems || source.Count == 0)
            throw Invalid("Select at least two complete control families, including their labels, and at most 256 items.");
        if (families.Any(f => string.IsNullOrWhiteSpace(f.Id) || string.IsNullOrWhiteSpace(f.Name) || f.Count < 2 || f.Count > MaximumItems)
            || families.Select(f => f.Id).Distinct(StringComparer.Ordinal).Count() != families.Count
            || families.Select(f => f.Name).Distinct(StringComparer.Ordinal).Count() != families.Count
            || families.Select(f => f.Count).Distinct().Count() != 1)
            throw Invalid("Each family must have a unique name and the same declared Count of at least two.");
        int count = families[0].Count;
        if ((long)families.Count * (count + 1) != source.Count || source.Select(i => i.Id).Distinct().Count() != source.Count)
            throw Invalid("Every family needs exactly one label and every control from index 1 through Count.");
        if (source.Any(i => !Coordinate(i.X) || !Coordinate(i.Y) || !Size(i.Width) || !Size(i.Height)
            || !double.IsFinite(i.Right) || !double.IsFinite(i.Bottom) || i.Right >= int.MaxValue || i.Bottom >= int.MaxValue))
            throw Invalid("The selection needs finite drawn sizes and nonnegative whole-unit positions in range.");

        var rows = families.Select(f => new { Family = f, Items = source.Where(i => i.FamilyId == f.Id).OrderBy(i => i.Index).ToArray() }).ToArray();
        foreach (var row in rows)
        {
            if (row.Items.Length != count + 1 || !row.Items.Select(i => i.Index).SequenceEqual(Enumerable.Range(0, count + 1)))
                throw Invalid("A family has a missing or duplicate label or control index.");
            if (!Aligned(row.Items.Select(i => i.Y)) && !Aligned(row.Items.Select(i => i.Y + i.Height / 2)))
                throw Invalid("Each family must occupy one aligned horizontal row.");
            for (int j = 1; j < row.Items.Length; j++)
                if (row.Items[j - 1].Right > row.Items[j].X + 0.000001)
                    throw Invalid("Place the label first, then non-overlapping controls in index order from left to right.");
        }
        rows = rows.OrderBy(r => r.Items.Min(i => i.Y)).ToArray();
        for (int r = 1; r < rows.Length; r++)
            if (rows[r - 1].Items.Max(i => i.Bottom) > rows[r].Items.Min(i => i.Y) + 0.000001)
                throw Invalid("Control family rows must not overlap.");
        for (int c = 0; c <= count; c++)
        {
            var cells = rows.Select(r => r.Items[c]).ToArray();
            if (!Aligned(cells.Select(i => i.X)) && !Aligned(cells.Select(i => i.X + i.Width / 2)))
                throw Invalid("Labels and equal control indices must line up in columns.");
        }
        double x = source.Min(i => i.X), anchorY = source.Min(i => i.Y);
        var heights = Enumerable.Range(0, count + 1).Select(c => rows.Max(r => r.Items[c].Height)).ToArray();
        var moves = new List<TransposeMove>();
        foreach (var row in rows)
        {
            double y = anchorY;
            for (int c = 0; c <= count; c++)
            {
                var item = row.Items[c];
                if (!Coordinate(x) || !Coordinate(y) || x + item.Width >= int.MaxValue || y + item.Height >= int.MaxValue)
                    throw Invalid("The transposed layout would exceed the supported coordinate range.");
                moves.Add(new(item, x, y));
                y += Math.Ceiling(heights[c]) + gap;
            }
            x += Math.Ceiling(row.Items.Max(i => i.Width)) + gap;
        }
        return new(Array.AsReadOnly(rows.Select(r => r.Family).ToArray()), moves.AsReadOnly(), count, gap);
    }
}
