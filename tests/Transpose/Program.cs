using Designer.Modules.BetterPacking;

int passed = 0;
void Check(string name, Action test) { test(); Console.WriteLine("PASS " + name); passed++; }
void Require(bool value) { if (!value) throw new Exception("Assertion failed"); }
void Reject(Action test) { try { test(); } catch (ArgumentException) { return; } throw new Exception("Expected rejection"); }
TransposeFamily[] families = [new("a", "Level", 3), new("b", "Mute", 3)];
TransposeItem[] Items() => Enumerable.Range(0, 2).SelectMany(r => Enumerable.Range(0, 4).Select(c =>
    new TransposeItem(r * 4 + c, r == 0 ? "a" : "b", c, 20 + c * 120, 30 + r * 60,
        c == 0 ? 100 : r == 0 ? 75.5 : 25, r == 0 ? 32.5 : 20))).ToArray();
TransposePlan Plan(TransposeItem[] items, int gap = 20) => TransposeLayout.Plan(families, items, gap);
Check("family rows become columns; indices become rows", () =>
{
    var p = Plan(Items());
    Require(p.Moves.Count == 8 && p.Count == 3 && p.Families[0].Name == "Level");
    foreach (var move in p.Moves)
    { Require(move.X == (move.Before.FamilyId == "a" ? 20 : 140)); Require(move.Y == 30 + move.Before.Index * 53); }
});
Check("source order does not change mapping", () => Require(Plan(Items().Reverse().ToArray()).Moves.SequenceEqual(Plan(Items()).Moves)));
Check("immutable positions-only plan preserves input records", () =>
{
    var source = Items(); var copy = source.ToArray(); var p = Plan(source);
    Require(source.SequenceEqual(copy) && p.Moves.All(m => ReferenceEquals(m.Before, source[m.Before.Id])));
});
Check("dimension-aware rows and columns have configured clearance", () =>
{
    var p = Plan(Items());
    foreach (var a in p.Moves) foreach (var b in p.Moves.Where(b => b.Before.Id != a.Before.Id))
        Require(a.X + a.Before.Width + 20 <= b.X || b.X + b.Before.Width + 20 <= a.X
            || a.Y + a.Before.Height + 20 <= b.Y || b.Y + b.Before.Height + 20 <= a.Y);
});
Check("zero and maximum gap are supported", () => { Plan(Items(), 0); Plan(Items(), 10000); });
Check("gap outside range rejected", () => { Reject(() => Plan(Items(), -1)); Reject(() => Plan(Items(), 10001)); });
Check("missing label rejected", () => Reject(() => Plan(Items()[1..])));
Check("duplicate index rejected", () => { var a = Items(); a[2] = a[2] with { Index = 1 }; Reject(() => Plan(a)); });
Check("duplicate object identity rejected", () => { var a = Items(); a[2] = a[2] with { Id = 1 }; Reject(() => Plan(a)); });
Check("mixed family rejected", () => { var a = Items(); a[2] = a[2] with { FamilyId = "unknown" }; Reject(() => Plan(a)); });
Check("unequal declared counts rejected", () => Reject(() => TransposeLayout.Plan([families[0], families[1] with { Count = 2 }], Items(), 20)));
Check("duplicate family names rejected", () => Reject(() => TransposeLayout.Plan([families[0], families[1] with { Name = "Level" }], Items(), 20)));
Check("single family and count-one grids rejected", () =>
{ Reject(() => TransposeLayout.Plan([families[0]], Items()[..4], 20)); Reject(() => TransposeLayout.Plan(families.Select(f => f with { Count = 1 }).ToArray(), Items(), 20)); });
Check("out-of-order indices rejected", () => { var a = Items(); (a[1], a[2]) = (a[1] with { X = a[2].X }, a[2] with { X = a[1].X }); Reject(() => Plan(a)); });
Check("misaligned rows and columns rejected", () =>
{ var a = Items(); a[1] = a[1] with { Y = 35 }; Reject(() => Plan(a)); a = Items(); a[6] = a[6] with { X = 277 }; Reject(() => Plan(a)); });
Check("overlapping row objects rejected", () => { var a = Items(); a[0] = a[0] with { Width = 121 }; Reject(() => Plan(a)); });
Check("overlapping family rows rejected", () => { var a = Items().Select(i => i.FamilyId == "b" ? i with { Y = 50 } : i).ToArray(); Reject(() => Plan(a)); });
Check("center-aligned heterogeneous rows accepted", () =>
{ var a = Items().Select(i => i with { Y = i.Y + (i.Index == 0 ? 10 : 0), Height = i.Index == 0 ? i.Height - 20 : i.Height }).ToArray();
  // Keep all label heights positive while placing their centers on the row center.
  a[4] = a[4] with { Y = 95, Height = 10 }; Plan(a); });
Check("nonfinite and fractional positions and invalid sizes rejected", () =>
{
    foreach (double value in new[] { double.NaN, double.PositiveInfinity, double.NegativeInfinity, -1, 0.5, (double)int.MaxValue })
    { var a = Items(); a[0] = a[0] with { X = value }; Reject(() => Plan(a)); a = Items(); a[0] = a[0] with { Y = value }; Reject(() => Plan(a)); }
    foreach (double value in new[] { double.NaN, double.PositiveInfinity, -1, 0 })
    { var a = Items(); a[0] = a[0] with { Width = value }; Reject(() => Plan(a)); a = Items(); a[0] = a[0] with { Height = value }; Reject(() => Plan(a)); }
});
Check("output coordinate overflow rejected before apply", () =>
{ var a = Items().Select(i => i with { Y = i.Y + int.MaxValue - 200 }).ToArray(); Reject(() => Plan(a, 10000)); });
Check("selection limit rejected", () => Reject(() => Plan(Enumerable.Range(0, 257).Select(i => Items()[0] with { Id = i }).ToArray())));
Console.WriteLine($"{passed} transpose layout checks passed.");
