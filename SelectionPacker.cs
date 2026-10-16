using QsysMods.Core;
using System.Collections;
using System.Runtime.CompilerServices;
using System.Windows;

namespace Designer.Modules.BetterPacking;

internal static class SelectionPacker
{
    private sealed record Item(object Model, Point Position, Size Body, Size Drawn, bool IsInternalConnector);

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
            result.Add(new Item(model, position, body, drawn,
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
