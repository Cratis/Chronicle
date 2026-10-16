<!-- cratis-ai-managed: skills/cratis-screenplay-legacy-extraction/references/re-entry-and-passes.md -->
# Re-entry and layered passes

## Contents

- 1. Re-entry (first step of every run after the first)
- 2. Breadth first, then one feature per pass
- 3. Asking during extraction
- 4. Promote evidence at acceptance

Legacy extraction runs over many sessions on a system that keeps changing. Two habits keep it
bounded and honest: come back by recorded source identity instead of rereading everything, and
widen the candidate in layers instead of producing one huge shallow-everywhere model. Both live in
`EVIDENCE.md` (`evidence-table-template.md`) and sit beside the evidence stages L0-L4: L0-L4 say
*how strong* the evidence is, passes say *how much of the system* is in scope.

## 1. Re-entry (first step of every run after the first)

Reuse the source identity defined in `cratis-screenplay-modeling-lifecycle`
(`references/verdicts-and-modes.md`, "Source identity"): the commit plus the digest of the examined
inputs. Do not invent a second definition. Git is used read-only: `git -C <legacy-repo>` queries
only, never a checkout, reset or stash, and never in the live checkout when the brief forbids
touching it.

Per feature, take the identity from the last row of its analysis history (evidence template,
section "Analysis history"; for an accepted model, the feature's newest `note` from section 4), then:

1. `git -C <repo> merge-base --is-ancestor <commit> HEAD` : is the recorded commit still an ancestor?
2. `git -C <repo> log --reverse --oneline <commit>..HEAD -- <feature folders>` : what touched the
   feature since, oldest first. The folders come from the feature's "Where each feature lives"
   table (evidence template, section 6) or, for an accepted model, the same table in its note.
3. `git -C <repo> status --porcelain -- <feature folders>` : uncommitted changes count too.

Then decide per feature:

| Finding | Action |
|---|---|
| Nothing touched the feature folders | Leave the feature alone. Say "unchanged since the recorded identity". |
| Files changed | Reread only those files. Refresh the `path:start-end` locators that point into them. Mark every evidence row whose locator moved or vanished `stale` until reread, then update or retire it. Re-apply the decisions that depended on those rows and ask about what the diff cannot explain. |
| Files changed outside every feature's folders | Candidates for a new feature or a missing "Where each feature lives" entry. Ask before adding. |
| Commit no longer reachable (rewritten history), no git at all, or the recorded identity was dirty | Reread that feature in full (for a dirty identity, at least the files the diff names plus the files that were dirty then). Say so in the history row. |

Every run appends a row to each feature's analysis history, also when nothing changed (format and
rules: evidence template, section 7).

### Freeze verification

When the interview recorded a freeze agreement, the same commands over the legacy repository
*check* it: any behavior change since the recorded identity is reported against the freeze boundary
(`interview-findings.md`) before more evidence is gathered. A broken freeze makes the candidate
stale for the affected features; it never silently passes. A documentation-only extraction that
recorded "freeze not applicable" skips this.

### Relation to the model

Re-entry refreshes *evidence*. It never edits an accepted model on its own: a changed behavior that
affects an accepted model is a model edit request through the modeler, review and acceptance
(`cratis-screenplay-modeling-lifecycle`). Evidence for a model that does not exist yet is the only
place where "update the candidate to match what was reread" is the default.

## 2. Breadth first, then one feature per pass

| Pass | Scope | Contents |
|---|---|---|
| 0 | whole system | Modules and features only, one feature per user journey, with the milestone events in story order. No properties, no specifications. Each ordering claim names its source. |
| 1 | one feature | Slices with identity and the business-critical properties, the success specification, the one or two rules that matter, screens only where a person decides. |
| 2 and later | the same feature | Full properties, rejection and denial specifications from code and tests, reactions, translations, alternate paths. |

"Deeper" means more business: decisions, rules, failure modes. It never means more technical detail.
Every pass still cites evidence; passes widen the scope, they do not relax the evidence rules.

Grouping rule: code is split by operation (create, find, show and update endpoints); a feature is
the journey that uses them, so register, find, view and update of one thing are one feature. When in
doubt, merge. A pass that finds sub-journeys offers them as new features instead of growing the
current one.

A pass covers the feature's whole journey from its trigger to every outcome, not its first
slice. Run the phase skills on the feature in story order (discovery plotting, slice design,
automations and translations, scenario coverage) and skip a phase skill's interview where the
evidence already answers it; ask only where the code stops. Never add detail that belongs to a
deeper pass: it would carry evidence the pass has not gathered. Examples are part of depth: Pass 1
gives one typed `example` per key command or event, Pass 2 gives realistic values shaped by the
test fixtures (invented where fixtures hold personal data).

```screenplay
domain Clinic.Visits

concept PetId : Uuid
concept VisitReason : String

module Visits
  feature BookingAVisit
    example TypicalBooking : BookVisit
      description "Shape from the booking test fixtures (E12); values invented"
      reason = "Annual vaccination"
    slice StateChange BookVisit
      command BookVisit
        petId  PetId identifier
        reason VisitReason
        produces event VisitBooked
          reason VisitReason = reason
      specification BookingATypicalVisit
        when TypicalBooking
          petId = "3f2a1b0c-9d8e-4f7a-b6c5-d4e3f2a1b001"
        then VisitBooked
          for "3f2a1b0c-9d8e-4f7a-b6c5-d4e3f2a1b001"
          reason = "Annual vaccination"
```

**Pass check** before reporting a pass: re-read the candidate (files or MCP), not memory; V1 is
clean; the feature tells its journey from trigger to outcome; no detail from a deeper pass crept in;
every slice cites evidence; every decision of this pass is a row; this pass's history row exists.
Three to twelve states per UI flow is typical (`ui-observation.md`).

Pass 0 order: ask which feature to deepen next. Unattended, choose by risk and available evidence,
and say that you chose. Features still at Pass 0 go to the loss report's "Not yet extracted"
section (`loss-report-template.md`); a partial candidate may go to review with them listed.

End every pass with: what changed, the sub-journeys found, the open questions, and the choice
"which feature next, deeper here, or stop".

A Pass 0 skeleton compiles as a normal candidate:

```screenplay
domain Clinic.Visits

module Visits
  description "Pass 0 skeleton. Evidence: E1, E2. Where the code lives is in EVIDENCE.md section 6"
  feature BookingAVisit
    description "Story: PetRegistered, then VisitBooked, then VisitCompleted or VisitCancelled. Order source: UI walk E2, confirmed by the clinic expert; VisitCancelled after VisitBooked is assumed (Q3). Not yet extracted: properties, rules, specifications"
    slice StateChange BookVisit
      description "Pass 0. Evidence: E1"
      command BookVisit
        produces event VisitBooked
```

## 3. Asking during extraction

Build what the evidence shows; do not ask experts to confirm each derived declaration (that is the
expert verification session, `expert-verification.md`). Ask only about order and unclear
requirements, one question at a time, always as hypothesis, evidence and question: "From
`CheckinsController.Post` the attendant issues the ticket before the hook is chosen (E1). Is that
the real order?" Never place a step at a guessed position: ask, or leave an anchored `question`
note (section 4). An answer is a decision as well: record it as a decision row (evidence template
section 8) in the same batch as the edit it causes. The person outranks the code; when the answer
contradicts the code, the row has kind `override`.

When the order stays unclear after a pass, ask once whether a running instance exists
(`ui-observation.md`) before asking the expert for the order directly.

## 4. Promote evidence at acceptance

`EVIDENCE.md` is a work record. It is not committed and does not outlive the work area, so once
the candidate is accepted the evidence ids cited in descriptions would resolve to nothing, the next
re-entry would have no baseline and the next hand-written-change check would have to ask for a
start commit. Before the candidate moves to acceptance (P6), copy into the model what later runs
need, in the three homes the model already has:

| What | Home | Why there |
|---|---|---|
| Analysis history, where each feature lives (area, folder, key symbols), feature-wide decisions (where the journey starts and ends, its order against other features, what was merged or split) and "not modeled" with the reason | one `note`-kind model note per feature per run, anchored to the feature | Notes are committed with the model, survive renames and moves, and never touch compiler verdicts or rendered code. One note per run means two people re-entering do not edit the same lines, which `documentation` blocks would force |
| Legacy facts that explain a slice: the legacy HTTP status and check order, the decision rows behind it, evidence ids | slice `documentation` (fenced Markdown); `description` stays a business summary | `description` renders as an XML doc comment in generated code; `documentation` does not |
| Open decisions (`assumed` rows) and unresolved order or requirement questions | `question` notes anchored to the declaration they affect | The question travels with the element until an answer becomes a specification (model-authoring `references/model-notes.md`) |

Line-level locators (`path:start-end`), the evidence rows, the read log and the loss report stay in
the evidence file. A note names folders and key symbols, never values, so it stays useful without
the evidence file and stays safe to commit.

A run note, as its text:

```text
Run 2, pass 1, source identity: <commit + digest>. Scope: src/Checkins/. Result: 4 slices, Q3 open.
Lives in: API src/Checkins/ (CheckinsController, CheckinService); persistence db/checkins/ (Checkins,
Hooks); tests tests/Checkins/. Decisions: journey starts at the attendant's check-in and ends at
hand-back; the nightly hook reset is not modeled (no known owner). Evidence: E1-E9.
```

Add it with the `screenplay` MCP server's `propose-model-notes` tool (`kind: "note"`, anchor the
feature's semantic id), review with `read-proposal view=notes`, then apply. Acceptance runs
`screenplay <model-root> --open-questions report` and lists what is still open.

Both homes are committed. Never put personal data, secrets, production values or credentials in a
note or in documentation; locators and redacted route templates only. A client may require legacy
evidence to stay out of the model repository. Then only evidence ids and the business-level text
are committed, the evidence file is archived where the client allows, and the next re-entry starts
from that archive or from a full reread. Say which applies in the interview findings.

A candidate kept under `.ai-work/screenplay/<model-slug>/candidate/` promotes when it moves to the
model root, not before.
