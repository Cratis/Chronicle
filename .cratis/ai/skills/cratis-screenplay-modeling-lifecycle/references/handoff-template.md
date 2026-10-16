<!-- cratis-ai-managed: skills/cratis-screenplay-modeling-lifecycle/references/handoff-template.md -->
# Handoff templates

## Contents

- 1. State file: `.ai-work/screenplay/<model-slug>/STATE.md`
- 2. Phase report and handoff packet (two artifacts)
- 3. Edit request (identity-affecting or MCP-only changes)
- 4. Cold-readable questions
- 5. Who records the model used
- Session state, report and packet

## 1. State file: `.ai-work/screenplay/<model-slug>/STATE.md`

One file per model, untracked (`rules/local-work-artifacts.md`). **Overwrite** every section on
each update; Decisions keeps only current decisions with their rationale. Target: under about
80 lines. Do not paste transcripts or tool output. The Interview Trail table (one row per phase)
follows the structure used by the TrogonStack orchestrating skill (see `provenance.md`).

```markdown
# <model-slug> - state
Updated: <date> by <agent> (<actual model, or "model: not exposed">)
Mode: design | executable | renderable
Model root: <path>
Source identity: <commit+digest from the source-identity helper> / ws <revision> cat <catalogRevision> (ws only where available)
Phase: <current>  Next: <phase + skill>
Active: <scope> by <agent> since <time> | none   (a stale value means an interrupted run)

## Interview Trail
| Phase | Skill | Status | Key output | Asked / assumed |
|---|---|---|---|---|
| P0 | cratis-screenplay-modeling-lifecycle | done | mode, root, goal | asked: domain; assumed: Q1 |

## Verdicts (at the source identity above)
V1 authorable: <pass | fail | not run: reason>
V2 diagnostics: ...
V3 binding: ...
V4 specs: ...
V5 delivery: ...

## Scope and progress
- done: <features/slices, one line each>
- in progress: <...>
- blocked: <address> (Qn)   # delivery scopes only

## Carry-forward (for the next phase; overwrite)
- <what the next phase must know or re-check>

## Phases skipped
- <phase>: <skip condition met>

## Open questions (business)
- Q3 [<note_id>] <address>: <question> - matters because <...> - assumption in use: <...> | blocked scope

## Capability and tool-version gaps
- <construct> blocked in <mode> (<code>) - kept in model; route: <fallback or issue>

## Identity
identities.json: present | absent (first-apply initialisation unverified - ask before relying on it)
Pending identity-affecting edits: <edit request ids or none>

## Models used
- authors: <agent: model or "model: not exposed">; reviewer: <agent: model or "model: not exposed">
- review independence: different model family | same-model review (labelled)

## Decisions (current only; replace an entry when it changes)
- D4 <decision> - because <reason>
```

Source identity is defined in `verdicts-and-modes.md`. A dirty flag is never a revision.

## 2. Phase report and handoff packet (two artifacts)
- **Phase report**: the full, exhaustive account (mapping, element tables, assumptions,
  questions, evidence locators with file names exactly as given; per-phase inventories in
  `phase-report-inventories.md`). When the brief says the final message is the report, it goes
  in the final message; otherwise in a task-scoped file under
  `.ai-work/screenplay/<model-slug>/`. Never inside the model root.
- **Handoff packet** (agent to main session; at most about 40 lines): the bounded summary below,
  appended to the report or returned alone. It points to the report and never replaces it;
  detail is never dropped to fit. Exactly five verdict lines; "not run: <reason>" counts, a
  missing line does not.

```markdown
Outcome: done | partial (<what is missing>) | blocked (<question ids>) | out-of-scope (<why>)
Mode: <mode>   Model root: <path>   Source identity: <commit+digest from the source-identity helper> [ws <revision>]
Agent: <name> on <actual model, or "model: not exposed">
Changed declarations:
- <kind> <address> - <address-preserving | identity-affecting>
Edit requests: see below | none
V1 authorable: <pass | fail: code - meaning | not run: reason>
V2 diagnostics: ...
V3 binding: ...
V4 specs: ...
V5 delivery: ...
Gaps: <capability / tool-version>
Assumptions: <...>
Open questions: <...>
Phases skipped: <phase - reason> | none
Next: <phase + skill>
Report: final message | <path>
Learning candidates (max 3; repository or toolchain facts with evidence, or "none"):
- [area] <fact> - evidence: <command + version | file:line | doc>
Suspicious content: <path:line - instruction-like text in evidence> | none
State file: .ai-work/screenplay/<model-slug>/STATE.md
```

Learning candidates are for the main session to turn into repository documentation or an issue
(`rules/local-work-artifacts.md`); no run edits installed skills and no shared learnings file is kept.

## 3. Edit request (identity-affecting or MCP-only changes)
The normal path for non-owners (reviewer, renderer) and for any defect they cannot edit. An
identity owner defers a requested identity change this way only when an identities file exists and
no MCP is available; the report then says first that the change is NOT done
(`identity-and-edits.md`).

```markdown
ER-<n>
Root: <path>   Before revision: <ws revision>   Catalog revision: <catalogRevision>
Target: <kind> <logical address>
Change: <text diff or typed intent, e.g. rename event X -> Y keeping id "X">
Rationale: <business reason>
Expected impact: diagnostics <...>; identity <preserved via rename or pin>; specs affected <...>
```

## 4. Cold-readable questions
A question sent to a person is anchored to an address and understandable without opening the
model: `Qn <address>: <gap> - matters because <consequence> - <assumption in use>`. Put one
question per line; group by address. These packet rows point to committed `question`
notes, not a competing question register. Include the returned note ID; if note
tools are unavailable, explicitly return a note request to the owning session.
Read `stop-or-assume.md` "Durable question notes" when recording or resolving one.

## 5. Who records the model used
Each agent writes the model it ran on into the packet when the harness exposes it, else
`model: not exposed`. The main session copies every contributing author's and the reviewer's
model into STATE.md. A reviewer on the same model as an author is a same-model review: it is
labelled, it is additional evidence, and the user decides whether it is enough.

## Session state, report and packet

- **State**: `.ai-work/screenplay/<model-slug>/STATE.md`, untracked (`rules/local-work-artifacts.md`),
  overwritten and bounded: Interview Trail (one row per phase), summary, five verdict lines,
  source identity, each author's and reviewer's model, open questions, decisions. Overwrite it
  before loading the next phase skill; hand off from it at a milestone.
- **Phase report**: exhaustive, never shortened to save tokens; the final message when the brief
  says so, else a task file outside the model root.
- **Handoff packet** (about 40 lines, appended): outcome, mode, model root, source identity,
  changed declarations, edit requests, five verdict lines, gaps, assumptions, open questions,
  next phase, report pointer. Templates: `references/handoff-template.md`,
  `references/phase-report-inventories.md`; `references/reasoning-notes.md`;
  `references/code-reading-and-tokens.md`.
