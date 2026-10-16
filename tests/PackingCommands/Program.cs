using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Designer.Modules.BetterPacking;
using QsysMods.Core;
using Model = QSC.QSys.Design.Object;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        _ = new Application();
        var root = new Grid();
        var canvas = new FixtureCanvas(); root.Children.Add(canvas);
        int native = 0, errors = 0, passed = 0;
        bool enabled = true, textFocus = false;
        var settings = new PackingSettings(37);
        var all = new PackingElementOptions(true, true, true, true, true, true, true, true);
        var commands = new Dictionary<PackAlignment, RoutedCommand>();
        var replacements = new List<CommandReplacement>();
        foreach (var alignment in Enum.GetValues<PackAlignment>())
        {
            var command = new RoutedCommand("Pack" + alignment, typeof(Program));
            commands.Add(alignment, command);
            root.CommandBindings.Add(new CommandBinding(command, (_, _) => native++, (_, e) => e.CanExecute = true));
            replacements.Add(new(new("packing/" + alignment, "better-packing", "Better Packing", "1", command.Name, command.Name,
                "Element preferences", Scope: ControlScope.Any), command,
                () => new(textFocus ? ReplacementAvailability.Blocked : SelectionPacker.Availability(canvas, settings), canvas),
                target => SelectionPacker.Pack(alignment, settings.Gap, (_, _) => "receipt", target)));
        }
        using var router = new ReplacementRouter(root, () => enabled ? replacements : [], _ => errors++);
        void Check(string name, Action action) { action(); Console.WriteLine("PASS " + name); passed++; }
        void Assert(bool value) { if (!value) throw new Exception("Packing integration contract failed"); }
        void Select(params Model[] models)
        {
            HostSelection.Graphics = models.Select((model, i) =>
            {
                model.Position = new(100 + i * 200, 100 + i * 200);
                return new QSC.QSys.Graphic.Object { obj = model, GraphicCanvas = canvas, Size = model.Size };
            }).ToArray();
        }
        void Invoke(PackAlignment alignment, bool better)
        {
            int n = native, o = HostSelection.Operations;
            Assert(commands[alignment].CanExecute(null, canvas));
            commands[alignment].Execute(null, canvas);
            Assert(native == n + (better ? 0 : 1) && HostSelection.Operations == o + (better ? 1 : 0));
        }
        void Blocked()
        {
            int n = native, o = HostSelection.Operations;
            Assert(!commands[PackAlignment.Left].CanExecute(null, canvas));
            commands[PackAlignment.Left].Execute(null, canvas);
            Assert(native == n && HostSelection.Operations == o);
        }
        foreach (var alignment in Enum.GetValues<PackAlignment>())
            Check(alignment + " routes components once with the saved gap", () =>
            {
                Select(new QSC.QSys.Design.Component(), new QSC.QSys.Design.DerivedComponent());
                var bindings = HostSelection.Graphics.Select(g => g.obj.Binding).ToArray();
                Invoke(alignment, true);
                var a = HostSelection.Graphics[0].obj; var b = HostSelection.Graphics[1].obj;
                Assert(PackingLayout.IsHorizontalRow(alignment) ? b.Position.X - a.Position.X - a.Size.Width == 37
                    : b.Position.Y - a.Position.Y - a.Size.Height == 37);
                Assert(bindings.SequenceEqual(HostSelection.Graphics.Select(g => g.obj.Binding)));
            });
        Check("Default graphics, copied controls and mixed selections fall through unchanged", () =>
        {
            foreach (var pair in new Model[][]
            {
                [new QSC.QSys.Design.Graphics.Header(), new QSC.QSys.Design.Graphics.TextBlock()],
                [new QSC.QSys.Design.ControlStyle(), new QSC.QSys.Design.ControllerControlStyle()],
                [new QSC.QSys.Design.Component(), new QSC.QSys.Design.Graphics.GroupBox()]
            })
            {
                Select(pair); var before = pair.Select(m => m.Position).ToArray();
                foreach (var alignment in commands.Keys) Invoke(alignment, false);
                Assert(before.SequenceEqual(pair.Select(m => m.Position)));
            }
        });
        var factories = new Func<Model>[]
        {
            () => new QSC.QSys.Design.ControlStyle(), () => new QSC.QSys.Design.ControllerControlStyle(),
            () => new QSC.QSys.Design.ControlSurface(), () => new QSC.QSys.Design.Graphics.Header(),
            () => new QSC.QSys.Design.Graphics.TextBlock(), () => new QSC.QSys.Design.Graphics.GroupBox(),
            () => new QSC.QSys.Design.GraphicImage(), () => new QSC.QSys.Design.Graphics.Region(),
            () => new QSC.QSys.Design.Graphics.Popup()
        };
        foreach (var editor in new[] { PackingEditor.Schematic, PackingEditor.Uci })
            Check(editor + " opt-ins pack every added category with the gap", () =>
            {
                canvas.Editor = editor; settings = new(37) { Schematic = all, Uci = all };
                foreach (var create in factories)
                {
                    Select(create(), create()); Invoke(PackAlignment.Left, true);
                    Assert(HostSelection.Graphics[1].obj.Position.Y == 167);
                }
            });
        Check("Polygon packing uses actual drawn bounds without changing vertices", () =>
        {
            canvas.Editor = PackingEditor.Schematic; settings = new(37) { Schematic = all };
            foreach (var alignment in commands.Keys)
            {
                Select(new QSC.QSys.Design.Graphics.Region(), new QSC.QSys.Design.Graphics.Region());
                HostSelection.Graphics[0].BoundsOffset = new Vector(12.25, -20.75);
                HostSelection.Graphics[1].BoundsOffset = new Vector(-10.5, 7.5);
                Invoke(alignment, true);
                var a = HostSelection.Graphics[0].rc; var b = HostSelection.Graphics[1].rc;
                double gap = PackingLayout.IsHorizontalRow(alignment) ? b.Left - a.Right : b.Top - a.Bottom;
                Assert(gap >= 37 && gap < 38);
                Assert(HostSelection.Graphics[0].BoundsOffset == new Vector(12.25, -20.75)
                    && HostSelection.Graphics[1].BoundsOffset == new Vector(-10.5, 7.5));
            }
        });
        Check("Schematic and UCI preferences remain independent", () =>
        {
            settings = new(37) { Schematic = new(Controls: true) };
            Select(new QSC.QSys.Design.ControlStyle(), new QSC.QSys.Design.ControlStyle());
            canvas.Editor = PackingEditor.Uci; Invoke(PackAlignment.Left, false);
            canvas.Editor = PackingEditor.Schematic; Invoke(PackAlignment.Left, true);
        });
        Check("Mixed opt-ins require every category and re-read preferences at execution", () =>
        {
            settings = new(37) { Schematic = all };
            Select(new QSC.QSys.Design.ControlStyle(), new QSC.QSys.Design.Graphics.Header());
            Assert(commands[PackAlignment.Left].CanExecute(null, canvas));
            settings = settings with { Schematic = all with { Headers = false } }; Invoke(PackAlignment.Left, false);
            settings = settings with { Schematic = all }; Invoke(PackAlignment.Left, true);
        });
        Check("Unknown types, groups and Flexboxes remain native even with every option enabled", () =>
        {
            foreach (var unknown in new Model[] { new(), new QSC.QSys.Design.Group(), new QSC.QSys.Design.Graphics.Flexbox() })
            { Select(new QSC.QSys.Design.Component(), unknown); Invoke(PackAlignment.Left, false); }
        });
        Check("Control panels stay native even when UCI categories are enabled", () =>
        {
            settings = new(37) { Schematic = all, Uci = all }; canvas.Editor = PackingEditor.Other;
            Select(new QSC.QSys.Design.ControlStyle(), new QSC.QSys.Design.ControlStyle()); Invoke(PackAlignment.Left, false);
            canvas.Editor = PackingEditor.Schematic;
        });
        Check("Unsafe geometry, locks, hidden/shared items and canvas mismatch cannot fall through", () =>
        {
            var corruptions = new Action<QSC.QSys.Graphic.Object>[]
            {
                g => g.IsCanvasLocked = true, g => g.IsLocked = true, g => g.IsVisible = false,
                g => g.Size = new Size(0, 30), g => g.Offset = new Vector(3, 0), g => g.GraphicCanvas = new FixtureCanvas()
            };
            foreach (var corrupt in corruptions)
            { Select(new QSC.QSys.Design.ControlStyle(), new QSC.QSys.Design.ControlStyle()); corrupt(HostSelection.Graphics[0]); Blocked(); }
        });
        Check("Host alignment rules block control-tool mode and stale eligibility", () =>
        {
            Select(new QSC.QSys.Design.ControlStyle(), new QSC.QSys.Design.ControlStyle());
            Assert(commands[PackAlignment.Left].CanExecute(null, canvas));
            QSC.QSys.Commands.Align.Can = false; Blocked(); QSC.QSys.Commands.Align.Can = true;
        });
        Check("Text focus never uses the stale selection", () =>
        { textFocus = true; Blocked(); textFocus = false; });
        Check("Blocked text focus continues input routing and preserves shortcuts", () =>
        {
            var command = commands[PackAlignment.Left];
            command.InputGestures.Add(new KeyGesture(Key.L, ModifierKeys.Control | ModifierKeys.Shift));
            CanExecuteRoutedEventArgs? observed = null;
            var observe = new CanExecuteRoutedEventHandler((_, args) => observed = args);
            canvas.AddHandler(CommandManager.PreviewCanExecuteEvent, observe, handledEventsToo: true);
            textFocus = true; Assert(!command.CanExecute(null, canvas)); textFocus = false;
            canvas.RemoveHandler(CommandManager.PreviewCanExecuteEvent, observe);
            Assert(observed is { Handled: true, CanExecute: false, ContinueRouting: true } && command.InputGestures.Count == 1);
        });
        Check("Unrelated commands retain their native route", () =>
        {
            var command = new RoutedCommand("Unrelated", typeof(Program));
            root.CommandBindings.Add(new CommandBinding(command, (_, _) => native++));
            int n = native, o = HostSelection.Operations; command.Execute(null, canvas);
            Assert(native == n + 1 && HostSelection.Operations == o);
        });
        Check("Operation failure cannot invoke native packing afterwards", () =>
        {
            int n = native, o = HostSelection.Operations, e = errors;
            HostSelection.FailOperation = true; commands[PackAlignment.Left].Execute(null, canvas);
            Assert(native == n && HostSelection.Operations == o && errors == e + 1); HostSelection.FailOperation = false;
        });
        Check("The existing master opt-out restores every native command", () =>
        { enabled = false; foreach (var alignment in commands.Keys) Invoke(alignment, false); enabled = true; });
        Check("Preferences matrix edits a draft and preserves independent columns", () =>
        {
            var saved = new PackingSettings(); var ui = new PackingOptionsEditor(saved);
            Assert(ui.Read(PackingEditor.Schematic) == saved.Schematic && ui.Read(PackingEditor.Uci) == saved.Uci);
            var control = ui.View.Children.OfType<ComboBox>().Single(c => Equals(c.ToolTip, "Uci · Controls and indicators"));
            control.SelectedIndex = 1;
            Assert(ui.Read(PackingEditor.Uci).Controls && !saved.Uci.Controls && ui.Read(PackingEditor.Schematic) == saved.Schematic);
            var cancelled = new PackingOptionsEditor(saved); Assert(!cancelled.Read(PackingEditor.Uci).Controls);
        });
        Check("Disposal restores native routing", () => { router.Dispose(); Invoke(PackAlignment.Left, false); });
        Console.WriteLine($"{passed} WPF packing integration checks passed (fixture host; no Designer acceptance).");
    }
}
