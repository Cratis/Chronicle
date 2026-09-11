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
- Report failures and skipped checks honestly.

For this repository, `Source/Verification` validates corpus structure, profile
composition, skill scenarios, and native package behavior. `Source/Harness.Setup`
verifies that repository harness adapters still point to `.cratis/ai`.
