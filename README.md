# Better Packing

Improved schematic packing, extracted from Schematic Tools 0.16. It accounts for exposed control-pin footprints, mixed components and Layout items, connector adjacency, and host rounding. The six boundary operations and two new pin-aligned operations are available from **Tools > Q-SYS Mods > Better Packing** in this unreleased source candidate.

**File > Preferences > Better Packing** stores Gap (default **20**, whole numbers 0–10000) and one **Disable default Q-SYS control overrides** checkbox. It is unchecked by default: Pack Left, Right, Top, and Bottom all use Better Packing on the schematic. Checking it restores all four native actions. Existing shortcuts, Tools > Pack, and right-click Pack use the same action exactly once; toggling the setting never resets user keys. UCI/control-panel packing stays native; invalid selection and text focus remain blocked.

Each directional Pack action has one shortcut entry, using the actual native defaults or saved user keys. Core selects Better Packing or Q-SYS as its handler. Pack Center, Vertical Pack Center and the two pin commands are additional actions, unbound by default. Better Shortcuts is optional. Core owns routing and one group choice; Better Packing owns its preference UI. Changes immediately update the Better Shortcuts draft; Preferences OK saves and Cancel discards them. There is no first-run/per-direction approval for Packing. The general Mod Controls page is hidden unless another mod supplies reviewable proposals.

## Pin-aligned packing (unreleased)

**Pack to Top Pins** aligns each selected object's highest main pin on the highest existing pin row. **Pack to Bottom Pins** aligns each lowest main pin on the lowest existing row. Items form a horizontal row using full drawn widths and the existing Gap/connector-adjacency rule. Both input and output pins participate; these are geometric anchors, not an attempt to infer intended electrical connections or compatible signal domains.

The commands read actual rendered pin centers on one unlocked schematic canvas. This resolves the 5-unit center of a compact connector against a component's 6-unit first pin without hard-coding an offset. Default anchors exclude exposed drawer pins. **File > Preferences > Better Packing > Use exposed control pins for pin-aligned packing** selects those instead; internal connectors retain their sole pin. Missing pins, stale canvas coordinates or positions that cannot preserve exact alignment are rejected before mutation.

Full drawer bounds remain the footprint; pin anchors affect vertical alignment only. Existing boundary commands and their shortcuts retain their behavior. New commands use the existing grouped move/undo and required receipt path, and can receive shortcuts through Core.

Ten pure layout checks cover 10/12-unit connectors, top/bottom rows, mixed pins, open drawers, long widths, invalid anchors and rounding. Native pin collection, nested canvases, attached wires, undo/redo and save/reopen still need Designer acceptance. See [pin-alignment issue #5](https://github.com/joshua-small/qsys-better-packing/issues/5).

Versions before 0.1.3 registered four extra, identically named directional actions. Assigning one could clear the standard action during conflict overwrite. Core now migrates those old assignments onto the canonical native action, preserving distinct keys and explicit editor overwrite consent. An empty old standalone assignment does not clear the native action. No shortcut rows are merged merely because their display names match.

Migration preserves a saved legacy opt-out or a previous all-off selection. A partial per-direction selection becomes the default all-on group. An explicit competing mod choice is preserved; re-enabling Packing then offers overwrite confirmation for the entire group. Original per-action records remain as history and no longer control this group.

Better Packing **0.2.0** requires [Q-SYS Mods Core](https://github.com/joshua-small/qsys-mods-core) **0.1.6 or newer within 0.1.x** and the exact supported Designer 10.5 build. The package and signed catalog enforce this dependency; Better Shortcuts remains optional. The 0.2 release range preserves the immutable dependency policy and pins of earlier 0.1.x packages. Use Mod Manager 0.8+ and its bundle migration; do not enable beside Schematic Tools 0.16 or earlier.

## Development

Clone Core beside this repository. Build with .NET 10.0.112 and local references:

```text
dotnet build Designer.Modules.BetterPacking.csproj -c Release -p:DesignerDir="C:\Program Files\QSC\Q-SYS Designer 10.5"
dotnet run --project tests/PackingLayout.Tests.csproj
dotnet run --project tests/PinPacking/PinPacking.Tests.csproj
```

The 18 layout-contract checks pass. `tests/PackingCommands` runs 11 checks against the shared Core router in an unshown Windows WPF tree; it does not launch Designer. Clone Core alongside this repo to build that fixture. `tests/LayoutProbe` retains the scoped, disposable Designer geometry probe; it requires separate local fixtures. Shared Preferences/defaults are exercised by Core's native driver. These checks do not establish hardware acceptance.

On 2026-10-14, matching 0.1.3 builds were checked in Designer 10.5 on a disposable two-component schematic. Ctrl+Shift+L/R/T/B each executed Better Packing once with Gap 20; operation receipts confirmed the final positions and each operation was undone. With the opt-out enabled, Ctrl+Shift+L used Q-SYS's zero-gap packing; disabling the opt-out restored Better Packing. Better Shortcuts showed one assigned row per direction and the selected provider. Clear/Reset, saving/reopening, and cancelling a staged opt-out were checked. The test insertions were undone and default overrides restored. UCI passthrough, menu/context-menu paths, invalid selections and the wider editor matrix remain tracked in [issue #2](https://github.com/joshua-small/qsys-better-packing/issues/2).

Feature source originated in `joshua-small/qsys-schematic-tools` commit `13c52e6`; public source contains no vendor binaries or decompiled implementation. [Split tracking](https://github.com/joshua-small/qsys-schematic-tools/issues/4).
