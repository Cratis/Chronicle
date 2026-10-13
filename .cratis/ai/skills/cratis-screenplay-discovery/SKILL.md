---
name: cratis-screenplay-discovery
description: "Discover a domain and its event timeline by interview and live storming, landing results directly in `.play` as modules, personas with Does/Reads/Cannot, features and past-tense events with essential fields, in story order, with branches, way-outs and endings named. Use when starting a model, exploring a workflow, supporting a human workshop, or turning notes, tickets or documents into events. Not for: command or read-model design (use `cratis-screenplay-slice-design`), reviewing a finished model (use `cratis-screenplay-model-review`), or reverse-engineering code (use `cratis-screenplay-legacy-extraction`)."
license: MIT
---
<!-- cratis-ai-managed: skills/cratis-screenplay-discovery/SKILL.md -->

# Discovery: events, personas and the story, straight into `.play`

## Purpose

Find out what the business does and which facts it must remember, and write those
facts into a `.play` model while the conversation is still going. Output: a compiling
skeleton of modules, personas (Does / Reads / Cannot), features and events with
essential fields, ordered as the story happens, plus the open questions, rules and
assumptions. Commands, read models and screens come later (`cratis-screenplay-slice-design`).
Phases, modes, verdicts and the handoff packet live in `cratis-screenplay-modeling-lifecycle`;
syntax and tool commands in `cratis-screenplay-toolchain`.

## When to use / when not

- Use: a new model; a new workflow in an existing model; a brief, ticket, transcript,
  spreadsheet or process document to turn into candidate events; a timeline to check for
  completeness; a human workshop the agent supports (`references/human-workshop.md`).
- Not for: commands, inputs, read models, queries, screens (`cratis-screenplay-slice-design`);
  stream boundaries and invariants (`cratis-screenplay-streams-and-consistency`); reactions,
  captures, external translation (`cratis-screenplay-automations-and-translations`);
  scenarios (`cratis-screenplay-scenario-coverage`); auditing (`cratis-screenplay-model-review`);
  existing code or a database as the source (`cratis-screenplay-legacy-extraction`).
- Small change to an existing model (one or two events): enter at step 5.
- Model-first: work in `.play` where an accepted model under the model root covers the scope or
  the repository is opted in (the root holds a committed `.play` file (`git ls-tree -r --name-only HEAD` lists a `.play` file there, narrowed to `-- <root>` when a root is configured), or the project explicitly set
  `mcpServers.screenplay.root` in `.cratis/ai.json`). Otherwise stay code-first; only the
  entry-point session proposes a model, at most once per session; the master definition is in
  `cratis-screenplay-modeling-lifecycle`. A direct request to model this scope is itself consent.

## Evidence and privacy

- Do not paste production data, customer records or personal data into hosted models or
  sub-agents without the user's approval. Use invented examples in the model and in
  specifications.
- Classify personal data on the concept as soon as it shows up.

## Verified product sources

Current pins are listed once in `cratis-screenplay-toolchain` `references/versions.md`. The
statements below were checked at these older tags (Screenplay v4.66.0 `c89198b`, cratis CLI v3.28.3,
Stage v4.24.2, Arc v22.50.5, Chronicle v19.32.0), kept as provenance, not as current pins.
Model notes use Screenplay `v4.144.0:Documentation/screenplay/model-notes.md:12-77`
and decision 0045:40-79; tool sources: model-authoring `references/model-notes.md`.

| Fact used here | Source |
| --- | --- |
| Persona syntax, report-only, policies must be declared | Screenplay `v4.66.0:Documentation/screenplay/personas.md` |
| Description order; authored/import board order and order-preserving MCP layouts | Screenplay `v4.125.0:Documentation/screenplay/grammar.md`, `folders.md`, `imports.md` |
| Single-line and fenced `description` (`FencedText`) | `v4.66.0:Documentation/screenplay/slices.md#descriptions`, `grammar.md:1084-1088` |
| `<Type>[]`, shapes must be declared; `@pii` and `reason` on concepts | `v4.66.0:Documentation/screenplay/events.md`, `concepts.md` |
| `then denied`, `given caller`, `then error` | `v4.66.0:Documentation/screenplay/specifications.md` |
| PII on an event-source id is rejected by Chronicle (CHR0034); nullable event properties warn (CHR0012) | Chronicle `v19.32.0:Source/Clients/DotNET.CodeAnalysis/DiagnosticIds.cs` |
| Any `@pii` or `@sensitive` on a concept fails binding (PLAY0268) at 4.66.0; current markers are in `cratis-screenplay-command-surface` | `v4.66.0:Source/DotNET/Screenplay/Semantics/SemanticModelBinder.Concepts.cs:21-24` |

Every complete `screenplay` fence in this skill compiles with the standalone compiler
4.139.0 (`--warnaserror`).

## Interview phase

**Skip if** the input is a written spec with rules, the named experts are present, and the
existing model already lists the workflows and personas. Otherwise ask when information is
missing or ambiguous, guided by the critical questions below and their follow-ups; with
everything in hand, go straight to the procedure and do not ask questions whose answers you
already have. One question per turn, the one that unblocks the most; offer two to four likely
answers when the domain suggests them; use the harness's question tool if it has one. Findings
feed plotting and the handoff. Full menu with follow-up triggers: `references/interview-questions.md`.

Critical questions:

1. **How complete is the input?** (written spec / documented rules / rough list / word of
   mouth / running system). Thin input means a heavier step 4. Follow up: for a rough list or
   word of mouth, probe for missing scenarios and ask the rules to be stated.
2. **Who has the final word on business meaning?** Follow up: only engineers present means
   every meaning is an assumption to confirm; recommend inviting the domain expert.
3. **Where is it complex, disputed or fragile today?** (the hotspots). Follow up, per hotspot:
   what goes wrong there, what happened the last time.
4. **Which rules must always or never hold?** Follow up, per rule: what does the person see
   when refused, and must the business remember the breach (a fact only if so)?
5. **Who takes part, and what may each role not do?** Follow up: for each Cannot, is it an
   authority limit or a rule for everyone?

With no input at all, open with: "Describe the starting situation, then the steps people take
to reach the intended result." With a brief, restate domain and goal in two sentences and confirm.

**Unattended: assume visibly.** Never stop to ask. Take the most reasonable assumption,
record the open question and assumption as a `question` note anchored to the element,
not chat or `description` text. STATE.md carries its note ID and assumption for handoff;
settled reasoning belongs in `documentation`. Keep decisions apart from assumptions.
Read [discovery questions](references/plotting-and-handoff.md#durable-discovery-questions)
when recording or answering a question: `refs`, external decisions and realization.
Never put secrets or personal data in note text; `.screenplay/notes.json` is committed.

## Procedure

1. **Frame.** Read what you were given and the existing `.play` model first; existing events
   are the starting list. Judge the input (question 1). Choose the mode (model-first or offer).
2. **Map the territory (whole domain, shallow).** What the business does, who takes part, the
   main processes, outside parties, hotspots, and the workflow that matters most. Land it as:
   - `module` per business capability (not per actor, team or layer), with a `description`;
   - `persona` per role that acts or reads, with Does / Reads / Cannot in its `description`
     and its `policy` lines (`references/personas-and-causes.md`);
   - non-human causes (schedules, outside parties, our own follow-up) in STATE.md;
   - `feature` per workflow a single expert could tell end to end; `description` = goal and
     what success means. Do not split one flow; do not merge two stories.
3. **Storm one workflow** with the loop in `references/storming-loop.md`: pull candidate facts
   from any input, apply the naming and not-an-event filters (`references/event-naming.md`),
   write them into the model, report the change, ask one question.
4. **Sweep wide, then filter.** After a first story exists, run the lenses in
   `references/divergent-sweep.md`. List first, judge second. Each lens ends as a modeled fact,
   a set-aside, an open question, or "does not apply, because ...". Unattended: model a lens
   fact only when the input implies it; otherwise record an open question.
5. **Plot: order and branch** (`references/plotting-and-handoff.md`). Walk from the first
   trigger to every end. In each event's slice `documentation` record what it comes **after**,
   what **causes** it (persona, schedule, outside party, other fact) and what it is **only
   allowed under** (intent). At every event ask what can happen next *instead*, including
   nothing, and which next steps may happen in any order. Classify forks: *outcome branch* (one
   decision, exclusive results: same feature, sibling events) or *separate story* (decided
   before the process starts, everything after differs: its own feature). Name every terminal
   state and every undo or correction path; a dead end is an open question.
6. **Essential fields.** Each event gets the identity it belongs to (implicit through the
   slice's future `for`; name the subject and what tells two apart in STATE.md) plus the one or
   two business facts that make it meaningful. A simple state transition may carry no payload
   beyond its identity: do not pad events with fields just to reach a count. Domain names; `<Type>[]` for collections (declare the `type`); a
   `concept` for a value with its own meaning; classify personal data on the concept with
   bare `pii` and a reason now; operational secrets use `secret` (4.127.0).
   Either blocks binding (PLAY0268); record the gap, never drop protection.
   Ask what use, lawful basis, retention, recipient/transfer and erasure needs the
   data serves; declare those in a `purpose`, not a concept reason. Special/criminal
   qualifiers describe the value, not legal permission. See command-surface
   `references/compliance.md` for purpose coverage checks and the processing record.
   For who-is-calling details, distinguish token claims from a keyed single-result
   identity-source query or opaque implementation. The `identity` block is metadata;
   executable detail reads remain unsupported. See `references/context.md` in command-surface.
7. **Validate, gate, hand off.** After a coherent batch of edits (not every sentence) run V1
   with the tool and version named (`cratis-screenplay-toolchain`). Stop storming a workflow
   when the user says it is complete or both closing questions return nothing new; unattended,
   when every sweep lens has an outcome. Hand off to `cratis-screenplay-slice-design` with
   the packet in `references/plotting-and-handoff.md`.

## Rules

### Compiler contracts (the tool enforces these)
- Slice types are a closed set: `StateChange`, `StateView`, `Automation`, `Translate`. In
  discovery an event lives in the `StateChange` slice that will produce it, named after the
  probable command; a slice with only an `event` compiles.
- A `persona` names declared policies only (unknown policy = error); `description` appears
  at most once, anywhere in its body.
- Declare each event once in the whole application; other slices refer to it by name.
- Boards keep authored declaration and import order: imports expand depth-first, glob
  matches alphabetically. Unimported folder files remain visible after ranked siblings.
  MCP `expand-layout` preserves this order with import barrels; an import-less
  `PlayFileWriter.Expand` layout falls back to path order. Order is presentation, not
  execution or identity. Keep causes and branches in the feature `documentation` and STATE.md.
- A misspelt keyword inside a slice is only warning PLAY0029 and the block disappears: always
  validate with warnings as errors.

### Modeling defaults (follow unless the domain gives a reason not to)
- An event is a fact the business wants to remember: past tense, business verb, two to four
  words, specific (`BerthAssigned`, not `BerthUpdated`). No generic edit events.
- One business decision usually yields one event; several facts recognised separately may
  yield several, with the reason in slice `documentation`.
- Calculated values are views, not events, unless the calculation is itself a decision the
  business must be able to show later (a quoted price, a fee set for the season).
- Personas are roles with a purpose. A read-only persona is legitimate; a role that neither
  acts nor reads is a question. Avoid a generic `User`.
- Every event traces to a cause (persona action, schedule, other fact, outside party); every
  party named in a process is a persona or a non-human cause.
- A hand-over from an outside party is named as the fact we learn from it
  (`MooringPermitGranted`). If most of a story is the outside party's steps, model only what
  crosses to us.
- A rule that depends on stored state is recorded in `documentation` on the slice it constrains
  (`only if ...`), marked as stated intent not enforced in the model today, with its target
  enforcement named for `cratis-screenplay-streams-and-consistency` and
  `cratis-screenplay-slice-design`. Never state copies, attestation booleans or rules living
  only in prose: `description` text reaches code only as XML doc comments, never as behavior.
- Ask how the work is done today without the system; question steps that exist only for software.

### Review questions (ask; do not apply mechanically)
- A different story, or a different ending of the same story?
- Does an optional field hide two situations (two events), or one fact with an optional detail?
- Can several be in progress at once for the same party?
- What happens when it fails, is withdrawn, expires, or is corrected afterwards?
- Owned by this business, or reported to it by an outside party?
- Can two parties want the same scarce thing at once? Who wins?
- After this point, can it still be called off, and by whom?

## Identity-affecting edits on an existing model

Renaming, moving or removing an event, feature, module or slice in a model with persisted
identities (`.screenplay/identities.json`) or stored events is identity-affecting: the identity
owner performs it (`cratis-screenplay-modeling-lifecycle`). Fresh, unpersisted models: plain edits.
Prefer typed, identity-preserving MCP operations; a subagent without MCP returns the request.

## Gate

Binding, checked against fresh tool output, not memory. Discovery is done only when all hold,
and the report says which verdicts were not run (V2 to V5 are not run in discovery):

- V1 passes with warnings as errors (tool and version named).
- Every workflow in scope has a start trigger, ordered events, named terminal states, branches
  classified, and every sweep lens answered.
- Every event except a starting one has an *after* and a *cause*; every non-terminal state has
  a way out or a recorded reason it has none.
- No generic edit event or command name (`Update/Edit/Save/Set/Change<Noun>`,
  `<Noun>Updated/Changed/Edited/Saved`); no two events mean the same thing.
- Every persona has a purpose, something it does or reads (or a question saying why not), and
  Cannot lines or "none known"; every Cannot line is listed as a denial candidate.
- Essential fields present; personal data classified on concepts.
- Rules register, denial candidates, competition and duplicate candidates, hotspots, open
  questions and assumptions are carried in STATE.md; open questions are also committed
  `question` notes with anchors, not only said in chat.

## Verify

- Run V1, quoting the tool, its version and the exit status.
- Read the model back in story order (feature descriptions, then events) and compare with what
  the user said; list differences.
- Check the Gate bullets one by one and report each as met or not met.

## Route near misses

- Commands, inputs, read models, screens, lineage: `cratis-screenplay-slice-design`.
- Streams, constraints, protected decisions: `cratis-screenplay-streams-and-consistency`.
- Clocks, reactions, outside data: `cratis-screenplay-automations-and-translations`.
- Specs for Cannot, competition, duplicates: `cratis-screenplay-scenario-coverage`.
- Grammar for event, persona and concept: `cratis-screenplay-command-surface`; the nine-step
  facilitation questions: `cratis-screenplay-event-modeling`.
- Audit of a finished model: `cratis-screenplay-model-review`.
- Existing code or database as the source: `cratis-screenplay-legacy-extraction`.
- Diagram of the result: `cratis-event-model-diagram`.
- Syntax, diagnostics and tools: `cratis-screenplay-toolchain`.

## References (load on demand)

- `references/interview-questions.md` - questions by topic, why, follow-ups.
- `references/storming-loop.md` - the loop, edit kinds, a compiled discovery skeleton.
- `references/event-naming.md` - naming rules, not-an-event filter, repairs.
- `references/personas-and-causes.md` - role catalogue, Cannot resolved to executable gates.
- `references/divergent-sweep.md` - lenses for the wide pass.
- `references/domain-analysis-template.md` - read when requirements arrive as prose and a person must confirm your reading before you model: the note shape, requirement checks, expert questions.
- `references/plotting-and-handoff.md` - after / caused by / only if, plot format, whose turn it is, Storyline seed, handoff packet.
- `references/facilitation.md` - running sessions, disagreement, closing, unattended runs. Read when facilitating a group, handling disagreement, or closing an attended or unattended session.
- `references/human-workshop.md` - supporting a human workshop: preparation, run sheet, live scribing.
- `references/worked-session.md` - a short session end to end. Read when you need an end-to-end example of discovery from thin input.
- `references/provenance.md` - sources and attribution. Read when checking source attribution or adaptation history.

## Lineage

Adapted in part from TrogonStack `eventmodeling-brainstorming-events` and
`eventmodeling-plotting-events` (MIT); details in `references/provenance.md`.
