<!-- cratis-ai-managed: skills/cratis-chronicle-reactor/references/domain-failures.md -->
## Domain failures must leave a visible state

A recorded failure fact is not complete handling if the user-facing workflow
still looks running indefinitely. Give each terminal failure a reliable owner
that transitions the affected attempt/workflow promptly to failed, retryable, or
another explicit outcome, and project a safe reason for the reader. Keep secrets
and raw provider payloads out of public failure details.

This is an application reliability convention, **not** a requirement for a
separate failure reactor. An existing reliable state machine that consumes the
failure event and owns the transition is sufficient; adding another consumer can
race or duplicate it. Add a reactor only when the transition/effect has no owner.
Use a watchdog for missing signals or lost work, not as the normal consumer of a
failure already known. Distinguish domain failure from an observer partition
failure: the latter also needs operational monitoring and recovery.

Specify failure-to-visible-state, repeated delivery, stale failure from an older
attempt, and recovery/retry behavior. Prove the normal failure transition without
waiting for the watchdog timeout.
