namespace Designer.Modules.BetterPacking;

internal sealed record PinPackingItem(PackingItem Bounds, IReadOnlyList<double> PinOffsetsY);

internal static class PinPackingLayout
{
    // Full drawn widths determine spacing. Pin centers determine only the row's vertical alignment.
    internal static PackedPosition[] Plan(IReadOnlyList<PinPackingItem> items, bool bottom, int gap)
    {
        var bounds = items.Select(i => i.Bounds).ToArray();
        var row = PackingLayout.Plan(bounds, PackAlignment.Top, gap);
        var offsets = new Dictionary<int, double>();
        foreach (var item in items)
        {
            if (item.PinOffsetsY.Count == 0 || item.PinOffsetsY.Any(y => !double.IsFinite(y) || y < 0 || y > item.Bounds.Height))
                throw new ArgumentException("Every selected item needs a rendered pin in the chosen pin group. Nothing moved.");
            offsets.Add(item.Bounds.Id, bottom ? item.PinOffsetsY.Max() : item.PinOffsetsY.Min());
        }
        var anchor = bottom ? bounds.Max(i => i.Y + offsets[i.Id]) : bounds.Min(i => i.Y + offsets[i.Id]);
        return row.Select(p =>
        {
            var y = anchor - offsets[p.Id];
            var rounded = Math.Round(y);
            if (Math.Abs(y - rounded) > 0.000001 || rounded < 0 || rounded >= int.MaxValue)
                throw new ArgumentException("Exact pin alignment needs an unsupported component position. Nothing moved.");
            return p with { Y = rounded };
        }).ToArray();
    }
}
