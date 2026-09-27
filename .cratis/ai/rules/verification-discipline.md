---
applyTo: "**/*"
paths:
  - "**/*"
---
<!-- cratis-ai-managed: rules/verification-discipline.md -->

# Verification Discipline

Verification answers whether changed behavior works. It does not establish who
authored a file or preserve a chain of evidence about how it arrived.

- Run the narrowest relevant build, type check, lint, and specifications while iterating; explain when a wider gate is required before completion or push/PR.
- Run one phase per command with its real exit status. Budget targeted checks around 120 seconds and build/test phases around 300 seconds; explain any longer budget first, and obtain explicit approval before a phase exceeds 600 seconds. Stop on a failure; do not blindly retry or repeat passing tests on unchanged inputs without a flaky-test hypothesis or required matrix.
- Separate status/fetch from builds, Debug from Release, each test project, and frontend lint/compile/test/build. Do not combine these into an opaque command with a large outer timeout. Expose progress and cancellation; report the precise failed or timed-out phase and do not continue dependent phases after failure.
- Never use `build/test | grep | sort/head/tail` as gate evidence. Capture the producer's exit status before summarising output; `pipefail` alone is insufficient with early-closing consumers such as `head`. Printed zero-error counts are not proof of success. Preserve needed full diagnostics in task-scoped `.ai-work/` logs rather than hiding them in `/tmp`.
- Add a focused specification for every behavior or rejection rule you change.
- Keep each check deterministic and runnable locally and in CI.
- Never replace a real behavior check with a checksum, inventory, generated
  receipt, or provenance record.
- Documentary evidence — a receipt, ledger, attestation, hash inventory,
  snapshot, escrow copy, or ad-hoc `verify-*` script — is not verification and
  is not produced unless a repository workflow (such as a governed release) or
  the user asks for it. The check's own output is the evidence.
- Report the conclusion and any real uncertainty in a line or two, not the
  audit trail. Show the output when asked, when a claim is contested, or when
  the check failed. Report failures and skipped checks honestly.
