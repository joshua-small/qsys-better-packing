# Better Packing

Improved schematic packing, extracted from Schematic Tools 0.16. It accounts for exposed control-pin footprints, mixed components and Layout items, connector adjacency, and host rounding. All six operations are available from **Tools > Q-SYS Mods > Better Packing**.

**File > Preferences > Better Packing** stores Gap (default **20**, whole numbers 0–10000) and one **Disable default Q-SYS control overrides** checkbox. It is unchecked by default: Pack Left, Right, Top, and Bottom all use Better Packing on the schematic. Checking it restores all four native actions. Existing shortcuts, Tools > Pack, and right-click Pack use the same action exactly once; toggling the setting never resets user keys. UCI/control-panel packing stays native; invalid selection and text focus remain blocked.

The six standalone mod actions remain separate and unbound by default. Better Shortcuts can assign them keys and show which provider handles native Pack actions, but is optional. Core owns the shared routing and stores one group choice; Better Packing owns its preference UI. Changes immediately update the Better Shortcuts draft; Preferences OK saves and Cancel discards them. There is no first-run/per-direction approval for Packing. The general Mod Controls page is hidden unless another mod supplies reviewable proposals.

Migration preserves a saved legacy opt-out or a previous all-off selection. A partial per-direction selection becomes the default all-on group. An explicit competing mod choice is preserved; re-enabling Packing then offers overwrite confirmation for the entire group. Original per-action records remain as history and no longer control this group.

Requires [Q-SYS Mods Core](https://github.com/joshua-small/qsys-mods-core) **0.1.2 or newer within 0.1.x** and the exact supported Designer 10.5 build. Deploy the matching Core/Packing/Shortcuts test builds together. Current signed package policy predates the new API minimum; it must be updated before public release. Use Mod Manager 0.8+ and its bundle migration; do not enable beside Schematic Tools 0.16 or earlier.

## Development

Clone Core beside this repository. Build with .NET 10.0.112 and local references:

```text
dotnet build Designer.Modules.BetterPacking.csproj -c Release -p:DesignerDir="C:\Program Files\QSC\Q-SYS Designer 10.5"
dotnet run --project tests/PackingLayout.Tests.csproj
```

The 18 layout-contract checks pass. `tests/PackingCommands` runs 11 checks against the shared Core router in an unshown Windows WPF tree; it does not launch Designer. Clone Core alongside this repo to build that fixture. `tests/LayoutProbe` retains the scoped, disposable Designer geometry probe; it requires separate local fixtures. Shared Preferences/defaults are exercised by Core's native driver. These checks do not establish hardware acceptance.

Feature source originated in `joshua-small/qsys-schematic-tools` commit `13c52e6`; public source contains no vendor binaries or decompiled implementation. [Split tracking](https://github.com/joshua-small/qsys-schematic-tools/issues/4).
