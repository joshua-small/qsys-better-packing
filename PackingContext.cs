namespace Designer.Modules.BetterPacking;

internal enum PackingFocus { Blocked, Schematic, OtherCanvas }
internal sealed record PackingCommandContext(PackingFocus Focus, object? Canvas = null);
