<!-- cratis-ai-managed: skills/cratis-screenplay-scenario-coverage/references/provenance.md -->
# Provenance

Lineage of `cratis-screenplay-scenario-coverage`. The method was first drafted for the Cratis
corpus from Screenplay specification semantics and refined against two
external event-modeling skill sets.

## Adapted closely (MIT)
Source: TrogonStack/agentskills at `7b249d3ee42d8b7e11fa564141ebd5fbc37aadf1`, MIT, Copyright (c)
2025 Straw Hat, LLC. Full notice in this skill's `LICENSE`.

| Source path | Adapted into | How |
|---|---|---|
| `plugins/trogonstack-eventmodeling/skills/eventmodeling-elaborating-scenarios/SKILL.md`, Interview Phase and Critical Questions | `SKILL.md` "Interview phase" | Skip condition and four critical questions kept in structure (coverage depth, known edge cases and rules, how scenarios will be used, who reviews); rewritten for Screenplay specifications, the coverage matrix and unattended assumptions |
| same file, Workshop Facilitation Guide (Before the Workshop, During per command and view, Multi-Role Review, Common Workshop Mistakes, Tips for Rapid Creation) | `references/scenario-workshop.md` | Structure and the six-step per-command cycle adapted; steps extended to the full scenario catalogue; time-boxes and per-command quotas deliberately dropped because they conflict with obligation-driven coverage |
| same file, Quality Checklist and Gherkin Best Practices (explicit givens, explicit outcomes, named reasons), and its Workflow sections 1-5 (command, state validation, view, error path, compensation) | `SKILL.md` "Quality checklist", `references/scenario-examples.md` "Good and bad forms" and categories | Mapped onto `.play` specification style (exact events, pinned messages, `for`, one behavior per spec) |
| same file, Scenario Organization (happy path, validation, state violation, duplicates, alternatives, error handling, compensation) | `references/scenario-catalogue.md` ordering, `references/scenario-examples.md` categories | Extended with denial, competing claim, ordering, evolution and view types |

## Adapted closely (Martin Dilger and Nebulit GmbH)

Source: https://github.com/Nebulit-GmbH/agentic-engineer at commit `07b0f30648d663cb588d7e2c7aa031af9dfc21f2`,
by Martin Dilger and Nebulit GmbH (https://nebulit.de), used with the explicit consent of Martin Dilger and Nebulit GmbH.

| Source file | Used in | How |
|---|---|---|
| `.claude/skills/eventmodeling-elaborating-scenarios/SKILL.md`: Interview Phase, Workshop Facilitation Guide, Multi-Role Review, Common Workshop Mistakes, Tips for Rapid Creation | `SKILL.md` "Interview phase", `references/scenario-workshop.md` | Text shared with TrogonStack's version (rows above, MIT; rationale, "what if" capture and multi-role review passages are not Nebulit-only); the Nebulit file is recorded here for completeness. Time-boxes and the three-to-five quota are kept only as optional pacing (`scenario-workshop.md` "Optional pacing"), subordinate to the coverage matrix |
| same file: GWT vs. Storyline decision rule, Workflow 0 (decide per read model before drafting any payload; one reused schema may indicate a skipped judgment), todo lists as the prime storyline candidate, repeated event type as a driver | `SKILL.md` Procedure 5, `references/view-and-story-specs.md` "Worked family: a todo list" | Adapted closely; storylines become lifecycle families of independent specs because Screenplay has no multi-step specification; storyline JSON, beats and layout dropped (board transport) |
| same file: Scenario Types (seven questions, "not a good/bad pair", skip only for a domain reason) | `references/scenario-catalogue.md` | Idea; our own, broader catalogue kept |
| same file: Quality Checklist (no command has only two scenarios; read models a separate, equally mandatory pass; why behind rules documented) | `SKILL.md` "Quality checklist" | Adapted closely; board posting and `expectEmptyList` items dropped |
| same file: Gherkin Best Practices (good and bad givens, outcomes, rejections), Key Principles | `references/scenario-examples.md` "Good and bad forms" | Shared with TrogonStack's version (row above); adapted closely, mapped to `.play` specification style |
| `.claude/skills/eventmodeling-elaborating-scenarios/references/examples.md`: command, state validation, view, list-view, error-path and compensation examples | `references/scenario-examples.md` | Category structure and cases (retry after failure, state violations, reversal with a consequence, list and accumulating views) adapted closely; shared with TrogonStack's version for most categories; rewritten as Screenplay specifications in the compiled `references/invoicing-dues-example.md` (runnable) and `references/invoicing-dues-design.md` (design mode, stored-state rules), with the capture, retry and refund-request specs added by us |
| `.claude/skills/examples/SKILL.md`: neighborhood-first reading, never overwrite existing examples, short domain-specific values, report fields changed and reused | `references/scenario-catalogue.md` "Example data" | Handoff report (fixtures changed, preserved, reused value origins, inconsistencies) adapted; rest idea, our own wording kept (board tooling, `add_field_examples` and name-based type guessing dropped because declared types decide values) |
| `.claude/skills/eventmodeling-elaborating-scenarios/SKILL.md`: Step 3 skip unresolved steps, board-only restrictions, "every scenario produces events" | none | Considered and not adopted: removing a prerequisite changes the test; Screenplay has rejections and queries |

### Eventmodelers Build-Kits

Source: https://github.com/Nebulit-GmbH/Eventmodelers-Build-Kits, by Martin Dilger and Nebulit GmbH
(https://nebulit.de), used with the explicit consent of Martin Dilger and Nebulit GmbH.

| Source skill | Used in | How |
|---|---|---|
| `eventmodeling-elaborating-scenarios` | `references/scenario-catalogue.md` sections 2 and 5, *Example data*; `references/scenario-examples.md`; `references/coverage-matrix.md` *Cell values*; `SKILL.md` | adapted closely: a rejection earns a scenario only if a domain expert could recognize the rule, the duplicate question put as a business question, essential values shown in the body with the rest left out, descriptions that carry no fact a step could state, and a closed list of technical cases left unspecified; concept-rule and once-only specifications are kept on purpose, and a repeat the business would notice is never technical |
| `eventmodeling-elaborating-scenarios` (examples, error-state and compensation scenarios) and `eventmodeling-identifying-outputs` (error states shown) | `references/scenario-catalogue.md` sections 9 (*Shown*, *Recoverable*) and 10 (follow-up work after a reversal) | copied and translated: the payment-failure view update and the retry after failure, and the compensation triggered by a cancellation, restated as view and retry specifications and an Automation-slice specification; the board scenario syntax is dropped |

## Cratis-original
Coverage matrix and obligation derivation from declarations, cell vocabulary (`spec`, `n/a`,
`recorded`, `open`, `question`, `gap`), scenario types per Screenplay semantics, denial fixtures
from the Boolean gate, "once, idempotent, exactly once" separation, view lifecycle families,
Chronicle verification obligations, version skew for reaction cascades, spec forms per mode, and
the berth reservations example.

## Product facts checked
- Screenplay `Documentation/screenplay/specifications.md` (actions, outcomes,
  `then denied`, `then no readmodel`, `then query`, `then events in any order`, PLAY0352,
  PLAY0389) and the capability notes in `cratis-screenplay-toolchain`
  `references/executable-subset.md`.
- Stage `STAGE-ESM-015` and `STAGE-ESM-011` admission diagnostics for policies and
  fixtures (`Documentation/guides/build-renderer-target.md`).
