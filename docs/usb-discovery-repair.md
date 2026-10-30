# USB parameterless discovery lifetime repair

Baseline: `740d5cb5e2ca838545e4dff99e04fb45770f0fed`. This bounded change repairs the discovery lifetime shared by `openport()` and `openport_overlapped()` only. Fifteen supplied candidates map to twelve expected source improvements and three retained debts; only server analysis confirms closure.

## Defect and repair

Both methods previously leaked their CoTaskMem detail buffer and HDEVINFO device-information list, performed duplicate unchecked enumeration, and continued after unchecked size probes. The overlapped variant converted the detail pointer to Int32 before adding its path offset, failing on ordinary high x64 addresses. Both accumulated unused VID/PID substrings into private static state and issued registry/HID GUID calls whose results they did not use.

A readonly injected opener now gives both instance methods the same native-adapter path. Device lists and detail buffers belong to a discovery call. Enumeration selects only interface index zero in GUID_DEVINTERFACE_USBPRINT, preserving existing selection. The size probe must fail with ERROR_INSUFFICIENT_BUFFER and supply a supported, even, representable size. Detail cbSize is 6 for Unicode x86 or 8 for x64; the path starts at byte offset 4 on both. The returned size must fit the allocated buffer. Unicode decoding searches only that bounded region and requires a nonempty terminated path. Buffer initialization prevents an unwritten zero from appearing to be a terminator. No VID/PID parsing is needed to pass the opaque path to CreateFile.

Every acquired detail buffer is freed with FreeCoTaskMem. Every acquired HDEVINFO is destroyed with SetupDiDestroyDeviceInfoList, never CloseHandle. Cleanup finishes before CreateFile, so a discovery-cleanup failure cannot orphan a newly opened device handle. Destroy failure throws TscException with its native error. A cleanup exception is attached to an existing primary exception's Data rather than replacing it; ordinary discovery return-false plus cleanup failure throws explicitly. Allocation/programming exceptions still propagate.

## Compatibility and limits

Public instance signatures and integer device-handle ABI remain unchanged. Discovery failures return false without replacing prior HidHandle. An attempted CreateFile returning -1 still publishes -1 and returns false. Successful open publishes its handle. Access0xC0000000, share3, disposition3, synchronous flags0 and overlapped flags0x40000080 are unchanged. Public native declarations and other open/read/trace methods are untouched. The native adapter uses explicit Unicode entry points, matching supported Windows CharSet.Auto behavior.

The shared printer-handle lifecycle remains legacy behavior: reopening may overwrite an existing handle, concurrent open/write/close is unsupported, and this repair does not reset a faulted USB writer. The two shared-handle S2696 sites remain explicit debt. The architecture literal in other discovery methods is also outside scope. Rollback is a normal revert of this discovery repair, restoring its documented leaks and x64 pointer defect; no dependency or configuration migration is required.

## Validation

31 fake-only discovery/publication tests cover both flags, cleanup-before-open, every discovery failure stage, invalid probe/returned sizes, absent/empty/out-of-bounds terminators, Unicode path bytes, x86/x64 layout, actual pointer-safe decoding, allocation and native cleanup failures, primary exception preservation, open failure/exception, interleaved discovery buffers, and legacy handle publication. No SetupAPI enumeration, physical USB, native device open, port or network access was executed. Tests allocate only private memory through Marshal and use fake native adapters. Real device discovery and open behavior still need separately authorized hardware QA. Existing USB write/font regression tests remain part of focused validation.

## Native contracts

- [SetupDiGetClassDevs ownership and INVALID_HANDLE_VALUE](https://learn.microsoft.com/en-us/windows/win32/api/setupapi/nf-setupapi-setupdigetclassdevsw)
- [Detail size probe, cbSize, returned size and opaque path](https://learn.microsoft.com/en-us/windows/win32/api/setupapi/nf-setupapi-setupdigetdeviceinterfacedetailw)
- [DestroyDeviceInfoList ownership](https://learn.microsoft.com/en-us/windows/win32/api/setupapi/nf-setupapi-setupdidestroydeviceinfolist)
- [FreeCoTaskMem matching allocation](https://learn.microsoft.com/en-us/dotnet/api/system.runtime.interopservices.marshal.freecotaskmem?view=net-10.0)

## Exact candidate mapping

| Key | Rule | Original line | Source plan |
| --- | --- | --- | --- |
| ec9ecb78-2471-4666-8c4d-ba6832dc6387 | csharpsquid:S2325 | 274 | Expected improvement; server confirmation pending |
| 5c090c2a-27a3-4dfb-923d-ec4db1bea8f3 | csharpsquid:S2696 | 279 | Expected improvement; server confirmation pending |
| b07499ad-4b64-45a7-a78b-f46bb7ec0790 | csharpsquid:S1192 | 299 | Literal also occurs in other discovery methods; outside scope |
| 2ef0e9be-0924-4cd2-9b5c-00e6c3f99fe0 | csharpsquid:S1643 | 314 | Expected improvement; server confirmation pending |
| caca3285-e406-41ab-9d0f-f5d709008bb5 | csharpsquid:S2696 | 314 | Expected improvement; server confirmation pending |
| 54ca02eb-6e64-4cf5-95b5-27acad4e1c4a | csharpsquid:S2696 | 316 | Expected improvement; server confirmation pending |
| d9974adb-2e5f-4968-9091-c362733e8f3d | csharpsquid:S1643 | 316 | Expected improvement; server confirmation pending |
| fdb02084-73c1-46cf-b3f9-0f882493a076 | csharpsquid:S2696 | 317 | Preserve shared device handle publication |
| 07e198cd-40a7-4c9a-bd8b-c904323d052e | csharpsquid:S2325 | 321 | Expected improvement; server confirmation pending |
| b0f61188-ff8c-4253-afc5-c4412794fc7b | csharpsquid:S2696 | 326 | Expected improvement; server confirmation pending |
| 48040c59-8515-48f8-9eb0-18b85ad5d3eb | csharpsquid:S2696 | 361 | Expected improvement; server confirmation pending |
| 79634754-b2ae-4b2e-95cf-a0333907368b | csharpsquid:S1643 | 361 | Expected improvement; server confirmation pending |
| 100ba2a4-f713-44dd-839d-76ba4454e38c | csharpsquid:S1643 | 363 | Expected improvement; server confirmation pending |
| 66738200-4c20-49d9-a7ca-a403b3dfdb3a | csharpsquid:S2696 | 363 | Expected improvement; server confirmation pending |
| cac8ac1c-b3ee-46fe-957f-958e0a55f449 | csharpsquid:S2696 | 364 | Preserve shared device handle publication |
