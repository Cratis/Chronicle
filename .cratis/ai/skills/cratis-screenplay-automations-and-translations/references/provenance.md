<!-- cratis-ai-managed: skills/cratis-screenplay-automations-and-translations/references/provenance.md -->
# Provenance

## Adapted closely (MIT)
Source: `TrogonStack/agentskills`, commit `7b249d3ee42d8b7e11fa564141ebd5fbc37aadf1`,
Copyright (c) 2025 Straw Hat, LLC, MIT. The complete notice is in this skill's `LICENSE`.

| Our content | From | Adaptation |
|---|---|---|
| `SKILL.md` Interview phase: skip condition, the sources and mapping questions with follow-ups, "missing correlation first" | `plugins/trogonstack-eventmodeling/skills/eventmodeling-translating-external-events/SKILL.md` (Interview Phase) | rewritten for Screenplay captures; added automation and recovery questions and the unattended rule |
| `references/worked-integration.md` structure: catalog, technical analysis, correlation, translation rules, scenarios, failure and recovery, output | same file, Workflow 1 to 7 and Output Format | new domain (invoicing, synthetic provider); target-field completeness table, recovery owner column and `capture`/`constraint` mapping added; the payment and geofence examples are not reused |
| `references/integration-contracts.md` question table and the quality checklist ideas (every type has a rule, correlation, external ids kept for dedup, validation, missing data) | same file, Quality Checklist and Key Principles | merged into the contract and the audit |

Reviewed and not adopted: the translating skill's "Default Gracefully" principle (sensible
defaults for missing data) is replaced by "never fabricate a default"; its log-and-drop failure
handling is replaced by recorded failure facts. `plugins/trogonstack-eda` was skimmed; nothing
adopted.

## Adapted closely (Martin Dilger and Nebulit GmbH)

Source: https://github.com/Nebulit-GmbH/agentic-engineer at commit `07b0f30648d663cb588d7e2c7aa031af9dfc21f2`,
by Martin Dilger and Nebulit GmbH (https://nebulit.de), used with the explicit consent of Martin Dilger and Nebulit GmbH.

| Source file | Used in | How |
|---|---|---|
| `.claude/skills/eventmodeling-designing-automation-chains/SKILL.md`: resolve the chain when introduced; exhaustive per-automation verification | `SKILL.md` intro and "Gate", `references/audit-format.md` | the six verification questions adapted closely as "Per-chain questions", translated to Screenplay (no board, no `validate_model`) |
| same: pending membership not status; opening and closing events | `references/automation-patterns.md` section 2, `references/audit-format.md` question 6 | adapted closely; our todo-list example kept |
| same: worker only for a new decision; redundant second stage and the `Synced` tell | `references/automation-patterns.md` section 7 "Redundant second stage" | adapted closely, marina berth example in place of Martin Dilger and Nebulit GmbH's |
| same: silently skipping a missing target | `SKILL.md` "Gate", `references/audit-format.md` | idea inverted: an absent target is an unresolved dependency |
| same: compulsory queue for every automation, "every trigger is an event", never-closing translation queue, mandatory translation command, board placement and connections | not adopted | wrong for Screenplay (direct reactions, clock and application triggers, `capture` are supported); idea only |
| `.claude/skills/eventmodeling-translating-external-events/SKILL.md` step 5 and its examples reference (section 5): per-record scenario format | `references/integration-contracts.md` "Per-record-type contract" | adapted closely, invoicing example; log-and-drop failure handling replaced by failure facts |
| `.claude/skills/eventmodeling-designing-automation-chains/SKILL.md`: field mapping and cardinality on every todo-list field | `references/automation-patterns.md` section 2 | adapted closely; board mapping syntax not used |
| `.claude/skills/eventmodeling-translating-external-events/SKILL.md`: interview, catalog, target-field check, recovery ownership | `SKILL.md` "Interview phase", `references/integration-contracts.md`, `references/worked-integration.md` | idea; our own wording kept (TrogonStack base above) |
| `.claude/skills/build-automation/SKILL.md`: model authoritative, no invented mappings or filters, repeated-command cases | `references/realization-and-gap-fill.md`, `references/cases-to-specify.md` | idea; our own wording kept; implementation recipe not adopted |

## Carried forward
`references/automation-patterns.md`, `translation-patterns.md`, `effects-and-idempotency.md`,
`cases-to-specify.md` and both examples come from the earlier Cratis drafts of this skill.

## Eventmodelers Build-Kits

Source: https://github.com/Nebulit-GmbH/Eventmodelers-Build-Kits, by Martin Dilger and Nebulit GmbH
(https://nebulit.de), used with the explicit consent of Martin Dilger and Nebulit GmbH.

| Source skill | Used in | How |
|---|---|---|
| `build-automation-workflow` (axon stack): the decision table of signals that call for a workflow | `references/automation-patterns.md` §10 | copied and translated: the signal rows are kept, each answered with a Screenplay shape (chained Automation slices, pending-work views, clock reactions); the engine, step-handler and test mechanics are not adopted; the timeout and compensation-order checks follow its workflow tests |
| `build-webhook` (supabase stack): inbound webhooks verify the provider's own signature, and the endpoint contract is written for an integrator who cannot read the code | `references/realization-and-gap-fill.md` (capture row), `references/integration-contracts.md` "Publish the inbound contract" | copied and translated: provider-signature authentication and the integrator-facing contract are kept; the function-registration and local-serve mechanics are dropped; "generate the aggregate id when the caller omits it" is not adopted because it breaks retry identity |
