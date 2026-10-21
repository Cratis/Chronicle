<!-- cratis-ai-managed: skills/cratis-screenplay-render-and-gap-fill/references/hand-written-changes.md -->
# Investigating hand-written code changes

## Contents

- Scope and baseline
- Inspect the changes
- Finding kinds
- Judge each difference
- Resolution routing
- Report
- Reconcile the record

<!-- Copyright (c) Cratis. All rights reserved. -->
<!-- Licensed under the MIT license. See LICENSE file in the project root for full license information. -->

A read-only investigation of code changes against the slice contract. It edits neither code nor
model: findings become re-delivery briefs or model edit requests. An accepted model remains the
contract; new code is evidence of a difference, not authority to revise the model.

## Scope and baseline

1. Name the repository, feature or slices and target ref. Default: `HEAD` plus uncommitted changes.
   For another branch, verify the ref with `git -C <repo> rev-parse --verify <ref>`, trying
   `origin/<branch>` if appropriate. Read with `git -C <repo> show <ref>:<path>`, never switch the
   checkout, because that can disturb somebody else's work. Do not mix working-tree edits into
   an investigation of another ref.
2. Use the fallback record's `last verified` code identity, or the extraction's anchored analysis
   note. An explicitly supplied starting commit overrides it. Read the element map, fields,
   shared seams, approved divergences and feature-level legacy-source notes before judging.
3. Check the baseline resolves and is an ancestor of the target:
   `git -C <repo> merge-base --is-ancestor <baseline> <ref>`. A missing, unreachable, non-ancestor
   or dirty identity needs one agreed starting commit. Offer the nearest recorded `delivered at`
   or `last verified` commit; otherwise show `git -C <repo> log --oneline -10` as candidates,
   not as an assumed baseline.
4. Without usable Git history, say that this investigation cannot run. Route to
   `cratis screenplay conform <model-root> --project <path>` for structural evidence and
   `cratis-application-slice-conformance` for a contract review; neither invents a change range.

## Inspect the changes

1. Share one history read among scopes with the same baseline. List commits oldest first over
   the recorded slice files, shared seams and `Customizations/`:
   `git -C <repo> log --reverse --format='%H %s (%an)' <baseline>..<ref> -- <paths>`.
   Inspect each commit's changes, grouped by file. Include uncommitted changes separately only
   for the default target. A net diff alone can hide a hand edit followed by another delivery.
2. A delivery record naming a commit in `delivered at` identifies its as-built code. Stage's
   ownership manifest identifies managed files, not the author of an arbitrary commit. Attribute
   later edits to their actual commits, including edits in a file another delivery created;
   `git -C <repo> diff <delivered-commit>..<ref> -- <file>` shows the candidates, not the full
   attribution. Stage-managed output uses the manifest/refusal procedure in
   `drift-and-fallback-record.md`, not hand-written repair.
3. Map changed files to slices through the element map, field chains and specification mapping,
   then structural addresses from `conform`, then names as a last resort. A business change
   mapping to no slice is itself a finding: use the recorded feature folders to locate it, or
   state that it needs a proposed slice. Read only those contracts and the changed code at the
   target ref, not just the diff hunk.
4. Separate behavior from noise per file. Dependency, CI, formatting and documentation edits,
   generated Arc proxies, corpus configuration, harness adapters, work records, lockfiles,
   `bin/` and `obj/` normally belong to tooling or generated-output review. Domain-generated
   values, serialization attributes, test helpers, logging and refactors are noise only when
   behavior is preserved. A changed default, authorization gate or disabled test is not noise
   merely because it shares a file or commit with infrastructure edits.

## Finding kinds

| Kind | A code change in the range that … | Model side |
|---|---|---|
| Fields | adds, removes or renames a command input, event property or read-model/query-result field; changes type, optionality or identity | `command`, `event`, `readmodel`, `query`, `identifier` |
| Rules | changes validation, a guard, error, default, authorization or a specification so the contract no longer describes the behavior | rules, constraints, `authorize`, `specification` |
| Screens | changes a page, component or route's displayed or submitted values | `screen`, forms, bindings |
| Wiring | changes appended events, decision reads, projection inputs or reaction/capture links | `produces`, `reads`, `from`, triggers, `invokes`, captures |
| Declarations | adds, removes or renames a command, event, read model, query or reaction | the declaration |
| Slices | adds an uncovered behavior, or removes code a delivery record shows was delivered | the slice |

## Judge each difference

- Report only changes in the agreed range. A pre-existing mismatch is not caused by this range;
  do not call it approved merely because it existed at the baseline. Return any blocking
  pre-existing defect separately for a conformance review.
- A change restoring the accepted contract is no finding; mention restoration in the summary.
  An approved, recorded divergence is no new finding. An unapproved difference still is.
- Model-only scope without delivery evidence is undelivered work, not a code-change finding.
  A declaration or slice whose element map shows delivery but whose code was removed **is** a
  Declarations or Slices finding; do not hide it as undelivered work.
- One difference is one finding at the declaration where it starts. Name the affected chain,
  not duplicate findings for every downstream carrier.

## Resolution routing

Default with an accepted model: restore code by re-delivery, with the identity effects named.
A wanted behavior change instead needs a model edit request, review and acceptance before code
follows it. With an extraction candidate and no accepted model, propose updating the candidate
from the evidence and retain expert confirmation; code is not an accepted business decision.

| Finding | Restore code | Alternative model edit request |
|---|---|---|
| Fields or Wiring | `slice-implementer`, or re-render Stage-owned scope | modeler using `cratis-screenplay-slice-design`; event/identity effects stated |
| Rules | re-deliver the contracted rule and specifications | `cratis-screenplay-scenario-coverage` and the owning command/consistency skill |
| Screens | re-deliver the contracted page | `cratis-screenplay-read-surface` and `cratis-screenplay-ui-composition` |
| Declarations added in code | removal needs a human decision, not deletion from this investigation | discovery or slice design, with identity effects |
| Delivered slice removed in code | re-deliver | removal through the lifecycle's identity owner |

For an evidently accidental edit (misspelled property, debugging leftover, disabled test), say
why restoration is recommended. A declined finding does not silently authorize divergence:
record the reason and approver if the owner deliberately accepts it.

Every model edit request affecting a field, command or read model names all affected screens,
forms and bindings in the same request, to be changed in place. Adding another screen beside
an inconsistent one leaves the defect. Use the `screenplay` MCP server's `find-references` with
`kind: "Property"` and follow `screen-binding` links (model-authoring
`references/property-links.md`); incomplete links require bounded manual inspection too.

## Report

Group findings by kind, omitting empty sections. Each checkbox carries the range's responsible
commits (identity, subject, author), resolving file locator, declaration, model behavior, code
behavior and proposed resolution. A short report looks like:

```markdown
## Code changes against the contract: <scope>
Range: <baseline> -> <ref> [plus uncommitted changes]
Inspected: <n> commits, <n> behavior-changing files

### Fields
- [ ] <Module/Feature/Slice> <declaration>: model <x>; code <y>.
  Responsible: <commit, subject, author>; delivery: <recorded identity>.
  Location: <repository-relative path and locator>; proof: <specification>.
  Proposed: restore | model edit request <change>; screens: <affected list>.

### Summary
<What changed, the largest consequence, and the first recommended resolution.>
Commits: <one per line; delivery commits identified by their record>.
Not relevant: <noise files and reasons>.
```

No behavior change in the range: say so in one line, including the inspected scope. Partial
coverage is not a clean report; name paths or evidence that could not be inspected.

## Reconcile the record

Apply no changes from the report itself. The owner chooses resolutions; model edits are batched
through the modeler, code repairs are queued for delivery, and declined findings retain their
reasons. Advance `last verified` only after every finding has a decided resolution and the
resulting code and specifications have been checked. Unanswered or failed resolutions keep
the baseline unchanged; an empty finding list still requires the checks before advancing it.

After writing a fallback entry, check every declaration address resolves (`find-declaration`
on the `screenplay` MCP server) and every element, field and specification row is nonempty.
Field mappings name the concrete input-to-code-to-storage chain where applicable, or state
`no counterpart, because …`. A complete record navigates evidence; it is not behavioral proof.
