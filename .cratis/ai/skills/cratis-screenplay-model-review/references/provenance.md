<!-- cratis-ai-managed: skills/cratis-screenplay-model-review/references/provenance.md -->
# Provenance

## Closely adapted (MIT)

Source: https://github.com/TrogonStack/agentskills at commit
`7b249d3ee42d8b7e11fa564141ebd5fbc37aadf1`, path `plugins/trogonstack-eventmodeling/skills/`.
MIT, Copyright (c) 2025 Straw Hat, LLC. The complete notice is in this skill's `LICENSE`.

| Source skill | Adapted into | How |
|---|---|---|
| `eventmodeling-validating-event-models-checklist` | `checklist.md` (phase layout, per-phase check counts), `report-template.md` (per check: status, element, evidence; anti-patterns block; final questions; verdict and success criteria), `anti-patterns.md` ("Calculation events", "Circular dependencies", "Shared versus handler-owned state", "Persistent versus ephemeral state" translated to Screenplay), `review-questions.md` (per-subject questions and per-domain examples) | Structure and wording adapted closely; every code and class term mapped to Screenplay constructs; the 16 checks became the phased checklist with additions |
| `eventmodeling-validating-event-models` | `checklist.md` T (allowed and refused transitions per entity), C7 and C8 (unique semantics, corrections name what they correct), F3, G (role attribution and coverage), D and E (read models serve real queries, regenerable), `report-template.md` (critical and warning findings, completeness table) | Adapted; the command-state rule is not adopted as such (Screenplay decisions read stored state through `reads`; see `anti-patterns.md`) |
| `eventmodeling-checking-completeness` | `checklist.md` A (origin and destination for every field, every command input, every read-model field), A8 (step contracts), G (role coverage), `references/worked-example.md` (field-trace format) | Adapted; matrices replaced by lineage over `.play` declarations |

## Adapted closely (Martin Dilger and Nebulit GmbH)

Source: https://github.com/Nebulit-GmbH/agentic-engineer at commit `07b0f30648d663cb588d7e2c7aa031af9dfc21f2`,
by Martin Dilger and Nebulit GmbH (https://nebulit.de), used with the explicit consent of Martin Dilger and Nebulit GmbH.

| Source file | Used in | How |
|---|---|---|
| `.claude/skills/wdyt/SKILL.md`: business-analyst persona, language rule, question categories A to I, persona reminders, summary shape | `SKILL.md` "Business-question pass", `business-questions.md` (Ground rules, "Question categories", Output format) | Adapted closely; board comments, drawings (arrows, group loops) and board API calls dropped, questions go in the report; the shape category I keeps Martin Dilger and Nebulit GmbH's plain-language wordings but only asks about a submit that settles several things, since Screenplay screens may host several independent actions |
| `.claude/skills/eventmodeling-designing-automation-chains/SKILL.md`: every automation needs a todo list, translation versus worker, redundant second stage, no status field, closing path, quality checklist | `checklist.md` F8 to F12 and the automation-chain audit, `report-template.md` "Automation chains", `review-questions.md` | Adapted as review checks translated to `Automation` and `Translate` slices. Not adopted: a translation's todo list is never closed; here a translation holding real pending work needs a terminal disposition (the audit format of `cratis-screenplay-automations-and-translations`), and an artificial never-closing queue is flagged instead |
| `.claude/skills/eventmodeling-core-rules/SKILL.md`: Critic Mode, Flow and Causality, Open Questions vs. Decided Failures, Offline-First Thinking | `SKILL.md` "Procedure" (stance), `checklist.md` E9 and F13, `report-template.md` "Defect or business question?", `review-questions.md` | Adapted closely; the single-issuer command rule, lanes, linked copies and board mechanics are not adopted |
| `.claude/skills/handle-comment/SKILL.md`: one remark per node with a kind, resolving means answering | `SKILL.md` "Findings" | Adapted; the board place, resolve and delete actions are dropped, the remark is a finding pinned to one declaration |
| `.claude/skills/analyze-existing-model/SKILL.md`: spec coverage with names, structural gaps per slice type, summary in 2 to 4 sentences | `SKILL.md` "Audit pass", `report-template.md` Inventory, `model-inventory-report.md` (gather steps, report shape, structural-gap table) | Adapted closely; board and slice-status steps replaced by the Screenplay MCP views, slice status breakdown dropped (Screenplay has none), `validate_model` replaced by `find-modeling-smells` and `find-assertion-gaps` |
| `.claude/skills/eventmodeling-validating-event-models/SKILL.md`, `-checklist/SKILL.md`: event-entity mapping (Check 6.2), report with blockers and next steps, common issues, closing questions that gate the verdict | `checklist.md` B2, `report-template.md` Verdict, `review-questions.md` | Martin Dilger and Nebulit GmbH's changes to the TrogonStack base, adapted; the "exactly one issuer" and "screen wired to more than one command always fails" rules are not adopted (Screenplay commands may have several origins and a screen several actions) |
| `.claude/skills/eventmodeling-checking-completeness/SKILL.md`: field matrix, slice coverage, "search before calling data missing" | `checklist.md` A1 to A8 | Already adapted from the TrogonStack base (role coverage is TrogonStack's, credited above); Martin Dilger and Nebulit GmbH's slice-border and linked-copy checks are not adopted (no columns or linked copies in `.play`) |

### Eventmodelers Build-Kits

Source: https://github.com/Nebulit-GmbH/Eventmodelers-Build-Kits, by Martin Dilger and Nebulit GmbH
(https://nebulit.de), used with the explicit consent of Martin Dilger and Nebulit GmbH.

| Source skill | Used in | How |
|---|---|---|
| `stressor-analysis` | `references/stressor-analysis.md` | copied and translated: the goal sentence, the external floor and examples, the more-stressors step, the thin-coupling note and the Hit-by column; chat snippets dropped |
| `data-journey` | `references/model-inventory-report.md` *Information journey* | copied and translated: the role table, name variants, sounds-related fields as different information, the headline; the property-link tracer replaces upstream's mapping walk |
| `analyze-existing-model` | `references/model-inventory-report.md` | adapted closely: cost tiers, coverage split by slice kind, brief conversational mode, delivery status from tracking issues |
| `walkthrough` | `references/walkthrough.md` | copied and translated: one step per slice, the explain and impact modes chosen by whether the subject is modeled, widening limits, the step fields, faithfulness and the pre-return check; the sketch is a `.play` fragment instead of board elements and HTML |
| `wdyt` | `references/business-questions.md` | adapted closely: never re-ask an open question, the person picks which questions are kept, the owning session records them as notes |

## Other lineage

- Event Modeling (Adam Dymitruk) and Martin Dilger, *Understanding Eventsourcing*:
  completeness, vertical slices, the structural-shape signals.
- Alexey Zimarev, Oskar Dudycz, Vaughn Vernon and others on stream and boundary design:
  the boundary rows of `anti-patterns.md`.
- Draft lineage of the first draft of this skill: the field-copy signal, the element sweep
  and the entity walk are Cratis additions, grounded in the advisory modeling-smell report.
- Rejected anti-patterns (`checklist.md` R): Cratis/Screenplay strategy decisions and the
  closure comments of the Screenplay issues that rejected them.
