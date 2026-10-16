<!-- cratis-ai-managed: skills/cratis-screenplay-model-review/references/quick-self-check.md -->
# Quick self-check (modeler, P4)

About ten minutes, before the independent review.

1. V1 clean with warnings as errors (folder), and V3 when executable or renderable.
2. No PLAY0029 ever (a dropped construct); no unresolved-name warnings.
3. Every event production has `for` (operation productions take none); no event-source id copied into a payload without a consumer.
4. Every read-model field has an origin; every event has a consumer or terminal reason.
5. Every command has a reachable, authorized origin (screen or form action, or a reaction to an event, clock or application trigger; a capture appends facts and a command after it is reached through a reaction).
6. No gated command is invoked by a reaction without a recorded trusted-path decision.
7. State-dependent rules are `reads` + `require ... message` marked NOT enforced; no
   caller-supplied state, attestation boolean (`confirmsX == true`) or handler/hint prose.
8. Coverage matrix filled; every `n/a` has a domain reason.
9. Persona text says what each role cannot do, each line backed by a gate and a `then denied`
   spec (or recorded as a gap); no generic "User".
10. Real rationale in fenced Markdown `documentation`, finalized at P3's end;
    summaries in `description`, open questions as anchored `question` notes,
    with note IDs in session state, never only chat or description text.
11. Every Automation has a pending-work view or a stated reason it needs none (F8-F13).
Any "no" is fixed or explained before P5.
