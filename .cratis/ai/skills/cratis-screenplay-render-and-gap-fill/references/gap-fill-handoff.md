<!-- cratis-ai-managed: skills/cratis-screenplay-render-and-gap-fill/references/gap-fill-handoff.md -->
# Gap-fill hand-off to `slice-implementer`

## Contents

- One scope per brief
- What comes back
- Rules the model states only in prose
- Automation and Translate scope
- Re-delivery

For scope the renderer rejects (case C in `SKILL.md`) and for hand-written scope next to a
generated base (case B). The render phase reports the capability boundary; the implementer
authors the code with the `.play` slice and its specifications as the contract. Contract
reconciliation, specification mapping, delta delivery and the final status are owned by
`cratis-application-slice-conformance`; this file only says what the brief carries and what
comes back.

## One scope per brief
One fallback record entry (`drift-and-fallback-record.md`), one scope, one implementer. The brief
is a set of paths and addresses, not pasted contents:
- model root, source identity, the qualified slice addresses (`Module/Feature/Slice`) in scope,
  their specifications and the referenced contract locations;
- the blocking codes from the probe and the fallback record entry id;
- target project, owned paths, and the repository's conventions (Arc and Chronicle hand-written
  slices; `rules/vertical-slices.md`);
- the skill per slice kind: State Change `cratis-screenplay-command-surface`; State View
  `cratis-screenplay-projections` and `cratis-screenplay-read-surface`; Automation and Translate
  `cratis-screenplay-automations-and-translations` and `cratis-screenplay-captures-and-reactions`;
  the code skills `cratis-arc-command`, `cratis-chronicle-read-model`, `cratis-chronicle-reactor`;
- rules that live in prose today, each with its intended realization and a test (see "Rules the
  model states only in prose");
- known unsupported semantics, open questions and the checks required before reporting done;
- the standing instruction, verbatim in spirit: *The `.play` slice and its specifications are
  the specification. Do not invent fields, rules or defaults. Each specification becomes a test
  with the same name; never weaken, skip or delete one to pass. Do not edit managed files.
  Return a field inventory (command, event and read-model properties mapped to code locations)
  and list anything you added that the model does not state (expected: none). Explicit
  realization requirements in the slice `description` or `documentation` bind adapters and fallback code, cannot
  contradict executable parts and are never supplemented with inferred rules; descriptive prose
  that is not an explicit requirement is a hint. The repository's pattern wins over a
  generic template; report the mismatch. Stop on a contradiction and return an edit request.*

The brief also carries, from the slice build prompts (`cratis-application-slice-conformance`
`references/build-prompts.md`): the contract is always true and the code follows it; a slice is
`done` only if every scenario in the contract is implemented and no specification lacks an
executable equivalent; a re-delivery can be only added specifications; touch only the slice's
own paths; do not change existing specification files unless an approved contract revision says so (`build-prompts.md` section 1); when the
requirements hit the delivery block list, return an edit request and report `blocked`; non-behavioral
assumptions follow `build-prompts.md` section 3 and remain visible. Ownership is assigned per brief,
one implementer per scope; a fallback record or `STATE.md` line records it and is not a lock, and atomic claiming is used
only where a tracker provides a verified operation. Git effects follow the brief's separate
authorization.

Before coding, the implementer reads each scoped slice completely: descriptions,
specifications, concepts, inherited authorization, referenced events and views, and resolves
ambiguous addresses; contracts are never inferred from names.

## What comes back
- Status `done | partial | blocked` from `cratis-application-slice-conformance`.
- The realization fields from `drift-and-fallback-record.md`, "Fallback record", in the slice's
  result packet: element and specification mappings, files, shared seams, assumptions, identities
  and check results, with no second schema.
- Edit requests for the modeler session: address, change, rationale. Walkthrough-derived cases
  belong in accepted `.play` specifications first, never as a second behavioural contract in
  code-only tests. Integration tests may check wiring and persistence without inventing rules.
- The gate: the repository's Tier 1 checks, plus integration evidence for the behaviour claimed;
  unavailable checks are `not run: <reason>`. The delivered code then goes through code review.

## Rules the model states only in prose
A rule that exists only in a description, or a state-dependent rule the model marks as not
enforced, is unenforced in generated scope. List it in the fallback record as a target requirement. In
hand-written code it needs an atomic enforcement point: a protected read or a concurrency scope
(Stage-rendered apps scaffold the Arc/Chronicle set in `cratis-stage-rendering-and-sandbox` `references/rendered-application.md`: protected-read APIs
are available to gap-fill, with projection/key admission and active enrollment), or a Chronicle constraint. A stale view or
a caller-supplied status is not enforcement.

## Automation and Translate scope
Stage renders supported event-triggered Automation as Chronicle reactors.
Translate/captures and refused reactions are gap-fill against the slice and its specifications;
see Stage's `references/admission.md` for precise limits. The realization follows the actual construct and
trigger, not the slice label: a capture is an ingestion adapter, an event reaction (including a
translator reaction) is a reactor, a clock or application trigger is a scheduler or host signal. Where they are
registered depends on the case. Case B (a generated base exists, and the user authorized the
gap-fill): the registration goes through the seams in `Customizations/Program.cs`, which does
not bind a modeled reaction or capture. Case C (no generated application): there is no generated
`Program.cs` or partial hook, so the registration follows the hand-written application's own
composition conventions (`rules/vertical-slices.md`, `cratis-chronicle-reactor`).
Declare the trusted actor with reaction `runs as system role "<Role>"`
; `given caller` is not an invocation actor.
An older compiler rejects the syntax. The CLI/Stage pair
(`cratis-screenplay-toolchain/references/renderable-subset.md`) renders role/authenticated reaction identity; claim-reading/opaque system gates still
refuse STAGE-ESM-034. Gap-fill retains the declared role and uses Arc
`[ExecuteCommandsAsSystem]` for returned commands. Checked model and diagnostics:
captures-and-reactions `references/invocation-identity.md`.

A reaction that both produces events and invokes commands keeps its single modeled form
(`STAGE-ESM-037` is Stage's refusal, not a model defect). Hand-write one reactor returning the events
and the commands: in Arc (the Stage scaffold includes the handler, but mixed reactions remain refused because redelivery can duplicate events) the runtime appends the events first as one batch, then
runs the commands in order, with no transaction across the two effects; a failing command stops the
later ones but keeps the committed events, and a retry can append the events again, so make the
handler idempotent independently of `[OnceOnly]`, which excludes replay only, not recovery
or crash redelivery (Chronicle `Documentation/code-analysis/CHR0022.mdx`;
see `cratis-chronicle-reactor`). Return a
stable array or list. The Stage scaffold pins an older Arc, so raise the version through
`Customizations/Dependencies.props` only after a restore and build proves it. Never split the
reaction or drop an effect to dodge rendering.

Preserve declared `reaction batches atomic` boundaries: each trigger occurrence's
direct productions and each selected refusal branch are separate batches, with
invoked commands independent. Atomic mode refuses STAGE-ESM-043.
Requires Stage 4.139.0 for v22 admission; older Stage reports -016.
The declaration is not external-effect deduplication.
For described handlers, hand off only payload computation under the v23 result
contract; Stage STAGE-IMPL-002 means no stubs, even Stub mode. Protected v25 reads
render from Stage 4.138.0 (STAGE-ESM-044 from older bundles, or for shapes it cannot
preserve) and need generated-app admission/enrollment tests wherever you hand-write them;
ordinary injection never supplies protection.
Semantic Host/executor support is a separate result, not generated-app enforcement.

## Re-delivery
The work list is the full in-scope contract delta since the fallback record's last verified source
identity (specifications, rules, mappings, realization notes), then the whole contract is
reconciled again and the field inventory redone. The specification delta is one fallback record field.
When the scope later renders (a revisit trigger fires), plan the migration to managed output
with the user: the hand-written files collide with planned paths and must move first.
