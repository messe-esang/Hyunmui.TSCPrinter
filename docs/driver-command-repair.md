# Driver raw command ownership and write completion

## Scope and contracts

The ten raw driver command entry points share per-call managed payload ownership, a captured printer handle, one StartPagePrinter call, and a checked partial-write loop. Port 2 string commands now select hPrinter2. sendbinary delegates to the byte-array command overload. Public names, overloads, return types, font methods, and open/close job/page lifecycle remain unchanged.

The command buffer is cloned before starting a page. Each successful partial write advances exactly its returned count; the next native call receives only the remaining bytes. A false native result or zero/negative/oversized progress returns failure immediately. The payload is completed before CRLF is attempted; failed prefixes are never replayed, and no CRLF follows a payload failure. CRLF failure is also reported. Boolean commands return false; string NOCRLF returns -1 (success remains 1). Empty payloads start the page and only write an explicitly requested CRLF.

Removed newly unused private command counts, pointers, CRLF string, and pointer-based WritePrinter declaration. No unmanaged command allocation remains. The static dwWritten and CRLF_byte are still used by out-of-scope operations and remain; shared hPrinter slots and their open/close concurrency are separate debt. This does not guarantee concurrent command/job atomicity on the same physical printer; it removes shared temporary-buffer corruption and captures the selected handle once.

## ANSI and protocol compatibility

DriverAnsiEncoding queries GetACP once, measures then fills a managed array using WideCharToMultiByte with the explicit UTF-16 character count. The native byte count, not string.Length, controls transmission. Embedded NUL and the suffix after it are retained; no terminator is sent. Empty strings do not invoke conversion APIs. Nonpositive native results throw Win32Exception before starting a page; an inconsistent positive count is rejected.

For the active ANSI code page the converter uses WC_NO_BEST_FIT_CHARS (0x400), matching .NET 10 Marshal.StringToCoTaskMemAnsi behavior. An active UTF-8 code page uses flags 0, because Windows rejects the best-fit flag for UTF-8; UTF-8 has no best-fit mapping. GetACP's resolved page is used for both passes. There is no dependency addition, global provider registration, Encoding.Default substitution, or culture-based code-page guess. This deliberately fixes DBCS truncation caused by the old character-count-as-byte-count bug.

References:
- https://github.com/dotnet/runtime/blob/v10.0.0/src/libraries/System.Private.CoreLib/src/System/Runtime/InteropServices/Marshal.cs
- https://github.com/dotnet/runtime/blob/v10.0.0/src/libraries/System.Private.CoreLib/src/System/Runtime/InteropServices/Marshal.Windows.cs
- https://learn.microsoft.com/en-us/windows/win32/api/stringapiset/nf-stringapiset-widechartomultibyte

UTF-8 remains UTF-8. GB2312 and Big5 retain Encoding.GetEncoding and existing missing-provider exceptions. string[] remains unchanged: each nonempty element sends Encoding.Default bytes then a separate CRLF command, producing two StartPage calls and three CRLF sequences. Hex overload behavior, including the non-transmitting string[] overload, is separate debt. No overload/static-only rearrangement is targeted.

## Validation and risk

- First fake-only run found one test compilation error (Func<bool> passed to Assert.Throws instead of Action); corrected the fixture. Next fake run: 54 passed.
- Windows conversion-only comparison: 5 passed for ASCII, non-ASCII, embedded-NUL suffix, NUL-only, and empty strings. Expected bytes are measured from scoped Marshal.StringToCoTaskMemAnsi allocations freed in finally. No GDI, spooler, printer, network, or DB API executes in these tests.
- After removal of newly unused private declarations and the exact -1 return assertion: 73 passed, comprising 55 fake command/conversion cases, 5 native conversion-only cases, and the 13 existing fake-GDI font cases.
- Parent review added a direct production selector regression: 1 passed. It sets unique sentinel values for the six private handles, invokes the actual selector for ports 0 through 5 and invalid ports -1/6, explicitly distinguishes port 2 from port 1, and restores every handle in finally. Its xUnit collection disables parallelization; no native function executes. Only this affected test was rerun after adding it.
- dotnet format whitespace executed for the three changed production files and two new test files. One workspace-load warning was emitted; compilation and tests succeeded.
- Focused command: dotnet test Hyunmui.TSCPrinter.Tests/Hyunmui.TSCPrinter.Tests.csproj --artifacts-path <temporary-directory> --filter "FullyQualifiedName~DriverCommandTests|FullyQualifiedName~DriverAnsiEncodingTests|FullyQualifiedName~DriverAnsiNativeTests|FullyQualifiedName~DriverWindowsFontTests" --verbosity minimal
- Full consumer hook and server analysis are pending parent review. No device run or publication was performed.

Rollback is one coherent submodule commit plus its consumer reference/test links when integrated. Rolling back restores the previous buffer leak, port-2 misrouting, DBCS truncation, and misleading success results. No printer job-state migration is involved.

## Assigned source targets

35 targets: 33 static temporary-state writes, one duplicated wrong-port branch, one duplicate byte-array sender. Resolution remains pending server analysis. The other 18 assigned findings (12 static-only, 3 overload ordering, 3 hex concatenations) are not targeted.

| Key | Rule | Original line |
| --- | --- | --- |
| b30b3f04-2a0d-48af-9c72-436f5006b3d9 | csharpsquid:S2696 | 302 |
| 63ff02d4-de11-4450-be92-6134e3bf2c4e | csharpsquid:S2696 | 303 |
| 180e1761-da8d-453d-a629-30d789521880 | csharpsquid:S2696 | 304 |
| 0fb65d2c-0e56-4c7d-9989-f190029054b7 | csharpsquid:S2696 | 305 |
| 8878a115-5c24-4c2c-b536-6df7dfe08bcd | csharpsquid:S2696 | 374 |
| cbd04b10-4d39-4862-a660-3cf39cd943e3 | csharpsquid:S2696 | 375 |
| 65dea00b-fddf-4c9a-8b90-b0cf74c6af22 | csharpsquid:S2696 | 376 |
| 9c488d32-3cb8-4196-addc-334f7077bc48 | csharpsquid:S2696 | 390 |
| 546d8638-1eec-4207-b8fc-719bb0070d28 | csharpsquid:S2696 | 391 |
| 95c975ba-f990-4f9f-92b1-ac3c0e2b84d0 | csharpsquid:S2696 | 392 |
| 784f1f20-2e55-4729-a76d-4f7e3fd182f5 | csharpsquid:S2696 | 406 |
| fc62ae5b-c8ca-4aa5-852a-c4ad18045a69 | csharpsquid:S2696 | 407 |
| 63647a3b-e0b0-4b39-a5df-2c1b58d33638 | csharpsquid:S2696 | 408 |
| 54689b81-6542-42d9-8eef-cc95ffbe6522 | csharpsquid:S2696 | 424 |
| 22ed2e1e-2ca8-4264-8ce9-de95c9eba014 | csharpsquid:S2696 | 425 |
| 6c881c52-6e80-463a-9752-11eeeade63b0 | csharpsquid:S2696 | 426 |
| 16dba867-923c-4e5e-aa0f-96f4ed7730b2 | csharpsquid:S2696 | 427 |
| 70cb79fa-188a-486f-a0d1-85326d2b4080 | csharpsquid:S1871 | 436 |
| b3b7c0f4-5e8a-4821-afc9-c119d2055ca5 | csharpsquid:S2696 | 495 |
| dd979e03-58fc-4ba3-be49-0b6222828940 | csharpsquid:S2696 | 496 |
| 3d0f2317-d97f-4c8c-bf05-7652e75a7f04 | csharpsquid:S2696 | 497 |
| 0ae50d6b-a645-4b2e-acf1-8198ff680a69 | csharpsquid:S2696 | 498 |
| 847f2b32-fe5c-4b53-9586-ccd54b669ba4 | csharpsquid:S2696 | 510 |
| 0578087d-83ec-47a4-b52e-f82467f5747a | csharpsquid:S2696 | 511 |
| 88f80e90-24a6-4286-96bf-c1e7e95754a7 | csharpsquid:S2696 | 512 |
| 6456e5d3-fb1e-4b36-9c10-34a8c6de265e | csharpsquid:S4144 | 520 |
| 69f35cb0-81d5-4f2b-aecf-35304d3e37ff | csharpsquid:S2696 | 522 |
| 62bf3a46-1780-49d6-9b0a-2c6bc65418ed | csharpsquid:S2696 | 523 |
| 97a2d04d-be9c-44b0-908c-2a8fa3373b59 | csharpsquid:S2696 | 524 |
| 2733c5ae-b7bf-4d97-b914-2663e0626fc2 | csharpsquid:S2696 | 537 |
| 8ee9cd00-669e-46ed-9a25-eb66e49be084 | csharpsquid:S2696 | 538 |
| 91b83742-f856-415d-b7d1-eb64af9e2fd1 | csharpsquid:S2696 | 539 |
| 70d0052f-f0fe-440e-b2ec-1d19f9771eec | csharpsquid:S2696 | 597 |
| 5c40d52b-e61d-48d3-9d47-2893ce16b3cc | csharpsquid:S2696 | 598 |
| c05151d5-b06f-4798-8335-215f9d3d4846 | csharpsquid:S2696 | 599 |
