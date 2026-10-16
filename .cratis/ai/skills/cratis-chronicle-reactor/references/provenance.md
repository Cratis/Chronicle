<!-- cratis-ai-managed: skills/cratis-chronicle-reactor/references/provenance.md -->
# Provenance

## Method lineage

| Idea | Used in | Credit |
| --- | --- | --- |
| Successful work is removed from its pending-work view and removal is specified | `slice-delivery-checklist.md`, Checklist and Specifications | Martin Dilger / Nebulit GmbH, Eventmodelers build kits (`prototype`), used with their explicit consent; idea, our own wording |

## Eventmodelers Build-Kits

Source: https://github.com/Nebulit-GmbH/Eventmodelers-Build-Kits, by Martin Dilger and Nebulit GmbH
(https://nebulit.de), used with their explicit consent.

| Source skill | Used in | How |
|---|---|---|
| kurrent `build-automation` and `idempotent-dispatch-patterns`; opencqrs/umadb `build-automation` | `slice-delivery-checklist.md`, `specifications.md` | copied and translated: contract-authorized duplicate-refusal handling per dispatch, narrow result matching, independent fan-out; command exceptions replaced by Arc pipeline results |
| axon/kurrent `build-automation` test cases | `specifications.md` | copied and translated: matching/non-matching state, temporal cases and per-item fan-out; independent Chronicle observers replace subscription-group ordering, and passive reads are not claimed as as-of snapshots |

## Adapted closely (Martin Dilger and Nebulit GmbH)

Source: https://github.com/Nebulit-GmbH/agentic-engineer at commit `07b0f30648d663cb588d7e2c7aa031af9dfc21f2`,
by Martin Dilger and Nebulit GmbH (https://nebulit.de), used with the explicit consent of Martin Dilger and Nebulit GmbH.

| Source file | Used in | How |
|---|---|---|
| `.claude/skills/build-automation/SKILL.md`: reactor checklist: output fields traced to a stated source, skip conditions stated in the contract, repeated-input cases | `SKILL.md` "Specifications" and "Verify" | idea; our own wording kept |
| `.claude/skills/build-automation/SKILL.md`: read the slice definition first as the source of truth, build the command handler before the processor, single / conditional / multiple-command processor patterns, no route for an automation, closing checklist (no invented mappings, no invented filters, no guessed fields, idempotency of the command) | `references/slice-delivery-checklist.md` | Adapted closely as an `IReactor` delivery checklist: processors became reactor handlers returning commands or events, the storyline-derived test note became a separate reactor mapping specification. Not adopted: the Emmett reactor, `processor.ts`, processor id, lock and retry configuration, the dead-letter store, `schema.migrate()`, Postgres event store bootstrap, and "no dedicated reactor test" (the field mapping is the contract here) |

The `WithStrictEventSubscription()` scope statement and the scenario sequence-number behavior are verified against the Chronicle source (`Source/Clients/Testing/ReadModels/ReadModelScenario.cs`, `Source/Clients/Testing/Reactors/ReactorScenario.cs`).
