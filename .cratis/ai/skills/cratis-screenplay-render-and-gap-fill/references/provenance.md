<!-- cratis-ai-managed: skills/cratis-screenplay-render-and-gap-fill/references/provenance.md -->
# Provenance

## Moved and rewritten (Cratis)
- The render workflow of the earlier model skill set: capability probe, renderability
  classification, delivery protocol, drift and fallback ledger. Rewritten for Stage 4.24 and cli
  3.27.1, then re-verified on cli 3.28.2 (Stage 4.24.1) and 3.28.3 (Stage 4.24.2) by running `cratis render`; the
  helper scripts are not shipped (their steps are the manual commands in `references/delivery-protocol.md`).
- The fallback-conformance checklist moved into `cratis-application-slice-conformance`
  (`references/checklists.md`); this skill links it and keeps no copy.
- Three delivery cases (admission failure, generated base plus authorized gap-fill, fully
  hand-written delivery) and the whole-model rule: from the #493 plan review of render versus gap-fill.

## Adapted closely (Martin Dilger and Nebulit GmbH)

Source: https://github.com/Nebulit-GmbH/agentic-engineer at commit `07b0f30648d663cb588d7e2c7aa031af9dfc21f2`,
by Martin Dilger and Nebulit GmbH (https://nebulit.de), used with the explicit consent of Martin Dilger and Nebulit GmbH.

| Source file | Used in | How |
|---|---|---|
| `.claude/skills/build-state-change/SKILL.md` and `build-state-view/SKILL.md`: verify against a contract with population, update and removal checks; repeat rejected versus idempotent success | `references/gap-fill-handoff.md`, via `cratis-application-slice-conformance` | Idea; our own wording kept |
| `.build-kit/CLAUDE.md`, `.build-kit/lib/prompt.md`, `.build-kit/lib/backend-prompt.md`: the specification as oracle, never weakened; one scope per brief; the repository pattern beats a template; added-specifications re-delivery; ambiguity escalates; claim conflict | `references/gap-fill-handoff.md` "One scope per brief" and the build-prompts paragraph | Restored from #493; adapted closely, with the claim translated to per-brief ownership and git left to separate authorization |
| `.claude/skills/update-slice-status/SKILL.md`: closed status vocabulary and the already-in-status claim guard | `references/gap-fill-handoff.md`; the `done \| partial \| blocked` status | Idea; our own wording kept (statuses differ on purpose) |

No TrogonStack/agentskills text is used in this skill.
