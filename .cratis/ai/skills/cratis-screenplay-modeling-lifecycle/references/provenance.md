<!-- cratis-ai-managed: skills/cratis-screenplay-modeling-lifecycle/references/provenance.md -->
# Provenance and licences

## Contents

- Shared template for sibling skills (copy this header; replace the tables)
- 1. Adapted from MIT sources
- 2. Adapted closely (Martin Dilger and Nebulit GmbH)
- Eventmodelers Build-Kits
- Not adopted from agentic-engineer (rejected, with reasons)
- 3. Method lineage

## Shared template for sibling skills (copy this header; replace the tables)

Every new Screenplay method skill keeps a `references/provenance.md` shaped like this one. Three
sections, in this order:

1. **Adapted from MIT sources.** One row per passage, procedure, checklist, interview protocol or
   output format adapted closely from a licensed source: skill and file in the source, source
   commit, what it became here (section or reference), and how it was adapted. If this section is
   not empty, the skill `LICENSE` carries the "Third-party notices" section with the **complete**
   MIT text (copyright line, permission notice and disclaimer). Closely adapted material is
   attributed to the source it came from, not to a neighboring one.
2. **Adapted closely (Martin Dilger and Nebulit GmbH).** One row per item taken from Martin Dilger and Nebulit GmbH's
   agentic-engineer: source file, where it is used, how. Mark "idea; our wording kept" where only
   the idea was taken. The skill `LICENSE` carries the Nebulit notice.
3. **Method lineage.** Public method sources (books, talks, standards), cited as sources of ideas.

Do not name a person's tooling, a harness's model or a private path. Update the commit pins here
when a source is rechecked, and say what was rechecked. A shingle (8-word overlap) scan across
the changed artifacts is a warning aid only, never legal clearance.

## 1. Adapted from MIT sources

Source: TrogonStack/agentskills, plugin `trogonstack-eventmodeling`, commit
`7b249d3ee42d8b7e11fa564141ebd5fbc37aadf1`. MIT licence, Copyright (c) 2025 Straw Hat, LLC. The
full notice is in this skill's `LICENSE` ("Third-party notices").

| Source skill and file | Became | Adaptation |
|---|---|---|
| `eventmodeling-orchestrating-event-modeling/SKILL.md`, "Workflow" (per-step **Input / Output to carry forward / Gate**) | `phases.md`, every phase P0-P9 | The template is applied to the Screenplay lifecycle; steps, inputs and gates rewritten for `.play` work |
| same file, "Interview Phase" (skip condition, five intake questions, one confirmation sentence) | `SKILL.md` "Interview phase (P0)" | Questions and confirmation sentence adapted to Screenplay modes; unattended behavior added |
| same file, "Capture findings" and Interview Trail table | `handoff-template.md` section 1 | The trail moved into STATE.md under `.ai-work/screenplay/`, with an asked/assumed column |
| same file, "Mid-Workflow Entry" | `SKILL.md`, `phases.md` "Resume mid-workflow" | Entry points table and identity recomputation added |
| same file, "Final Output" and "Quality Checklist" | `phases.md` "Final output" and "Quality checklist (closing)" | Items rewritten around Screenplay verdicts, specifications and review |

## 2. Adapted closely (Martin Dilger and Nebulit GmbH)

Source: https://github.com/Nebulit-GmbH/agentic-engineer at commit `07b0f30648d663cb588d7e2c7aa031af9dfc21f2`,
by Martin Dilger and Nebulit GmbH (https://nebulit.de), used with the explicit consent of Martin Dilger and Nebulit GmbH.

| Source file | Used in | How |
|---|---|---|
| `.claude/skills/eventmodeling-orchestrating-event-modeling/SKILL.md`: "Do not cut corners to save tokens or effort" (forbidden rationalizations, self-catch trigger, flag trade-offs to the user) | `SKILL.md` "Do not cut corners to save tokens or effort", `references/completeness-self-check.md` | Adapted closely. Board rationalizations (delete a node to dodge a placement conflict, "session-context" read model, collapsed translation chain) translated to Screenplay (modeled reaction, missing view, translation merged into worker behavior, denial and protection) |
| same file: "Phase Transition Protocol" (what was done, carry-forward, open questions; trail row to Done) | `SKILL.md` "Lifecycle", `references/phases.md` "Phase transition protocol" | Adapted closely. Memory file replaced by STATE.md |
| same file: "Documenting decisions inline" and Step 11 "What the note should actually contain" (cold reader, simple chapter stays brief) | `references/reasoning-notes.md` | Adapted closely. Feedback-lane reasoning uses `documentation`; counts and verdicts stay in STATE.md |
| same file: "Interview Phase" and "Mid-Workflow Entry" | `SKILL.md` "Interview phase (P0)" | Already adapted from TrogonStack (section 1); Martin Dilger and Nebulit GmbH's confirmation sentence kept |
| `.claude/skills/eventmodeling-interview-protocol/SKILL.md`: "When to ask", "Unless told not to ask", "Recording the outcome" | `references/stop-or-assume.md` "Interview protocol" | Adapted closely. Interview file replaced by the STATE.md Interview Trail |
| `.agent-modeling-kit/CLAUDE.md`: per-turn steps (screen the prompt, connect once, resolve context, start marker, invoke the matched skill never raw calls, questioning rule, close, learnings) | `references/per-turn-protocol.md`, `SKILL.md` "Run protocol" | Adapted closely. Board prompts and status calls translated to briefs, `Active:` in STATE.md and the `Outcome:` line; `<promise>` sentinels, `progress.txt` and learnings promotion not adopted |
| `.agent-modeling-kit/AGENTS.md`: diagnose why a command failed before retrying it | `references/per-turn-protocol.md` step 5 | Adapted closely; unknown-outcome MCP applies are never retried (toolchain trap 42) |
| orchestrating `SKILL.md`: cross-context and integration gaps stated with the finding and the viable resolutions, matching the posted question | `references/reasoning-notes.md` item 6 | Adapted closely; the comment becomes an open question or STATE.md decision |
| `.agent-modeling-kit/CLAUDE.md`: "never leave a prompt neither progressed nor closed" | `SKILL.md` "Run protocol", `references/stop-or-assume.md` | Idea; our wording kept |
| `.claude/skills/request-feedback/SKILL.md`: when escalation applies and when it does not; do not guess and build anyway; write the question for a cold reader | `references/stop-or-assume.md`, `references/handoff-template.md` section 4 | Idea; our wording kept (question format, calibration) |
| `.claude/skills/update-prompt-status/SKILL.md` and kit "exactly two status updates per prompt" | `references/per-turn-protocol.md` step 7 | Idea; one start marker and one closing outcome per turn, our wording |
| `.claude/skills/eventmodeling-core-rules/SKILL.md`: modeling and critic postures never mixed; a known refusal is a documented rule | `SKILL.md` "Modes", "Do not cut corners to save tokens or effort" | Idea; our wording kept |
| `.claude/skills/connect/SKILL.md`: discover capabilities once per session | `SKILL.md` "Run protocol", `references/identity-and-edits.md` | Idea; our wording kept |
| `.claude/skills/eventmodeling-core-rules/SKILL.md`: modes (modeling versus critic), element definitions and naming, causality, role catalog, field lineage, open question versus decided failure, offline-first, anti-patterns, structural shapes, quick self-check | `references/core-rules-digest.md` | Adapted closely as a one-page card translated to `.play` constructs. Not adopted: lanes, the single-issuer command rule, linked copies, the translation chain with a never-closing todo list, "read model never drives command validation" (Screenplay `reads` + `require`), the update-feeding-read-model procedure; "bed" is a candidate signal here, not an always-flag |

## Eventmodelers Build-Kits

Source: https://github.com/Nebulit-GmbH/Eventmodelers-Build-Kits, by Martin Dilger and Nebulit GmbH
(https://nebulit.de), used with the explicit consent of Martin Dilger and Nebulit GmbH.

| Source skill | Used in | How |
|---|---|---|
| `eventmodeling-core-rules` ("Concrete Over Generic") | `references/core-rules-digest.md` *Anti-patterns to reject early*, *Quick self-check*; `cratis-chronicle-event-modeling` `SKILL.md` | copied and translated: one real flow with real values instead of a generic structure, with the carve-out for a platform that is itself the product |
| `eventmodeling-orchestrating-event-modeling` (every role has a screen) and `eventmodeling-storyboarding-events` | `references/phases.md` P2 gate | adapted closely: a persona's Does and Reads lines reach a screen; a read-only persona stays legitimate |
| kit `CLAUDE.md` (only touch slices whose status is `Created` unless confirmed; a subagent inherits no session state) | `references/phases.md` *Slices under delivery*; `references/per-turn-protocol.md` steps 2 and 3 | adapted closely: delivery status in the plan replaces board status; naming the slice is the approval for exactly those declarations |
| `eventmodeling-designing-event-models`, `eventmodeling-core-rules`, `eventmodeling-plotting-events` | `cratis-chronicle-event-modeling` `SKILL.md` *Decide before implementing*, *Verify the brief* | adapted closely: command issuers, story order, refused transitions and the rejected event kinds, for the code-first brief |

## Not adopted from agentic-engineer (rejected, with reasons)

Recorded once here for the whole method family, checked against the same commit
(`07b0f30648d663cb588d7e2c7aa031af9dfc21f2`). A rejection stays rejected unless a released Screenplay or
Chronicle capability removes its reason.

| Source file or topic | Reason |
|---|---|
| `connect`, `learn-eventmodelers-api`, `load-slice`, `update-prompt-status`, and the 24 `references/api-fallback.md` files | Transport and credentials for the eventmodelers board. The Screenplay MCP server (`cratis screenplay mcp`) is the only transport; its tool catalog is `cratis-screenplay-model-authoring` `references/mcp-tools.md` and the root is `mcpServers.screenplay.root` in `.cratis/ai.json` |
| `place-element`, `html-screen`, `storyboard-screen`, `discover-storyboard` HTML reconstruction | Board lanes, columns, cells and HTML or wireframe rendering. Screenplay screens are declarative and Stage renders them; a recreated page is never evidence |
| `.agent-modeling-kit/README.md`, `lib/config.js`, `package.json`, `package-lock.json`, root `CLAUDE.md` and `README.md` | Board runtime, config walk, package stubs and an Emmett/Postgres/Express/Flyway template; Chronicle and Arc are the runtime here |
| `.agent-modeling-kit/CLAUDE.md` and `AGENTS.md`: `<promise>`, progress file, board prompt queue, post-question-and-continue autonomy | Board-specific loop control. Our per-turn protocol and stop-or-assume keep the useful parts, and Cratis stops for consequential unknowns |
| `eventmodeling-designing-event-models/references/examples.md` | The order example embeds a calculated `total` event field and a generic domain; Cratis examples are compiled |
| Single issuer per command; a screen wired to more than one command always fails; "the bed" as an always-flag | Screenplay commands may have several origins and a screen several actions; the shape stays a candidate signal |
| "A read model never drives command validation" | Screenplay commands declare `reads` and `require`; a decision uses stored state only through declared `reads` |
| Compulsory todo list per automation; never-closing translation queue; every trigger is an event | Direct reactions, clock and application triggers and `capture` are supported; a translation holding pending work gets a terminal disposition |
| "Default gracefully" and log-and-drop for missing external data | Defaults are never fabricated and failures are recorded facts |
| Linked copies, backward-arrow handling, `session:` field mappings, `validate_model` batch calls | Board mechanics; caller identity is `context` and the model has no columns |
| "No natural end" red flag, event-granularity tip, archival splits, `no computed fields` as an absolute | Long-lived identities are normal here, generic update events are defects, and decided calculations are facts |
| Emmett decide/evolve structure, `processor.ts`, processor ids and locks, DLQ store, SQL operations, Flyway, Testcontainers, `routes.ts` | A different runtime; Chronicle and Arc own delivery, projection and transport |

## 3. Method lineage

- Event Modeling: Adam Dymitruk (eventmodeling.org): the four patterns, the workshop steps,
  information completeness.
- Understanding Eventsourcing: Martin Dilger. Practitioner guidance: Oskar Dudycz, Dennis Doomen,
  Greg Young, Mathias Verraes, Alberto Brandolini.
- Screenplay and Stage strategy: the model-first principles and rejected anti-patterns in
  `principles.md` come from the Cratis Screenplay roadmap and strategy decisions, written for this
  corpus.
- Phase gates, verdicts V1-V5, identity ownership, source identity, handoff packet and STATE.md are
  Cratis-specific constructions.
