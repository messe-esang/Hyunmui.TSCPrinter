# Serial Windows-font rendering repair

## Reason and scope

The serial `windowsfont` and `windowsfontunicode` methods duplicated the Ethernet raster pipeline, wrote shared static geometry and buffers, accumulated clipping between calls, allocated unmanaged memory per row, and released GDI handles as COM objects. Both entry points now reuse `WindowsFontCommand` and `WindowsFontRenderer` from base commit `39c40461d01d94aac2016fc99815cb3404844b16`.

Public method signatures and the public parameterless constructor remain available. The constructor initializes a renderer and writer factory without touching any port. At each font call the factory captures the current `_serialPort` once. A successful `SerialPort.Write(byte[], offset, count)` returns the requested count to the shared command; a failure propagates without catch, retry, reconnection, or extra terminator. The raw packet contains its single CRLF already, so the adapter never calls `sendcommand`.

No public interop declarations, serial open/close methods, receiver threads, shared `_serialPort` lifecycle, or other print commands were changed. Same-port concurrent calls and racing close/open operations remain unsupported; local rendering isolation does not imply serial transport synchronization.

## Compatibility and intentional corrections

Normal in-bounds right-angle output preserves raw BITMAP header/payload/CRLF ordering, font style mapping, and ANSI/Unicode routing. The legacy ignored `fontunderline` argument stays ignored. The eight-argument public signatures remain accepted S107 debt.

The reused helper intentionally fixes negative-origin cropping to emit exactly the advertised row width, resets geometry per call, uses invariant header numbers and the 2400-by-2400 canvas bounds, and releases selected GDI objects in deterministic order before the serial write. Empty text, zero measured dimensions, and fully clipped output write no packet. Null text and invalid measured extents fail before transport; GDI failures now throw instead of continuing with invalid handles. These behaviors match the shared Ethernet repair; they are not assertions of physical-printer equivalence for malformed or out-of-bounds legacy requests.

## Validation and limitations

`ComportWindowsFontTests` exercises both public font methods with fake GDI and a memory writer: all four right-angle geometries, original request fields, exact header/payload and one CRLF, negative-origin then normal calls, independent overlapping renderers, call-time writer capture, and a single propagated serial-write failure after resource cleanup. Empty, null and fully clipped cases do not write. Existing pure `WindowsFontCommandTests` cover invalid extents, GDI failure and cleanup paths. No serial ports were opened and no native GDI, printer, SMS, network transport, package publish, or live registration action was run.

Focused command: `dotnet test Hyunmui.TSCPrinter.Tests/Hyunmui.TSCPrinter.Tests.csproj --filter "FullyQualifiedName~ComportWindowsFontTests|FullyQualifiedName~WindowsFontCommandTests" --verbosity minimal` (55 passed).

Rollback: revert this submodule commit and restore the consumer pointer if integrated. Reverting also restores unsafe native cleanup, shared raster state, and inconsistent clipped-row output. Consumer source-link integration and the parent full hook are coordinated separately.

## Snapshot target mapping

The supplied snapshot has 45 findings. The two font methods contain 44: 42 source-addressed findings and two retained S107 findings. The next method's `printphoto` S2325 key `c21c289b-54c2-4be3-a45c-82cf5d3e6781` is excluded from this change. Server confirmation is pending; no inventory was recollected.

| Key | Rule | Baseline line | Disposition |
| --- | --- | --- | --- |
| 193c373b-7deb-4aea-ada2-a6900b62455d | csharpsquid:S3776 | 727 | Source addressed; server confirmation pending |
| f19ef4ab-03fa-494e-af11-d13dfd335d49 | csharpsquid:S107 | 727 | Preserved public signature; accepted debt |
| b994444d-dcb9-40c2-b45a-cfb35084d9e2 | csharpsquid:S1854 | 738 | Source addressed; server confirmation pending |
| 00ccff9f-8fc2-456f-9df1-8abf3a3589f7 | csharpsquid:S1481 | 761 | Source addressed; server confirmation pending |
| ab4df80b-1e54-4fc3-a792-b5592ce45eda | csharpsquid:S1481 | 762 | Source addressed; server confirmation pending |
| 8d70c66a-f9c0-40da-841f-05a6c175d1c7 | csharpsquid:S2696 | 763 | Source addressed; server confirmation pending |
| ffb54afe-1cc7-4a63-a6ab-6b7f53342fbd | csharpsquid:S2696 | 764 | Source addressed; server confirmation pending |
| 0d81e69e-636f-4b87-a354-44dc348c491c | csharpsquid:S2696 | 787 | Source addressed; server confirmation pending |
| 2be51a3e-81df-4a65-a667-426adab184ae | csharpsquid:S2696 | 788 | Source addressed; server confirmation pending |
| 91862578-3bb3-480e-84c2-5d5ab08d2189 | csharpsquid:S125 | 793 | Source addressed; server confirmation pending |
| 4fe3c380-b2e3-4489-9a80-6755b28d14ca | csharpsquid:S125 | 797 | Source addressed; server confirmation pending |
| 369c78fe-46bb-441b-b4c5-3bf7de6e1fa1 | csharpsquid:S125 | 801 | Source addressed; server confirmation pending |
| 963f910e-ff7a-4df0-87e5-f4141b810588 | csharpsquid:S2696 | 817 | Source addressed; server confirmation pending |
| bf8d9c72-272c-47c8-b71a-ed1087752906 | csharpsquid:S2696 | 818 | Source addressed; server confirmation pending |
| 85bfaff4-bff1-4342-a78c-004afc9648c6 | csharpsquid:S2696 | 821 | Source addressed; server confirmation pending |
| c8363dc2-3c63-4aa1-b07d-5e1f52b682f7 | csharpsquid:S2696 | 826 | Source addressed; server confirmation pending |
| 0150d5a9-9f9a-40cf-bc4f-a4a3e4294aa7 | csharpsquid:S1215 | 831 | Source addressed; server confirmation pending |
| 1dec0ecb-052c-4605-8021-113bb29229bc | csharpsquid:S1117 | 833 | Source addressed; server confirmation pending |
| a21da940-447c-47f9-9158-a76dafd1c2a1 | csharpsquid:S1117 | 835 | Source addressed; server confirmation pending |
| 890b4338-dbf5-4888-b782-7640e688ff03 | csharpsquid:S1481 | 840 | Source addressed; server confirmation pending |
| e7598d28-67e4-43e9-9593-16756933c9f1 | csharpsquid:S1215 | 848 | Source addressed; server confirmation pending |
| c3a14a1e-aba4-43f4-83e0-4b1e46251717 | csharpsquid:S1215 | 855 | Source addressed; server confirmation pending |
| 01e7b8b2-7294-4cfb-b8c2-86835abc0d7f | csharpsquid:S3776 | 858 | Source addressed; server confirmation pending |
| 8b75c299-5587-46b9-8c67-d86df7722e0c | csharpsquid:S107 | 858 | Preserved public signature; accepted debt |
| fb24af7b-3888-4243-ae7d-0220b83cca1c | csharpsquid:S1854 | 869 | Source addressed; server confirmation pending |
| 2a6020bb-c8d8-4272-a416-bb87d0c7b647 | csharpsquid:S1481 | 892 | Source addressed; server confirmation pending |
| 9249a2ed-7dde-4be1-bbe2-0cdf36cd20ed | csharpsquid:S1481 | 893 | Source addressed; server confirmation pending |
| c1e96efc-2905-4785-a8e2-bfbb8179082e | csharpsquid:S2696 | 894 | Source addressed; server confirmation pending |
| 78728069-8d58-4af3-bb32-e7780bffa45c | csharpsquid:S2696 | 895 | Source addressed; server confirmation pending |
| 65bd7dfa-5ffd-4ac5-b6f9-186d22b2ea78 | csharpsquid:S2696 | 918 | Source addressed; server confirmation pending |
| 7b94ad00-1334-427c-af87-1fcc79bdd1e5 | csharpsquid:S2696 | 919 | Source addressed; server confirmation pending |
| a4784065-5e48-4f22-b2f4-2890fc6f7f34 | csharpsquid:S125 | 924 | Source addressed; server confirmation pending |
| 08bb3b46-2082-4ee9-9cfe-166ce08b207a | csharpsquid:S125 | 928 | Source addressed; server confirmation pending |
| 6dbb07cd-40fa-4925-89d6-76293bc88d5a | csharpsquid:S125 | 932 | Source addressed; server confirmation pending |
| c458eb92-f45a-4e55-abf9-bcf446d58546 | csharpsquid:S2696 | 948 | Source addressed; server confirmation pending |
| 14672438-ffc4-4c86-8dbc-fa4f8ae0d622 | csharpsquid:S2696 | 949 | Source addressed; server confirmation pending |
| 6350f9be-5e8b-4b2e-ac41-d18a3fc84d37 | csharpsquid:S2696 | 952 | Source addressed; server confirmation pending |
| 1cb4e993-a75f-4ea7-a28f-fd7bc0e7f8b1 | csharpsquid:S2696 | 957 | Source addressed; server confirmation pending |
| 7dbb88e5-4e56-473b-9be8-b1b1468df360 | csharpsquid:S1215 | 962 | Source addressed; server confirmation pending |
| 6f3c6bc8-d3e5-45f4-9ca2-885315a36a24 | csharpsquid:S1117 | 964 | Source addressed; server confirmation pending |
| 9370ea6a-caaf-4049-a9ad-836259aec512 | csharpsquid:S1117 | 966 | Source addressed; server confirmation pending |
| 26b26396-e207-4fb2-99f7-f9c97c42647e | csharpsquid:S1481 | 971 | Source addressed; server confirmation pending |
| bd843c55-9e58-4d02-a533-a02d0bb5e7b3 | csharpsquid:S1215 | 979 | Source addressed; server confirmation pending |
| f6afeb17-1b7b-45f1-a73e-2ff89547bd83 | csharpsquid:S1215 | 986 | Source addressed; server confirmation pending |
