# Better Packing

Improved schematic packing, extracted from Schematic Tools 0.16. It accounts for exposed control-pin footprints, mixed components and Layout items, connector adjacency, and host rounding. All six operations are available from **Tools > Q-SYS Mods > Better Packing**.

**File > Preferences > Better Packing** stores Gap (default **20**, whole numbers 0–10000) and **Replace Default Pack Behavior** (default **on**). When enabled, native schematic shortcuts, Tools > Pack, and right-click Pack actions use the same improved operation exactly once. When disabled, native behavior resumes; the mod's operation window remains available. UCI/control-panel packing keeps native behavior. Text editing and unsupported focus cannot trigger schematic packing.

Requires [Q-SYS Mods Core](https://github.com/joshua-small/qsys-mods-core) 0.1.x and the exact supported Designer 10.5 build. Use Mod Manager 0.8+ and its bundle migration; do not enable this beside Schematic Tools 0.16 or earlier. Better Shortcuts can assign gestures to the six registered packing commands but is optional.

## Development

Clone Core beside this repository. Build with .NET 10.0.112 and local references:

```text
dotnet build Designer.Modules.BetterPacking.csproj -c Release -p:DesignerDir="C:\Program Files\QSC\Q-SYS Designer 10.5"
dotnet run --project tests/PackingLayout.Tests.csproj
```

The 18 layout-contract checks pass. `tests/PackingCommands` checks native routing in the Windows WPF runtime. `tests/LayoutProbe` retains the scoped, disposable Designer geometry probe; it requires separate local fixtures. Shared Preferences/defaults are exercised by Core's native driver. These checks do not establish hardware acceptance.

Feature source originated in `joshua-small/qsys-schematic-tools` commit `13c52e6`; public source contains no vendor binaries or decompiled implementation. [Split tracking](https://github.com/joshua-small/qsys-schematic-tools/issues/4).
