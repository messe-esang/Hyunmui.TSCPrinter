# LPT Windows font raster repair

Baseline: `39c40461d01d94aac2016fc99815cb3404844b16`. Only the two LPT Windows-font methods, their font-only shared state and the per-instance native test seam change. The committed shared renderer and other transports remain unchanged.

## Behavior and ownership

Like the Ethernet defect, LPT font calls shared bitmap bytes, coordinates and persistent clipping offsets. A negative-coordinate call affected later calls and other instances; concurrent rendering could mix the data. Headers declared clipped row widths while the writer sent original-width bytes starting at column zero. The font functions also released GDI objects using COM `Marshal.Release`, omitted `ReleaseDC`, and allocated unmanaged copies and forced collection inside the row loop.

Both public methods now create a per-call request and use the reviewed `WindowsFontCommand`/`WindowsFontRenderer` and stateless native adapter. The instance `lptstream` is captured before rendering. The write callback returns the requested count only after `FileStream.Write(packet, offset, count)` returns successfully. Exceptions propagate; it never reports success first or disposes, flushes, opens or closes the stream. The packet has one header, exact cropped rows and one CRLF, without the extra line endings of unrelated `sendcommand` helpers. A single complete packet replaces the old multiple stream calls, preserving successful byte-stream contents while removing malformed clipping.

Public eight-argument signatures, the public parameterless constructor, native public declarations and open/close methods are preserved. Eleven private GDI declarations made unused by this repair are removed; preexisting unused declarations remain outside scope. An internal constructor injects a file stream and native adapter for tests; normal instances use the same reviewed GDI adapter. Font name/height, weight rule, ignored underline argument, text entry-point distinction, four right-angle geometry calculations and arbitrary-angle fallback remain as documented in [the shared repair](ethernet-windows-font-repair.md).

Shared behavior includes no output for empty/zero/fully clipped rasters, rejection of measured dimensions beyond the fixed canvas before writing, and synchronous native cleanup before transport. Same-instance open/close races and actual port behavior remain outside this repair.

## Verification and rollback

Fourteen focused tests call the actual public LPT font methods with fake GDI and a memory-capturing `FileStream` backed by a disposable temporary file. No native GDI, port, printer or network call occurs. They verify both method variants at all four right angles, exact packet bytes, no extra CRLF, negative-then-normal calls, independent instances, stream capture during rendering, native cleanup before a writer exception, preservation of `ObjectDisposedException`, stream ownership, empty and fully clipped output. Existing shared renderer coverage supplies detailed GDI failure and packet-boundary tests.

The source subset has 38 findings: 36 implementation targets and two retained S107 public-signature findings. Server analysis must confirm closure; the source mapping below is a plan, not a server result.

Rollback is this submodule commit's revert or restoring the consumer pointer to the baseline. That restores the malformed clipping, shared raster state and unsafe native cleanup. Physical LPT printer output, driver buffering and port timing are not validated here.

## Deferred USB investigation (no USB code changes)

The inspected USB font subset contains the same geometry/GDI defects, but its writer needs a separate transport design. `usb.WriteToStream(byte[])` around baseline line 1311 and the private `(byte[], int)` overload around 1371 call overlapped `WriteFile` and return status `1`, not a byte count. Passing this return value to `WindowsFontCommand.Send` would repeatedly resend overlapping data. The private row overload throws `WAIT_OBJECT_0` even after an overlapped completion; both forms have unchecked partial-write/completion paths and event-handle cleanup gaps on exceptions. Managed buffer/OVERLAPPED lifetime must remain valid until completion or confirmed cancellation.

USB uses raw `CreateFile` device handles, not spooler jobs. Handles are shared statics and some open paths use `FILE_FLAG_OVERLAPPED`; others do not. A correct follow-up must explicitly define synchronous/overlapped completion, actual byte counts, pending versus terminal errors, timeout cancellation and draining, event ownership, pinned buffer/OVERLAPPED lifetime and handle capture. Required no-device tests include immediate/partial/pending writes, completion failure, timeout/cancellation races and cleanup at each failure stage. Real device validation must be separately authorized after that design. No packet parsing/re-splitting workaround or USB writer change is included in this LPT commit.

## Supplied target mapping

| Key | Rule | Baseline line | Disposition |
| --- | --- | --- | --- |
| 5e821352-8cd6-4a81-8270-3fdadb730a80 | csharpsquid:S107 | 430 | Preserved signature; accepted debt |
| d450650c-48e0-4e82-97ba-6b4a7fc36293 | csharpsquid:S3776 | 430 | Source addressed; server confirmation pending |
| 2b7d5f4e-1e53-4663-9349-823d1ece6db5 | csharpsquid:S1854 | 441 | Source addressed; server confirmation pending |
| a4a8d968-96fb-4648-9791-41f1b43d6df4 | csharpsquid:S1481 | 464 | Source addressed; server confirmation pending |
| 6f227ef8-2fe4-4484-a229-bad29c8db0b9 | csharpsquid:S1481 | 465 | Source addressed; server confirmation pending |
| 0ecd8ac3-4a96-445d-985a-171beadd272c | csharpsquid:S2696 | 466 | Source addressed; server confirmation pending |
| a69cd05c-3737-4180-b33d-9e1a07f0c752 | csharpsquid:S2696 | 467 | Source addressed; server confirmation pending |
| bc2452fc-a077-4113-b822-fd804b170e70 | csharpsquid:S2696 | 490 | Source addressed; server confirmation pending |
| 0afea20b-2511-46bb-aec1-1e084082f270 | csharpsquid:S2696 | 491 | Source addressed; server confirmation pending |
| 5e4d5373-e7dc-489b-b115-e895da76fb96 | csharpsquid:S2696 | 520 | Source addressed; server confirmation pending |
| 1390674b-789a-4a1e-8448-be527604a66b | csharpsquid:S2696 | 521 | Source addressed; server confirmation pending |
| a02022ed-b286-4bcb-8838-ff5974761c20 | csharpsquid:S2696 | 524 | Source addressed; server confirmation pending |
| 92c27105-1018-4344-9b71-ba54665de77b | csharpsquid:S2696 | 529 | Source addressed; server confirmation pending |
| 298789e3-9a68-4c02-aca8-0152783bf01b | csharpsquid:S1215 | 534 | Source addressed; server confirmation pending |
| d5a55ed8-a416-4bb8-9819-d73a4f857a66 | csharpsquid:S1117 | 536 | Source addressed; server confirmation pending |
| 667e2375-e515-43b5-af9c-5f5090e4c34c | csharpsquid:S1117 | 538 | Source addressed; server confirmation pending |
| 353af736-b516-43f3-a450-469f7c53890d | csharpsquid:S1481 | 543 | Source addressed; server confirmation pending |
| 7d71abed-d6b9-4351-816d-b240b7d8fcc8 | csharpsquid:S1215 | 551 | Source addressed; server confirmation pending |
| 3bd81fdd-6c61-47a8-891f-c2d4c003fcf7 | csharpsquid:S1215 | 558 | Source addressed; server confirmation pending |
| 44a122e5-9dca-4ed7-957c-613d7dfca6a4 | csharpsquid:S3776 | 561 | Source addressed; server confirmation pending |
| e12c3487-d6c8-4ced-8101-f71ffb8529b3 | csharpsquid:S107 | 561 | Preserved signature; accepted debt |
| 7bf6225b-912a-4bb1-bb0a-4e3b9441a93d | csharpsquid:S1854 | 572 | Source addressed; server confirmation pending |
| d6d19ca9-337e-488f-bc0c-a22ca59c0835 | csharpsquid:S1481 | 595 | Source addressed; server confirmation pending |
| 085b6149-cebd-41cb-aa91-456e9b91cf2a | csharpsquid:S1481 | 596 | Source addressed; server confirmation pending |
| 9e30a317-4d69-4454-afd7-9a0ba0bc6e98 | csharpsquid:S2696 | 597 | Source addressed; server confirmation pending |
| 8131a2bd-ffdd-4376-a428-2cbc63740a7d | csharpsquid:S2696 | 598 | Source addressed; server confirmation pending |
| dd3a6bca-8038-4521-a32c-16680486ac9b | csharpsquid:S2696 | 621 | Source addressed; server confirmation pending |
| 7af221f7-76a0-4ce0-af6a-c8497627aec5 | csharpsquid:S2696 | 622 | Source addressed; server confirmation pending |
| 84061d64-733a-4852-8198-eb38e7b79f62 | csharpsquid:S2696 | 651 | Source addressed; server confirmation pending |
| ba1c245a-8231-41f1-b300-8edbfcf7c393 | csharpsquid:S2696 | 652 | Source addressed; server confirmation pending |
| 7e6b3f36-8683-44e1-b57f-19d01c835919 | csharpsquid:S2696 | 655 | Source addressed; server confirmation pending |
| c0fc5101-4408-46d7-8391-76d43576ac8f | csharpsquid:S2696 | 660 | Source addressed; server confirmation pending |
| 67da74bd-abd8-4df9-b637-08baa3645b61 | csharpsquid:S1215 | 665 | Source addressed; server confirmation pending |
| 633101a0-ed31-4a6d-93d7-855cb57471ed | csharpsquid:S1117 | 667 | Source addressed; server confirmation pending |
| e6f3a079-f7e1-4d2f-8f51-b0e1c2d4c963 | csharpsquid:S1117 | 669 | Source addressed; server confirmation pending |
| ff66d3e9-5f45-4a7c-9030-a4e7c512709e | csharpsquid:S1481 | 674 | Source addressed; server confirmation pending |
| 10c45e86-6fa2-4965-b7d2-e05fac697eee | csharpsquid:S1215 | 682 | Source addressed; server confirmation pending |
| f291b4d6-ac6c-480b-98b9-6604e3afbf51 | csharpsquid:S1215 | 689 | Source addressed; server confirmation pending |
