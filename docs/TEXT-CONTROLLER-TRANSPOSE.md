# Text Controller grid transpose

The **Preview Text Controller Transpose…** action supports a rectangular
selection on a schematic. Open it in the Better Packing window, or assign its
command through Mods Core. No shortcut is assigned by default. Native Pack
routing and the per-editor category preferences are independent of this action.

Select at least two complete control families copied from **one Text Controller**,
including one plain text label per family. Each family must have the same declared
Count of at least two. The selection can contain at most 256 items. Selecting a
subset of the controller's families is supported; selecting only part of a
family is not.

Before:

```text
Level   Level 1   Level 2   Level 3
Mute    Mute 1    Mute 2    Mute 3
```

After:

```text
Level     Mute
Level 1   Mute 1
Level 2   Mute 2
Level 3   Mute 3
```

The existing label objects become column headers. Controls keep their original
identities, controller bindings, index order, sizes, styling and orientation.
Only their positions change; controls are neither rotated nor recreated.

## Recognition and spacing

- Each label must exactly match its declared family name. Control membership
  comes from the source controller's declared families and exact indexed control
  names, not from guessing a trailing number in an arbitrary label.
- A family must occupy one horizontal row: label on the left, then indices
  1 through Count. Rows cannot overlap. Each row and each index column must align
  by its leading edge or center within one canvas unit.
- Family order follows the existing top-to-bottom row order. The result anchors
  at the selection's minimum X and minimum Y, and places columns left to right.
- The saved **Gap** applies horizontally and vertically. Each new column reserves
  its widest item; each new row reserves its tallest item. Fractional sizes are
  rounded up when advancing the next row or column, retaining whole-unit model
  positions and at least the requested clearance. A saved gap of zero is allowed.
- Missing or duplicate labels/cells, unequal counts, unknown or ambiguous control
  names, mixed controllers, unrelated objects, hidden/locked/detached items,
  unavailable geometry and unsupported coordinate spaces reject the whole action.
  UCI editors, component panels, control-definition objects and arbitrary grids
  are outside this command's supported scope.

## Preview, apply and cancellation

The read-only diagram shows the selected items' drawn footprints before and
after. It does not render their complete appearance. Hover a box to see its
family, index, coordinates and size. Surrounding unselected objects are not part
of this preview or a collision check; review the available schematic space.

**Cancel**, Escape or closing the preview makes no model changes and adds no undo
entry. **Apply transpose** validates the selection, document, canvas, source
controller, bindings, geometry and saved gap again, then uses the Core position
transaction helper once. One Undo restores the original layout; Redo reapplies
it. Editing a selected model or binding while the preview is open invalidates
it, even if that edit is subsequently reversed. Any failed apply attempt closes
the preview; create a fresh preview before retrying. No native fallback runs.

This shares the existing position-transaction helper with packing. The planner
and session remain separate so a future general packing preview can reuse the
preview/apply separation without broadening this command's accepted selections.
The quick-gap HUD and general collision preview remain separate features.

## Validation boundary

Portable planner tests cover mapping, original-grid recognition, spacing,
nonfinite/overflow rejection and unchanged input. Windows fixture tests run the
production selection reader, session and Core transaction helper against
synthetic host objects, including cancellation, stale previews, one grouped
apply, Undo/Redo and rollback after a position-setter failure.

```text
dotnet run --project tests/Transpose/Transpose.Tests.csproj -c Release
dotnet run --project tests/TransposeCommands/TransposeCommands.Tests.csproj -c Release
```

The second command requires Windows and WPF; neither test requires vendor DLLs.
The automated suites contain 125 checks across packing, pins, routing and
transpose. They are distinct from application acceptance.

The 0.4.0 module passed 92 bounded native assertions on Designer 10.5.0 with Core
0.1.10. These cover copied-label and control recognition, rendered preview,
Cancel/Escape/close, physical Apply and one-step Undo/Redo, identity and binding
preservation, native Pack separation, stale/rejection cases, nested schematics,
and separate save/reopen. Fresh-process checks compare persisted semantic
records; process-local object references are not persistent identifiers.

With Core 0.1.11, 16 additional assertions checked rendered tooltips, stale-session
rejection after a plain design replacement and a fractional-property probe. All
twelve items in that probe settled to integer drawn bounds. Fractional drawn-size
acceptance is therefore not demonstrated, although the pure planner tests cover
fractional sizes. Those additional assertions do not establish full acceptance.

Designer 10.5.1 and an editable custom component-panel GUI remain unverified.
A controlled same-process replacement of a UCI-containing design exited with
Better Packing both present and absent, using the same fixture bytes and native
replacement operation. Neither comparison run retained a transpose session.
This reproduces the exit without Better Packing; other modules remained loaded,
so it does not establish an unmodified Designer baseline or identify the cause.
That native replacement route remains outside accepted coverage. The separate
plain-design stale-preview check passed. No connected Core or hardware behavior
is claimed. Core's exact-build opt-in permits startup on an untested host; it
does not establish feature compatibility.
