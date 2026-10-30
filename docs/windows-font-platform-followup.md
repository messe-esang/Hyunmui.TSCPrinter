# Windows-font platform boundary follow-up

This follow-up addresses the 20 supplied new findings from the wave 04 Gate evaluation without changing public Ethernet interop signatures, LOGFONT/SIZE types, transport APIs, or bitmap bytes.

The shared GDI implementation now lives in the standalone internal `WindowsFontGdi` class. Its 13 private native imports moved with it; Ethernet, serial, driver and LPT rendering use this platform implementation. The private `NativeCreateBitmap` declaration explicitly targets the unchanged native `CreateBitmap` export. Public legacy imports remain available on Ethernet.

Geometry computes its draw origin with ordinary branches. Null font content reports `request` as the actual argument name with an explanatory message, covered by the command test. The native test uses source-generated LibraryImport in a partial class, requiring unsafe compilation in both test projects; no analyzer suppressions were added.

GetGuiResources legitimately returns zero when no GDI objects remain. The test now captures the last error immediately (SetLastError enabled), accepts zero only with no native error, and preserves the original before/after eight-handle growth bound. This corrects an assumption about unrelated process handles without weakening the leak bound. Reference: https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-getguiresources

Validation: all 92 Windows-font tests passed after the extraction and test correction; the 10 native GDI tests passed again after the private entry-point rename. No printer/device transport, USB repair, or package publishing was performed. Parent consumer integration retains the existing source links and adds only AllowUnsafeBlocks for generated test interop.

Rollback: revert this follow-up submodule commit and its consumer pointer/configuration change together. Prior font fixes remain in the base history; reverting this follow-up reintroduces the reported platform-boundary and test-interop findings.

## Exact new findings

Server confirmation remains pending.

| Key | Rule |
| --- | --- |
| 69ccb8bc-801e-4c32-a6bf-ef44864785ea | external_roslyn:SYSLIB1054 |
| defad584-dad3-413c-a4ac-b599a3acc8ac | csharpsquid:S3358 |
| a6e001ad-b67f-4d8e-9ab6-8a55218abcfb | csharpsquid:S3928 |
| 38a08b28-80e2-420e-a02b-7ccc687591e0 | csharpsquid:S3398 |
| 67286b6a-62f5-444d-b83c-53975c71e076 | csharpsquid:S3398 |
| 8fed7999-3ae2-4572-8750-e96f8fdd06e0 | csharpsquid:S3398 |
| 76f9569f-9a9e-4713-9117-a26a7a188cd2 | csharpsquid:S3398 |
| 2db90dd5-e9e6-4254-80ad-a31553c49f23 | csharpsquid:S3398 |
| 731a99f9-54ea-46a6-8777-2067511efb0b | csharpsquid:S3398 |
| b7dc720a-1cfa-414c-8e83-06d505bc9498 | csharpsquid:S3398 |
| 4f0ab9a7-435a-4f6e-bf21-ba3d916c76a7 | csharpsquid:S3398 |
| 4281a90e-6667-49f3-8283-be289fafbe18 | csharpsquid:S3398 |
| d4b5e1c0-a456-4273-8176-37836e5fc11d | csharpsquid:S3398 |
| afa5fded-72e0-455b-a540-02f7605778a5 | csharpsquid:S3398 |
| ab13f268-0bf7-441d-a7db-8a7700b1a168 | csharpsquid:S3398 |
| 5c263516-1f76-40ce-bf38-6a020b010ad6 | csharpsquid:S3398 |
| 26dac717-ce80-467a-8fe6-ae670e30ec57 | csharpsquid:S927 |
| 54e2a549-f14b-4488-ab40-76947a9a1232 | csharpsquid:S3218 |
| b6b5b9ef-94f4-4761-8ac1-dfc3c34b867f | csharpsquid:S3218 |
| 2e4b7753-52ae-4494-b2de-1308aca8ce04 | csharpsquid:S3218 |
