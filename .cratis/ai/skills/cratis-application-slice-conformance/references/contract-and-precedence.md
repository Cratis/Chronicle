<!-- cratis-ai-managed: skills/cratis-application-slice-conformance/references/contract-and-precedence.md -->
# Contract, precedence and conflicting evidence

## Which contract
Decide opt-in per `general.md` "Model first"; where `cratis-screenplay-modeling-lifecycle` is installed, run its `scripts/model-first-status.mjs` for the decision, model root and drafts. SKILL.md step 1 suffices when the Screenplay skills are not installed.
Staged or untracked `.play` files are drafts, not the contract; a `.play` outside the model root is not a contract.
For a committed model with working-tree edits, HEAD remains the contract until the change is committed.

| Situation | Contract | Code's role |
| --- | --- | --- |
| The slice is in a `.play` model | the `.play` slice and its specifications | derived; the model stays the oracle for hand-written or gap-filled code |
| Opted in, scope unmodeled | discovery and slice design first; then the `.play` slice | none until modeled |
| An accepted model under the model root exists (the repository is opted in) but lacks this behaviour; a `.play` file outside the root is ignored here | propose adding it to the model first; code first only if the user chooses, or `.play` cannot express it; then say the model lags the code | provisional |
| No model, not opted in (or declined) | the agreed outline (Module/Feature, slice type, fields, events, rules, scenarios), recorded where the team tracks work, if anywhere | derived |

Hand-written code is right for scope the renderer rejects, adapters and infrastructure.
Never hand-edit Stage-managed output (`cratis-stage-rendering-and-sandbox`); change the `.play`
or the unmanaged extension code.

## Precedence (highest first)
1. Executable parts: specifications, mappings, constraints, authorization, rules.
2. The slice `description`, its fenced `documentation`, and realization notes: they explain intent
   and never license a contradiction or an undocumented default. Realization notes are the explicit
   realization requirements stated in the `description` or `documentation`: they bind adapters and
   fallback code, cannot contradict executable parts, and are never supplemented with inferred
   rules; other prose is only a hint.
3. Issue or ticket text.
4. Existing code: no authority.

## Conflicting evidence: stop, record, ask
Triggers: prose contradicts a mapping or spec; two specifications disagree; a spec contradicts
its own description; a requirement is ambiguous or missing a decision.
1. Read the contract and the slice-type skills fully first; most slices raise no question.
2. Do not guess and do not build the contested part anyway.
3. Stop that scope with `Status: blocked`. Write both readings with their addresses.
4. For a `.play` contract return an **edit request**: address, change, reason. For an outline,
   ask one specific question.
5. Finish unaffected scope only if it stands on its own; otherwise report `partial`.

An edit request that touches a persisted name is identity-affecting; route it to the identity
owner (`cratis-screenplay-modeling-lifecycle`, `references/identity-and-edits.md`).

## Template versus repository
The repository's established pattern wins over a skill's generic template. Record each
mismatch as a reusable learning for the parent; do not edit shared notes yourself.
Reusable learnings are conventions, gotchas, cross-file couplings and test setup that apply
beyond this slice (0 to 3 bullets). Not slice details, debugging notes or anything already
documented in the repository.
