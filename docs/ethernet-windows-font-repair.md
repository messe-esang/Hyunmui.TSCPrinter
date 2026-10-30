# Ethernet Windows font raster isolation and ownership

Baseline: `029188420771a59273b10a7f2f6dc6ca6dc4649d`. This repair covers only `ethernet.windowsfont`, `ethernet.windowsfontunicode`, their private raster state, and the internal rendering/packet helpers. No other transport implementation or connection lifecycle changes.

## Defects and scope

The two font methods shared geometry, clipping offsets and bitmap bytes across every Ethernet instance. A negative-coordinate call permanently changed `iTop`/`imgShiftX`; even a later sequential call at a positive position inherited the crop. Concurrent renders could replace another call's geometry or bitmap between its header and payload. The old sender also declared a cropped width while sending the original row width from byte zero. These are one raster ownership/packet defect, so replacing only five static assignments would leave incorrect output.

Every render now owns its geometry and 2400×2400 monochrome buffer (300-byte stride, 720000 bytes). The packet builder copies each cropped row from the correct offset with the exact declared width. Negative X retains the legacy intended whole-byte clipping (`ceil(-x/8)`); negative Y clips rows. No static state survives the call. A complete mode-1 packet, including CRLF, is captured before transport. Short writes continue from the remaining offset; zero/invalid progress throws `IOException`.

The parent-approved inventory contains 44 findings in the two methods. The 16 S2696 shared-state sites and the following 26 related implementation sites are addressed together: S1117×4, S1215×6, S125×6, S1481×6, S1854×2, S3776×2. These counts identify source scope, not a claim that server analysis has closed them. The two S107 findings remain accepted debt because public signatures still have eight parameters.

## Native ownership and compatibility

Rendering is synchronous. A screen DC belongs to `GetDC`/`ReleaseDC`; the memory DC belongs to `CreateCompatibleDC`/`DeleteDC`. Owned font and bitmap objects are restored out of the memory DC before `DeleteObject`. Cleanup runs in `finally` on success and failure, before any transport callback. Failed restore attempts delete the owning memory DC before deleting still-selected owned objects. Cleanup checks failure results and attempts remaining releases; an existing rendering exception remains primary. An OS cleanup failure is reported, not presented as guaranteed successful resource release.

The borrowed `WHITE_BRUSH` stock object is used to clear the raster and is never deleted. Previously `FillRect` was called with a null brush and its failure ignored. Native creation, selection, measurement, drawing, colors, bitmap read length and cleanup results are checked. `Marshal.Release` calls on non-COM, already-deleted GDI handles are removed, as are per-row unmanaged copies and forced collections.

Contracts: [SelectObject restoration](https://learn.microsoft.com/en-us/windows/win32/api/wingdi/nf-wingdi-selectobject), [ReleaseDC and same-thread ownership](https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-releasedc), [Marshal.Release is COM reference counting](https://learn.microsoft.com/en-us/dotnet/api/system.runtime.interopservices.marshal.release?view=net-10.0).

Public method names, signatures, text entry-point selection, `lfEscapement`, font height/name and the legacy weight rule (`fontstyle < 2 ? 400 : 700`) are preserved. `fontunderline` remains ignored; italic/underline/strikeout remain zero. Angles 0/90/180/270 retain their origin calculations. Other angles are still accepted with the existing geometry fallback; this repair does not promise correct arbitrary-angle raster geometry.

Empty text, zero measured size or fully clipped output sends no bytes. A measured output dimension outside 0–2400 fails before any transport. Negative coordinates use wide intermediate arithmetic to avoid overflow. These explicit boundaries replace malformed/negative packet dimensions or out-of-bounds row reads.

The sender captures the instance's existing socket without opening any connection or changing timeout/close behavior. Different renderer instances no longer share raster data. Concurrent operations on the same socket, or send/close races, retain the existing unsupported transport behavior and need a separate concurrency contract.

## Verification and limits

`WindowsFontCommandTests` uses synthetic bitmap buffers and a fake GDI adapter, with no native functions, sockets or devices. Cases cover all four right-angle geometries, exact row bytes/CRLF, negative then normal calls, interleaved and concurrent buffers, full clipping, empty/zero/oversized dimensions, partial/invalid/throwing writers, legacy font/Unicode choices, failures at every acquired-handle stage, restore/deletion failures and same-thread cleanup. The same source is linked into `EventGate.Printer.Tests`.

After parent review of the adapter and cleanup code, the first isolated real-Windows GDI test run passed 10/10. It calls only the repaired internal adapter and an in-memory writer, never the old unsafe methods or a printer/socket. ANSI/Unicode entry-point variants at four right angles contain ink and background and have consistent packet dimensions. Thirty-two clipped-then-normal cycles reproduce identical normal bytes. After warmup, GDI resource-count growth stays within an eight-handle tolerance. Two overlapping renderers each repeat twelve times and match their serial packet bytes. Native tests are in a nonparallel `Windows font GDI` test collection and are also source-linked into the consumer tests; they require Windows GDI and an installed Windows font.

No physical printer, external network or arbitrary-angle visual print validation was performed. A raster containing ink does not by itself prove every requested glyph or printer firmware renders identically. NuGet publishing and external consumer rollout remain separate operations.

Rollback is a submodule commit revert or restoring the consumer pointer to the baseline above. That also restores shared raster state, inconsistent clipping and unsafe native cleanup, so use only as an explicit incident decision.

## Snapshot target mapping

These are the exact 44 supplied findings; no new server inventory was fetched during this repair.

| Key | Rule | Baseline line | Disposition |
| --- | --- | --- | --- |
| 3d01d90a-022f-45ac-9be9-4f7b42f2cc78 | csharpsquid:S107 | 1115 | Preserved public signature; accepted debt |
| a6d828f2-503c-4021-b326-1251e1c5e465 | csharpsquid:S3776 | 1115 | Source addressed; server confirmation pending |
| 53347251-3d1c-40d7-b055-04d292298423 | csharpsquid:S1854 | 1126 | Source addressed; server confirmation pending |
| 5f6fb907-c434-4b83-bd42-6c3755b9acfd | csharpsquid:S1481 | 1149 | Source addressed; server confirmation pending |
| f10a207a-ded4-4ec3-aae4-0514828b81f2 | csharpsquid:S1481 | 1150 | Source addressed; server confirmation pending |
| 0049d666-18a8-44f9-928d-afb7db3806cc | csharpsquid:S2696 | 1151 | Source addressed; server confirmation pending |
| 07a8800f-1b04-4fa1-b28e-4619e5bda262 | csharpsquid:S2696 | 1152 | Source addressed; server confirmation pending |
| 5f8b0530-4576-4f75-ab35-88611ca315ee | csharpsquid:S2696 | 1175 | Source addressed; server confirmation pending |
| 16ed88df-3b45-4b8b-858c-864fc4dbfe84 | csharpsquid:S2696 | 1176 | Source addressed; server confirmation pending |
| f36e2e47-ac47-41da-85ee-ce822b61a58b | csharpsquid:S125 | 1181 | Source addressed; server confirmation pending |
| def40d4e-c27d-4296-935e-6d3b88fdb2d8 | csharpsquid:S125 | 1185 | Source addressed; server confirmation pending |
| 2243e3e4-ca79-481d-b188-7e9b42b8c59f | csharpsquid:S125 | 1189 | Source addressed; server confirmation pending |
| 39c1c95a-cdfa-40fe-b23a-ff069eda1896 | csharpsquid:S2696 | 1205 | Source addressed; server confirmation pending |
| 0e1c48ac-bcb9-41cb-aa3c-c642741eecb8 | csharpsquid:S2696 | 1206 | Source addressed; server confirmation pending |
| 059687ea-93f9-4197-8e78-ae14c514416f | csharpsquid:S2696 | 1209 | Source addressed; server confirmation pending |
| dc63aca6-fc1d-484f-817c-7997fde8026c | csharpsquid:S2696 | 1214 | Source addressed; server confirmation pending |
| df0e0141-545c-4ac1-b02e-22d804594b15 | csharpsquid:S1215 | 1219 | Source addressed; server confirmation pending |
| 05a96cb7-3232-4785-a933-975b9fccb32f | csharpsquid:S1117 | 1221 | Source addressed; server confirmation pending |
| 4d451072-ade7-437a-bfdd-0f7c0fea0070 | csharpsquid:S1117 | 1223 | Source addressed; server confirmation pending |
| 86196839-81dd-4e25-8b78-dab9dfb040fd | csharpsquid:S1481 | 1228 | Source addressed; server confirmation pending |
| 692befa8-b850-4d87-827e-37460c8b1bea | csharpsquid:S1215 | 1236 | Source addressed; server confirmation pending |
| ae3639e1-7080-4dde-add7-52e564ec5b3b | csharpsquid:S1215 | 1243 | Source addressed; server confirmation pending |
| 9bfaae00-cc7a-4db6-aa4b-b09bf6cb594b | csharpsquid:S107 | 1246 | Preserved public signature; accepted debt |
| a6f7c3cf-0723-485c-889c-15191db5c21c | csharpsquid:S3776 | 1246 | Source addressed; server confirmation pending |
| a4f1a80b-6fdc-4f87-954c-59af0f1378ef | csharpsquid:S1854 | 1257 | Source addressed; server confirmation pending |
| fb41bb17-4437-41b3-9b0a-191e16ffa9ff | csharpsquid:S1481 | 1280 | Source addressed; server confirmation pending |
| c7e29279-241d-4083-9389-abbb0815e968 | csharpsquid:S1481 | 1281 | Source addressed; server confirmation pending |
| ac7c1171-8348-4362-987f-e84b169729c2 | csharpsquid:S2696 | 1282 | Source addressed; server confirmation pending |
| e76b7f41-ef7a-477d-9c40-18020a1a2188 | csharpsquid:S2696 | 1283 | Source addressed; server confirmation pending |
| 26a37e0f-5117-4be9-b3a6-9b6309f77754 | csharpsquid:S2696 | 1306 | Source addressed; server confirmation pending |
| a5a9c8f6-b7a9-48dd-ac49-d05995a006ad | csharpsquid:S2696 | 1307 | Source addressed; server confirmation pending |
| 3254cb23-231a-4328-b648-77c7ff0d001c | csharpsquid:S125 | 1312 | Source addressed; server confirmation pending |
| e45b7dd8-6796-4e08-827e-8a9541350036 | csharpsquid:S125 | 1316 | Source addressed; server confirmation pending |
| 51f3d6d5-7f2e-4895-9728-03f621512b01 | csharpsquid:S125 | 1320 | Source addressed; server confirmation pending |
| d9fca366-b78d-483e-8fab-143bbd007702 | csharpsquid:S2696 | 1336 | Source addressed; server confirmation pending |
| 18b1cd26-931c-4702-b7f3-c91940bdd504 | csharpsquid:S2696 | 1337 | Source addressed; server confirmation pending |
| 1d10bb60-4d5e-47cc-bbac-975756987222 | csharpsquid:S2696 | 1340 | Source addressed; server confirmation pending |
| 743117d3-31fc-4c77-af52-e2794ee0541e | csharpsquid:S2696 | 1345 | Source addressed; server confirmation pending |
| 5c06bf8d-6b0c-47c6-ba41-b19a5569abd2 | csharpsquid:S1215 | 1350 | Source addressed; server confirmation pending |
| 291cf656-3302-4843-adce-666ad71500e7 | csharpsquid:S1117 | 1352 | Source addressed; server confirmation pending |
| c24cf7b2-2235-4714-8c8e-c9946d1ca640 | csharpsquid:S1117 | 1354 | Source addressed; server confirmation pending |
| 2a1d7d4d-7a5c-40a9-801e-8279a8f5f85c | csharpsquid:S1481 | 1359 | Source addressed; server confirmation pending |
| 30f44c8c-60c5-48a5-8081-79bb3943b03b | csharpsquid:S1215 | 1367 | Source addressed; server confirmation pending |
| b4f5ee23-58c4-4111-ba88-67372c52b6d3 | csharpsquid:S1215 | 1374 | Source addressed; server confirmation pending |
