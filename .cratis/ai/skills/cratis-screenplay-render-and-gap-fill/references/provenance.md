<!-- cratis-ai-managed: skills/cratis-screenplay-render-and-gap-fill/references/provenance.md -->
# Provenance

## Moved and rewritten (Cratis)
- The render workflow of the earlier model skill set: capability probe, renderability
  classification, delivery protocol, drift and fallback record. Rewritten for Stage and the CLI and checked
  by running `cratis render`; the
  helper scripts are not shipped (their steps are the manual commands in `references/delivery-protocol.md`).
- The fallback-conformance checklist moved into `cratis-application-slice-conformance`
  (`references/checklists.md`); this skill links it and keeps no copy.
- Three delivery cases (admission failure, generated base plus authorized gap-fill, fully
  hand-written delivery) and the whole-model rule: from the plan review of render versus gap-fill.

## Adapted closely (Martin Dilger and Nebulit GmbH)

Source: https://github.com/Nebulit-GmbH/agentic-engineer at commit `07b0f30648d663cb588d7e2c7aa031af9dfc21f2`,
by Martin Dilger and Nebulit GmbH (https://nebulit.de), used with the explicit consent of Martin Dilger and Nebulit GmbH.

| Source file | Used in | How |
|---|---|---|
| `.claude/skills/build-state-change/SKILL.md` and `build-state-view/SKILL.md`: verify against a contract with population, update and removal checks; repeat rejected versus idempotent success | `references/gap-fill-handoff.md`, via `cratis-application-slice-conformance` | Idea; our own wording kept |
| `.build-kit/CLAUDE.md`, `.build-kit/lib/prompt.md`, `.build-kit/lib/backend-prompt.md`: the specification as oracle, never weakened; one scope per brief; the repository pattern beats a template; added-specifications re-delivery; ambiguity escalates; claim conflict | `references/gap-fill-handoff.md` "One scope per brief" and the build-prompts paragraph | Adapted closely, with the claim translated to per-brief ownership and git left to separate authorization |
| `.claude/skills/update-slice-status/SKILL.md`: closed status vocabulary and the already-in-status claim guard | `references/gap-fill-handoff.md`; the `done \| partial \| blocked` status | Idea; our own wording kept (statuses differ on purpose) |

No TrogonStack/agentskills text is used in this skill.

### Eventmodelers Build-Kits

Source: https://github.com/Nebulit-GmbH/Eventmodelers-Build-Kits, by Martin Dilger and Nebulit GmbH
(https://nebulit.de), used with the explicit consent of Martin Dilger and Nebulit GmbH.

| Source skill | Used in | How |
|---|---|---|
| `detect-model-drift` | `references/hand-written-changes.md`; `references/drift-and-fallback-record.md` investigation pointer; `SKILL.md` step 8 | copied and translated: scoped baseline/ref reads, delivery attribution, finding kinds and report, behavior-preserving noise, removal of delivered code distinguished from undelivered scope, affected screens in each edit request and resolution routing; used with their explicit consent; model-first restoration remains the default rather than changing the model to fit code |
| `record-evidence` (classic stack) | `references/drift-and-fallback-record.md` *Fallback record*; `references/hand-written-changes.md` record checks; `references/gap-fill-handoff.md` result pointer | adapted closely: element, field and specification mapping including concrete input-to-storage chains, resolving addresses and nonempty rows, files, shared seams, assumptions and checks per slice, with no code excerpts; used with their explicit consent |
