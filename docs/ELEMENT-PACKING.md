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
| Polygons | `Graphics.Region` |

`ControlStyle` includes component-bound controls; `ControllerControlStyle`
derives from it and is also classified as a control. Each accepted model must have exactly one selected
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
rounding. Polygons (`Graphics.Region`) additionally use `Graphic.Region.rc`
for their drawn origin: polygon vertices can be offset from the stored model
position. The planner retains that offset, including fractional offsets, while
rounding model positions to whole units. It rounds clearance upward so the
visible gap is at least the requested value and less than one unit larger;
alignment remains within half a unit. Vertices are not changed.
A single Core `HostSelection.ApplyPositions` transaction applies the
whole plan. It preserves object identity and changes positions only; control
bindings, labels and values are not edited. Existing connector pairs remain
flush. Geometry or execution failures are blocked and never fall through to a
second native action.

## Evidence and remaining checks

The selection reader checks object identities, drawn geometry, locks and
visibility before planning. Position changes use the host's grouped transaction
helper so the selection can be restored with one Undo.
[Q-SYS Help documents the native Pack operations](https://help.qsys.com/Content/Q-SYS_Designer/Using_the_Schematic/Labeling_and_Organizing_Schematic_Elements.htm).

The automated suites exercise the production policy/reader/planner against
fixture objects and actual WPF routed events. They do not exercise native
Designer type construction, input focus, the host undo manager or file saving.
For 0.2.1, a separate hash-matched Designer 10.5 copy with a redirected profile
passed 86 native assertions. They exercised headers, text blocks, group boxes,
images, polygons with different fractional vertex offsets, and component-bound
`ControlStyle` instances on schematic and UCI canvases. All six directions
retained the requested visible gap (with host rounding), one-step undo/redo,
control bindings and polygon vertices. Fifteen category choices loaded with
the accepted defaults; Cancel preserved settings; OK/reopen retained changes.
All six real routed schematic commands used native zero-gap packing by default
and the configured gap after opt-in. The disposable design was saved through
Designer and reopened in a fresh isolated process with the same object
positions/sizes, control bindings and polygon vertices; preferences also
persisted across that restart. These were in-process native API/routed-command assertions, not
physical keyboard/mouse tests; the UCI canvas fixture used an owned test window.

Remaining acceptance includes physical menu/key invocation in the normal UCI
editor, center-key migration, floating/docked contexts, the expanded master
switch in Designer, mixed selections and nested-schematic regression with the
new routing policy. Portable and WPF checks cover their underlying policy and
router where documented above. Keep those application contexts distinct from
the native geometry checks. Do not extend tests to a connected Core without
an explicitly scoped target.

The proposed HUD is a separate interaction: Alt + right-drag horizontally for
Horizontal Gap or vertically for Vertical Gap, with an on-canvas readout.
It requires separate axis values, input-conflict checks and a decision about
settings-only adjustment versus a live layout preview. The longer-term intent
is broad Better Packing coverage once adjusting gaps is fast; current native
fallbacks must not be mistaken for permanent exclusions.
