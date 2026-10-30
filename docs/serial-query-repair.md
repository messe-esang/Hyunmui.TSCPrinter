# Serial receive and query lifecycle repair

## Reason and scope

The legacy readers shared receive buffers, completion flags and background threads. Short framed reads appended one byte outside the received range; a full 1024-byte chunk could index past the buffer. The ACK reader inspected a different zero-filled buffer instead of the data just read. Query callers could return an earlier response or wait forever after a worker timeout. openport_read installed an event that drained incoming bytes into a private unread field, competing with query readers.

Replace this receive/query group with synchronous request-local results. Public signatures remain unchanged. Instance queries use a readonly SerialQuery dependency; public static wrappers construct a stateless helper. A query captures its SerialPort once and locks a gate keyed by that exact SerialPort object through ConditionalWeakTable. The lock spans request, preserved 300ms delay and reception. It does not cover general send/open/close operations or claim complete printer thread safety.

Removed the private unread event handler and its registration, along with the obsolete thread, flag, response strings and shared read buffer. Repository C# searches found no external callers of the legacy read helpers or accesses to read_data. External deployed SDK clients cannot be ruled out. The class's SMBStatus_usb calls the framed query and can now receive a TimeoutException.

## Explicit contracts

- Command byte sequences and WriteLine calls remain unchanged, including printerserial's CRLF in its string plus the port's configured WriteLine newline. The existing 300ms delay remains before response reading.
- General string queries return ASCII from the first nonempty read, just as the old readstream path did. A read timeout now throws TimeoutException to the synchronous caller instead of hanging or returning stale data. Partial unterminated framed data is not returned as success.
- printerstatus preserves the first response byte and read-timeout result zero. Request-write timeouts still propagate because only the receive delegate converts timeout to zero.
- Public legacy void readers retain their signatures, consume data, and swallow only TimeoutException. Other exceptions propagate to their caller.
- ENDLINE reception preserves direct byte-to-char mapping, including bytes above 127 and embedded NUL. The marker is excluded. A linear prefix matcher handles split and overlapping marker prefixes. Already consumed suffix bytes in the terminating read are discarded as before, not cached for a later query. ACK uses the original 256-byte read size; ENDLINE uses 1024; single-read status/text uses 256.
- A finite ReadTimeout becomes a cumulative receive budget starting after the 300ms delay. Each read receives the remaining time; repeated positive or zero-byte reads cannot renew the budget. A monotonic clock supplies elapsed time. Explicit SerialPort.InfiniteTimeout (-1) remains infinite. No arbitrary response-size limit is added.
- Original timeout settings are restored after reading. A restoration failure is surfaced after a successful operation, but does not mask an existing primary failure. Port closure/replacement outside query locks remains a separate lifecycle concern.

SerialPort.Read and ReadExisting contracts:
https://learn.microsoft.com/en-us/dotnet/api/system.io.ports.serialport.read?view=net-10.0-pp
https://learn.microsoft.com/en-us/dotnet/api/system.io.ports.serialport.readexisting?view=net-10.0-pp

## Validation

Initial fake-focused run: 33 passed. Parent review corrected status write-timeout propagation and ACK read size, and replaced mutable static test injection with readonly per-instance injection. Corrected focused run: 34 passed, first attempt.

Tests exercise actual public instance query routing via the internal constructor, plus the exact shared helper used by the stateless public static wrappers. There is no mutable global test override. They cover request bytes/newlines, 300ms timing calls, every ENDLINE split, overlapping prefixes, a 1024-byte read, byte characters, consumed suffix, successive independent queries, ACK, timeout/error contracts, cumulative finite deadlines, infinite timeout, setting restoration, captured port changes, and serialized same-port queries. Two unopened SerialPort objects verify the production adapter gate is keyed by port identity; neither is opened and no serial I/O executes.

Focused command: dotnet test Hyunmui.TSCPrinter.Tests/Hyunmui.TSCPrinter.Tests.csproj --artifacts-path <temporary-directory> --filter FullyQualifiedName~ComportQueryTests --verbosity minimal

Full consumer validation and final baseline integration remain pending parent review. No hardware, printer, network, or DB calls were made. Font, encoding/hex, bitmap and general command sending are outside this change.

## Impact and rollback

This deliberately replaces unbounded/stale receive behavior with explicit string-query timeout exceptions and complete request-local responses. Callers that relied on waiting indefinitely must handle TimeoutException. There is no public signature or device-configuration migration. Rollback is reverting the coherent submodule commit and consumer pointer/test link; doing so restores the documented hangs, receive corruption and discarded-event data risks.

## Assigned targets

34 supplied non-font targets, pending server resolution. Static-only/public naming/interoperability and already repaired font findings are excluded.

| Key | Rule | Original line |
| --- | --- | --- |
| 644bb5da-1fae-48a3-a9e6-7cef047066f4 | csharpsquid:S1450 | 23 |
| eae7d718-6058-4f82-ac27-c421b8a9840a | csharpsquid:S1450 | 46 |
| da16f8ba-1571-4c8e-b646-19e4519500ad | csharpsquid:S4487 | 47 |
| 24bfd560-1d3f-4e1a-8f5c-ccff524e137d | csharpsquid:S907 | 508 |
| 0f044f04-a6b9-48ae-adbd-87932f8a0a06 | csharpsquid:S1481 | 510 |
| 4fdffd59-4dba-437b-bd8a-150e22241cd5 | csharpsquid:S108 | 511 |
| 99ce1bbf-5538-40e6-8310-3eb6599cefb7 | csharpsquid:S1643 | 535 |
| ee8000c5-ded5-4201-af25-104fb9212943 | csharpsquid:S907 | 537 |
| c046cc91-bf11-40f8-b5ef-1a8e3b1b1030 | csharpsquid:S907 | 540 |
| e839105b-f9f2-48fb-9325-c0e82422fa62 | csharpsquid:S1481 | 542 |
| cfb09c8c-d239-42c7-98a4-cd6653f2f734 | csharpsquid:S108 | 543 |
| 12bf155c-9636-4283-ba1b-c516cdd829d2 | csharpsquid:S1643 | 565 |
| 8235ea13-b1a7-4125-93d2-ab002fa6fdc1 | csharpsquid:S907 | 567 |
| b64452f2-7862-4c93-ba8a-f0190f8b98dd | csharpsquid:S907 | 570 |
| 8c21ceba-6295-4f1a-b47f-6f7072a1e73d | csharpsquid:S1481 | 572 |
| 3a41574b-cc07-48c6-aaee-8c21ba91db83 | csharpsquid:S108 | 573 |
| 520ae40c-218a-4f9e-afe7-859a86595c21 | csharpsquid:S2696 | 595 |
| 3d10ab4c-e494-4987-ab41-a503164a5db2 | csharpsquid:S907 | 598 |
| 88e1a239-b197-4aea-9319-496b269d24a0 | csharpsquid:S1481 | 600 |
| 0b6cb057-4a11-45d3-8f0a-5ba2ca9d2238 | csharpsquid:S108 | 601 |
| b5ee39dd-b82b-4fad-b0f6-807dfadad76e | csharpsquid:S1481 | 608 |
| 0aa4dbb0-10cd-4073-be99-5af426f81d77 | csharpsquid:S2696 | 617 |
| 81a3d034-cf8f-462c-8a23-cae0703d66ba | csharpsquid:S1481 | 627 |
| 15bb135f-8864-4bca-95fe-b0cf6e73df8a | csharpsquid:S2696 | 632 |
| c116361c-781a-490f-bdcf-a06646aa643c | csharpsquid:S1481 | 642 |
| 08e65f9a-1e78-49b4-b6b8-501c4b5df34f | csharpsquid:S2696 | 647 |
| e0c909a8-72ce-4f14-bb0a-6f6d257afd2c | csharpsquid:S1481 | 657 |
| c9295409-01a6-4759-b5e0-6a2fb844bea6 | csharpsquid:S2696 | 662 |
| 306a1dc5-c8d3-4822-ad8c-ebfdf1a69cf6 | csharpsquid:S1481 | 672 |
| 45ffd577-7027-48da-b837-28e1262e7e93 | csharpsquid:S2696 | 677 |
| df1c621e-2c9e-404a-bf14-ed4b292728f0 | csharpsquid:S1481 | 687 |
| 8ee64de7-3288-4d68-908e-f5608cdb9713 | csharpsquid:S2696 | 692 |
| 683e7501-828d-4e69-8816-8864810afe3f | csharpsquid:S1481 | 702 |
| 4a839fe3-37e6-4404-b8c5-675c3a82f83e | csharpsquid:S2696 | 707 |
