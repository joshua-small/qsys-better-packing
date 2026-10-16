namespace Designer.Modules.BetterPacking;

internal sealed record PinPackingAction(string Id, string Title, bool Bottom, bool Drawer);

internal static class PinPackingActions
{
    // Keep the two existing shortcut identities for the main-pin commands.
    // Drawer commands have independent keys, with no default assignments.
    internal static readonly PinPackingAction[] All =
    [
        new("better-packing/pins-top", "Pack to Top Main Pins", false, false),
        new("better-packing/pins-bottom", "Pack to Bottom Main Pins", true, false),
        new("better-packing/pins-drawer-top", "Pack to Top Drawer Pins", false, true),
        new("better-packing/pins-drawer-bottom", "Pack to Bottom Drawer Pins", true, true)
    ];
}
