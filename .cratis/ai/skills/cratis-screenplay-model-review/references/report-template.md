<!-- cratis-ai-managed: skills/cratis-screenplay-model-review/references/report-template.md -->
# Review report template

## Contents

- Finding fields
- Root causes
- Defect or business question?
- Question notes
- Verdict rules
- Findings across a re-review
- Wording

Keep the report to findings and evidence; no file dumps. Addresses and paths, not contents.
The check-by-check layout (per phase: how many checks; per check: status, element, evidence;
anti-patterns; final questions; verdict; success criteria) is adapted from TrogonStack
`eventmodeling-validating-event-models-checklist` (see `provenance.md`). A complete filled
example: `worked-example.md`.

When the brief says the final message is the report, the whole report is that message. Never
write the report into the model root.

```text
# Model review: <model or scope>
Mode: <design|executable|renderable>   Stance: critic   Review kind: <self-check|independent|audit|proposal>
Model root: <path>
Source identity: <commit + digest from the source-identity helper, cratis-screenplay-modeling-lifecycle references/verdicts-and-modes.md "Source identity">
Workspace: <modelRevision / catalogRevision from MCP, when connected>  (a different thing; never the source identity)
Scope: <whole model | module/feature/slice list | edit request id>
Authors: <agent>, model: <model or "not exposed">, one entry per contributing author
Reviewer: <agent>, model: <model or "not exposed">, fresh context: yes|no
Independence: independent (fresh context, different model family from every author)
            | same-family (additional review; the user decides whether it is enough)

## Verdict lines (independent; each is a result or "not run: reason")
V1 authorable:                <pass (tool, version, N files) | fail: PLAYnnnn at file:line>
V2 executable diagnostics:    <clean | n diagnostics: codes | not run: reason>
V3 binding-ready:             <ready | blocked: codes | not run: reason>
V4 reference specs run:       <route, expected vs discovered, passed/failed/unsupported; overall unbound | not run: no route>
V5 rendered, target-verified: admission <...>; publication <...>; build <...>; tests <...>   (each "not run: reason" when absent)
Gaps: <capability gaps and tool-version gaps, e.g. a false PLAY0285 on cascades from a cratis before 3.28.2 bundle>

## Inventory (read-only audit; MCP describe-application view=summary, find-assertion-gaps)
Modules n, features n, slices n (StateChange n, StateView n, Automation n, Translate n),
events n, commands n, read models n, specs n.
Slices with at least one assertion: n of total (pct). Without: <names, at most 10, then "and N more">.
Structural gaps per slice (skip gaps documentation explicitly calls intentional): <slice: what is missing, or none>.

## Phase 1: Element sweep (7 checks)
Check S1: <short title>
Status: PASS | FINDING #n | N/A: <reason> | NOT RUN: <reason>
Element: <address of the declaration>
Evidence: <the sweep line: names, counts, the spec found or NONE>
...one block per check, every id in the phase...

## Phase 2: Entity walk (5 checks)
...
(one section per phase of checklist.md, in order, each stating its check count)

Sweep summary: gated elements n, with denial spec n, NONE: <names>; field-copy signals: <names or none>.

## Automation chains (F8-F13; every Automation and Translate slice)
| Declaration | Location | Pending-work view | Opened by | Closed by | Result (complete / open / blocked) |

## Lineage matrix (scope)
| Element.field | Origin | Destinations | Status |

## Coverage matrix
<from the coverage matrix of `cratis-screenplay-scenario-coverage`; mark empty cells and weak n/a reasons>

## Modeling reasoning
Real decisions have fenced Markdown `documentation` on their owner, finalized at
P3's end; `description` stays a summary. No prose is enforcement or review evidence.

## Dependencies
Per slice: events consumed and the producing slice (orientation only, not build order); cycles found.

## Anti-patterns identified
CRITICAL | MAJOR | MINOR: <pattern name>, at <address>
Problem: <why it breaks the business or the model>
Violates: <checklist ids>
Fix: <the smallest change that removes the consequence; edit class>

## Findings
| # | Severity | Kind | Address (file:line) | Rule (tier) | Consequence | Fix |
|---|---|---|---|---|---|---|
Kinds: defect | question | note. One declaration per row (or an explicit list of names).

## Edit requests (for the owning session; this review changed nothing)
| # | Finding | Address | Operation | Edit class | Reason |

## Business questions
| # | Where | Question (plain language) | Why it matters |

## Final questions
Q1 newcomer readability: yes | no (<what is in the way>)
Q2 view rules changeable without rewriting facts: yes | no (<event, field>)
Q3 deliverable without guessing a rule: yes | no (<unresolved rule>)

## Verdict
Status: PASS | PASS WITH WARNINGS | FAIL
Ready to accept (P6): YES | NO - <what first>
Delivery readiness: <design only | executable | renderable>  (as far as the verdict lines allow)
Confidence: high | medium | low (why)
Blockers: <n critical findings>   Fixes: <n targeted, or "redesign">
Next steps: <1-3, in order>
Summary: <2-4 sentences: maturity, the most important risk, one concrete next step>
```

## Finding fields
- **Severity**
  - critical: the model states something false, loses or corrupts business facts, breaks a
    security or privacy requirement (including protection removed to pass a tool), or fails V1
    in any mode or the gate the chosen mode requires;
  - major: a business rule, path or scenario type is missing or unenforced without being
    recorded; an invariant without atomic enforcement; a gated command behind a reaction
    without a trusted-path decision; a structural shape with a demonstrated consequence;
  - minor: naming, clarity, rationale placement, coverage gaps with low business impact.
- **Kind**: defect (the model contradicts agreed facts, the language, the chosen mode or a rule
  it states itself), question (only a domain person can answer; it becomes a defect when the
  answer contradicts the model), note (no consequence; at most one line).
- **Address**: kind and name path as MCP `search-declarations` reports it (or
  `Module/Feature/Slice/<kind> <Name>`), plus `file:line` with the file name exactly as given.
  Every finding is pinned to one declaration: one declaration per row, or an explicit list of
  names; never a wildcard or a quantifier ("most", "remaining"). A gap affecting N elements
  lists all N names.
- **Rule**: the checklist id or anti-pattern name, with its tier (C/D/Q).
- **Consequence**: what goes wrong for the business or the system if it stays. No consequence
  means not a finding (at most a note).
- **Fix**: the smallest change that removes the consequence, in the right edit class
  (address-preserving text edit, or an identity-affecting change returned as an edit request).

## Root causes
Group findings on the same event or command and ask whether one design defect explains them (a
generic update event that skips a constraint, is missing from a projection and has no
precondition is one defect with three symptoms). Report the root cause as the finding with the
symptoms under it, and never propose a fix that keeps the defect (adding the generic event to a
constraint or projection).

## Defect or business question?
- Defect: the reviewer can say what is wrong.
- Business question: only a domain person can answer it ("Can this be undone?", "Who may do
  this for someone else?"). It becomes a defect only after the answer contradicts the model.
- Do not disguise a defect as a question, nor raise a question you could answer from the model.
- An open question is genuinely unanswered. A refusal the business already decided is a
  `then error` or `then denied` specification, not a question (E9).

## Question notes

On Screenplay 4.144.0, inspect `list-model-notes` with scope and `state: "all"`,
including `view: "findings"`. `model-note.open-question` is an info MCP/CLI-only
finding, no PLAY code and no V1 failure. List orphaned questions explicitly;
review `orphanedNotes` on source proposals, and treat null/unreadable as unknown.
A read-only reviewer never writes notes: return a `question` note request for the
owning session, anchored to the element with consequence and assumption. Existing
questions should be cited by note ID; new questions must not live only in chat,
session state or `description` text. Never put secrets or personal data in note
text, authors, references or resolutions.

Read model-authoring [model notes](../../cratis-screenplay-model-authoring/references/model-notes.md)
when preparing that request or judging resolution evidence. Cite the project's
own decisions with `refs: [{ "id": "D123" }]`; the owner resolves with
`by: "external-decision"` and required `ref: { "id": "D123" }` when D123 answers
it. Verify the answer became a specification, a description or a project-owned
decision record; a resolved note is not behavioral proof. Ask the owner to reanchor
or resolve affected notes before source apply; resolution alone leaves an orphan.

The optional `--open-questions fail` gate is in standalone Screenplay 4.144.0,
not Cratis CLI 3.53.1. Report it separately from V1–V5. Evidence: Screenplay
`v4.144.0:Documentation/screenplay/model-notes.md:12-77,93-121`,
`Documentation/screenplay/mcp/reference.md:457-520`; source lines in the linked guide.

## Verdict rules
- **FAIL**: any critical finding, or a verdict line the mode requires fails.
- **PASS WITH WARNINGS**: no critical; one or more major or minor findings, or open business
  questions that could change the model.
- **PASS**: no findings; open questions, if any, cannot change the model's structure.
- A "no" to a final question prevents PASS.
- **Success criteria.** The model is validated when: every phase is evaluated with no check
  skipped without a stated reason; no critical finding stands; every final question is "yes";
  every verdict line the mode requires is a result, not "not run". It needs fixes when a check
  fails with named evidence, an anti-pattern hit stands, or a final question is "no", and the
  fixes are targeted. It should be redesigned, not patched, when several phases fail or a fix
  would rewrite the core timeline; say so in the summary.
- Confidence drops when evidence was not run (V2/V3 not run, no coverage matrix, partial scope).
- Independence is separate from the verdict. A review by the same model family as an author is
  labelled same-family and never presented as independent; it is not blocked as a matter of
  authority, and the user decides whether it is enough.

## Findings across a re-review
A re-review keeps the earlier numbering and reports only the changed scope. Each earlier finding
gets one state: `fixed` (evidence), `open`, `accepted by the user` (with the answer),
`rejected by the reviewer` (with the reason) or `superseded by #n`. New findings take new
numbers. At most two fix rounds, then escalate to the user with what remains.

## Wording
Never use shape nicknames (left chair, right chair, bed, book shelf) in text meant for business
stakeholders; describe the concern in plain words. Reviewer-only sections may use them.
