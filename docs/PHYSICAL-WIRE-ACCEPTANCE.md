# Physical dragging and attached wires — 2026-10-16

Compact Connectors 0.1.0 and Better Packing 0.2.2 passed the scoped checks below
in an isolated Designer 10.5 installation. No product fix or replacement release
was needed. Component Auto Width 0.1.0 and Mods Core 0.1.7 were also loaded.
The test profile redirected native roaming/local/Documents/user storage; ten
recorded personal-preference/vendor files and the original fixture retained
their hashes. Only disposable copies were saved.

## Physical input

Seventeen mouse drags were followed by native coordinate and wire observations.
The desktop used 100% Windows display scaling, with Designer at 100% and 200%
zoom. These were physical mouse inputs, separate from the native API packing
matrix below.

| Coverage | Result |
| --- | --- |
| Wired input and output connectors in Container, second-level Container and Channel Group | Both directions snapped to actual main pin rows. A 10-unit connector at Y=181 has its center at Y=186, matching a Gain at Y=180. |
| Channel Group control connectors | Input aligned with the first exposed control pin at Y=214; output aligned with the last at Y=224. These two control connectors were unwired. |
| Open drawer, nested component movement | Moving another wired Gain aligned its top at Y=208 with the target's main-body bottom, and at Y=230 with the retained outer drawer bottom. The body-bottom check also passed at 200% zoom. |
| Closed drawer comparison | With the target's exposed controls switched off, its drawn height returned to 28 and the same Y=208 bottom alignment remained available. |
| Root schematic | A Container snapped to Flex In's main-body bottom while the drawer stayed open. Moving Flex In itself retained this alignment. The original component-top target also worked. |
| Native tolerance | Releases a few units above/below a row snapped to it. A connector 12 units away stayed off the row; bringing it back near the row snapped again. This does not measure every threshold boundary. |
| Physical undo/redo | Ctrl+Z restored the complete pre-drag position set; Ctrl+Y restored the dragged position set. Wire identities and attachment survived both. |

## Attached-wire packing and persistence

The guarded [driver](../tests/PinProbe/README.md) created nine native audio wires:
input-to-Gain, Gain-to-Gain and Gain-to-output in each of the three nested scopes.
Twenty-four changed packing operations passed: top/bottom × main/exposed pin
anchors × all endpoints/stationary connected peer × three scopes. Each checked
pin alignment, unchanged labels and sizes, identical endpoint objects, two-sided
wire membership, rendered endpoints, nonempty paths, one-step undo and redo.
Native model setup and these packing invocations were programmatic; they are
not claims of physical wire creation or menu/shortcut invocation.

A separate output was saved and reopened in a fresh native process. All nine
wire endpoint IDs and coordinates matched, and all nine rendered paths reached
their attached pins. The source fixture hash remained unchanged. The local
receipt summary contains 34 cross-checks over the retained observations.

The test observer was corrected to tolerate intentionally closed panels and to
avoid undoing a prior edit when a pack is a no-op. Neither correction changed
product code. Raw native receipts, test designs and vendor binaries remain local.

## Additional wire types

The same installed release payloads passed 40 further native packing operations
at 100% Windows display scaling. Every fixture connection was accepted by
Designer's native wiring rules before creation; no domain was forced or rule
patched. These are programmatic packing and persistence checks, not additional
physical drags.

| Native wire domain | Fixture and scopes | Changed packing operations | Persisted wires |
| --- | --- | ---: | ---: |
| Control | Exposed Gain controls; Container, nested Container, Channel Group | 24 | 9 |
| AVH (AV Stream video) | AV Stream Routers; Container and nested Container | 8 | 6 |
| Mediacast | Camera/MediaCast Routers; Container and nested Container | 8 | 4 |

Top/bottom operations covered all connected objects and selections with stationary
connected peers. Control cases exercised main and exposed pin anchors; video
cases used main pins. Each checked exact alignment, unchanged labels/sizes,
endpoint identity, two-sided membership, nonempty rendered paths and one-step
undo/redo. Across three separate saved designs, all 19 wire endpoint IDs,
coordinates and domains matched after fresh-process reopen; all 19 rendered
paths reached their pins. The source fixture and nine of ten recorded
personal/vendor files stayed unchanged. The shared native `Prefs.xml` was
rewritten during concurrent Designer activity; byte identity is not claimed for
that file, and its current contents were preserved. No product fix, version bump
or reinstall was needed.

The native MediaCast rules reject a Router-output-to-Router-input chain. That
rejection was observed and retained in both Container scopes; only the valid
connector-to-Router wires were created. Channel Groups' native Audio/Control
connector types were not converted to video. Earlier tracking's generic
"network wire" label is clarified by the two concrete network-video domains
above; the inspected host has no separate `Network` wiring-domain enum member.
These observations do not establish device streaming or arbitrary network types.

## Owner feedback and follow-up candidate

The owner subsequently tested Windows Display Scale, Application Zoom and Design
Zoom and reported no effect. Exact values were not supplied, so this is
owner-observed acceptance for the settings tried, not an exhaustive scale matrix.
Repeating every display-scale combination is no longer a blocker without a
specific regression. The owner also confirmed three concrete follow-ups: four
explicit Main/Drawer pin actions, Undo/Redo while the Better Packing window has
focus, and retention of connector corner snapping alongside pin snapping.

The 0.3.0 Packing, 0.1.8 Core and 0.1.1 Compact Connectors update addresses
those changes. The Input x and Output x labels are created only by the disposable
fixture driver; product hooks preserve native/custom/empty labels.

## Feedback update physical acceptance

After the other chat released Computer Use, the changed product code passed
focused physical checks in the same isolated exact-build Designer installation.
The test observer uses a background dispatcher callback below render/input and
explicit readiness guards instead of a fixed ten-tick startup delay. These are
test-driver changes only.

| Physical input | Observed result |
| --- | --- |
| Click Top Main, Bottom Main, Top Drawer and Bottom Drawer in the Better Packing window | Two connectors and an open-drawer Gain aligned to the requested native pin row in a nested Container. Recorded rows were respectively Y=105, 186, 105 and 224. All nine Control connections remained attached with matching rendered endpoints. |
| Ctrl+Z and Ctrl+Y with that window focused | Restored the exact pre-pack and post-pack position sets. The initial run exposed duplicate native Undo gestures in the shared chord matcher. Core now deduplicates those gestures, with regression coverage; the repeated physical test passed. |
| Wired Control connector drag | Reached the Gain main-body corner row at connector Y=180, the adjacent main-pin row at Y=181 (pin Y=186), and an exposed drawer-pin row at Y=209 (pin Y=214). |
| Wired AV Stream and MediaCast connector drags | Each reached corner Y=180 and adjacent pin-aligned Y=181. The retained native corners no longer disappear when pin snapping is active. |
| Fresh-process reopen of each separate Control, AV Stream and MediaCast output | All 19 endpoint-ID pairs, endpoint coordinates and domains matched the saved observations; all 19 rendered wire paths reached their pins. |

Source fixtures remained unchanged. Nine of ten recorded personal/vendor files
retained their hashes; the shared native Prefs.xml changed during concurrent
Designer activity and was preserved. The isolated profile was verified before
the fixture driver ran. No regular installed mod payload was changed by testing.

The physically exercised product payloads were:

| Payload | SHA-256 |
| --- | --- |
| Core 0.1.8 loader | `3ef0d9323d856ffa4b66ef62ff25c03a66a5033e7ea07113133b1259b458e89c` |
| Core 0.1.8 runtime | `9fd519e94bdb6bd712793eaf9fa954ec4ebcb7a55502cc8ba9ff02c95f7067f7` |
| Better Packing 0.3.0 | `a28df9cfa620f5b8c6637af23838d13e1d52d89088bb4d566df0c74ccec3a42c` |
| Compact Connectors 0.1.1 | `e0e51e06e6cca9dc23db27747d5d9e8fbce704268414264bfc3cfbb3fc250d38` |

Component Auto Width 0.1.0 was present unchanged. Earlier Control drag observations
preceded the Core duplicate-gesture fix; the four-button/history checks and both
video drag cases used the corrected Core payload above. Compact/Packing payloads
were identical throughout the follow-up. These results establish the exercised
Designer geometry and history paths, not device streaming or hardware behavior.

## Exercised release identity and limits

| Payload | SHA-256 |
| --- | --- |
| Designer DLL | `0e7150f8dc02e0159e019188780fb003ce025f5ec18c491609cf337d849bda60` |
| Compact Connectors 0.1.0 DLL | `09bcf491b5c7c80efa5c618f655ba1a88ec5193bd681d0ca27d29d37221d8dd6` |
| Better Packing 0.2.2 DLL | `f3e1adada5544ca20458162917faf4e69680a88713a42ff7c1cd4abb9376571f` |

Held-modifier Alt/copy and Ctrl-bypass gestures, mixed-selection drag domains,
native 12-unit connectors, broader component/pin counts and the wider editor/load-order
matrix remain outside this physical follow-up. Owner-observed scale tests above
supersede the former broad display-scale gate. Existing
[packing acceptance](https://github.com/joshua-small/qsys-better-packing/issues/2)
and [geometry integration tracking](https://github.com/joshua-small/qsys-automatic-geometry/issues/4)
retain that residual scope. This is Designer geometry/connectivity evidence,
not DSP or hardware validation. Whole-bundle Snake routing remains deferred.
