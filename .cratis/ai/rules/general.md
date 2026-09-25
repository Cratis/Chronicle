<!-- cratis-ai-managed: rules/general.md -->
# Cratis — Project Instructions

Cratis repositories come in **two profiles**, and the rules are scoped to them. **Identify your profile first** — it decides which rules apply.

- **Application profile (default)** — you are *building an application on Cratis*: event-sourced CQRS with **Cratis Chronicle** + **Cratis Arc**, vertical slices, read models persisted to MongoDB/EF Core, and a React + Cratis Components (4.x) frontend in MVVM. Most of this corpus targets this profile.
- **Framework profile** — you are *contributing to a Cratis framework repository itself* (Arc, Chronicle, Fundamentals, Components, …). These are **libraries** — source generators, the Chronicle kernel (Orleans grains + storage), client SDKs, a React component library — **not** vertical-slice event-sourced apps. The application-architecture rules here **do not apply**; follow **[framework.md](./framework.md)**.

**How to tell:** if the repo's own package is `Cratis.*` / `@cratis/*` and it *builds* the framework, you are in the framework profile. If it *consumes* Cratis to build a product, you are in the application profile.

**See [profiles.md](./profiles.md) for a complete list of all available profiles and how to configure them.**

Profile-specific rules declare a **`profile:`** in their frontmatter (`application` or `framework`); a rule **without** one is **universal** and applies everywhere — C#/TypeScript style, code quality, specs (`Cratis.Specifications`), documentation, commits/PRs, American English. In this file, everything from **Project Layout** through the **Implementation Workflow** is *application profile* (skip to the Framework profile section if you're contributing to the framework); Philosophy, Authority, Verification, Quality Gates, and the closing sections are universal.

> **Arc is a standalone CQRS framework — not bound to event sourcing.** Even within the application profile, Arc provides model-bound commands/queries, validation, authorization, and full-stack proxy generation, and works **without** Chronicle (Arc.Core does not depend on Chronicle). A `[Command]` `Handle()` does not *have* to append events — it can return a response, return `void`, or work through injected services. The event-sourcing behavior (a returned event gets appended; `EventForEventSourceId`; "never inject `IEventLog`") comes from the **Arc + Chronicle** integration. This application is event-sourced, so the slice guidance assumes event-sourced commands — read the event-centric rules as the *house default for this app*, not universal Arc laws.

The framework is convention-over-configuration. **Idiomatic Cratis is the goal — not custom abstractions over it.** When something is unclear, prefer the Cratis convention; do not invent. The rules and skills under `.cratis/ai/` are the authoritative answer — if your question is not answered there, ask rather than inferring framework behavior from package internals.

## Project Philosophy

Every rule here serves **ease of use**, **productivity**, and **maintainability**:

- **Lovable APIs** — APIs should be pleasant to use: sane defaults, flexible, extensible, overridable. If an API feels awkward, it is wrong.
- **Easy to do things right, hard to do things wrong** — convention over configuration; artifact discovery by naming/attributes; minimal boilerplate. The framework guides you into the pit of success.
- **Events are facts** — immutable records of what happened. Past tense, one purpose, never ambiguous. If you reach for a nullable property on an event, you need a second event.
- **Strongly-typed primitives are the foundation — get them right first.** The most important, load-bearing decisions in every slice are the domain primitives: `ConceptAs<T>` value types and `EventSourceId<T>` identities (never raw `Guid`/`string`/`int` for a domain value); `ConceptValidator<T>` for invariants that travel with a value everywhere it appears; `CommandValidator<T>` for command-level rules; and past-tense, self-describing `[EventType]` names. These are not boilerplate or an afterthought — they are what makes the model type-safe end to end, the rules enforceable in one place, the events trustworthy forever, and the generated proxies meaningful. Treat naming and typing them precisely as the highest-value craftsmanship in the codebase; a slice built on sloppy primitives is wrong no matter how good the rest is.
- **High cohesion through vertical slices** — everything for a behavior lives together: backend, frontend, specs. Navigate by feature, not by technical layer.
- **Full-stack type safety** — shared models flow from C# through proxy generation to TypeScript. End-to-end typing without manual synchronization.
- **Specialization over reuse** — focused, purpose-built read models over one model reused across conflicting scenarios.
- **Consistency is king** — when in doubt, follow the established pattern.

When these instructions don't cover a situation, apply these values to make the call.

## Three Levels of Authority

Every rule below is one of three kinds — know which, because they carry different weight:

- **Framework contract** — enforced by Arc/Chronicle source, analyzers, or runtime. Violating it breaks the build or behaves wrongly. (e.g. `[Command]` needs a public instance `Handle()`; model-bound queries are static methods on `[ReadModel]`; nullable event properties raise a Chronicle analyzer warning.)
- **Cratis Application convention** — the house default for maintainability and consistent generated code. The framework does **not** enforce it, but follow it for consistency. (e.g. the slice folder shape, one backend file per small slice, declaration order.)
- **Product policy** — belongs in a downstream app's own `.cratis/ai/`, not this generic corpus. (e.g. specific roles, locales, design systems.)

Where a rule is convention rather than contract, this file says so. Do not claim "the framework requires this" for a convention.

## Project-Specific Instructions

This corpus is the shared, generic instruction set common to every Cratis
repository. Individual projects need extra context that does not belong here,
such as product composition, approved environment names, directions for
obtaining credentials, and other local conventions ("Product policy" above).

- Read repository-owned documentation as the canonical project-specific context when it
  exists.
- Read repository-owned documentation only as the documented legacy fallback when
  repository-owned documentation does not exist; never merge both contexts.
- Project context may explain which approved secret mechanism or local setup to
  use, but it must never contain credential values, tokens, keys, passwords, or
  other secrets.
- Project-specific guidance wins when it deliberately narrows shared behavior,
  but it may not weaken organization security, authorization, or required
  quality gates.

## Collaboration Default

Default to agentic behavior: inspect local rules, skills, code, tests, and generated patterns; make conservative assumptions supported by that context; implement and verify end to end when feasible. A user's direct request authorizes every clearly named in-scope action, including external effects such as pushing, opening or merging a pull request, applying the requested label, deployment, issue mutation, and operations against a named environment. The user is the authority for their request; do not require them to identify another approver, decision record, or formal operation profile.

Don't interrupt with questions the repository can answer. Make reversible implementation choices and report them rather than asking the user to approve an implementation plan they already asked you to carry out. Once the user authorizes an action, do not ask them to authorize it again unless the target or consequence materially changes. Ask only when the answer can't be found locally, reasonable product or domain choices have meaningfully different consequences, a consequential effect was not included in the request, the change is hard to reverse and its scope is unclear, or the user asked for checkpoints.

When a question is necessary, write it for the person doing the work, not for the governance system: explain the concrete choice, why it matters now, the consequence of each option, and the recommended option in plain language. Never present unexplained internal labels such as "execution authority," "capability contract," "delegation architecture," "operation profile," or "decider." Do not bundle unrelated verdicts. Ask who should be named in a durable decision record only after explaining that a significant decision has been made and why preserving it is warranted; a decision record is never a prerequisite for carrying out the user's direct request.

## Destructive operations

Before a destructive or bulk external mutation whose exact targets, consequences,
or recovery are not already clear from the conversation, show those details and
obtain explicit user authorization. A sufficiently bounded direct request is that
authorization; do not add a second confirmation step. Re-read the target state
immediately before acting and stop only when drift invalidates the authorized scope
or recovery plan. Git history rewrites remain prohibited unless the user explicitly
requests one.

## Shared AI Distribution

Two invariants; the mechanics of every channel (`cratis ai install`, `@cratis/pi`,
the plugin marketplaces) are in [ai-distribution.md](./ai-distribution.md):

- Never copy or synchronize `.cratis/ai`, `.agents`, `.claude`, `.github`, or `.pi`
  trees between repositories. In a consuming repository, never patch managed
  files under `.cratis/ai/` by hand — update through the channel that installed
  them. The authored corpus in `Cratis/AI` is changed and reviewed here.
- The consuming repository owns its project facts, confidential behavior, and
  local skills; installing, updating, or uninstalling shared AI never merges,
  overwrites, or removes them.

## Verification Discipline

A claim is only as good as the signal behind it — a build result, a test run, a lint pass, observed app behavior — not the model's own confidence. Internal reasoning *plans* the work; external signals *confirm* it.

- **Confirm "done"/"fixed"/"correct" against a fresh signal — never self-assessment.** Run the relevant gate and observe it pass *this time*.
- **After a fix, re-run the gate that failed.** Don't argue yourself to green.
- **A green build is not behavioral correctness.** Compilation proves it builds, not that the slice does the right thing — that's what specs and exercising the UI are for.
- **Report the conclusion and what you didn't verify, in a line or two.** Show the output when asked, when a claim is contested, or when the check failed — see [verification-discipline.md](./verification-discipline.md).

---

# Application profile

> **Building an application on Cratis?** Project layout, slice types, slice naming, the seventeen slice rules, the implementation workflow and the application quality gates are in [application-profile.md](./application-profile.md), which loads only for repositories that select an application profile. If you are contributing to a Cratis framework repo, see **Framework profile** below.

---

# Framework profile

> You are contributing to a Cratis framework repository (Arc, Chronicle, Fundamentals, Components, …). **The Application-profile sections above do not apply** — there are no vertical slices, model-bound `[Command]`/`[ReadModel]` artifacts, projections/read-models, or MVVM app components here; these are libraries. Follow **[framework.md](./framework.md)** for repo structure, library/API design, source generators, the Chronicle kernel, and the framework quality gates. The universal sections (below, and every `profile: universal` rule) still apply.

---

# Both profiles (universal)

## Definition of Done

- The affected solution/project builds with zero warnings and zero errors.
- Relevant specs for every affected project pass.
- For public-facing changes (clients, SDKs, public APIs, developer-facing behavior), documentation is added or updated and its verification passes.

## Where to Look

| For | Location |
| --- | --- |
| **AI Corpus Profiles** — what profiles exist and how to configure them | `profiles.md` |
| **Contributing to a Cratis framework repo** (framework profile) | `framework.md`; creating a repository in the Cratis organization: `new-repository-intake.md` |
| Slice anatomy (commands, `Provide()`, validators, events, projections, read models, reactors, constraints, compliance, cross-slice) | `vertical-slices.md` |
| C# / TypeScript style | `csharp.md`, `typescript.md` |
| Service lifetimes — why anything taking a scoped dependency is scoped or transient, never a singleton | `csharp.md` |
| React + Arc + Cratis Components + MVVM + dialogs | `react.md`, `components.md`, `dialogs.md` |
| Frontend engineering quality & testing | `frontend-quality.md`, `frontend-testing.md`, `storybook.md` |
| Spec patterns — universal `Specification` base (both profiles) | `specs.md`, `specs.csharp.md`, `specs.typescript.md` |
| Spec patterns — the four `*Scenario` helpers (application only) | `specs.scenarios.csharp.md` |
| Strongly-typed values (`ConceptAs<T>`, `EventSourceId<T>`) | `concepts.md` |
| Shared term definitions (event, projection, reducer, reactor, observer, DCB, …) | `glossary.md` |
| Diagnosing a misbehaving slice | read model stale or startup crash: **cratis-chronicle-projection** (*Startup-crash traps*); reactor paused or quarantined: **cratis-chronicle-reactor** (*Failure behavior*); proxy missing: **cratis-arc-command** (*Generate the TypeScript proxy*) |
| Inspecting or operating a **running** Chronicle store (failed partitions, replays, browsing events) with the `cratis` CLI | the **cratis-chronicle-cli-operations** and **cratis-cli-terminal-workbench** skills |
| EF Core read models / migrations | `efcore.md`, `efcore.specs.md` |
| PRs / commits | `pull-requests.md`, `git-commits.md` |
| Reading, citing and superseding a decision record | the **cratis-engineering-decision-record** skill |
| Whether you are allowed to do the thing you are able to do | `capability-is-not-authority.md` |
| What must stop and ask a human | `capability-is-not-authority.md` (absent authority for a consequential effect: stop and ask) |
| What counts as evidence that something works | `verification-discipline.md` |
| Where session notes, plans and handovers may live | `local-work-artifacts.md` |
| Exit-code meaning and wrappers that lose a verdict | `exit-codes-and-wrappers.md` |
| Writing a scan, allowlist or destructive pass that cannot pass vacuously | `guards-and-fuses.md` |
| How the shared corpus is installed, updated and rolled back | `ai-distribution.md` |
| Event modeling / schema migration / calling commands from code / paging / cross-cutting metadata / multi-tenancy | the matching skills |
| Designing an information system, business process or information flow as a **Screenplay** `.play` model | the **cratis-screenplay-event-modeling** skill, then the per-surface `cratis-screenplay-*` skills |
| Rendering a settled `.play` model into an application | the **cratis-stage-rendering-and-sandbox** skill |
| Step-by-step recipes | `.cratis/ai/skills/` |

## Source-of-Truth Discipline

- **Rules define invariants; skills define workflows.** A skill may refine how to apply a rule but must not contradict it. On conflict, follow the stricter invariant and fix the stale artifact.
- **Skills and rules are the authoritative answer.** If not answered there, ask. Don't infer Cratis behavior from package internals.
- Only make high-confidence suggestions.
- Don't change dependency manifests / lockfiles / `global.json` / NuGet config unless explicitly asked.
- When asked to **ship** or **land** changes, use the **ship-changes** prompt (`.cratis/ai/prompts/ship-changes.prompt.md`); invoking it is what authorizes the branch → commits → push → PR → merge → cleanup chain it describes. A request to only commit, only push, or only open a PR authorizes exactly that step, under [Git commits](./git-commits.md) and [Pull requests](./pull-requests.md) — do not route it through ship-changes and do not add the later steps.

## General

- **American English** in all code, comments, and docs (initialize, behavior, color, serialize…).
- Treat warnings as errors; never suppress warning output.
- Reuse the active terminal for commands; create a new one only when the current one is busy or fails.
- All files start with the standard license header:

```csharp
// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
```

## Local AI work artifacts — `.ai-work/` only

Plans, handovers, session notes, scratch analyses and research dumps are **work records, not documentation**: they live only in the gitignored `.ai-work/` at the repository root, never enter git history, and are never the only copy of a durable decision (those go in `decisions/`). A follow-up that must outlive the session is an issue, not a file. Full rule: [local-work-artifacts.md](./local-work-artifacts.md).
