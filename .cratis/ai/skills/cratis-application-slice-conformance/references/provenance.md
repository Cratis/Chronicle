<!-- cratis-ai-managed: skills/cratis-application-slice-conformance/references/provenance.md -->
# Provenance

## Moved and rewritten (Cratis)
- The fallback-conformance checklist of the model render workflow: contract reconciliation,
  State Change, State View, Chronicle runtime guarantees, evidence. Now `references/checklists.md`;
  the render workflow links here and keeps no second copy. References renamed to the corpus skills.

## Adapted closely (Martin Dilger and Nebulit GmbH)

Source: https://github.com/Nebulit-GmbH/agentic-engineer at commit `07b0f30648d663cb588d7e2c7aa031af9dfc21f2`,
by Martin Dilger and Nebulit GmbH (https://nebulit.de), used with the explicit consent of Martin Dilger and Nebulit GmbH.

| Source file | Used in | How |
|---|---|---|
| `.claude/skills/build-state-change/SKILL.md`: final verification checklist (commands, events, specifications, nothing invented) | `references/checklists.md` "State Change"; `SKILL.md` steps 2-3 and "Gate" | Adapted closely; slice.json becomes the `.play` slice or agreed outline, emmett types become Arc commands and Chronicle events; the six checks are kept as the closing "Final verification" tick-list in `references/checklists.md`. Not adopted: the Emmett decide/evolve structure, `routes.ts` and the state complexity guide |
| `.claude/skills/build-state-view/SKILL.md`: final checklist (every field has a source, subscribed events equal modeled events, no extra columns, one test per specification) | `references/checklists.md` "State View" | Adapted closely; migration, `canHandle` and Knex items translated to read-model properties, projection sources and storage mapping; Knex connection items not adopted (Emmett specific) |
| `.claude/skills/build-automation/SKILL.md`: checklist (every processor implemented, command fields map only from the trigger, no invented conditions, idempotency specified, no routes, processor registered in startup and event store bootstrap verified) | `references/checklists.md` "Automation and Translation" including "Real startup participation" | Adapted closely; processor/DLQ items translated to the Chronicle reactor and its failure behavior; startup registration translated to discovery, event store and event sequence checks; `start()`/`stop()`, processor-id and `schema.migrate()` not adopted (Emmett specific) |
| `.build-kit/lib/backend-prompt.md`: one slice per iteration, planned can be only added specifications, the slice is always true, done criteria, escalating ambiguity, progress report, consolidated patterns, learnings filter | `references/build-prompts.md` sections 3-5; `SKILL.md` steps 4-6 and "Gate" | Adapted closely; `index.json`, `progress.txt`, board status, `<promise>` sentinels, per-step progress appends, commit and merge steps, and the slice-only done gate not adopted (replaced by the result packet, the completion gate and separate git authorization) |
| `.build-kit/lib/prompt.md` and `.build-kit/lib/AGENT.md`: task loop, claim conflict as a concurrency guard, progress format, learnings file | `references/build-prompts.md` sections 3-5 | Adapted closely; the claim becomes coordinator-assigned ownership (a record is not a lock), task queue and board events not adopted |
| `.build-kit/CLAUDE.md`: strict path limitation, slice type determination, mandatory build flow, verify against the slice, no edits to test files, ignore case in names, session-start learnings | `references/build-prompts.md` sections 1-2; `SKILL.md` step 4 | Adapted closely; the path limitation became owned paths with bounded reads of referenced contracts; TypeScript and `npm` specifics replaced by the repository's runner |
| `.build-kit/AGENTS.md`: learnings that the repository pattern beats the template; a mapping-versus-notes conflict resolved in favor of notes | `references/contract-and-precedence.md`; `references/build-prompts.md` section 5 | Repository-pattern idea adapted; the notes-over-mapping rule is not adopted because executable parts beat prose here |

No TrogonStack/agentskills text is used in this skill.

### Eventmodelers Build-Kits

Source: https://github.com/Nebulit-GmbH/Eventmodelers-Build-Kits, by Martin Dilger and Nebulit GmbH
(https://nebulit.de), used with the explicit consent of Martin Dilger and Nebulit GmbH.

| Source skill | Used in | How |
|---|---|---|
| `record-evidence` (classic stack) | `build-prompts.md` result pointer; Slice Implementer output, using the fallback record in `cratis-screenplay-render-and-gap-fill` | adapted closely: one row per element, field and specification, files created and modified, shared seams, assumptions and checks, with no code excerpts; the board note dropped |

| modeling-kit `prototype` decision scenarios; shared `load-slice` and build prompt summary warning | `build-prompts.md` mandatory flow; `checklists.md` State Change | adapted closely: read all specifications together, read declarations rather than summaries, and refuse only for contracted rules |
| shared `request-feedback` question form | `build-prompts.md` Escalation | adapted closely: name the slice, field/rule/specification and missing choice so the owner can answer without prior context |

## Method lineage

| Idea | Used in | Credit |
| --- | --- | --- |
| Visible non-behavioral assumptions while decisions still block | `build-prompts.md` escalation; Slice Implementer contract gate | Martin Dilger / Nebulit GmbH, Eventmodelers build kits (`classic backend-prompt`, `classic CLAUDE.md`), used with their explicit consent; idea, our own wording |
| Look for an existing realization before selecting one delivered sibling | `build-prompts.md` mandatory flow; Slice Implementer realization search | Martin Dilger / Nebulit GmbH, Eventmodelers build kits (`classic build-state-change`, `classic build-state-view`), used with their explicit consent; idea, our own wording |
| Completed work leaves the pending-work view | `checklists.md` Automation and Translation | Martin Dilger / Nebulit GmbH, Eventmodelers build kits (`prototype`), used with their explicit consent; idea, our own wording |
