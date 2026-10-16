namespace Designer.Modules.BetterPacking;

internal enum PackingEditor { Schematic, Uci, Other }
internal enum PackingElement { Unknown, Component, Layout, Control, Header, TextBlock, GroupBox, Image, Shape }

// Optional properties preserve the saved gap and old master opt-out on upgrade.
public sealed record PackingSettings(int Gap = 20, bool ReplaceDefaultPackBehavior = true, bool ControlsManagedByCore = false)
{
    // Retain the legacy stored value when saving other preferences. Explicit
    // Main/Drawer commands now choose the pin group independently of this value.
    public bool PackToExposedControlPins { get; init; }
    public PackingElementOptions Schematic { get; init; } = new(Components: true, Layout: true);
    public PackingElementOptions Uci { get; init; } = new();

    public void Validate()
    {
        if (Gap is < 0 or > 10000) throw new ArgumentException("Gap must be between 0 and 10000.");
        if (Schematic is null || Uci is null) throw new ArgumentException("Packing choices must be specified for both editors.");
    }
}

public sealed record PackingElementOptions(bool Components = false, bool Layout = false, bool Controls = false,
    bool Headers = false, bool TextBlocks = false, bool GroupBoxes = false, bool Images = false, bool Shapes = false)
{
    internal bool Enabled(PackingElement element) => element switch
    {
        PackingElement.Component => Components, PackingElement.Layout => Layout, PackingElement.Control => Controls,
        PackingElement.Header => Headers, PackingElement.TextBlock => TextBlocks, PackingElement.GroupBox => GroupBoxes,
        PackingElement.Image => Images, PackingElement.Shape => Shapes, _ => false
    };
}

internal static class PackingPolicy
{
    internal static bool LegacyOverridesEnabled(PackingSettings old, IReadOnlyDictionary<string, bool> decisions)
    {
        // The old group had only four directions. Adding center commands must not
        // make its saved all-off choice look like a partial selection during migration.
        var original = new[] { "left", "right", "top", "bottom" }.Select(d => "better-packing/replace-" + d);
        if (original.All(id => decisions.TryGetValue(id, out var accepted) && !accepted)) return false;
        return decisions.Keys.Any(id => id.StartsWith("better-packing/replace-", StringComparison.Ordinal))
            || old.ControlsManagedByCore || old.ReplaceDefaultPackBehavior;
    }

    internal static readonly (PackingElement Element, string Name)[] Choices =
    [
        (PackingElement.Component, "Components"), (PackingElement.Layout, "Layout items and connectors"),
        (PackingElement.Control, "Controls and indicators"), (PackingElement.Header, "Headers"),
        (PackingElement.TextBlock, "Text blocks"), (PackingElement.GroupBox, "Group boxes"),
        (PackingElement.Image, "Images"), (PackingElement.Shape, "Polygons")
    ];

    // One provider for the entire selection. Unknown objects always retain native packing.
    // This decision precedes geometry validation and any mutation; it is not error recovery.
    internal static bool UseBetter(PackingSettings settings, PackingEditor editor, IReadOnlyList<PackingElement> elements)
    {
        var options = editor == PackingEditor.Schematic ? settings.Schematic : editor == PackingEditor.Uci ? settings.Uci : null;
        return options is not null && elements.Count > 0 && elements.All(options.Enabled);
    }

    // Reviewed Designer 10.5 model families. Never opt in the generic Design.Object base:
    // groups, Flexboxes and future host objects retain their native handling.
    internal static PackingElement Classify(Type type)
    {
        for (Type? current = type; current is not null; current = current.BaseType)
        {
            var kind = current.FullName switch
            {
                "QSC.QSys.Design.Component" => PackingElement.Component,
                "QSC.QSys.Design.Container" or "QSC.QSys.Design.ChannelGroup" or "QSC.QSys.Design.ContainerPin"
                    or "QSC.QSys.Design.Graphics.Popup" or "QSC.QSys.Design.Mux.Muxer" or "QSC.QSys.Design.Mux.Demuxer" => PackingElement.Layout,
                "QSC.QSys.Design.ControlStyle" or "QSC.QSys.Design.ControlSurface" => PackingElement.Control,
                "QSC.QSys.Design.Graphics.Header" => PackingElement.Header,
                "QSC.QSys.Design.Graphics.TextBlock" => PackingElement.TextBlock,
                "QSC.QSys.Design.Graphics.GroupBox" => PackingElement.GroupBox,
                "QSC.QSys.Design.GraphicImage" => PackingElement.Image,
                "QSC.QSys.Design.Graphics.Region" => PackingElement.Shape,
                _ => PackingElement.Unknown
            };
            if (kind != PackingElement.Unknown) return kind;
        }
        return PackingElement.Unknown;
    }
}
