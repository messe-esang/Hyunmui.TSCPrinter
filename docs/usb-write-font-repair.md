# USB raw write and Windows font repair

Baseline: `6f47c6f0ee0647b3cc973563511db8fd66649f12`. This coherent change addresses the transport safety prerequisite and the two font methods together. The 42 supplied issues comprise 40 expected source improvements and two accepted S107 public-signature debts. Only server analysis can confirm closure.

## Behavior and compatibility

The public `WriteToStream` overloads still return status **1** after full success. The internal writer returns actual bytes; partial writes continue from the actual completed offset. Each call captures its existing integer handle once. Immediate `WriteFile` success uses its output count, including handles opened synchronously. Only ERROR_IO_PENDING enters asynchronous waiting. Immediate failure, failed completion and zero/oversized counts now report errors rather than silently truncate output.

Input bytes are copied once, then pinned for each request. Each request has pointer-sized unmanaged OVERLAPPED storage and its own initially unsignaled manual-reset event. Pending storage remains valid until that private event signals and completion returns a terminal result. Cancellation uses the same captured handle and specific OVERLAPPED. ERROR_NOT_FOUND is a cancellation race, not completion proof.

The existing two-second timeout is a cancellation threshold, not a maximum call duration. The writer requests cancellation and drains completion before cleanup; draining can exceed two seconds. It preserves `TscException("WAIT_TIMEOUT")`. Wait errors likewise preserve the original operation/error. Secondary drain and event-close errors are attached to Exception.Data rather than replacing a primary failure. Event-close failure after otherwise successful output is reported. No device handle is closed by the writer.

Concurrent device close remains unsupported. If terminal completion cannot be proved (failed drain, adapter exception, or signal with ERROR_IO_INCOMPLETE), the writer retains the pin, event and OVERLAPPED in a thread-safe quarantine and attaches `UsbWriteCompletion` diagnostics. That writer rejects subsequent calls and partial-loop continuations before new allocation until process restart. Already in-flight calls may finish or add their own uncertain request. There is no background reaper, speculative free or automatic retry. This rare fail-closed containment trades retained resources for prevention of native use-after-free; ordinary completed cancellation releases everything normally. A driver that never completes cancellation can block the draining call indefinitely.

Font methods reuse the reviewed WindowsFontCommand/WindowsFontGdi implementation: per-call 2400x2400 monochrome geometry/buffer, exact clipped row payload/header, one BITMAP packet with mode 1 and CRLF, native cleanup before transport. ANSI/Unicode, four principal rotations, legacy arbitrary-rotation fallback and ignored underline/style behavior remain consistent with the shared renderer. Empty/fully clipped output writes nothing, oversized measured geometry is rejected before transport. Public eight-argument signatures and public GDI interop are preserved; only newly unused private font declarations and the broken private row writer are removed. Global port open/close, read paths, other transports and public handle ABI are unchanged.

## Evidence and limitations

Focused fake-only regression suite: 34 tests (22 raw writer, 12 public USB font cases). Tests cover immediate and pending partial writes, captured handles, caller buffer mutation, forced GC while pending, pointer-width layout, immediate/completion/count failures, timeout and cancellation races, wait/drain exceptions, inconsistent completion, quarantine lifetime, subsequent-write rejection, in-flight partial-loop fault rejection, event allocation/close failures, ANSI/Unicode at all four rotations, negative-then-normal rendering and independent instances, native cleanup before transport failure, empty/clipped output and public status shape.

No native USB call, physical printer, port or network I/O was performed. Native adapter declarations were reviewed, but real USB-driver cancellation/timing and printed output require separate authorized hardware QA. Existing shared renderer native evidence is recorded in `ethernet-windows-font-repair.md` and `windows-font-platform-followup.md`. Rollback is a normal revert of this USB repair; it restores the documented unsafe legacy write/font paths, so reverting is not a claim those paths are safe.

Native contract references:

- [WriteFile buffer and asynchronous lifetime](https://learn.microsoft.com/en-us/windows/win32/api/fileapi/nf-fileapi-writefile)
- [GetOverlappedResult captured handle and completion event](https://learn.microsoft.com/en-us/windows/win32/api/ioapiset/nf-ioapiset-getoverlappedresult)
- [CancelIoEx request-specific cancellation and required completion](https://learn.microsoft.com/en-us/windows/win32/api/ioapiset/nf-ioapiset-cancelioex)
- [OVERLAPPED layout and manual-reset event](https://learn.microsoft.com/en-us/windows/win32/api/minwinbase/ns-minwinbase-overlapped)

## Owned issue mapping

| Key | Rule | Original line | Source status |
| --- | --- | --- | --- |
| d4d793f7-4eb7-44d6-b5bb-278069835e01 | csharpsquid:S107 | 1561 | Accepted debt: preserve public API |
| f3a735e9-8368-49ec-bee3-50b649d42802 | csharpsquid:S3776 | 1561 | Expected improvement; server confirmation pending |
| d8c1a347-5518-4a40-af85-2066c5e73cb3 | csharpsquid:S1854 | 1572 | Expected improvement; server confirmation pending |
| 2f95dad6-511e-4662-96c7-0d0c2d377094 | csharpsquid:S1481 | 1595 | Expected improvement; server confirmation pending |
| d340534d-8373-40a6-a929-48e3d037225e | csharpsquid:S1481 | 1596 | Expected improvement; server confirmation pending |
| 463fdddf-02ef-42a4-a900-ae2587c71d6c | csharpsquid:S2696 | 1597 | Expected improvement; server confirmation pending |
| a902991a-f8c2-45c0-9e67-70d7e831bbee | csharpsquid:S2696 | 1598 | Expected improvement; server confirmation pending |
| fca52d89-af14-4fe5-a500-180cd1b0fb3b | csharpsquid:S2696 | 1621 | Expected improvement; server confirmation pending |
| 9f20ce5a-b53d-49e4-b71c-481148a5ee12 | csharpsquid:S2696 | 1622 | Expected improvement; server confirmation pending |
| 36485e43-65c7-4403-914d-971f2510c37e | csharpsquid:S2696 | 1651 | Expected improvement; server confirmation pending |
| ba07f6ed-b7ce-48fd-8e5f-e8d1e68417f8 | csharpsquid:S2696 | 1652 | Expected improvement; server confirmation pending |
| e8e08317-80db-4ced-9e97-8baac810effc | csharpsquid:S2696 | 1655 | Expected improvement; server confirmation pending |
| 728f986e-6010-4b78-86f1-fc4bf33d0f2a | csharpsquid:S2696 | 1660 | Expected improvement; server confirmation pending |
| b7f92ca0-0c82-4cb0-bfc3-8ad5f5f8d795 | csharpsquid:S1215 | 1664 | Expected improvement; server confirmation pending |
| 3d46b703-8937-40c0-a7bf-16623b025994 | csharpsquid:S1117 | 1666 | Expected improvement; server confirmation pending |
| cf5b81eb-ee6e-4b6f-8a96-18314ce5b165 | csharpsquid:S1117 | 1668 | Expected improvement; server confirmation pending |
| b7261a91-17b6-4997-b22f-d9275d5d66a6 | csharpsquid:S1481 | 1673 | Expected improvement; server confirmation pending |
| 3f4d78ad-95ca-4584-9e97-19805c88473e | csharpsquid:S1215 | 1681 | Expected improvement; server confirmation pending |
| f7eb320a-31e1-4ea4-960f-5ad3ec33081f | csharpsquid:S1215 | 1688 | Expected improvement; server confirmation pending |
| 25b233bd-6bfa-48a1-a1ca-7281527b1b2d | csharpsquid:S3776 | 1691 | Expected improvement; server confirmation pending |
| 73ccf724-6982-4180-8f05-0ea910118ba6 | csharpsquid:S107 | 1691 | Accepted debt: preserve public API |
| be175ab5-f7a0-43b5-9eaa-d14305a6cbe7 | csharpsquid:S1854 | 1702 | Expected improvement; server confirmation pending |
| 1b503942-de13-41b1-af22-d9ff05e84cc3 | csharpsquid:S1481 | 1725 | Expected improvement; server confirmation pending |
| c33a1f3f-96bf-4c6b-ab8b-e69b4e7e2f61 | csharpsquid:S1481 | 1726 | Expected improvement; server confirmation pending |
| ca98d855-dfd9-4d07-8a02-028b048deb72 | csharpsquid:S2696 | 1727 | Expected improvement; server confirmation pending |
| 2dc51edb-98d5-4b3e-bc05-6110c13493c3 | csharpsquid:S2696 | 1728 | Expected improvement; server confirmation pending |
| fac7f046-82fb-4c38-8952-c7aca4f06dcd | csharpsquid:S2696 | 1751 | Expected improvement; server confirmation pending |
| e383e530-447d-44fd-b505-4b526daf41ab | csharpsquid:S2696 | 1752 | Expected improvement; server confirmation pending |
| d81310b8-5d33-4609-a32a-64de4c66f29f | csharpsquid:S2696 | 1781 | Expected improvement; server confirmation pending |
| 24ceb813-46eb-4620-8da8-6b6999896341 | csharpsquid:S2696 | 1782 | Expected improvement; server confirmation pending |
| 0dad77ce-fc35-42e5-88af-2690eddd741a | csharpsquid:S2696 | 1785 | Expected improvement; server confirmation pending |
| 5890bd0a-868d-4beb-a49d-a6555859f427 | csharpsquid:S2696 | 1790 | Expected improvement; server confirmation pending |
| 84437d2f-0fe5-4775-846a-6980d6f06305 | csharpsquid:S1215 | 1794 | Expected improvement; server confirmation pending |
| 7e6ab550-e146-486d-8c9c-7d89b8a0cae6 | csharpsquid:S1117 | 1796 | Expected improvement; server confirmation pending |
| d9d2d49c-269a-48e5-9f18-7b762f8e3765 | csharpsquid:S1117 | 1798 | Expected improvement; server confirmation pending |
| f497eb76-a050-462c-9fc1-e9308d6a12d4 | csharpsquid:S1481 | 1803 | Expected improvement; server confirmation pending |
| f5209ef5-6d83-4b9e-8098-27d55802ef2e | csharpsquid:S1215 | 1811 | Expected improvement; server confirmation pending |
| 1531ab33-a07c-4caa-a30c-ad9d4b313f96 | csharpsquid:S1215 | 1818 | Expected improvement; server confirmation pending |
| d3d359e4-68c8-4025-be35-e2fcc2527d1f | csharpsquid:S1116 | 1336 | Expected improvement; server confirmation pending |
| e721d2bb-6eb6-4b3f-bd5f-5c8ffb9222d0 | csharpsquid:S1116 | 1366 | Expected improvement; server confirmation pending |
| de320b17-f384-417e-acde-80e27820f19d | csharpsquid:S3241 | 1371 | Expected improvement; server confirmation pending |
| f98ac0c4-8cde-4dbe-be0f-f5ab6394c64c | csharpsquid:S1116 | 1389 | Expected improvement; server confirmation pending |
