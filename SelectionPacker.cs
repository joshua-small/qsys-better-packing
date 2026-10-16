using QsysMods.Core;
using System.Collections;
using System.Runtime.CompilerServices;
using System.Windows;

namespace Designer.Modules.BetterPacking;

internal static class SelectionPacker
{
    private sealed record Item(object Model, object Graphic, Point Position, Size Body, Size Drawn, bool IsInternalConnector, Vector DrawnOffset);

    internal static PackingEditor EditorForCanvas(object canvas) => HostSelection.IsSchematicCanvas(canvas)
        ? PackingEditor.Schematic : HostSelection.RequiredProperty(canvas, "IsUciPage") is true ? PackingEditor.Uci : PackingEditor.Other;

    internal static ReplacementAvailability Availability(object canvas, PackingSettings settings)
    {
        try
        {
            var editor = EditorForCanvas(canvas);
            if (editor == PackingEditor.Other) return ReplacementAvailability.OutsideScope;
            var elements = HostSelection.SelectedModels().Select(m => PackingPolicy.Classify(m.GetType())).ToArray();
            if (!PackingPolicy.UseBetter(settings, editor, elements)) return ReplacementAvailability.OutsideScope;
            return CanPackOn(canvas) ? ReplacementAvailability.Ready : ReplacementAvailability.Blocked;
        }
        catch { return ReplacementAvailability.Blocked; }
    }

    private static Item[] ReadItems(object? expectedCanvas = null)
    {
        Application.Current.Dispatcher.VerifyAccess();
        // Preserve the host's edit/control-tool boundary and canvas-lock rules.
        if (HostSelection.HostType("QSC.QSys.Commands.Align").GetProperty("Can")?.GetValue(null) is not true)
            throw new InvalidOperationException("Select at least two movable items in an editing canvas. Nothing moved.");
        var models = HostSelection.SelectedModels();
        var objectType = HostSelection.HostType("QSC.QSys.Graphic.Object");
        var graphics = ((IEnumerable)HostSelection.GlobalSelection).Cast<object>()
            .Where(objectType.IsInstanceOfType).ToArray();
        var result = new List<Item>();
        object? canvas = null;
        foreach (var model in models)
        {
            if (PackingPolicy.Classify(model.GetType()) == PackingElement.Unknown)
                throw new InvalidOperationException($"This object uses native packing: {model.GetType().Name}. Use Tools > Pack for this selection. Nothing moved.");
            var matches = graphics.Where(g => ReferenceEquals(HostSelection.RequiredProperty(g, "obj"), model)).ToArray();
            if (matches.Length != 1)
                throw new InvalidOperationException("A selected item does not have one unambiguous drawn instance. Nothing moved.");
            var graphic = matches[0];
            var currentCanvas = HostSelection.RequiredProperty(graphic, "GraphicCanvas");
            if (HostSelection.RequiredProperty(graphic, "IsCanvasLocked") is not false
                || HostSelection.RequiredProperty(graphic, "IsLocked") is not false
                || HostSelection.RequiredProperty(graphic, "IsVisible") is not true
                || (expectedCanvas is not null && !ReferenceEquals(currentCanvas, expectedCanvas)))
                throw new InvalidOperationException("The selected items must be visible on the same unlocked canvas. Nothing moved.");
            if (canvas is null && EditorForCanvas(currentCanvas) == PackingEditor.Other)
                throw new InvalidOperationException("Select items on a schematic or UCI page. Component control panels use native packing. Nothing moved.");
            canvas ??= currentCanvas;
            if (!ReferenceEquals(canvas, currentCanvas))
                throw new InvalidOperationException("Select items on one canvas. Nothing moved.");
            var position = (Point)HostSelection.RequiredProperty(model, "Position");
            var graphicPosition = (Point)HostSelection.RequiredProperty(graphic, "Position");
            var body = (Size)HostSelection.RequiredProperty(model, "Size");
            var drawn = (Size)HostSelection.RequiredProperty(graphic, "Size");
            if (graphicPosition != position || !PackingLayout.FootprintIsReady(body.Width, body.Height, drawn.Width, drawn.Height))
                throw new InvalidOperationException("The drawn item geometry is not ready or uses a different coordinate space. Nothing moved.");
            var offset = new Vector();
            if (PackingPolicy.Classify(model.GetType()) == PackingElement.Shape)
            {
                var bounds = (Rect)HostSelection.RequiredProperty(graphic, "rc");
                if (bounds.IsEmpty || bounds.Size != drawn || !double.IsFinite(bounds.X) || !double.IsFinite(bounds.Y))
                    throw new InvalidOperationException("The polygon bounds are not ready. Nothing moved.");
                offset = bounds.Location - position;
            }
            result.Add(new Item(model, graphic, position, body, drawn,
                HostSelection.HostType("QSC.QSys.Design.ContainerPin").IsInstanceOfType(model), offset));
        }
        return result.ToArray();
    }

    private static object Describe(Item item) => new
    {
        type = item.Model.GetType().FullName,
        isInternalConnector = item.IsInternalConnector,
        sessionIdentity = RuntimeHelpers.GetHashCode(item.Model),
        position = new { x = item.Position.X, y = item.Position.Y },
        bodySize = new { width = item.Body.Width, height = item.Body.Height },
        drawnSize = new { width = item.Drawn.Width, height = item.Drawn.Height },
        drawnOffset = new { x = item.DrawnOffset.X, y = item.DrawnOffset.Y },
        extraDrawnHeight = item.Drawn.Height - item.Body.Height
    };

    internal static object ReadFootprints()
    {
        // Keep the receipt field name for existing consumers; type identifies Layout items.
        try { return new { components = ReadItems().Select(Describe).ToArray(), error = (string?)null }; }
        catch (Exception error) { return new { components = Array.Empty<object>(), error = error.Message }; }
    }

    internal static bool CanPackOn(object canvas)
    {
        try { return ReadItems(canvas).Length >= 2; }
        catch (Exception) { return false; }
    }

    private static PinPackingItem ReadPinItem(Item item, int id, bool exposed)
    {
        if (!HostSelection.IsSchematicCanvas(HostSelection.RequiredProperty(item.Graphic, "GraphicCanvas"))
            || !Host.Type("QSC.QSys.Graphic.PinProvider").IsInstanceOfType(item.Graphic))
            throw new ArgumentException("Pin packing supports components, connectors and Signal Snakes on a schematic. Nothing moved.");
        var offsets = new List<double>();
        foreach (var graphicPin in ((IEnumerable)HostSelection.RequiredProperty(item.Graphic, "pins")).Cast<object>())
        {
            var pin = HostSelection.RequiredProperty(graphicPin, "InnerPin");
            if (!item.IsInternalConnector && (bool)Host.Field(pin, "UserCreated") != exposed) continue;
            var center = (Point)HostSelection.RequiredProperty(graphicPin, "CanvasCenter");
            var local = (Point)HostSelection.RequiredProperty(pin, "Center");
            if (Math.Abs(center.X - item.Position.X - local.X) > 0.000001 || Math.Abs(center.Y - item.Position.Y - local.Y) > 0.000001)
                throw new InvalidOperationException("Pin coordinates are not ready in this schematic canvas. Nothing moved.");
            offsets.Add(center.Y - item.Position.Y);
        }
        return new(new(id, item.Position.X, item.Position.Y, item.Drawn.Width, item.Drawn.Height, item.IsInternalConnector), offsets);
    }

    internal static bool CanPackPinsOn(object canvas, bool exposed)
    {
        try
        {
            var items = ReadItems(canvas);
            return items.Length >= 2 && items.Select((item, id) => ReadPinItem(item, id, exposed)).All(i => i.PinOffsetsY.Count > 0);
        }
        catch { return false; }
    }

    internal static string PackPins(bool bottom, bool exposed, int gap, Func<string, object, string> receipt, object? canvas = null)
    {
        var items = ReadItems(canvas);
        var pins = items.Select((item, id) => ReadPinItem(item, id, exposed)).ToArray();
        if (pins.Any(p => p.PinOffsetsY.Count == 0))
            throw new ArgumentException("Every selected component needs visible " + (exposed ? "drawer" : "main") + " pins. Nothing moved.");
        var plan = PinPackingLayout.Plan(pins, bottom, gap);
        return HostSelection.ApplyPositions(plan.Select(p => (items[p.Id].Model, new Point(p.X, p.Y))).ToArray(), "pack-pins", new
        {
            bottom, pinGroup = exposed ? "exposed-control" : "main-component", gap,
            alignment = "actual rendered pin centers; no electrical connection changes",
            footprint = "full drawn bounds, including drawers", items = items.Select(Describe).ToArray(),
            pinOffsets = pins.Select(p => new { id = p.Bounds.Id, p.PinOffsetsY }).ToArray()
        }, "Packed selected items to " + (bottom ? "bottom" : "top") + " pins.", receipt);
    }

    internal static string Pack(PackAlignment alignment, int gap, Func<string, object, string> receipt,
        object? expectedCanvas = null, string invocation = "button")
    {
        var items = ReadItems(expectedCanvas);
        var plan = PackingLayout.Plan(items.Select((c, i) =>
            new PackingItem(i, c.Position.X, c.Position.Y, c.Drawn.Width, c.Drawn.Height, c.IsInternalConnector, c.DrawnOffset.X, c.DrawnOffset.Y)).ToArray(), alignment, gap);
        var targets = plan.Select(p => (items[p.Id].Model, new Point(p.X, p.Y))).ToArray();
        return HostSelection.ApplyPositions(targets, "pack", new
        {
            alignment = alignment.ToString(), gap, invocation,
            connectorGap = 0, gapRule = "Zero between adjacent internal connectors; configured gap for every other pair",
            footprint = "Native Graphic.Object.Size; polygon rc origin; component control-pin drawers included; Popup Button body only",
            components = items.Select(Describe).ToArray()
        }, $"Packed {items.Length} items {alignment.ToString().ToLowerInvariant()}. Gap: {gap}; zero between input/output connectors.", receipt);
    }
}
