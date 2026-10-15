<!-- cratis-ai-managed: skills/cratis-screenplay-render-and-gap-fill/references/renderability-gate.md -->
# Renderability gate: classifying what the probe reports

The admitted subset itself (what Stage renders, member by member) is owned by
`cratis-stage-rendering-and-sandbox` (`references/admission.md`, the code table) and
`cratis-screenplay-toolchain` (`references/renderable-subset.md`); version facts are in
`cratis-screenplay-toolchain` `references/versions.md`. This file is about **classifying** what the
probe reports and choosing what to do. Do not copy the code table here.

## Pipeline that produces the codes
1. The CLI compiles and binds the **whole** model with its **bundled** Screenplay (4.147.1 in
   cratis 3.57.0, ESM up to v15 for rendering; older bundles bound less). Binding is never scoped:
   any binding error blocks even a `--scope` render. Binding errors are `PLAY0268` (outside the admitted vertical), `PLAY0269`
   (deferred), `PLAY0270` (report-only information), `PLAY0271` (legacy semantics) and `PLAY0273`
   (a specification shape).
2. Execution planning builds the plan and can emit `PLAN-*` diagnostics before Stage runs.
3. Stage plans the target and admits or rejects each member: `STAGE-ESM-0xx`; profile and path
   problems `STAGE-CRATIS-0xx`. An evolved event is reported with Stage's own `STAGE-ESM-026`
   (still refused at Stage 4.89.1); the CLI adds nothing. Stage admits ESM v1 to v15 by version and
   refuses each construct it does not render with its own code (see Stage's current code table, including `-024`, `-027`, `-028`, `-030` to `-041`).
4. Any error in the selected scope publishes nothing: the run ends with exit 5 and the diagnostics.
   Without `--scope` the scope is the whole application. With `--scope`, `PLAN-*` problems in
   unselected slices are warnings and `STAGE-ESM-*` refusals in unselected slices do not block;
   binding problems and application-level declarations still do.

The standalone `screenplay` tool can report V3 ready where the bundled binder refuses (newer
ESM). Only the probe answers renderability. `PLAY0270` entries are information, not blockers.

## Classification

| Class | Meaning | Typical examples (each verified in `admission.md`) | Action |
| --- | --- | --- | --- |
| **Model fix (in mode)** | the model is in, or the user chose, renderable mode and breaks its own subset in a way that has a renderable equivalent with the same meaning | a `produces` whose `for` is not the command identifier; one `StateChange` slice with two commands (`STAGE-ESM-004`); two projections for one read model (`-007`); a policy Stage cannot express portably (`-015`; role-only and opaque pure policies render at 4.73.1) | edit request to the modeler session with address, change and rationale |
| **Capability gap** | the intended meaning needs a construct Stage does not render yet | a snapshot list query (`-010`; observable lists and keyed or unkeyed lists bind and observable lists render), DateTime equality filters and required parameters after optional filters (`-010`); context/single-result filters, opaque `scoped to`/`performer` and composite `by` remain PLAY0268; Translate, clocks, captures, non-event triggers and opaque reaction bodies (`-024`); claim-reading/opaque `runs as` gates (`-034`), legacy `from` (`-035`), mixed effects in one handler (`-037`, retries can duplicate events; keep it whole), unsupported reaction mappings (`-039`); unsupported generated types/result combinations (`-028`/`-038`); `then no readmodel` (`-027`); evolved events (`STAGE-ESM-026`); conditional `produces when` (`-006`); `reads` (`PLAY0271`); code validation (`-005`); protected identities/enums (`-040`), non-text secrets (`-041`) or unadmitted explicit subject lineage (`PLAY0268`); a command `handler` (`PLAY0268`) | keep the model; ledger entry; options a to c in `SKILL.md`; link the Stage issue |
| **Tool skew** | the bundled compiler disagrees with documented semantics | the false `PLAY0285` on reaction cascades from a cratis before 3.28.2 bundle (cli#242); codes the standalone compiler does not raise | record in the session state and the ledger; never "fix" a correct model |
| **Environment** | not the model | restore failures, missing SDK, a destination that is not writable | fix the environment or report blocked |

## Triage rules
- A capability gap never becomes a model fix by deleting meaning. Turning a list screen into a
  keyed lookup, dropping `pii`, removing authorization or a rule, or splitting a conditional
  outcome into separate commands is a **modeling decision** for the user and the modeler, made
  for business reasons, not to please the renderer.
- A refusal never licenses dropping a specification, an event generation or a protection.
- Group codes by construct and count, report the first few with their `file(line,col)` location,
  and keep the full JSON in the probe folder. Do not paste the list.
- One error blocks the selected scope. Say how many slices are affected, and which slices
  would render if the blockers were removed (from the locations), marked as an estimate; offer a
  `--scope` render of the admitted slices as an option, never a silently thinner render.
- Admission can pass and a Debug test still fail. Admission is never a passing test; see
  `cratis-stage-rendering-and-sandbox` `references/render-example.md`.

## Stage issues to cite in ledger entries
Historical issue pointers (not a fresh issue-state check): portable query semantics Stage#58;
Translate and unsupported automation forms Stage#79; mapping expressions and projection blocks Stage#117; modeled authentication
Stage#120; per-persona screens Stage#194; evolved events Stage#204; mixed reaction effects Stage#207;
generated values/responses Stage#175 (now render on the pinned bundle). Closed: event generation Stage#165, descriptions as code docs
Stage#178, ESM v7 Stage#201. Check that an issue is still open before citing
it, and cite only numbers you have just read. A closed issue is a trigger to probe again.

## Reference outcomes
- The complete marina model of `cratis-stage-rendering-and-sandbox` `references/render-example.md`:
  admission ok, 30 artifacts, Debug build ok, 7 tests passed (cratis 3.28.2; on 3.53.1 admission and publication gave 56 artifacts, build and tests not re-run).
- The model in `worked-example.md`: admission refused with 2 blocking `PLAY0268` diagnostics, nothing published.
