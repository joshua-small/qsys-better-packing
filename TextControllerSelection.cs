using QsysMods.Core;
using System.Collections;
using System.ComponentModel;
using System.Globalization;
using System.Reflection;
using System.Text.Json;
using System.Windows;

namespace Designer.Modules.BetterPacking;

internal sealed record TransposeHostItem(object Model, object Graphic, object? Binding, object? ControllerId,
    string Name, string Label, Point Position, Size Body, Size Drawn);
internal sealed record TransposeSnapshot(object Document, object Canvas, object Root, object Controller,
    string Specification, TransposeHostItem[] Items, TransposePlan Plan);

internal static class TextControllerSelection
{
    private static object Property(object o, string name) => HostSelection.RequiredProperty(o, name);
    private static Type Type(string name) => HostSelection.HostType(name);
    private static InvalidOperationException Reject(string message) => new(message + " Nothing moved.");

    private static string Specification(object component)
    {
        var propertyName = (string)(Type("QSC.QSysComponents.DeviceControl").GetProperty("ControlPropertyName")?.GetValue(null)
            ?? throw Reject("Text Controller metadata is unavailable."));
        var getter = Type("QSC.QSys.Design.ComponentHelper").GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Single(m => m.Name == "GetComponentProperty" && m.IsGenericMethodDefinition && m.GetParameters().Length == 3);
        return (string)getter.MakeGenericMethod(typeof(string)).Invoke(null, [component, propertyName, ""])!;
    }

    internal static TransposeFamily[] ReadFamilies(string specification)
    {
        using var json = JsonDocument.Parse(specification);
        var specs = json.RootElement.GetProperty("ControlSpecs");
        if (specs.GetArrayLength() > 256) throw Reject("This controller has too many control families for transpose.");
        return specs.EnumerateArray().Where(s => !s.TryGetProperty("IsConnectionControl", out var connection) || !connection.GetBoolean())
            .Select(s =>
            {
                double count = s.TryGetProperty("Count", out var n) ? n.GetDouble() : 1;
                if (!double.IsFinite(count) || count < 1 || count > 256 || count != Math.Truncate(count))
                    throw Reject("A control family has an unsupported Count.");
                return new TransposeFamily(s.GetProperty("ID").GetString()!, s.GetProperty("Name").GetString()!, (int)count);
            }).ToArray();
    }

    internal static TransposeSnapshot Read(int gap)
    {
        Application.Current.Dispatcher.VerifyAccess();
        if (!Host.Enabled || Type("QSC.QSys.Commands.Align").GetProperty("Can")?.GetValue(null) is not true)
            throw Reject("Select a Text Controller grid in an editable schematic canvas.");
        var manager = Type("QSC.QSys.App").GetField("Manager")?.GetValue(null) ?? throw Reject("Designer is unavailable.");
        var document = Property(manager, "Document");
        var selection = ((IEnumerable)HostSelection.GlobalSelection).Cast<object>().Take(TransposeLayout.MaximumItems + 1).ToArray();
        var graphicType = Type("QSC.QSys.Graphic.Object");
        if (selection.Length == 0 || selection.Length > TransposeLayout.MaximumItems || selection.Any(g => !graphicType.IsInstanceOfType(g)))
            throw Reject("Select only the grid's placed controls and text labels, up to 256 items.");
        object? canvas = null, controller = null;
        var items = new List<TransposeHostItem>();
        foreach (var graphic in selection)
        {
            var model = Property(graphic, "obj");
            bool control = model.GetType() == Type("QSC.QSys.Design.ControlStyle");
            if (!control && model.GetType() != Type("QSC.QSys.Design.Graphics.TextBlock"))
                throw Reject("Transpose supports copied Text Controller controls and plain text labels only.");
            var current = Property(graphic, "GraphicCanvas");
            canvas ??= current;
            if (!ReferenceEquals(canvas, current) || !HostSelection.IsSchematicCanvas(current)
                || Property(graphic, "IsCanvasLocked") is not false || Property(graphic, "IsLocked") is not false
                || Property(graphic, "IsVisible") is not true || Property(model, "IsLocked") is not false || Property(model, "IsAttached") is not true)
                throw Reject("Every selected item must be visible and unlocked on the same schematic canvas.");
            var position = (Point)Property(model, "Position");
            var body = (Size)Property(model, "Size");
            var drawn = (Size)Property(graphic, "Size");
            var bounds = (Rect)Property(graphic, "rc");
            if ((Point)Property(graphic, "Position") != position || !PackingLayout.FootprintIsReady(body.Width, body.Height, drawn.Width, drawn.Height))
                throw Reject("The drawn grid geometry is not ready.");
            if (bounds.IsEmpty || bounds.Location != position || bounds.Size != drawn)
                throw Reject("This grid uses an unsupported drawn offset or coordinate space.");
            object? binding = null, id = null;
            string name = "", label = (string)Property(model, "Label");
            if (control)
            {
                binding = Property(model, "Value");
                id = Property(binding, "Id");
                name = (string)Property(binding, "Name");
                var owner = Type("QSC.QSys.Design.ComponentCache").GetMethods(BindingFlags.Public | BindingFlags.Static)
                    .Single(m => m.Name == "GetComponent" && m.GetParameters().Length == 1 && m.GetParameters()[0].ParameterType.IsInstanceOfType(id))
                    .Invoke(null, [id]) ?? throw Reject("A control's source component could not be found.");
                if ((string)Property(owner, "ClassName") != "device_controller_script" || Property(owner, "IsAttached") is not true)
                    throw Reject("All controls must belong to one attached Text Controller.");
                controller ??= owner;
                if (!ReferenceEquals(controller, owner)) throw Reject("Select controls from exactly one Text Controller.");
            }
            items.Add(new(model, graphic, binding, id, name, label, position, body, drawn));
        }
        if (controller is null || items.Select(i => i.Model).Distinct(ReferenceEqualityComparer.Instance).Count() != items.Count)
            throw Reject("The grid needs unique placed controls and labels.");
        var unlocked = HostSelection.SelectedModels();
        if (unlocked.Length != items.Count || items.Any(i => !unlocked.Contains(i.Model, ReferenceEqualityComparer.Instance)))
            throw Reject("Some selected items cannot be moved.");
        string specification = Specification(controller);
        var allFamilies = ReadFamilies(specification);
        if (allFamilies.Any(f => string.IsNullOrWhiteSpace(f.Id) || string.IsNullOrWhiteSpace(f.Name))
            || allFamilies.Select(f => f.Id).Distinct(StringComparer.Ordinal).Count() != allFamilies.Length
            || allFamilies.Select(f => f.Name).Distinct(StringComparer.Ordinal).Count() != allFamilies.Length)
            throw Reject("The controller has ambiguous family names or identities.");
        var cells = new List<TransposeItem>();
        for (int i = 0; i < items.Count; i++)
        {
            var item = items[i];
            var matches = allFamilies.SelectMany(f => Enumerable.Range(0, f.Count + 1).Select(index => (Family: f, Index: index)))
                .Where(p => item.Binding is null ? p.Index == 0 && item.Label == p.Family.Name
                    : p.Index > 0 && item.Name == (p.Family.Count == 1 ? p.Family.Name
                        : p.Family.Name + " " + p.Index.ToString(CultureInfo.InvariantCulture))).ToArray();
            if (matches.Length != 1) throw Reject("A selected label or control does not identify exactly one declared control family and index.");
            cells.Add(new(i, matches[0].Family.Id, matches[0].Index, item.Position.X, item.Position.Y, item.Drawn.Width, item.Drawn.Height));
        }
        var used = allFamilies.Where(f => cells.Any(c => c.FamilyId == f.Id)).ToArray();
        return new(document, canvas!, Property(canvas!, "Root"), controller, specification, items.ToArray(), TransposeLayout.Plan(used, cells, gap));
    }

    internal static bool Same(TransposeSnapshot before, TransposeSnapshot now) =>
        ReferenceEquals(before.Document, now.Document) && ReferenceEquals(before.Canvas, now.Canvas)
        && ReferenceEquals(before.Root, now.Root) && ReferenceEquals(before.Controller, now.Controller)
        && before.Specification == now.Specification && before.Plan.Gap == now.Plan.Gap
        && before.Items.Length == now.Items.Length && before.Items.All(a => now.Items.Any(b =>
            ReferenceEquals(a.Model, b.Model) && ReferenceEquals(a.Graphic, b.Graphic) && ReferenceEquals(a.Binding, b.Binding)
            && Equals(a.ControllerId, b.ControllerId) && a.Name == b.Name && a.Label == b.Label
            && a.Position == b.Position && a.Body == b.Body && a.Drawn == b.Drawn));
}

internal sealed class TransposeSession : IDisposable
{
    private readonly TransposeSnapshot snapshot;
    private readonly Func<int> gap;
    private readonly List<INotifyPropertyChanged> watched = [];
    private bool invalidated, finished;
    internal TransposePlan Plan => snapshot.Plan;
    internal TransposeSession(Func<int> gap)
    {
        this.gap = gap;
        snapshot = TextControllerSelection.Read(gap());
        try
        {
            foreach (var item in snapshot.Items.Select(i => i.Model).Concat(snapshot.Items.Where(i => i.Binding is not null).Select(i => i.Binding!))
                .Append(snapshot.Controller).Distinct(ReferenceEqualityComparer.Instance))
            {
                if (item is not INotifyPropertyChanged notify) throw new InvalidOperationException("The grid cannot be monitored for edits. Nothing moved.");
                notify.PropertyChanged += Changed;
                watched.Add(notify);
            }
        }
        catch { Dispose(); throw; }
    }
    private void Changed(object? sender, PropertyChangedEventArgs e) => invalidated = true;
    internal string Apply(Func<string, object, string> receipt)
    {
        Application.Current.Dispatcher.VerifyAccess();
        if (finished) throw new InvalidOperationException("This preview is closed. Create a new preview. Nothing moved.");
        // Consume every apply attempt, including stale or failed operations.
        Dispose();
        if (invalidated || !TextControllerSelection.Same(snapshot, TextControllerSelection.Read(gap())))
            throw new InvalidOperationException("The grid or gap changed after preview. Create a new preview. Nothing moved.");
        var result = HostSelection.ApplyPositions(Plan.Moves.Select(m => (snapshot.Items[m.Before.Id].Model, new Point(m.X, m.Y))).ToArray(),
            "transpose", new { families = Plan.Families.Select(f => f.Name).ToArray(), count = Plan.Count, gap = Plan.Gap },
            "Transposed the selected Text Controller grid. Undo restores its original positions.", receipt);
        if (Plan.Moves.Any(m => (Point)HostSelection.RequiredProperty(snapshot.Items[m.Before.Id].Model, "Position") != new Point(m.X, m.Y)))
            throw new InvalidOperationException("Designer completed the operation but adjusted a target position. Inspect the schematic and use Undo to restore the grid.");
        return result;
    }
    public void Dispose()
    {
        finished = true;
        foreach (var item in watched) item.PropertyChanged -= Changed;
        watched.Clear();
    }
}
