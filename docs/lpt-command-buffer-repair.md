# LPT string command buffer isolation

Baseline: `9e8ca4280366c428e1db47717d1b48877913c3d8`.

Five string command methods assigned one static byte array and then separately read that array and its Length as arguments to an instance stream write. Another instance could replace the shared array between those evaluations, sending its bytes or a mismatched count. Each method now retains its own local encoded bytes; the unused shared field is removed. This is one same-root-cause repair for five S2696 assignments plus their S1450 field.

ASCII, UTF8, GB2312 and Big5 selection is unchanged. The four newline methods retain their separate CRLF write; string NOCRLF retains UTF8, no extra newline and status 1. Encoding/write exceptions and port lifecycle are unchanged. No encoding provider is registered and no shared helper or public API changes.

Validation: 25 focused LptWindowsFontTests passed, including 11 new command cases. They verify configured encoding/framing, write/encoding failures and interleaved independent instance streams. Existing 14 font cases also passed. The static-field race is established by source evaluation order; the interleaved test verifies independent bytes without pretending to deterministically reproduce the old instruction-level race. On this unregistered .NET test host the unavailable GB2312/Big5 paths verify ArgumentException before any write; positive legacy-codepage byte output was not exercised. Tests support existing provider-enabled consumers without changing global provider configuration.

CLI whitespace formatter was limited to the owned test file; diff checks passed. No device/port/native printer I/O was run (memory-capturing FileStream test doubles only). Revert this bounded commit to restore the previous shared-field implementation; doing so restores the documented race. Sonar closure remains pending server verification.

| Key | Rule | Original line | Expected source result |
| --- | --- | --- | --- |
| 111267d0-c3b9-4888-8ac2-48cbd8deca02 | csharpsquid:S1450 | 36 | Shared field/assignment removed |
| 84baea4e-4f01-453b-8206-370e4c26d88a | csharpsquid:S2696 | 149 | Shared field/assignment removed |
| 1882d8d5-1b80-4ebf-aada-56a1f5122a5c | csharpsquid:S2696 | 218 | Shared field/assignment removed |
| 191ef848-86bf-44ad-89b9-c86990eed7f4 | csharpsquid:S2696 | 227 | Shared field/assignment removed |
| 668e5eb7-3a2e-4ee6-a452-85f3be2dc653 | csharpsquid:S2696 | 236 | Shared field/assignment removed |
| 7e9c4981-472a-4f75-ad68-ed754997360b | csharpsquid:S2696 | 248 | Shared field/assignment removed |
