<!-- cratis-ai-managed: skills/cratis-screenplay-legacy-extraction/references/provenance.md -->
# Provenance

| Content | Source | Licence | How used |
|---|---|---|---|
| Interview protocol (critical questions, follow-up triggers, interview flow, findings capture, green-light checklist) in `SKILL.md` and `references/interview-findings.md` | TrogonStack/agentskills `plugins/trogonstack-eventmodeling/skills/eventmodeling-integrating-legacy-systems/SKILL.md` at `7b249d3ee42d8b7e11fa564141ebd5fbc37aadf1` | MIT, Copyright (c) 2025 Straw Hat, LLC (notice in `LICENSE`) | Adapted closely: questions restructured around Screenplay extraction, the Cratis toolchain and the approval rules; the freeze question kept as the first gate |
| Freeze agreement, side-car pattern, extraction options, traffic routing phases, integration patterns, anti-patterns, quality checklist in `references/side-car-migration.md` | same file | MIT (as above) | Adapted: the target store is Chronicle, extraction is a code or Prologue concern, and the new behaviour is authored as a Screenplay model; the dual-write exception and its controls are kept |
| Evidence ladder, locators, loss report, unknowns, generator/Prologue coverage notes, rule modeling, signal-to-intent table, `references/intent-example.md` | Cratis/AI corpus drafts for this issue (#493), written for Cratis | MIT (Cratis) | Own work |

Verified product facts are listed under *Verified product sources* in `SKILL.md`.

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
