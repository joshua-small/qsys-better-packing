using Designer.Modules.BetterPacking;

int passed = 0;
void Check(string name, Action test) { test(); Console.WriteLine("PASS " + name); passed++; }
void Equal(double expected, double actual) { if (Math.Abs(expected - actual) > 0.000001) throw new Exception($"Expected {expected}; got {actual}"); }
void Reject(Action test) { try { test(); } catch (ArgumentException) { return; } throw new Exception("Expected plan rejection"); }
PinPackingItem[] items = [new(new(0, 100, 100, 82, 114), new double[] { 6,16,26 }), new(new(1, 300, 100, 30, 10, true), new double[] { 5 })];
Check("Top aligns actual 6-unit component and 5-unit connector pins", () => {
    var p = PinPackingLayout.Plan(items, false, 20); Equal(p[0].Y+6,p[1].Y+5); Equal(202,p[1].X);
});
Check("Bottom uses last pin rather than open drawer bottom", () => {
    var p = PinPackingLayout.Plan(items, true, 20); Equal(126,p[0].Y+26); Equal(126,p[1].Y+5);
});
Check("Native 12-unit connectors also align", () => {
    var other=items[1] with { Bounds=items[1].Bounds with { Height=12 }, PinOffsetsY=new double[]{6} };
    var p=PinPackingLayout.Plan([items[0],other],false,20); Equal(p[0].Y+6,p[1].Y+6);
});
Check("Long-label body height does not introduce a fixed bottom offset", () => {
    var p=PinPackingLayout.Plan([items[0] with { Bounds=items[0].Bounds with { Height=300 } }, items[1]],true,20);
    Equal(p[0].Y+26,p[1].Y+5);
});
Check("Full fractional widths still reserve horizontal space", () => {
    var p=PinPackingLayout.Plan([items[0] with { Bounds=items[0].Bounds with { Width=128.85 } }, items[1]],false,10); Equal(239,p[1].X);
});
Check("Explicit drawer-pin offsets are supported", () => {
    var p=PinPackingLayout.Plan([items[0] with { PinOffsetsY=new double[]{30,40,50} },items[1]],true,20); Equal(p[0].Y+50,p[1].Y+5);
});
Check("Top and bottom are stable on repeat", () => {
    foreach(bool bottom in new[]{false,true}) {
        var first=PinPackingLayout.Plan(items,bottom,20);
        var moved=first.Select(p=>items[p.Id] with { Bounds=items[p.Id].Bounds with { X=p.X,Y=p.Y } }).ToArray();
        if(!first.SequenceEqual(PinPackingLayout.Plan(moved,bottom,20))) throw new Exception("Drift");
    }
});
Check("Missing, nonfinite and out-of-bounds pins are refused", () => {
    foreach(var ys in new[]{Array.Empty<double>(), new[]{double.NaN},new[]{-1.0},new[]{115.0}})
        Reject(()=>PinPackingLayout.Plan([items[0] with { PinOffsetsY=ys },items[1]],false,20));
});
Check("Cannot silently round away exact alignment", () => Reject(()=>PinPackingLayout.Plan([items[0] with { PinOffsetsY=new[]{6.25} },items[1]],false,20)));
Check("Invalid selection and gap retain existing refusal rules", () => {
    Reject(()=>PinPackingLayout.Plan([items[0]],false,20));
    Reject(()=>PinPackingLayout.Plan([items[0],items[0]],false,20));
    Reject(()=>PinPackingLayout.Plan(items,false,-1));
});
Check("Rendered pin coordinates must be finite in every coordinate space", () => {
    foreach (var invalid in new[] { double.NaN, double.PositiveInfinity, double.NegativeInfinity })
    {
        if (PinPackingLayout.PinCoordinateIsReady(invalid, 100, 6)
            || PinPackingLayout.PinCoordinateIsReady(106, invalid, 6)
            || PinPackingLayout.PinCoordinateIsReady(106, 100, invalid)
            || PinPackingLayout.PinCoordinateIsReady(invalid, 100, invalid))
            throw new Exception("Nonfinite pin geometry was accepted.");
    }
});
Check("Pin coordinate checks retain finite offsets and rounding tolerance", () => {
    foreach (var (position, offset) in new[] { (100.0, 6.0), (100.0, -6.0), (0.0, 0.0), (100.0, 6.25) })
    {
        var center = position + offset;
        foreach (var noise in new[] { -0.0000005, 0, 0.0000005 })
            if (!PinPackingLayout.PinCoordinateIsReady(center + noise, position, offset))
                throw new Exception("Valid rendered pin geometry was rejected.");
        foreach (var shift in new[] { -1.0, -0.000002, 0.000002, 1.0 })
            if (PinPackingLayout.PinCoordinateIsReady(center + shift, position, offset))
                throw new Exception("Stale rendered pin geometry was accepted.");
    }
});
Check("Finite coordinate arithmetic overflow is rejected", () => {
    if (PinPackingLayout.PinCoordinateIsReady(double.MaxValue, -double.MaxValue, 0))
        throw new Exception("Unrepresentable coordinate difference was accepted.");
});
Console.WriteLine($"{passed} pin packing checks passed.");
