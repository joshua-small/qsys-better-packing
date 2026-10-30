using System.Text.Json;
using System.Windows;
using Designer.Modules.BetterPacking;
using QSC.QSys;
using QSC.QSys.Design;
using QSC.QSys.Util;
using QsysMods.Core;
using Model = QSC.QSys.Design.Object;
using Graphic = QSC.QSys.Graphic.Object;

internal static class Program
{
    private static int passed, gap;
    private static Component controller = null!;
    private static List<Model> items = [];
    private static List<Graphic> graphics = [];
    private static string Spec(int secondCount = 3) => JsonSerializer.Serialize(new { ControlSpecs = new[]
    { new { ID = "a", Name = "Level", Count = 3 }, new { ID = "b", Name = "Mute", Count = secondCount } } });
    private static void Reset()
    {
        gap = 20; Host.Enabled = true; QSC.QSys.Commands.Align.Can = true;
        App.Manager = new(); Selection.Global = new(); ComponentCache.Components.Clear(); items = []; graphics = [];
        controller = new() { Specification = Spec() }; ComponentCache.Components.Add(controller.Id, controller);
        var page = new FixturePage(); App.Manager.Document.Pages.Add(page);
        var canvas = new QSC.QSys.Graphic.GraphicCanvas { Root = page };
        for (int row = 0; row < 2; row++) for (int column = 0; column < 4; column++)
        {
            string family = row == 0 ? "Level" : "Mute";
            Model model = column == 0 ? new QSC.QSys.Design.Graphics.TextBlock { Label = family }
                : new ControlStyle { Value = new() { Id = controller.Id, Name = family + " " + column } };
            model.Position = new(20 + column * 120, 30 + row * 70); model.Size = new(column == 0 ? 100 : row == 0 ? 80 : 40, 30);
            var graphic = new Graphic { obj = model, GraphicCanvas = canvas, Size = model.Size };
            items.Add(model); graphics.Add(graphic); App.Manager.Models.Add(model); Selection.Global.Add(graphic);
        }
    }
    private static TransposeSession Preview() => new(() => gap);
    private static string Receipt(string kind, object details) => kind;
    private static Point[] Positions() => items.Select(m => m.Position).ToArray();
    private static void Require(bool condition) { if (!condition) throw new Exception("Assertion failed"); }
    private static void Reject(Action action) { try { action(); } catch (Exception e) when (e is InvalidOperationException or ArgumentException or System.Reflection.TargetInvocationException or JsonException or KeyNotFoundException) { return; } throw new Exception("Expected rejection"); }
    private static void Check(string name, Action action) { Reset(); action(); Console.WriteLine("PASS " + name); passed++; }
    private static void NoMutation(Action action)
    { var before = Positions(); int starts = App.Manager.Starts; Reject(action); Require(before.SequenceEqual(Positions()) && App.Manager.Starts == starts); }
    private static void Stale(Action change)
    { using var preview = Preview(); change(); NoMutation(() => preview.Apply(Receipt)); Require(App.Manager.Subscribers == 0 && controller.SubscriberCount == 0); }

    [STAThread]
    private static int Main()
    {
        _ = new Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };
        Check("preview and cancellation never mutate or create undo", () =>
        { var before = Positions(); using (var p = Preview()) { Require(p.Plan.Moves.Count == 8 && before.SequenceEqual(Positions())); }
          Require(before.SequenceEqual(Positions()) && App.Manager.Starts == 0 && !App.Manager.Undoable && App.Manager.Subscribers == 0 && controller.SubscriberCount == 0); });
        Check("production apply helper makes one operation and preserves identities, bindings, style and size", () =>
        {
            var before = Positions(); var sizes = items.Select(m => m.Size).ToArray(); var values = items.OfType<ControlStyle>().Select(m => m.Value).ToArray();
            using var p = Preview(); p.Apply(Receipt); var after = Positions();
            Require(!before.SequenceEqual(after) && App.Manager.Starts == 1 && App.Manager.Ends == 1);
            Require(items[0].Position == new Point(20, 30) && items[4].Position == new Point(140, 30) && items[3].Position == new Point(20, 180));
            Require(items.Select(m => m.Size).SequenceEqual(sizes) && items.OfType<ControlStyle>().Select(m => m.Value).SequenceEqual(values)
                && items.All(m => m.Style == "original-style" && m.Orientation == 90));
            App.Manager.Undo(); Require(before.SequenceEqual(Positions()) && !App.Manager.Undoable);
            App.Manager.Redo(); Require(after.SequenceEqual(Positions()));
            NoMutation(() => p.Apply(Receipt));
        });
        Check("cancelled session cannot apply", () => { var p = Preview(); p.Dispose(); NoMutation(() => p.Apply(Receipt)); });
        Check("selection enumeration order may change without changing the grid", () =>
        { using var p = Preview(); Selection.Global.Reverse(); p.Apply(Receipt); Require(App.Manager.Starts == 1); });
        Check("stale position rejects", () => Stale(() => items[1].Position = new(141, 30)));
        Check("edit then restore still invalidates preview", () => Stale(() => { var old = items[1].Position; items[1].Position = new(141, 30); items[1].Position = old; }));
        Check("stale size rejects", () => Stale(() => items[1].Size = new(81, 30)));
        Check("stale style notification rejects", () => Stale(() => { items[1].Style = "new"; items[1].Change("Style"); }));
        Check("stale label rejects", () => Stale(() => items[0].Label = "Renamed"));
        Check("stale binding rejects", () => Stale(() => ((ControlStyle)items[1]).Value = new() { Id = controller.Id, Name = "Level 1" }));
        Check("binding edit then restore invalidates preview", () => Stale(() => { var c = ((ControlStyle)items[1]).Value; c.Name = "Other"; c.Name = "Level 1"; }));
        Check("changed controller specification rejects", () => Stale(() => controller.Specification = Spec(2)));
        Check("changed selection rejects", () => Stale(() => Selection.Global.RemoveAt(7)));
        Check("changed document rejects", () => Stale(() => App.Manager.Document = new()));
        Check("changed canvas rejects", () => Stale(() => graphics[1].GraphicCanvas = new() { Root = graphics[0].GraphicCanvas.Root }));
        Check("changed gap rejects", () => Stale(() => gap = 40));
        Check("drawn geometry change rejects", () => Stale(() => graphics[1].Size = new(81, 30)));
        Check("new lock rejects without ignoring the locked item", () => Stale(() => graphics[1].IsLocked = true));
        Check("different controller rejected", () =>
        { var c = new Component { Specification = Spec() }; ComponentCache.Components.Add(c.Id, c); ((ControlStyle)items[1]).Value.Id = c.Id; NoMutation(() => Preview()); });
        Check("different component class rejected", () => { controller.ClassName = "gain"; NoMutation(() => Preview()); });
        Check("unrelated and non-graphic selection rejected", () => { Selection.Global.Add(new object()); NoMutation(() => Preview()); });
        Check("duplicate drawn instance rejected", () => { Selection.Global.Add(graphics[0]); NoMutation(() => Preview()); });
        Check("missing label rejected", () => { Selection.Global.RemoveAt(0); NoMutation(() => Preview()); });
        Check("ambiguous label rejected", () => { items[4].Label = "Level"; NoMutation(() => Preview()); });
        Check("unequal controller counts rejected", () => { controller.Specification = Spec(2); NoMutation(() => Preview()); });
        Check("family names containing spaces and digits retain exact membership", () =>
        {
            controller.Specification = Spec().Replace("Level", "Zone 2 Level"); items[0].Label = "Zone 2 Level";
            for (int i = 1; i <= 3; i++) ((ControlStyle)items[i]).Value.Name = "Zone 2 Level " + i;
            using var p = Preview(); p.Apply(Receipt); Require(items[0].Label == "Zone 2 Level");
        });
        Check("unknown control index and duplicate binding reject", () =>
        { ((ControlStyle)items[1]).Value.Name = "Level 10"; NoMutation(() => Preview());
          ((ControlStyle)items[1]).Value.Name = "Level 2"; NoMutation(() => Preview()); });
        Check("malformed family metadata and duplicate declared names reject", () =>
        { controller.Specification = "{}"; NoMutation(() => Preview()); controller.Specification = Spec().Replace("Mute", "Level"); NoMutation(() => Preview()); });
        Check("declared family count has to be finite, positive and integral", () =>
        { foreach (double count in new[] { -1.0, 0, 2.5, 257 })
          { controller.Specification = JsonSerializer.Serialize(new { ControlSpecs = new[] { new { ID = "a", Name = "Level", Count = count } } }); NoMutation(() => Preview()); } });
        Check("control-definition subclasses are not moved", () =>
        { var original = (ControlStyle)items[1]; graphics[1].obj = new ControllerControlStyle { Value = original.Value, Size = original.Size, Position = original.Position }; NoMutation(() => Preview()); });
        Check("unselected controller families are allowed", () =>
        {
            controller.Specification = JsonSerializer.Serialize(new { ControlSpecs = new[]
            { new { ID = "a", Name = "Level", Count = 3 }, new { ID = "b", Name = "Mute", Count = 3 }, new { ID = "c", Name = "Other", Count = 1 } } });
            using var p = Preview(); p.Apply(Receipt); Require(p.Plan.Families.Count == 2);
        });
        Check("UCI and component-panel canvases rejected", () => { App.Manager.Document.Pages.Clear(); NoMutation(() => Preview()); });
        Check("hidden locked detached and offset items rejected", () =>
        {
            graphics[0].IsVisible = false; NoMutation(() => Preview()); graphics[0].IsVisible = true;
            items[0].IsLocked = true; NoMutation(() => Preview()); items[0].IsLocked = false;
            items[0].IsAttached = false; NoMutation(() => Preview()); items[0].IsAttached = true;
            graphics[0].BoundsOffset = new(1, 0); NoMutation(() => Preview()); graphics[0].BoundsOffset = new();
            graphics[0].IsCanvasLocked = true; NoMutation(() => Preview());
        });
        Check("unavailable host and edit command reject", () =>
        { Host.Enabled = false; NoMutation(() => Preview()); Host.Enabled = true; QSC.QSys.Commands.Align.Can = false; NoMutation(() => Preview()); });
        Check("receipt failure happens before mutation and closes session", () =>
        { using var p = Preview(); NoMutation(() => p.Apply((_, _) => throw new InvalidOperationException("receipt failed"))); Require(App.Manager.Subscribers == 0); NoMutation(() => p.Apply(Receipt)); });
        Check("busy operation prevents mutation", () => { using var p = Preview(); App.Manager.Busy = true; NoMutation(() => p.Apply(Receipt)); });
        Check("setter failure invokes Core cancellation and restores every position", () =>
        { using var p = Preview(); var before = Positions(); items[2].FailNextMove = true; Reject(() => p.Apply(Receipt));
          Require(before.SequenceEqual(Positions()) && App.Manager.Starts == 1 && App.Manager.Cancels == 1 && !App.Manager.Undoable); });
        Check("completion adjustment reports a committed change instead of silently claiming the preview matched", () =>
        {
            using var p = Preview(); var before = Positions(); App.Manager.AdjustOnEnd = true;
            try { p.Apply(Receipt); throw new Exception("Expected completion warning"); }
            catch (InvalidOperationException error) { Require(error.Message.Contains("completed the operation") && !error.Message.Contains("Nothing moved")); }
            Require(App.Manager.Starts == 1 && App.Manager.Undoable && !before.SequenceEqual(Positions()));
            App.Manager.Undo(); Require(before.SequenceEqual(Positions()));
        });
        Check("all subscriptions removed on close", () =>
        { var p = Preview(); p.Dispose(); Require(items.All(m => m.SubscriberCount == 0) && controller.SubscriberCount == 0 && items.OfType<ControlStyle>().All(c => c.Value.SubscriberCount == 0)); });
        Require(!AppDomain.CurrentDomain.GetAssemblies().Any(a => a.GetName().Name is "Q-Sys Designer" or "ModuleCommon" or "QSysComponents"));
        Console.WriteLine($"{passed} transpose command checks passed. Synthetic host only; no Designer acceptance claimed.");
        return 0;
    }
}
