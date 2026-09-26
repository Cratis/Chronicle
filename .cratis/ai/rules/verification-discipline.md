---
applyTo: "**/*"
paths:
  - "**/*"
---
<!-- cratis-ai-managed: rules/verification-discipline.md -->

# Verification Discipline

Verification answers whether changed behavior works. It does not establish who
authored a file or preserve a chain of evidence about how it arrived.

- Run the narrowest relevant build, type check, lint, and specifications.
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
