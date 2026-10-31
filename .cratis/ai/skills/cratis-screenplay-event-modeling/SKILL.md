---
name: cratis-screenplay-event-modeling
description: "Entry point and router for model-first Cratis work: the short decision rule (model, or code), the nine-step workflow per behavior, the four slice types, the Cratis divergences from the generic method, the quick validation gate, file layout and what parsed-but-not-runnable means, then the phase skill to load next. Use when designing an information system, mapping a business process or information flow, deciding the event vocabulary or stream boundaries, turning a whiteboard model into `.play`, or when a request may change behavior in an application with a Screenplay model. Not for: Screenplay syntax and the compiler alone (use `cratis-screenplay-toolchain`), the lifecycle, verdicts and handoffs (use `cratis-screenplay-modeling-lifecycle`), or rendering a settled model (use `cratis-screenplay-render-and-gap-fill`)."
license: MIT
---
<!-- cratis-ai-managed: skills/cratis-screenplay-event-modeling/SKILL.md -->

# Event modeling with Screenplay (start here)

Event Modeling is the **method**: walk a business process left to right and write
down every behavior as a command that changes the system, a view that reads it,
an automation that runs off it, or a translation of outside data. Screenplay is
the **artifact**: one language that holds that whole model - concepts, commands,
events, read models, queries, screens, specifications - in files that compile.

You are a **facilitator, not a stenographer.** Ask probing questions, challenge
assumptions, keep asking *"and then what happens?"* after every event, every
command, every answer. Use business language. Do not discuss databases, APIs, or
frameworks during modeling.

**Ask only what the step needs:** ask the one question that changes the model most; in an unattended run assume visibly, never silently; contradictions, a third review round and missing approvals always stop. Read [references/facilitation-stance.md](references/facilitation-stance.md) for the full rule, and `cratis-screenplay-modeling-lifecycle`'s `references/stop-or-assume.md` when information is missing or contradictory, or when deciding whether unattended work may proceed.

**Do not cut corners to save tokens or effort.** A rule that needs more slices, events, views,
translation steps or specifications gets them written; budget is never a reason to delete a
modeled behavior, label a gap "accepted debt" or merge translating outside data into our facts
into the work that acts on them. Flag a real trade-off to the user instead of resolving it by cutting the model
(`cratis-screenplay-modeling-lifecycle`, `references/completeness-self-check.md`). Read `cratis-screenplay-modeling-lifecycle`'s `references/completeness-self-check.md` before every gate and whenever effort or token cost tempts a reduction in modeled behavior.

## Start here

**1. Decide the level (short form; master copy in `cratis-screenplay-modeling-lifecycle`).**
Check first that the method skills are installed; installing them never opts a
repository in. Decide opt-in per `general.md` "Model first"; where `cratis-screenplay-modeling-lifecycle` is installed, run its `scripts/model-first-status.mjs` for the decision, model root and drafts.
Otherwise stay code-first. Only the entry-point session proposes a model (at most once
per session, never for trivial, bug-fix, infrastructure, client, framework or
brownfield-maintenance work; unattended: record the recommendation in the final report).
Framework repositories and brownfield work that has not opted in stay code-first.

- **Model:** change the `.play`, verify, review, then render or gap-fill.
- **Code is right for** infrastructure, clients, Screenplay code attachments and
  handlers, adapters, and scope Stage cannot render yet (the model stays the contract).
- **Never:** use code as a shortcut around the model; change the model to match
  existing code; edit Stage-managed output; leave a modeled rule living only in
  code; weaken protection (authorization, `pii`, rules) so a model compiles or renders.

**2. Run the lifecycle.** Load `cratis-screenplay-modeling-lifecycle` for modes, the
independent verdicts V1-V5, the P0-P9 phases with their gates, stop-or-assume,
identity ownership and handoffs. Then load **one** phase skill. Small changes enter
at the phase where they belong and run the downstream gates for the changed scope.

**3. Pick the phase skill.**

| Phase | Skill |
| --- | --- |
| P0-P1 intake, timeline, personas, events | `cratis-screenplay-discovery` |
| P2 commands, read models, screens, field lineage | `cratis-screenplay-slice-design` |
| P2 stream identity, consistency, evolution | `cratis-screenplay-streams-and-consistency` |
| P2 reactions, work queues, clocks, captures | `cratis-screenplay-automations-and-translations` |
| P3 specifications and coverage | `cratis-screenplay-scenario-coverage` |
| P4-P5 self-check and independent review | `cratis-screenplay-model-review` |
| Existing system into a model (replaces P1-P2) | `cratis-screenplay-legacy-extraction` |
| P7-P9 execute, render, fall back, verify | `cratis-screenplay-render-and-gap-fill` |
| Tools, versions, verdict commands, diagnostics | `cratis-screenplay-toolchain` |
| Estimate, order or split delivery of an accepted model | `cratis-screenplay-delivery-planning` |

**4. Nine steps to phase skill.** [references/nine-steps.md](references/nine-steps.md)
keeps the activities, Screenplay output and facilitation questions of each step:

| Step | Activity | Phase skill |
| --- | --- | --- |
| 1-3 | Goal, brainstorm events, order them | `cratis-screenplay-discovery` |
| 4-6 | Wireframes, commands, read models | `cratis-screenplay-slice-design` |
| 7-8 | Automations, external integrations | `cratis-screenplay-automations-and-translations` |
| 9 | Decompose into vertical slices | `cratis-screenplay-slice-design` |
| after 9 | Specifications, then review | `cratis-screenplay-scenario-coverage`, `cratis-screenplay-model-review` |

Read [references/nine-steps.md](references/nine-steps.md) when facilitating a workflow and needing a step's activities, required output, or questions. Read [references/invoicing-example.md](references/invoicing-example.md) when you need a complete worked `.play` document (RegisterInvoice command and InvoiceList view) to copy the shape from.

## Verify

```shell
screenplay <model-folder> --warnaserror
```

(The standalone tool; `cratis screenplay validate <model-folder> --warnings-as-errors`
is the fallback, and neither names the other's verdict. Versions and commands:
`cratis-screenplay-toolchain`.)

- [ ] Zero errors **and zero warnings**. An unrecognized construct inside a slice
      is only a warning (`PLAY0029`) and its block is **silently dropped** - a typo
      can delete a whole projection while the exit code stays `0`.
- [ ] Every behavior is exactly one slice type, and the whole model validates
      against the eight Quick gate checks.
- [ ] Every event is past tense, single-purpose, and carries no event-source id.
- [ ] Personal data is classified on the `concept`, with a reason.
- [ ] Specifications name the rejections, not only the happy path.
- [ ] Bind with the runtime's compiler; tool versions: toolchain `SKILL.md`.
      Read [references/parsed-not-runnable.md](references/parsed-not-runnable.md) (section "A CLI and Stage that admit v15") when judging what Stage renders: Translate/captures and refused reactions stay gaps, and mixed effects stay `STAGE-ESM-037`.

## Parsed is not runnable

The compiler accepts far more than anything executes. Between the syntax tree and
any runtime sits the **executable semantic model (ESM)**, and it fails closed:
what it cannot represent reports `PLAY0268` or `PLAY0271` and the model does not
bind. Keep four states apart when you report on a model: **parsed** (the
`screenplay` tool), **bound** (ESM), **reference-executed** (specifications pass
the reference runner) and **target-executed** (Stage or a rendered application).
The independent verdicts V1 to V5 that report them are in
`cratis-screenplay-modeling-lifecycle`.

What binds depends on **which tool** you ask, so name the tool and its version:
Read [references/parsed-not-runnable.md](references/parsed-not-runnable.md) (section "What binds depends on which tool") when stating what a tool binds or renders (legacy reads and `concurrency` fail with `PLAY0271`; protected command reads opt into v25; `persona` declarations are report-only; an older bundle reports *Slice '<name>' of type '<type>' is not admitted by ESM v1.*).

The ESM versions, the full disposition table and the Step 7 clock example's binding
result are in [references/parsed-not-runnable.md](references/parsed-not-runnable.md)
and `cratis-screenplay-toolchain` (`references/executable-subset.md`). Read `references/parsed-not-runnable.md` when determining the required ESM version or interpreting binding dispositions. Read `cratis-screenplay-toolchain`'s `references/executable-subset.md` when checking binder admission or reference-execution limits. Model the wider
language freely when the `.play` file **is** the deliverable - documentation,
review, a shared description of a system. Never read a clean `screenplay` run as
evidence a construct works downstream. The language is frozen at ESM v1–v28 ([the freeze decision](https://github.com/Cratis/Screenplay/blob/main/decisions/0092-the-language-is-frozen-at-esm-v28.md)): model with it, and report a typed
Stage, `cratis render` or Studio refusal as a platform gap rather than waiting for new constructs.

## Locate the model

Look first for the project's existing `.play` files: the folder holding them is the model; a new model goes under `Source/` or `src/`, else `Screenplay/`, never under `.cratis/`. Read [references/file-layout.md](references/file-layout.md) (section "Locate the model") when the location is unclear or source might be copied between repositories. `cratis ai install` manages `.cratis/ai/`, never model files.

## Verified product sources

Which tool reports what, and how to read installed versions, live only in `cratis-screenplay-toolchain`
`SKILL.md`; read it before choosing a tool or reporting a version-dependent verdict.
Read [references/slice-type-rules.md](references/slice-type-rules.md) when deciding if a behavior is an Automation or a Translate slice.

## Two phases - discovery, then design

**Never jump into detailed workflow design without broad domain understanding.** Phase 1
maps the territory (actors, processes, outside systems, the most critical workflow; ask,
do not assume; it lands as `module`, `persona` and `import`; protocol in
`cratis-screenplay-discovery`). Phase 2 designs one workflow at a time through all nine steps
([nine-steps.md](references/nine-steps.md): activities, Screenplay output, questions).

## Keep modeling reasoning with its declaration

Read [references/modeling-reasoning.md](references/modeling-reasoning.md) when recording assumptions, boundaries or rejected alternatives on a declaration (`description`, `documentation`).

## The prime directive: do not lose information

Store what happened (events), not just current state. Events are immutable
past-tense facts in business language. **Every read-model field must trace back to
an event.** If a field has no source event, something is missing from the model -
it is not an optional column. Every value starts somewhere specific (a person's input, the
runtime's context, an allocated id, a view's calculation) and is carried by name from there
(slice-design `references/field-lineage.md`).

## The four patterns -> the four slice types

Every behavior is exactly one. An unknown slice type is a compile error:
*Unknown slice type '<x>' - expected StateChange, StateView, Automation or Translate*.

| Pattern | Slice type | Screenplay constructs |
| --- | --- | --- |
| Command -> Event | `StateChange` | `command` -> `produces` -> `event`, plus `validate`, `authorize`, `constraint` |
| Events -> Read Model | `StateView` | `readmodel`, `projection` or `reducer`, `query`, `screen` |
| Event -> decision -> Command/Event | `Automation` | `reaction` |
| External data -> Event | `Translate` | `capture` |

Read [references/parsed-not-runnable.md](references/parsed-not-runnable.md) (section "Trigger reads and event generations") when deciding whether a decided construct (trigger `reads`, event generations) is implemented. An accepted decision is not necessarily implemented.

## Where Cratis diverges from the generic method

Two divergences matter, and getting them wrong produces a model that will not
compile or will not be safe. Both are deliberate.

- **A command may read state.** The generic method forbids `ReadModel -> Command`
  edges. Screenplay ships `reads <ReadModel> [as <alias>] [by <property>]` so the
  model shows what a state-dependent decision consulted. Application-root
  `command reads protected` admits eligible command reads as v25: absent state
  rejects and matching decide/commit changes conflict. Read
  [protected reads](../cratis-screenplay-command-surface/references/protected-reads.md)
  for the strict shape and paths. Legacy reads remain PLAY0271; `concurrency` is
  not protection and cannot combine with protected reads. Stage 4.138.0 renders protected reads as Arc read protection (`cratis render` gets it from the first CLI that bundles it; CLI 3.75.0 refuses them with STAGE-ESM-044); semantic Host/executor support is not generated-app protection.
  Write state-dependent rules as `reads` plus `require` with an indented message;
  mark refused shapes as unenforced and name the intended target mechanism. Model uniqueness as a `unique` constraint. Never copy state into a
  command input, add an attestation flag, or hide the rule in `handler` prose. Do not
  use `reads` to fetch data the command could carry as input. A reaction trigger
  declares consulted views with `reads`, but trigger reads remain unprotected;
  decide in an invoked command instead.
- **Some validation *does* belong in the model.** The generic method routes format
  rules to the type system. Screenplay's type system *is* the `concept`, and a
  concept carries its own `validate` block - so a format rule lives on the concept
  and travels with every use. A state-dependent rule is not a format rule: it needs
  the modeled `reads` + `require` intent, specifications, and the recorded
  target-enforcement gap described above.

## Naming the primitives is the highest-value work

Name concepts before events, classify personal data at the concept, keep events past tense and single-purpose, and never put the event-source identity in an event payload.
Read [references/naming-primitives.md](references/naming-primitives.md) when declaring concepts, `pii` classification, event shapes, `identifier`, inline events or `id "<old name>"` pins.

## Quick gate - before specifications are called done

Run this after the GWT specifications are written. **Do not proceed with gaps.**
When one is found, ask the user to clarify, create the missing element, re-validate.
The full critic pass (evidence, severity, business-question review) is
`cratis-screenplay-model-review`.

1. Every `readmodel` property traces to an `event` (**backward trace**).
2. Every `event` feeds a projection, reaction, or capture target (**forward trace**).
3. Every `command` has documented rejection conditions.
4. Every `reaction` has a termination condition and cannot loop forever.
5. No `given`/`when`/`then` clause references an undefined element.
6. Every behavior is exactly one slice type.
7. Read-model fields use collection types where the domain allows concurrent
   instances - ask *"can there be more than one of these at once?"* for each field.
8. No cross-cutting infrastructure is modeled as a `Translate` slice.

## Choose a file layout

Choose the coarsest layout that keeps source and diffs readable; compile the folder, or its root file, as one application, because compiling files individually reports false unknown-type errors (`PLAY0165`, `PLAY0166`, `PLAY0167`).
Read [references/file-layout.md](references/file-layout.md) when choosing or changing the file layout, writing `import` barrels, or compiling a split model.
Read [multi-service models](../cratis-screenplay-streams-and-consistency/references/multi-service-models.md) when the system is several services with their own event stores: one model folder per service, never one folder over all of them.

## Route near misses

The lifecycle, toolchain and phase skills are in "Start here" above. Construct and neighbouring skills:

Read [references/neighbouring-skills.md](references/neighbouring-skills.md) when the need is a construct, authoring, rendering, diagram or Chronicle C# skill.

## Lineage
Method lineage, Nebulit material and licenses: `references/provenance.md`. Read it when checking method lineage, attribution, or adaptation permissions.
