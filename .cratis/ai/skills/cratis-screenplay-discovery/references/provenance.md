<!-- cratis-ai-managed: skills/cratis-screenplay-discovery/references/provenance.md -->
# Provenance

## Adapted closely (MIT, TrogonStack/agentskills)

Source: https://github.com/TrogonStack/agentskills at commit `7b249d3ee42d8b7e11fa564141ebd5fbc37aadf1`,
Copyright (c) 2025 Straw Hat, LLC. The full notice is in this skill's `LICENSE`.

| Source file (under `plugins/trogonstack-eventmodeling/skills/`) | Used in | How |
|---|---|---|
| `eventmodeling-brainstorming-events/SKILL.md`: Interview Phase (when to interview, critical questions with impact, why it matters and follow-up triggers, conditional entry, two-pass flow) | `SKILL.md` "Interview phase", `references/interview-questions.md` "Interview flow" | structure and four questions adapted; results land in STATE.md instead of a trail file |
| same: Workshop Facilitation Guide (goals, free brainstorm, gentle filtering dialogue, key points, tips for facilitators) | `references/human-workshop.md`, `references/facilitation.md` | adapted to marina examples and Screenplay; the agent scribes |
| same: Role Catalog (mandatory; description, key actions, permissions boundary; system actors with triggers) | `references/personas-and-causes.md`, `SKILL.md` step 2 | adapted to `persona` Does / Reads / Cannot and non-human causes; Cannot resolved to executable gates |
| same: Best practices and Quality Checklist | `SKILL.md` Gate, `references/event-naming.md` | adapted; absolute "all error conditions have events" not adopted |
| `eventmodeling-brainstorming-events/references/facilitating-event-modeling-workshops.md`: participants, invitation, workspace, templates, pacing, personalities, disagreement, remote, post-workshop, multi-day, checklists, success indicators | `references/human-workshop.md` | structure adapted closely; mapped to Screenplay constructs and Cratis phases |
| `eventmodeling-plotting-events/SKILL.md`: sequence, dependencies (can only happen after / triggered by / precondition), alternative paths, output format, quality checklist, principles | `references/plotting-and-handoff.md`, `SKILL.md` step 5 | adapted: after / caused by / only if in the slice `description`; plot format and checks |

Not adopted from the same sources, and why: the "never use an aggregate" rule and per-command state
examples (stream and state design belong to `cratis-screenplay-streams-and-consistency`);
the `.trogonai/interviews/` trail file (replaced by bounded STATE.md); e-commerce examples and
Gherkin forms; the `total (calculated)` field example (contradicts the calculated-value rule);
"capture every error condition as an event" (most failures are rejections).

## Adapted closely (Martin Dilger and Nebulit GmbH)

Source: https://github.com/Nebulit-GmbH/agentic-engineer at commit `07b0f30648d663cb588d7e2c7aa031af9dfc21f2`,
by Martin Dilger and Nebulit GmbH (https://nebulit.de), used with the explicit consent of Martin Dilger and Nebulit GmbH.

| Source file | Used in | How |
|---|---|---|
| `.claude/skills/timeline/SKILL.md`: fixed opening question, build first then summarize, one question per turn, stop conditions | `SKILL.md` "Interview phase", `references/storming-loop.md`, `references/interview-questions.md` | translated to Screenplay; opening question kept close to the original |
| `.claude/skills/eventmodeling-interview-protocol/SKILL.md`: when to ask, skip what is known, assume visibly when told not to ask | `SKILL.md` "Interview phase" | Adapted closely; trail file replaced by STATE.md |
| same: findings recorded per step | `SKILL.md` "Interview phase", `references/interview-questions.md` "Interview flow" | translated: Decisions and Assumptions lists in STATE.md |
| `.claude/skills/eventmodeling-storyboarding-events/SKILL.md`: step 6, processor todo list walked as a storyline | `references/plotting-and-handoff.md` "Storyline" | Translated to a view walkthrough seed for lifecycle specifications |
| `.claude/skills/eventmodeling-brainstorming-events/SKILL.md`: divergent journey vs decision point (Nebulit addition) | `SKILL.md` step 5 | idea; our own wording kept (outcome branch vs separate story) |
| same: essential fields only, no padding, payload-free simple transitions (lines 159-160) | `SKILL.md` step 6, `references/storming-loop.md` | Adapted closely; identity, concepts and `pii` rules kept |
| same: handover from an outside party is a single moment, not a relocation | `SKILL.md` Modeling defaults | idea; our own wording kept |
| `.claude/skills/eventmodeling-storyboarding-events/SKILL.md`: "whose turn is it in the story?" (consistent actor perspective) | `references/plotting-and-handoff.md` "Whose turn is it in the story?" | adapted closely; lanes, columns and screen placement not adopted |
| `.claude/skills/eventmodeling-brainstorming-events/SKILL.md`: Domain Analysis document (roles, entities, processes, candidate events, rules, questions), "Questions for Domain Expert", "Best Practices for Requirements Analysis", quality checklist | `references/domain-analysis-template.md` | Adapted closely as an optional work record in `.ai-work/screenplay/`; roles use Does/Reads/Cannot, chapters, cells, swimlane rules and `meta.fields` dropped, "all error conditions have events" replaced by the refusal-versus-fact question |
| `.claude/skills/eventmodeling-core-rules/SKILL.md`: Offline-First Thinking | `references/divergent-sweep.md` lens "Without software" | Promoted to a named lens; our own wording |

Not adopted: board, chapter, column and `place-element` mechanics; the board API and `connect` skill.

### Eventmodelers Build-Kits

Source: https://github.com/Nebulit-GmbH/Eventmodelers-Build-Kits, by Martin Dilger and Nebulit GmbH
(https://nebulit.de), used with the explicit consent of Martin Dilger and Nebulit GmbH.

| Source skill | Used in | How |
|---|---|---|
| `summarize` | `references/domain-analysis-template.md` *Intake rigor for documents* (now pointing to `references/document-intake.md`); `SKILL.md` *Evidence and privacy* | adapted closely: stated, implied or assumed marks, figures kept exactly with a locator, a contradiction as one question naming both passages, a stated perspective, a coverage pass against the model, changes marked on an amended document |
| `summarize` | `references/document-intake.md` | copied and translated: the fact kinds, reading rules for long, image and multi-file sources, the fact-to-model mapping, the faithfulness rule on generic knowledge, feature sizing, the per-feature blueprint summary (a generated report, never a second source); chapter and board mechanics dropped |
| `timeline` | `references/facilitation.md` | idea, own wording: a hotspot is a note beside the event, never an event |
| `brainstorming-events` (`eventmodeling-brainstorming-events`) | `references/plotting-and-handoff.md` "One feature, one story"; `references/human-workshop.md` "Run sheet: design session" | copied and translated: a chapter is a journey and not an operation, the too-fine signals, merge when in doubt; the storyboarding, inputs, outputs and boundaries steps with the shown-versus-entered correction |
| `core-rules` (`eventmodeling-core-rules`) | `references/event-naming.md` "Concrete over generic"; `SKILL.md` modeling defaults | copied and translated: one real flow with real values, also on a generic platform |

## Original to this corpus

The divergent sweep lenses, the not-an-event filter and its exceptions, the edit kinds, the
compiled discovery skeleton, persona Cannot resolution to gates and denial specs (S6), the
compiler-contract notes and identity-affecting edit routing.
