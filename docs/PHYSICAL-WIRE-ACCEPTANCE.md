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

## Exercised release identity and limits

| Payload | SHA-256 |
| --- | --- |
| Designer DLL | `0e7150f8dc02e0159e019188780fb003ce025f5ec18c491609cf337d849bda60` |
| Compact Connectors 0.1.0 DLL | `09bcf491b5c7c80efa5c618f655ba1a88ec5193bd681d0ca27d29d37221d8dd6` |
| Better Packing 0.2.2 DLL | `f3e1adada5544ca20458162917faf4e69680a88713a42ff7c1cd4abb9376571f` |

Other Windows display scales, physical drags with the newly attached non-audio
wires, mixed-selection drag domains, native 12-unit connectors, broader
component/pin counts and the wider editor/load-order matrix remain unverified. Existing
[packing acceptance](https://github.com/joshua-small/qsys-better-packing/issues/2)
and [geometry integration tracking](https://github.com/joshua-small/qsys-automatic-geometry/issues/4)
retain that residual scope. This is Designer geometry/connectivity evidence,
not DSP or hardware validation. Whole-bundle Snake routing remains deferred.
