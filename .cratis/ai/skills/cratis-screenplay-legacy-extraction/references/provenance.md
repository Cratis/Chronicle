<!-- cratis-ai-managed: skills/cratis-screenplay-legacy-extraction/references/provenance.md -->
# Provenance

| Content | Source | Licence | How used |
|---|---|---|---|
| Interview protocol (critical questions, follow-up triggers, interview flow, findings capture, green-light checklist) in `SKILL.md` and `references/interview-findings.md` | TrogonStack/agentskills `plugins/trogonstack-eventmodeling/skills/eventmodeling-integrating-legacy-systems/SKILL.md` at `7b249d3ee42d8b7e11fa564141ebd5fbc37aadf1` | MIT, Copyright (c) 2025 Straw Hat, LLC (notice in `LICENSE`) | Adapted closely: questions restructured around Screenplay extraction, the Cratis toolchain and the approval rules; the freeze question kept as the first gate |
| Freeze agreement, side-car pattern, extraction options, traffic routing phases, integration patterns, anti-patterns, quality checklist in `references/side-car-migration.md` | same file | MIT (as above) | Adapted: the target store is Chronicle, extraction is a code or Prologue concern, and the new behaviour is authored as a Screenplay model; the dual-write exception and its controls are kept |
| Evidence ladder, locators, loss report, unknowns, generator/Prologue coverage notes, rule modeling, signal-to-intent table, `references/intent-example.md` | Cratis/AI corpus drafts, written for Cratis | MIT (Cratis) | Own work |

Where product facts come from: `verified-sources.md`.

## Adapted closely (Martin Dilger and Nebulit GmbH)

Source: https://github.com/Nebulit-GmbH/agentic-engineer at commit `07b0f30648d663cb588d7e2c7aa031af9dfc21f2`,
by Martin Dilger and Nebulit GmbH (https://nebulit.de), used with the explicit consent of Martin Dilger and Nebulit GmbH.

| Source file | Used in | How |
|---|---|---|
| `.claude/skills/discover-storyboard/SKILL.md`: Step 0 capability check, Step 2 URL and discovery-guidance questions, default budget of 15 | `references/ui-observation.md` "Before navigating" | Adapted closely; the stop-completely rule becomes "block only this evidence source and continue" |
| `.claude/skills/discover-storyboard/SKILL.md`: Step 4 start-at-entry, guidance-driven exploration, priority order, skip list, three-part description (what it shows, how the user got here, possible actions as intent) | `references/ui-observation.md` "Walk", "Record each meaningful observation" | Adapted closely; approval and no-guessed-credentials rules kept from ours |
| `.claude/skills/discover-storyboard/SKILL.md`: before-and-after states, meaningful screen transitions, "systematic, not exhaustive", one pagination page | `references/ui-observation.md` "Walk" | Adapted closely; transitions keyed to actor, workflow and state, not URL alone |
| `.claude/skills/discover-storyboard/SKILL.md`: Step 4c grouping into flows, per-screen failure continues the run, final report | `references/ui-observation.md` "Group into flows", "Reconcile" | Translated to evidence grouping and observed/skipped/blocked/failed reporting |
| `.claude/skills/discover-storyboard/SKILL.md`: Step 7d per-screen progress line, Step 8 report | `references/ui-observation.md` "Walk" step 7 | Adapted: milestones per flow (flow, states against budget, observed versus failed), not a line per screen |
| `.claude/skills/eventmodeling-core-rules/SKILL.md` lines 175-182: Structural Shapes (bed, left chair, right chair, shelf; thresholds and what to ask) | `references/candidate-inventory.md` section 7 | Adapted closely; the bed softened to Screenplay's several-actions-per-screen semantics, shorthand names kept internal |
| `.claude/skills/discover-storyboard/SKILL.md`: chapters, grid cells, `create_screen`, HTML reconstruction as default, screenshots storage, credentials from URL | not used | Board transport dropped; HTML reconstruction is not evidence; credentials never guessed |
| `.claude/skills/analyze-existing-model/SKILL.md`: Steps 5a-5f (element inventory, status breakdown, spec coverage, structural gaps per slice type, orphans, structural shapes) and Step 6 report format | `references/candidate-inventory.md`, `SKILL.md` "L4" step 5 and "Gate" | Translated to `.play` constructs and the evidence table; read-only rule kept; board API, `validate_model` and status values dropped |

### Eventmodelers Build-Kits

Source: https://github.com/Nebulit-GmbH/Eventmodelers-Build-Kits, by Martin Dilger and Nebulit GmbH
(https://nebulit.de), used with the explicit consent of Martin Dilger and Nebulit GmbH.

| Source skill | Used in | How |
|---|---|---|
| `analyze-code-base` | `references/re-entry-and-passes.md` sections 1 and 2; `references/evidence-table-template.md` sections 6-9 | adapted closely: re-entry by recorded commit with incremental rereading, breadth first then one feature per pass with a detail budget, one feature per user journey, "deeper means more business"; the history, where-it-lives, decision and term tables; board, lane and chat mechanics dropped, the evidence table is the record |
| `analyze-code-base` | `references/signal-to-intent.md` (tests, reference data, generic engines, order, outbound outcomes); `references/interview-findings.md`, `references/ui-observation.md` | adapted closely: tests as hypotheses, a table read but never written is outside data, generic engines modeled by real cases, order taken from a walk or an expert, the running instance asked once and never a gate; restated as Screenplay evidence rules |
| `learn-architecture` | `references/evidence-table-template.md` section 10; `references/signal-to-intent.md` | adapted closely: record how the system realizes each kind of behavior and treat deviations as questions; a section of the evidence table, not a separate file |
| `domain-story` | `references/expert-verification.md` *Story retelling* | adapted closely: numbered "who does what to which work object" sentences, 3 to 15 per feature, gaps stated as sentences; picture and board links dropped; the form is Domain Storytelling by Stefan Hofer and Henning Schwentner |
| `analyze-code-base` | `references/re-entry-and-passes.md` sections 2 to 4 | adapted closely: a pass covers the whole journey, skip a phase interview where the evidence answers it, the pass check, propose first and ask one question, an answer is a decision, the analysis log promoted as committed model notes (one note per run) and anchored questions; board and chat mechanics dropped |
| `analyze-code-base` | `references/signal-to-intent.md`, `references/interview-findings.md`, `references/ui-observation.md` | adapted closely: callers as personas, code comments as rules at `Read` strength, visible labels as business terms, CI over README, three to twelve states per flow |
| `learn-architecture` | `references/evidence-table-template.md` section 11; `references/signal-to-intent.md` *Targeted read paths*; `references/side-car-migration.md` | copied and translated: the per-slice map of the code that realizes each slice and the write, read and background path traces, with the reference-feature advice; verification by running the legacy build dropped because it needs an approval |
| `record-evidence` | `references/evidence-table-template.md` section 11 *Field chain* | adapted closely: request field to entity property to column |
| `walkthrough` | `cratis-screenplay-model-review` `references/walkthrough.md`; `references/expert-verification.md` impact view | see the review skill's provenance |
| `domain-story` | `references/expert-verification.md` *Story retelling* (sentence form per slice kind, naming, scope) | adapted closely: the sentence forms per slice kind, one name per actor, one present-tense verb; picture dropped |

## Method lineage

The idea below is in our own wording; no upstream text, table or template is reused for it.

| Idea | Where used | Credit |
|---|---|---|
| Keep / change / new / remove impact view | `references/expert-verification.md` | Martin Dilger / Nebulit GmbH, Eventmodelers build kits (`walkthrough`), used with their explicit consent; the procedure itself is in the review skill's walkthrough reference |
