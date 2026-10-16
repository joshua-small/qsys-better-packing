# Disposable Designer acceptance driver

This test module must never be installed in regular Designer or shipped in a
release. Build it separately with the exact supported Designer/ModuleCommon
references. Copy it only into a disposable installation whose native preferences
are redirected to a test profile.

`PinProbe.cs` documents the required `.pin-lab-probe.json` configuration. The
driver requires a matching application directory, `.selection-probe-lab` marker,
command-line fixture path and SHA-256. It refuses existing receipt/save outputs,
requires disconnected Design mode, and checks the exact Designer hash.

`TestNested` produces a separate fixture with 17 internal connectors across a
Container, its nested Container, and a Channel Group. `ExpectedConnectorCount`
defaults to 11 for the original fixture; use 17 for that saved output.

`Physical: true` uses the saved nested fixture, verifies native storage
redirection, and adds nine wires through Designer's native wire creation method.
It opens the three nested canvases and labels two Gains plus input/output
connectors for observation. This setup changes window focus; do not run it while
another session is using the desktop.

The physical mode accepts a small local `physical-command.json` object containing
a unique `id` and an `action`: `snapshot`, `scope` (nested `index` 0 to 2, or -1
to observe the root),
`pack`, or `save`. Publish commands atomically. Archive each acknowledged receipt
before sending another command. A snapshot records coordinates, zoom, display
scale, exact module hashes and all nine wires' pin IDs and rendered endpoints.
It does not move objects. Use real mouse input for physical drag acceptance and
keep those observations distinct from the automated `pack` matrix.

Closed nested panels have no graphic wire instances. Their attachment/endpoint
identities are still checked; rendered-path checks apply to loaded instances.
A missing rendered wire on the currently observed canvas is an error. A fresh
reopen opens all three panels before verifying all nine rendered wire paths.

`pack` tests top/bottom main/exposed pin alignment with all endpoints selected
and with a stationary connected peer. Every operation checks endpoint identity,
attachment, nonempty rendered paths, endpoint coordinates, labels/sizes and
one-step undo/redo. It then restores original positions. When positions are
unchanged, the driver skips Undo because it could reach an earlier unrelated
edit. Receipts distinguish these cases from changed operations.

`save` writes only the configured new output. Relaunch with that output's path
and hash, `Physical: true`, `PhysicalReopen: true` and no `SaveAs` to read back
the nine wires in a new process without creating additional fixtures. Compare
the saved/reopened endpoint IDs and coordinates. Retain raw receipts and fixture
files locally; they are not product payloads or hardware acceptance.

`PhysicalResume: true` instead resumes observation/commands on a saved nine-wire
checkpoint. Use a new `SaveAs` output and receipt; the checkpoint stays unchanged.
`effectiveScale` records the canvas-to-window transform separately from the
nested canvas's stored `CanvasScaleFactor`, which can differ from the visible
zoom inherited from the root page.
