using QsysMods.Core;
using System.Collections;
using System.Runtime.CompilerServices;
using System.Windows;

namespace Designer.Modules.BetterPacking;

internal static class SelectionPacker
{
    private sealed record Item(object Model, object Graphic, Point Position, Size Body, Size Drawn, bool IsInternalConnector);

    // Normal 10.5 Schematic Elements > Layout entries, plus ordinary components.
    // Generic Graphic.Object also includes UCI controls and unreviewed object types.
    private static readonly IReadOnlyDictionary<string, string> GraphicTypes = new Dictionary<string, string>
    {
        ["QSC.QSys.Design.Component"] = "QSC.QSys.Graphic.Component",
        ["QSC.QSys.Design.Container"] = "QSC.QSys.Graphic.Container",
        ["QSC.QSys.Design.ChannelGroup"] = "QSC.QSys.Graphic.ChannelGroup",
        ["QSC.QSys.Design.ContainerPin"] = "QSC.QSys.Graphic.ContainerPin",
        ["QSC.QSys.Design.Graphics.Popup"] = "QSC.QSys.Graphic.Popup.PopupButton",
        ["QSC.QSys.Design.Mux.Muxer"] = "QSC.QSys.Graphic.Mux.Muxer",
        ["QSC.QSys.Design.Mux.Demuxer"] = "QSC.QSys.Graphic.Mux.Muxer"
    };

    private static string? GraphicTypeName(Type type)
    {
        // Host component locations can derive from Component; preserve that support.
        for (Type? current = type; current is not null; current = current.BaseType)
            if (GraphicTypes.TryGetValue(current.FullName ?? "", out var name)) return name;
        return null;
    }

    private static Item[] ReadItems(object? expectedCanvas = null)
    {
        Application.Current.Dispatcher.VerifyAccess();
        var models = HostSelection.SelectedModels();
        var objectType = HostSelection.HostType("QSC.QSys.Graphic.Object");
        var graphics = ((IEnumerable)HostSelection.GlobalSelection).Cast<object>()
            .Where(objectType.IsInstanceOfType).ToArray();
        var result = new List<Item>();
        object? canvas = null;
        foreach (var model in models)
        {
            var graphicName = GraphicTypeName(model.GetType());
            if (graphicName is null)
                throw new InvalidOperationException($"Packing supports schematic components, Layout items and internal input/output connectors. Unsupported selection: {model.GetType().FullName}. Nothing moved.");
            var graphicType = HostSelection.HostType(graphicName);
            var matches = graphics.Where(g => graphicType.IsInstanceOfType(g)
                && ReferenceEquals(HostSelection.RequiredProperty(g, "obj"), model)).ToArray();
            if (matches.Length != 1)
                throw new InvalidOperationException("A selected item does not have one unambiguous drawn instance. Nothing moved.");
            var graphic = matches[0];
            var currentCanvas = HostSelection.RequiredProperty(graphic, "GraphicCanvas");
            if (HostSelection.RequiredProperty(graphic, "IsCanvasLocked") is not false
                || (expectedCanvas is not null && !ReferenceEquals(currentCanvas, expectedCanvas)))
                throw new InvalidOperationException("The selected items must be on the same unlocked schematic canvas. Nothing moved.");
            if (canvas is null && !HostSelection.IsSchematicCanvas(currentCanvas))
                throw new InvalidOperationException("Select items on a schematic page or inside a Container or Channel Group schematic. Use native packing for UCI and control panels. Nothing moved.");
            canvas ??= currentCanvas;
            if (!ReferenceEquals(canvas, currentCanvas))
                throw new InvalidOperationException("Select items on one schematic canvas. Nothing moved.");
            var position = (Point)HostSelection.RequiredProperty(model, "Position");
            var graphicPosition = (Point)HostSelection.RequiredProperty(graphic, "Position");
            var body = (Size)HostSelection.RequiredProperty(model, "Size");
            var drawn = (Size)HostSelection.RequiredProperty(graphic, "Size");
            if (graphicPosition != position || !PackingLayout.FootprintIsReady(body.Width, body.Height, drawn.Width, drawn.Height))
                throw new InvalidOperationException("The drawn item geometry is not ready or uses a different coordinate space. Nothing moved.");
            result.Add(new Item(model, graphic, position, body, drawn,
                HostSelection.HostType("QSC.QSys.Design.ContainerPin").IsInstanceOfType(model)));
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
        if (!Host.Type("QSC.QSys.Graphic.PinProvider").IsInstanceOfType(item.Graphic))
            throw new ArgumentException("Pin packing supports components, connectors and Signal Snakes. Nothing moved.");
        var offsets = new List<double>();
        foreach (var graphicPin in ((IEnumerable)HostSelection.RequiredProperty(item.Graphic, "pins")).Cast<object>())
        {
            var pin = HostSelection.RequiredProperty(graphicPin, "InnerPin");
            if (!item.IsInternalConnector && (bool)Host.Field(pin, "UserCreated") != exposed) continue;
            var center = (Point)HostSelection.RequiredProperty(graphicPin, "CanvasCenter");
            var local = (Point)HostSelection.RequiredProperty(pin, "Center");
            // CanvasCenter falls back to local coordinates before the native visual is ready.
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
            new PackingItem(i, c.Position.X, c.Position.Y, c.Drawn.Width, c.Drawn.Height, c.IsInternalConnector)).ToArray(), alignment, gap);
        var targets = plan.Select(p => (items[p.Id].Model, new Point(p.X, p.Y))).ToArray();
        return HostSelection.ApplyPositions(targets, "pack", new
        {
            alignment = alignment.ToString(), gap, invocation,
            connectorGap = 0, gapRule = "Zero between adjacent internal connectors; configured gap for every other pair",
            footprint = "Native Graphic.Object.Size; component control-pin drawers included; Popup Button body only",
            components = items.Select(Describe).ToArray()
        }, $"Packed {items.Length} items {alignment.ToString().ToLowerInvariant()}. Gap: {gap}; zero between input/output connectors.", receipt);
    }
}
