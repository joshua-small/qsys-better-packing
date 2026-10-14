# Better Packing

Improved schematic packing, extracted from Schematic Tools 0.16. It accounts for exposed control-pin footprints, mixed components and Layout items, connector adjacency, and host rounding. All six operations are available from **Tools > Q-SYS Mods > Better Packing**.

**File > Preferences > Better Packing** stores Gap (default **20**, whole numbers 0–10000). **Mod Controls** reviews the four native Pack replacements together. New installations ask before enabling them; saved legacy replacement choices carry forward. Approved schematic shortcuts, Tools > Pack, and right-click Pack actions use the improved operation exactly once, preserving the native action's current keys. Disabling a replacement resumes native behavior without resetting user keys. UCI/control-panel packing stays native; invalid selection and text focus remain blocked.

The six standalone mod actions remain separate and unbound by default. Better Shortcuts can assign them keys and show which provider handles native Pack actions, but is optional. Core owns the shared routing and review; Better Packing no longer installs its own preview handlers. The preferences link opens the shared draft, where OK saves and Cancel discards changes.

Requires [Q-SYS Mods Core](https://github.com/joshua-small/qsys-mods-core) **0.1.1 or newer within 0.1.x** and the exact supported Designer 10.5 build. Deploy the matching Core/Packing/Shortcuts test builds together. Current signed package policy predates the new API minimum; it must be updated before public release. Use Mod Manager 0.8+ and its bundle migration; do not enable beside Schematic Tools 0.16 or earlier.

## Development

Clone Core beside this repository. Build with .NET 10.0.112 and local references:

```text
dotnet build Designer.Modules.BetterPacking.csproj -c Release -p:DesignerDir="C:\Program Files\QSC\Q-SYS Designer 10.5"
dotnet run --project tests/PackingLayout.Tests.csproj
```

The 18 layout-contract checks pass. `tests/PackingCommands` runs 11 checks against the shared Core router in an unshown Windows WPF tree; it does not launch Designer. Clone Core alongside this repo to build that fixture. `tests/LayoutProbe` retains the scoped, disposable Designer geometry probe; it requires separate local fixtures. Shared Preferences/defaults are exercised by Core's native driver. These checks do not establish hardware acceptance.

Feature source originated in `joshua-small/qsys-schematic-tools` commit `13c52e6`; public source contains no vendor binaries or decompiled implementation. [Split tracking](https://github.com/joshua-small/qsys-schematic-tools/issues/4).
