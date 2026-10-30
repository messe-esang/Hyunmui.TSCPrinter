# Driver Windows-font repair

Baseline helper commit: `39c40461d01d94aac2016fc99815cb3404844b16`. This change reuses its WindowsFontCommand/WindowsFontRenderer and GDI adapter; shared helpers and other transport implementations are unchanged.

## Contract and ownership

Both public eight-parameter methods and the public parameterless constructor remain available. Font height, weight threshold, face, rotation fallback, ANSI/Unicode choice, ignored underline parameter, BITMAP mode 1, row order, and one terminal CRLF are retained. For previously valid, non-clipped geometry, the raw packet remains header + tightly packed rows + CRLF. The two public S107 findings remain accepted compatibility debt.

Font scratch fields and per-call unmanaged copies are removed. GDI resources are released by the common renderer before transport. The raw WritePrinter adapter captures the existing printer handle once, uses a local written count, and copies only a remaining suffix after a partial write. It neither starts nor ends jobs/pages and does not call sendcommand/sendbinary (which add page operations and CRLF). Its managed byte-array overload requires no retained pin or unmanaged allocation. False native results surface as Win32Exception; zero, negative, or excessive successful byte counts are rejected by the common transport loop.

The openport/closeport API still owns the spool job and page. Its static hPrinter lifecycle and unrelated shared sendcommand fields remain separate debt: a captured handle avoids switching between partial writes, but concurrent close/reopen or other commands targeting that job are not made safe by this bounded repair.

## Intentional corrections and limits

Negative-X rows now skip the same whole bytes that the header subtracts; the former driver wrote the entire original row from column zero despite declaring a smaller width. Clipping offsets no longer accumulate across calls. Fully clipped or empty output sends nothing. Measured dimensions beyond the fixed 2400-square monochrome canvas are rejected by the shared renderer. These are corrections to malformed output rather than claims of byte parity for invalid legacy cases. GDI cleanup replaces the former Marshal.Release calls on non-COM handles/pointers, missing ReleaseDC, and per-row megabyte allocations/GC.Collect calls.

Focused driver tests execute no native GDI, Winspool, printer, or external network. This transport adapter is validated with fake GDI and an in-memory writer. The normal consumer guard also runs inherited native Windows GDI tests of the already reviewed common renderer, using memory transport; it does not execute this new WritePrinter adapter against a real spool job. Real spooler/firmware behavior and job lifecycle concurrency require separate owner-controlled validation.

## Validation

The first focused test run passed 13 cases, and the same 13 passed after private GDI declaration cleanup: public API request mapping at four rotations, exact header/payload/mode/CRLF, partial-write suffix ownership and captured handle, negative-then-normal calls, empty/fully-clipped calls, false/invalid raw-write results, GDI failure before transport, and overlapping driver instances. Each memory write asserts GDI-owned resources are already released. Test filtering selects only DriverWindowsFontTests, avoiding native GDI and physical-printer cases. No assertions or mandatory commit checks were bypassed. Eleven private imports made unused by this repair were removed after checking their references; public interop declarations and structs remain intact. The pre-existing unused Rectangle import and font constants were retained because they were not made unused by this change.

Command: `dotnet test Hyunmui.TSCPrinter.Tests/Hyunmui.TSCPrinter.Tests.csproj --artifacts-path <isolated-temp> --filter FullyQualifiedName~DriverWindowsFontTests --verbosity minimal` with process-only DOTNET_PROCESSOR_COUNT=4, MSBUILDDISABLENODEREUSE=1, DOTNET_CLI_USE_MSBUILD_SERVER=0.

Rollback: revert this submodule change to the helper baseline. The consumer pointer is updated separately after parent review. No NuGet publication or external rollout is included.

## Assigned issue mapping

The supplied subset contains 52 findings; 50 are addressed in source, with server confirmation pending, and two S107 findings remain debt. No fresh server inventory was fetched by this worker.

| Key | Rule | Baseline line | Disposition |
| --- | --- | --- | --- |
| 5d035663-c452-456c-8990-60a030d45656 | csharpsquid:S107 | 813 | Preserved public signature; accepted debt |
| d6bebf16-1f6a-4462-9934-e1803f48dbf4 | csharpsquid:S3776 | 813 | Source addressed; server confirmation pending |
| 6ba543e7-4df1-4736-97d8-c740c5f05079 | csharpsquid:S1854 | 824 | Source addressed; server confirmation pending |
| ed8f6591-9f74-403d-9604-5b2c7192e9ad | csharpsquid:S1481 | 847 | Source addressed; server confirmation pending |
| 66221a2b-547d-48aa-b78c-5a12760f6e12 | csharpsquid:S1481 | 848 | Source addressed; server confirmation pending |
| ff4ad8bf-911d-4b4a-868b-e32a1ff6d148 | csharpsquid:S2696 | 849 | Source addressed; server confirmation pending |
| 1b5a2095-2399-4cff-8500-096f2f2d0c22 | csharpsquid:S2696 | 850 | Source addressed; server confirmation pending |
| bfeeabca-6242-4a9f-8c05-8ce320ee832a | csharpsquid:S2696 | 873 | Source addressed; server confirmation pending |
| 150a40ea-2555-4516-86a6-31b7c13ed7a7 | csharpsquid:S2696 | 874 | Source addressed; server confirmation pending |
| 9b0daed6-3aeb-47ab-a90b-ff56271c5694 | csharpsquid:S125 | 879 | Source addressed; server confirmation pending |
| 8e569894-21dd-4ae6-a7e1-00047a93f03c | csharpsquid:S125 | 883 | Source addressed; server confirmation pending |
| 8ea28750-69f1-4f2a-a947-f57917d2c8f5 | csharpsquid:S125 | 887 | Source addressed; server confirmation pending |
| d709011d-bcbe-4d68-b31c-db358a04b2f7 | csharpsquid:S2696 | 903 | Source addressed; server confirmation pending |
| 2de3e39c-9a22-4fb4-b4a5-f52bb94bb77c | csharpsquid:S2696 | 904 | Source addressed; server confirmation pending |
| c16b2ffe-d4e0-4d74-ab7d-5f246d6743b6 | csharpsquid:S2696 | 907 | Source addressed; server confirmation pending |
| 219fc2aa-58e5-40f6-8e5e-e2bec1f063ce | csharpsquid:S2696 | 912 | Source addressed; server confirmation pending |
| d9cfe4dc-e26b-4208-9f43-1c3fdb34613b | csharpsquid:S2696 | 917 | Source addressed; server confirmation pending |
| 23bab3e6-5d0e-4039-a568-c16db26f85a4 | csharpsquid:S2696 | 918 | Source addressed; server confirmation pending |
| 58d4cf7f-d4e9-4c39-926b-8d9bd6d9ea48 | csharpsquid:S2696 | 919 | Source addressed; server confirmation pending |
| f07da125-444a-4c0d-90ce-db32b89edc13 | csharpsquid:S2696 | 920 | Source addressed; server confirmation pending |
| b8ecb4d2-ba9b-4cab-a626-b3462c566a76 | csharpsquid:S1215 | 922 | Source addressed; server confirmation pending |
| ceab1e51-1241-4c5b-8cfe-4d08dcb163e3 | csharpsquid:S1117 | 924 | Source addressed; server confirmation pending |
| deb686f9-d649-45c3-bb39-c71e1e0860bb | csharpsquid:S1117 | 926 | Source addressed; server confirmation pending |
| cc382c45-6950-48f5-830c-d359768456f7 | csharpsquid:S1481 | 931 | Source addressed; server confirmation pending |
| 48f669fc-8fc4-4785-8bef-1ab4fc8169f6 | csharpsquid:S1215 | 939 | Source addressed; server confirmation pending |
| 172b6ccb-b4ea-4a8a-8b47-68775f73c11b | csharpsquid:S1215 | 948 | Source addressed; server confirmation pending |
| 761f4b49-d491-47a6-ac16-c953458c73a7 | csharpsquid:S3776 | 951 | Source addressed; server confirmation pending |
| db89ed7b-dc16-4eec-a535-606570d18050 | csharpsquid:S107 | 951 | Preserved public signature; accepted debt |
| 4512041c-a1ff-4c1f-9245-19971d2d60de | csharpsquid:S1854 | 962 | Source addressed; server confirmation pending |
| b45ebd48-c827-4d18-8aed-01f2c5fa7769 | csharpsquid:S1481 | 985 | Source addressed; server confirmation pending |
| 535caf25-172d-473a-99b0-ea807360810e | csharpsquid:S1481 | 986 | Source addressed; server confirmation pending |
| a0c3d01a-91f7-49a8-bb78-2550aa74bdf1 | csharpsquid:S2696 | 987 | Source addressed; server confirmation pending |
| e0b66853-e221-404a-9c1d-cfcfb39e99fa | csharpsquid:S2696 | 988 | Source addressed; server confirmation pending |
| 1c3a6213-a7e8-4efe-8ce8-cf58d4aad680 | csharpsquid:S2696 | 1011 | Source addressed; server confirmation pending |
| 226fe60a-d447-4cab-8169-752fcd15a5f4 | csharpsquid:S2696 | 1012 | Source addressed; server confirmation pending |
| 2deca673-98cf-4e9e-ba25-aaacd4053806 | csharpsquid:S125 | 1017 | Source addressed; server confirmation pending |
| 6b90921b-deb4-4d58-a8ec-c5b42eec4de6 | csharpsquid:S125 | 1021 | Source addressed; server confirmation pending |
| e27784ce-4474-45c2-8e3a-ec668a62318b | csharpsquid:S125 | 1025 | Source addressed; server confirmation pending |
| 4e32c095-4b93-4616-bb16-8784a3b747e3 | csharpsquid:S2696 | 1041 | Source addressed; server confirmation pending |
| b293e592-5307-4a93-94e1-ef5fc56ff66f | csharpsquid:S2696 | 1042 | Source addressed; server confirmation pending |
| d1955977-eecd-46ae-801f-7e6769bc9664 | csharpsquid:S2696 | 1045 | Source addressed; server confirmation pending |
| c638a331-8968-499f-8b94-9f7a1b71eace | csharpsquid:S2696 | 1050 | Source addressed; server confirmation pending |
| 318682e6-8bf0-4bea-bbd0-ab35d3a54338 | csharpsquid:S2696 | 1055 | Source addressed; server confirmation pending |
| 47a1f2aa-dd0a-4c80-891c-2a8ad60b91be | csharpsquid:S2696 | 1056 | Source addressed; server confirmation pending |
| 0b9e9190-f081-4952-b491-65cc979c5de7 | csharpsquid:S2696 | 1057 | Source addressed; server confirmation pending |
| 4a0cf02d-068d-4478-b00d-33ff77a86ed2 | csharpsquid:S2696 | 1058 | Source addressed; server confirmation pending |
| acada501-be47-4118-8307-96759e13d408 | csharpsquid:S1215 | 1060 | Source addressed; server confirmation pending |
| 19224501-0843-46c8-b9d1-6f16a36ab135 | csharpsquid:S1117 | 1062 | Source addressed; server confirmation pending |
| cdb25997-fdbc-4573-afed-57f3f5f6cea8 | csharpsquid:S1117 | 1064 | Source addressed; server confirmation pending |
| 7fa52e18-b31a-4dab-89c8-85cdf386cef9 | csharpsquid:S1481 | 1069 | Source addressed; server confirmation pending |
| d88a7c21-a9c2-40e7-b77d-6dc1a1254ff1 | csharpsquid:S1215 | 1077 | Source addressed; server confirmation pending |
| badaf453-70c9-4e98-87ed-704724ff1672 | csharpsquid:S1215 | 1086 | Source addressed; server confirmation pending |
