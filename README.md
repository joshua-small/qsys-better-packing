# Better Packing

Pack schematic and UCI elements with a saved gap. Component packing accounts for exposed control-pin footprints, connector adjacency, and host rounding. All six operations are available through the native Pack commands and **Tools > Q-SYS Mods > Better Packing**.

**File > Preferences > Better Packing** stores Gap (default **20**, whole numbers 0–10000) and separate **Native / Better Packing** choices for each element category in the Schematic and UCI editors. The same gap is used in both editors and for both axes.

| Element category | Schematic default | UCI default |
| --- | --- | --- |
| Components | Better Packing | Not applicable |
| Layout items and connectors | Better Packing | Native |
| Controls and indicators | Native | Native |
| Headers | Native | Native |
| Text blocks | Native | Native |
| Group boxes | Native | Native |
| Images | Native | Native |
| Polygons | Native | Native |

Opt a category into Better Packing to use the saved gap for it. **If any selected type uses Native, the entire selection uses native packing.** Graphics and copied controls therefore remain packable with the default settings. Unknown types, Groups, Flexboxes and component control panels always retain native handling. Invalid geometry or an operation failure never triggers a second native operation. [Supported type families and validation limits](docs/ELEMENT-PACKING.md).

Layout includes Containers, Channel Groups, muxes, popup buttons and internal connectors. Adjacent internal connectors remain flush; other pairs use the saved gap. Nested Container/Channel Group schematics use the Schematic column; their control panels retain native handling. The UCI column applies to the UCI editor, not deployed UCI viewers.

**Disable default Q-SYS control overrides** remains the master switch. Checking it restores all six native Pack actions without resetting shortcuts or category choices. The six buttons in the Better Packing window explicitly use Better Packing for supported selections, independently of this switch and the category routing choices.

Each of the six Pack actions has one native shortcut entry, using the actual defaults or saved user keys. Core selects Better Packing or Q-SYS as its handler; existing assignments to the former standalone center actions migrate onto their native equivalents. Better Shortcuts is optional. Core owns command routing and the master switch; Better Packing owns category choices. Preferences OK saves the choices and Cancel discards the draft. The master switch immediately updates the Better Shortcuts draft; its provider description explains that actual routing depends on the selected types and editor. There is no first-run/per-direction approval for Packing. The general Mod Controls page is hidden unless another mod supplies reviewable proposals.

Versions before 0.1.3 registered four extra, identically named directional actions. Assigning one could clear the standard action during conflict overwrite. Core now migrates those old assignments onto the canonical native action, preserving distinct keys and explicit editor overwrite consent. An empty old standalone assignment does not clear the native action. No shortcut rows are merged merely because their display names match.

Migration preserves a saved legacy opt-out or a previous all-off selection. A partial per-direction selection becomes the default all-on group. Re-enabling Packing offers overwrite confirmation when another provider claims these commands. Original per-action records remain as history and no longer control this group. Older settings without category choices receive the defaults above; their saved Gap is retained.

The **0.3.0 source candidate** requires [Q-SYS Mods Core](https://github.com/joshua-small/qsys-mods-core) **0.1.8 or newer within 0.1.x** for operation-window undo/redo. Publication requires the new disjoint catalog range; existing 0.1.x/0.2.x package policies and pins remain immutable. This candidate has not yet passed physical Designer acceptance or been released/installed. Better Shortcuts remains optional. Use Mod Manager 0.8+ and its bundle migration; do not enable beside Schematic Tools 0.16 or earlier.

## Pin-aligned packing

Four actions align selected objects by actual rendered pin centers: **Pack to Top Main Pins**, **Pack to Bottom Main Pins**, **Pack to Top Drawer Pins**, and **Pack to Bottom Drawer Pins**. They appear in the Better Packing window and have separate Core shortcut entries with no default keys. They apply only to schematic components, connectors and Signal Snakes, including nested schematics.

Items form a horizontal row using full drawn widths and the saved Gap. Adjacent internal connectors remain flush. Main actions use the main component pins; Drawer actions use exposed control pins. Internal connectors retain their sole pin in either group. A selected component without the requested visible pins is rejected before mutation, as are stale coordinates and positions that cannot preserve exact alignment. This fixes the one-unit alignment difference between a compact connector's 5-unit center and a component's 6-unit first pin without guessing offsets.

The former global main/drawer checkbox is removed. Existing `pins-top` and `pins-bottom` shortcut identities now explicitly name Main pins; the two Drawer commands have new identities. The legacy stored preference is preserved when saving settings but no longer selects a command's pin group. Other shortcuts and packing preferences are unchanged.

The operation window opts into Core's document Undo/Redo routing, including current custom keys and chords. Text inputs retain local undo. Native history availability and disabled-owner guards still apply; preferences dialogs do not acquire document-history routing.

Drawer bounds remain part of the footprint. Pin packing changes geometry only and uses the existing grouped move/undo path. The six native boundary commands and per-editor choices retain their behavior.
## Development

Clone Core beside this repository. Build with .NET 10.0.112 and local references:

```text
dotnet build Designer.Modules.BetterPacking.csproj -c Release -p:DesignerDir="C:\Program Files\QSC\Q-SYS Designer 10.5"
dotnet run --project tests/PackingLayout.Tests.csproj
```

`tests/PackingLayout.Tests.csproj` runs 25 checks covering geometry, category policy, settings migration and serialization. `tests/PackingCommands` runs 24 checks in an unshown Windows WPF tree against the production selection reader, planner, preference matrix and shared Core router, using explicit fixture host objects. These tests cover native fallback, mixed selections, opted-in schematic/UCI geometry, locks, control-tool mode, cancellation of a preference draft and operation failure without fallthrough. Build in WSL and run the WPF test DLL with Windows .NET; the fixture does not launch Designer. Clone Core alongside this repo to build it. `tests/LayoutProbe` retains the scoped, disposable Designer geometry probe; it requires separate local fixtures.

For 0.2.1, 86 assertions in an isolated Designer 10.5 copy checked the new graphic families and component-bound controls on schematic/UCI canvases in all six directions, visible gaps, unchanged bindings/vertices, undo/redo, preference defaults/Cancel/OK/reopen, and all six real routed schematic commands using native zero gaps or opted-in gaps. The disposable design was saved and reopened with matching contents/bindings/vertices, and preferences persisted across a fresh process. These are native API/routed-command checks; normal UCI menu/key input, center-key migration, broader mixed/nested/floating contexts and the expanded master switch still need scoped application acceptance. [Evidence and limits](docs/ELEMENT-PACKING.md). The Alt + right-drag gap HUD and broader object coverage remain future work tracked in [issue #2](https://github.com/joshua-small/qsys-better-packing/issues/2).

On 2026-10-14, matching 0.1.3 builds were checked in Designer 10.5 on a disposable two-component schematic. Ctrl+Shift+L/R/T/B each executed Better Packing once with Gap 20; operation receipts confirmed the final positions and each operation was undone. With the opt-out enabled, Ctrl+Shift+L used Q-SYS's zero-gap packing; disabling the opt-out restored Better Packing. Better Shortcuts showed one assigned row per direction and the selected provider. Clear/Reset, saving/reopening, and cancelling a staged opt-out were checked. The test insertions were undone and default overrides restored. UCI passthrough, menu/context-menu paths, invalid selections and the wider editor matrix remain tracked in [issue #2](https://github.com/joshua-small/qsys-better-packing/issues/2).

Feature source originated in `joshua-small/qsys-schematic-tools` commit `13c52e6`; public source contains no vendor binaries or decompiled implementation. [Split tracking](https://github.com/joshua-small/qsys-schematic-tools/issues/4).

## Geometry update acceptance (2026-10-16)

A disposable exact-build Designer 10.5 copy with redirected preferences exercised Containers, Channel Groups and a second Container nesting level. Twelve top/bottom pin operations covered main and exposed drawer pins; each verified equal native CanvasCenter coordinates and one-step undo/redo. Six registered pin commands and twelve existing routed boundary commands ran with native nested-canvas focus. Three checks confirmed six body/drawer snap corners and the Wiring snap domain on compact connectors. The fixture was saved to a separate output and the original hash stayed unchanged. A fresh process reopened the output with all 17 internal connectors attached at height 10. The same run used the exact release payloads for Better Packing 0.2.2, Core 0.1.7, Component Auto Width 0.1.0 and Compact Connectors 0.1.0. Native Preferences displayed the pin-selection checkbox alongside the existing Schematic/UCI category choices.

Follow-up acceptance passed 17 physical drags at 100%/200% Designer zoom and 100% Windows display scaling, plus 24 attached-audio-wire top/bottom pin packing cases across the three nested scopes. Checks covered both connector directions, exposed control-pin drag targets, main-body/outer-drawer targets, open/closed drawers, near/outside tolerance, physical undo/redo and fresh-process save/reopen with all nine wire endpoints preserved. No product fix or replacement release was needed. [Physical-drag and attached-wire results](docs/PHYSICAL-WIRE-ACCEPTANCE.md).

Further native checks passed 40 control/AV Stream/MediaCast packing operations, with all 19 connections preserved through fresh-process save/reopen. Native wiring rules remained in force, including MediaCast Router-to-Router rejection. These additional wire-type checks were programmatic. Other display scales, physical drags with those non-audio wires, mixed-selection drag domains and wider component/pin counts remain unverified. This does not add physical shortcut/menu acceptance for the pin commands or DSP/hardware evidence. The [guarded driver](tests/PinProbe/README.md) only runs with a matching disposable path, marker and fixture hash; source and test fixtures contain no vendor code or binaries.
