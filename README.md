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
| Regions | Native | Native |

Opt a category into Better Packing to use the saved gap for it. **If any selected type uses Native, the entire selection uses native packing.** Graphics and copied controls therefore remain packable with the default settings. Unknown types, Groups, Flexboxes and component control panels always retain native handling. Invalid geometry or an operation failure never triggers a second native operation. [Supported type families and validation limits](docs/ELEMENT-PACKING.md).

Layout includes Containers, Channel Groups, muxes, popup buttons and internal connectors. Adjacent internal connectors remain flush; other pairs use the saved gap. Nested Container/Channel Group schematics use the Schematic column; their control panels retain native handling. The UCI column applies to the UCI editor, not deployed UCI viewers.

**Disable default Q-SYS control overrides** remains the master switch. Checking it restores all six native Pack actions without resetting shortcuts or category choices. The six buttons in the Better Packing window explicitly use Better Packing for supported selections, independently of this switch and the category routing choices.

Each of the six Pack actions has one native shortcut entry, using the actual defaults or saved user keys. Core selects Better Packing or Q-SYS as its handler; existing assignments to the former standalone center actions migrate onto their native equivalents. Better Shortcuts is optional. Core owns command routing and the master switch; Better Packing owns category choices. Preferences OK saves the choices and Cancel discards the draft. The master switch immediately updates the Better Shortcuts draft; its provider description explains that actual routing depends on the selected types and editor. There is no first-run/per-direction approval for Packing. The general Mod Controls page is hidden unless another mod supplies reviewable proposals.

Versions before 0.1.3 registered four extra, identically named directional actions. Assigning one could clear the standard action during conflict overwrite. Core now migrates those old assignments onto the canonical native action, preserving distinct keys and explicit editor overwrite consent. An empty old standalone assignment does not clear the native action. No shortcut rows are merged merely because their display names match.

Migration preserves a saved legacy opt-out or a previous all-off selection. A partial per-direction selection becomes the default all-on group. Re-enabling Packing offers overwrite confirmation when another provider claims these commands. Original per-action records remain as history and no longer control this group. Older settings without category choices receive the defaults above; their saved Gap is retained.

Better Packing **0.2.0** requires [Q-SYS Mods Core](https://github.com/joshua-small/qsys-mods-core) **0.1.6 or newer within 0.1.x** and the exact supported Designer 10.5 build. The package and signed catalog enforce this dependency; Better Shortcuts remains optional. The 0.2 release range preserves the immutable dependency policy and pins of earlier 0.1.x packages. Use Mod Manager 0.8+ and its bundle migration; do not enable beside Schematic Tools 0.16 or earlier.

## Development

Clone Core beside this repository. Build with .NET 10.0.112 and local references:

```text
dotnet build Designer.Modules.BetterPacking.csproj -c Release -p:DesignerDir="C:\Program Files\QSC\Q-SYS Designer 10.5"
dotnet run --project tests/PackingLayout.Tests.csproj
```

`tests/PackingLayout.Tests.csproj` runs 24 checks covering geometry, category policy, settings migration and serialization. `tests/PackingCommands` runs 22 checks in an unshown Windows WPF tree against the production selection reader, planner, preference matrix and shared Core router, using explicit fixture host objects. These tests cover native fallback, mixed selections, opted-in schematic/UCI geometry, locks, control-tool mode, cancellation of a preference draft and operation failure without fallthrough. Build in WSL and run the WPF test DLL with Windows .NET; the fixture does not launch Designer. Clone Core alongside this repo to build it. `tests/LayoutProbe` retains the scoped, disposable Designer geometry probe; it requires separate local fixtures.

The category preferences, new object families, UCI opt-in and center-command migration still require scoped Designer acceptance, including native menu/key invocation, Preferences OK/Cancel/reopen, undo/redo and save/reopen. Automated checks do not establish those application results or hardware acceptance. The Alt + right-drag gap HUD and broader object coverage remain future work tracked in [issue #2](https://github.com/joshua-small/qsys-better-packing/issues/2).

On 2026-10-14, matching 0.1.3 builds were checked in Designer 10.5 on a disposable two-component schematic. Ctrl+Shift+L/R/T/B each executed Better Packing once with Gap 20; operation receipts confirmed the final positions and each operation was undone. With the opt-out enabled, Ctrl+Shift+L used Q-SYS's zero-gap packing; disabling the opt-out restored Better Packing. Better Shortcuts showed one assigned row per direction and the selected provider. Clear/Reset, saving/reopening, and cancelling a staged opt-out were checked. The test insertions were undone and default overrides restored. UCI passthrough, menu/context-menu paths, invalid selections and the wider editor matrix remain tracked in [issue #2](https://github.com/joshua-small/qsys-better-packing/issues/2).

Feature source originated in `joshua-small/qsys-schematic-tools` commit `13c52e6`; public source contains no vendor binaries or decompiled implementation. [Split tracking](https://github.com/joshua-small/qsys-schematic-tools/issues/4).
