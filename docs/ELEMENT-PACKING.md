# Element packing contract

Native commands choose one provider for the complete current selection, using
the active editor and saved category preferences. A Native category or unknown
model family makes the complete selection native before any Better Packing
mutation. The explicit Better Packing window always attempts the improved
operation and explains unsupported selections instead of silently changing
providers. All six operations share this policy.

## Model families

These are private Designer 10.5 implementation observations, guarded by Core's
exact host and ModuleCommon hashes; they are not public Q-SYS APIs.
Base-family matches include derived types.

| Preference | Model families under `QSC.QSys.Design` |
| --- | --- |
| Components | `Component` |
| Layout items and connectors | `Container`, `ChannelGroup`, `ContainerPin`, `Graphics.Popup`, `Mux.Muxer`, `Mux.Demuxer` |
| Controls and indicators | `ControlStyle`, `ControlSurface` |
| Headers | `Graphics.Header` |
| Text blocks | `Graphics.TextBlock` |
| Group boxes | `Graphics.GroupBox` |
| Images | `GraphicImage` |
| Regions | `Graphics.Region` |

`ControllerControlStyle` derives from `ControlStyle`, covering controls copied
from components. Each accepted model must have exactly one selected
`Graphic.Object` instance with the same model identity. Its current drawn
position must equal its model position, and both body and drawn sizes must be
finite, positive and consistent with host rounding. The selection must share
one unlocked schematic or UCI canvas. Hidden/locked/shared-layer instances are
rejected; native `Commands.Align.Can` also preserves the control-tool/edit
boundary. The native guard is rechecked when executing, not only for menu state.

Schematic canvases use Core's document-page and nested-schematic traversal.
For other canvases, `GraphicCanvas.IsUciPage` identifies the UCI editor
(`Graphic.WebPage` returns true). Component control panels remain native.
Groups and Flexboxes are intentionally unclassified: they involve nested
coordinates or managed layout, so they continue to work through native packing.
Other unlisted elements also remain native rather than being admitted through
the generic `Design.Object` base.

The planner uses the existing drawn width/height contract and whole-unit host
rounding. A single Core `HostSelection.ApplyPositions` transaction applies the
whole plan. It preserves object identity and changes positions only; control
bindings, labels and values are not edited. Existing connector pairs remain
flush. Geometry or execution failures are blocked and never fall through to a
second native action.

## Evidence and remaining checks

The host type/geometry observations were inspected statically in the supported
Designer 10.5 assembly. `Design.Object.Position` uses the host undo-aware setter;
`ControlStyle.Position` delegates to that base implementation. `Graphic.Object`
supplies the model identity and lock/visibility properties used by the reader.
No vendor binary or decompiled source is included in this repository.
[Q-SYS Help documents the native Pack operations](https://help.qsys.com/Content/Q-SYS_Designer/Using_the_Schematic/Labeling_and_Organizing_Schematic_Elements.htm).

The automated suites exercise the production policy/reader/planner against
fixture objects and actual WPF routed events. They do not exercise native
Designer type construction, input focus, the host undo manager or file saving.
Before release, verify representative copied controls, each graphic family,
mixed types, both editor columns and nested schematics on a disposable design.
Check all six native menu operations, custom/default keys, center-key migration,
the master switch, save/reopen of preferences, Cancel, grouped undo/redo and
design save/reopen. Retain prior component/drawer/connector behavior. Do not
extend these checks to a connected Core without an explicitly scoped target.

The proposed HUD is a separate interaction: Alt + right-drag horizontally for
Horizontal Gap or vertically for Vertical Gap, with an on-canvas readout.
It requires separate axis values, input-conflict checks and a decision about
settings-only adjustment versus a live layout preview. The longer-term intent
is broad Better Packing coverage once adjusting gaps is fast; current native
fallbacks must not be mistaken for permanent exclusions.
