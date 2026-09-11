---
name: cratis-specification-by-example
description: Structure and name executable specifications the Cratis way — one behavior per specification, a for_/when_/and_ path that reads as an English sentence, layered given/ contexts, and an explicit decision about what is not worth specifying. Use when deciding how to organize, name, or scope specifications in any language. Do not use for language mechanics; route to the C# or TypeScript specification skill for those.
license: MIT
---
<!-- cratis-ai-managed: skills/cratis-specification-by-example/SKILL.md -->

# Cratis specification by example

Cratis calls automated tests **specifications**. That is not a vocabulary
preference — it changes what you write. A test asks "does this code still do
what it did yesterday?". A specification states what the software promises, in
the language of the domain, in a form a machine can check. The folder tree is
the table of contents; the file names are the sentences; the assertions are the
promises.

This skill is the language-agnostic layer. It settles the questions that are the
same in C#, TypeScript, and anything else: what to specify, how to name it,
where to put it, and when to stop.

## Verified product sources

| Source | Version | What it grounds |
| --- | --- | --- |
| `Cratis.Specifications` analyzers | `CRSPEC0001`–`CRSPEC0007` | The naming and structure rules below are machine-enforced in C#, not taste |

The seven diagnostics are declared in `Cratis.Specifications.CodeAnalysis.DiagnosticIds`:

| Id | Rule |
| --- | --- |
| `CRSPEC0001` | A test method inside a specification must be named `should_*` |
| `CRSPEC0002` | A file declares at most one specification |
| `CRSPEC0003` | A test method must not sit on a reusable `given/` context |
| `CRSPEC0004` | A `should_*` method without a test attribute never runs |
| `CRSPEC0005` | A lifecycle method must not call its base implementation |
| `CRSPEC0006` | A specification declaring test methods must be public so the runner finds it |
| `CRSPEC0007` | The action under test must not sit on a reusable `given/` context |

In a language without those analyzers the same rules hold; the reviewer enforces
them instead of the compiler. Reverify against the owning product repository
before claiming behavior for another version.

## Route near misses

- Writing the C# mechanics — the `Specification` base, `Establish`/`Because`,
  substitutes, assertions: use `cratis-specifications-csharp`.
- Writing the TypeScript mechanics — `describe`/`it`, Sinon, the Chai `should`
  interface: use `cratis-specifications-typescript`.
- Specifying an event-sourced application slice with the in-process scenario
  family: use `cratis-application-slice-specifications`.
- Deciding what a command, projection, reducer, or reactor *should do*: that is
  a modeling question. Settle the behavior first; a specification records a
  decision, it does not make one.

## Step 1 — State the behavior as a sentence

Before creating a file, say the specification out loud as one English sentence:

> **for** the changeset, **when** adding changes, **and** there are differences,
> it **should** record them.

Every clause becomes one level of the path. If the sentence does not survive
being spoken, the specification is not focused enough yet — that is the signal
to split it, not to write a longer name.

## Step 2 — Build the path from the sentence

```
for_<SubjectUnderTest>/
├── given/
│   ├── all_dependencies            ← substitutes every collaborator
│   └── a_<subject>                 ← builds the subject, layered on the above
├── when_<behavior>/                ← a behavior with several outcomes
│   ├── and_<condition>
│   ├── with_<state>
│   └── without_<requirement>
└── when_<simple_behavior>          ← a single outcome is a single file
```

- **`for_<Subject>`** names the thing being specified.
- **`when_<behavior>`** names the action. This is the only place the word
  **`when`** may appear.
- **Outcome names** use one of five prepositions: `and_`, `with_`, `without_`,
  `having_`, `given_`.

**Two `when`s in one path is always wrong.** A file named
`with_a_registered_migration_when_appending_a_generation_1_event` is two
sentences pretending to be one. Fold the context into the `when_` folder and let
the outcomes be flat files:

```
# Wrong
when_appending_event_with_migrations/
└── with_a_registered_migration_when_appending_a_generation_1_event

# Also wrong — a folder level that holds a single file
when_appending_event_with_migrations/
└── and_event_is_generation_1/
    └── with_a_registered_migration

# Correct
when_appending_event_with_registered_migration/
├── and_event_is_generation_1
├── and_event_is_generation_2
└── and_event_has_default_value
```

Add a sub-folder under `when_` only when that condition has its own several
outcomes. One outcome is one flat file.

## Step 3 — One behavior, one specification

Each distinct outcome is its own file. When a specification fails you should
know from its name alone which promise broke, without reading the diff.

- A file that specifies a whole class is not a specification; it is a test suite
  wearing the name.
- Assertions inside one file all describe the *same* outcome from different
  angles. A second setup means a second file.
- Optimize for readability over removing duplication. Repeating three lines of
  setup so a specification is self-contained is a good trade; a shared helper
  the reader must go and open is not.

## Step 4 — Put the world in `given/`, the action in the specification

A context captures the world *before* the action. Layer them:
`all_dependencies` substitutes the collaborators, `a_<subject>` builds the
subject on top of it, and the concrete specification adds only what is unique to
its case.

- Name a context `a_` or `an_` so it reads as "given an observer, when
  handling".
- **The action under test never appears in a context** (`CRSPEC0007`) and
  **assertions never appear in a context** (`CRSPEC0003`). A context that acts is
  a specification that several files silently share, and the failure it produces
  names the wrong subject.
- Contexts build in layers: `all_dependencies` → `a_reactor_handler` →
  `when_handling`.

## Step 5 — Decide what not to specify

Specify decisions, transformations, branching rules, and coordination between
collaborators — the places defects live. Leave alone:

- Auto-properties and properties that return a constructor parameter.
- Simple delegation that forwards to a collaborator and adds nothing.
- Logging. It is fragile to specify and worth nothing when it passes.
- Trivial null checks the type system already enforces.

A name beginning `when_getting_` or `when_returning_` is the tell: that is a
getter, not a behavior. Delete it rather than maintaining it.

## Step 6 — Make the outcome observable

A specification is only as good as the signal it reads.

- **Assert on the outcome, not the message.** A presentation string is copy;
  it changes for reasons that have nothing to do with the behavior. Assert on
  the identity of what failed — an error code, a constraint name, a typed
  result — never on the sentence shown to a user.
- **Never wait on the clock.** A sleep before an assertion passes because the
  machine happened to be fast, and writes today's latency into the suite. Await
  a completion signal, under a deadline. A timeout turns a hang into a named
  failure; a sleep turns a race into a coin flip.
- **Nothing ambient.** The real clock, a random value, the network, shared
  storage, or an ambient culture makes the outcome depend on something the
  specification never stated. Inject it.
- **A specification that cannot fail proves nothing.** If you cannot describe
  the change that would make it red, it is not specifying anything yet.

## Step 7 — Say what the specification did not cover

Finishing a behavior means naming its unspecified edges, not claiming the
behavior is proven. A suite that lists only what passed reads as if everything
was checked. Record the cases you deliberately left out and why — an untested
edge someone chose is a different thing from one nobody saw.

## Verify

- Every path reads as one English sentence, and `when` appears only in a
  `when_<behavior>` folder name.
- Every outcome file name starts with `and_`, `with_`, `without_`, `having_`, or
  `given_`.
- One specification per file; one outcome per specification.
- No action and no assertion sits on a `given/` context.
- Nothing trivial, delegated, or compiler-verified is specified.
- No assertion reads a presentation message string.
- No sleep, bare delay, or poll loop stands in for a completion signal.
- The suite runs green, and the report names what was not covered.
