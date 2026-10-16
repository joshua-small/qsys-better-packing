namespace Designer.Modules.BetterPacking;

internal enum PackingFocus { Blocked, Schematic, Uci, OtherCanvas }
internal sealed record PackingCommandContext(PackingFocus Focus, object? Canvas = null);
