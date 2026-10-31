<!-- cratis-ai-managed: skills/cratis-screenplay-modeling-lifecycle/references/vocabulary-map.md -->
# Vocabulary map

Use the Screenplay term in models and reports. Board terms are for orientation when a user
speaks in them; never model board mechanics (lanes, node ids, positions).

| Event Modeling | Screenplay (`.play`) | Chronicle / Arc (C#) | Cratis corpus skill area |
|---|---|---|---|
| State Change pattern | `StateChange` slice: `command`, `event`, `constraint`, `screen`, `specification` | command + handler appending events to the event log | `cratis-screenplay-*` commands/events; Chronicle event and command skills |
| State View pattern | `StateView` slice: `readmodel`, `projection`/`reducer`, `query`, `screen` | read model fed by a projection or reducer; query | projections, reducers, queries |
| Automation pattern | `Automation` slice: `reaction` (`when`, `invokes`/`produces`), clock/trigger occurrences | reactor (side effect) and/or follow-up command | Chronicle reactor skills (see "automation vs reaction") |
| Translation pattern | `Translate` slice: `capture`, or a `reaction` translating imported external events | ingestion/adapter appending own events | Screenplay capture skills |
| Command (blue) | `command` | command record + handler | commands |
| Event (orange) | `event` (past tense) | event type appended to an event sequence | events |
| Read model / view (green) | `readmodel` + `query` | read model + query | read models |
| Wireframe / screen | `screen` | UI (Arc proxies, frontend) | screens / frontend |
| Actor / persona lane | `persona` with `policy` lines | authorization (roles/claims) | personas/authorization |
| Swimlane (stream/system) | `module` (bounded area); event-source identity via `for` | event source id, event sequence | streams / event sources |
| Chapter / workflow | `feature` | feature folder | vertical slices |
| Slice | `slice` (one behaviour, not one artifact of each kind) | vertical slice folder | vertical slices |
| Scenario (Given/When/Then) | `specification` | generated spec class `when_<spec_name>` (Debug) or hand-written C# specs | `cratis-screenplay-specifications`; C# spec skills |
| Storyline (ordered beats) | a **family** of specifications sharing example data, each one transition (no storyline syntax) | several spec classes | specifications |
| Field validation | concept `validate` (every use) / command `validate`, `require` | `ConceptValidator<T>` / `CommandValidator<T>` | command validation |
| Role permission ("Cannot") | `policy` + `authorize` + `then denied` spec; ownership via `claim ... matches subject` | `[Authorize(Policy)]`, generated policies | authorization |
| Precondition on stream state | `command reads protected` + eligible `reads`/`require` (v25 reference protection); legacy/refused reads remain stated intent. the Cratis renderer renders protected reads as Arc read protection once the CLI bundles a Stage that supports it (older bundles refuse them with STAGE-ESM-044; releases in command-surface `references/protected-reads.md`); semantic-engine execution is separate from generated-app protection. Target Arc `[ProtectedDecision]` + `DecisionRead<T>` or Chronicle DCB needs separate verification | decision over a guarded read | command validation |
| Generated id (`derived:uuid4()`) | `generated` (authorable, not executable) | id supplied before `Handle()` | commands |
| External system / outbound call | `Translate` slice (inbound); `system`/`operation` (authorable, not executable) or Automation (outbound) | `ICommandOperation` / reactor | operations, reactors |
| HTML mockup | `screen` Level 1 + board (`visualize-model`) | default Scene composition | ui composition |
| Event sequence / inbox, outbox / observer | event source `for` identity / not modeled in `.play` (record as target) / `reaction` or projection | event sequence / inbox, outbox / observer | event sources, observers |
| Ready (corpus behaviour lifecycle) | P6 accepted on a source identity | n/a | n/a |
| Todo list | a read model listing open work + a `reaction` that closes items via events | read model + reactor | automations |

## Translation has one current meaning
Translation (`Translate` slice) is outside data becoming our own facts: a `capture`, or a
translator reaction over an imported external event. The corpus glossary defines it this way. Older
text in some corpus files used "translation" for a reactor that appends follow-up events or returns
a command for another slice; that is an **Automation** (`produces` or `invokes`). Model by the
Screenplay meaning, and when you meet the older usage, translate it and say so.

## Automation vs reaction vs reactor
- Event Modeling **Automation**: a processor triggered by events or time, often reading a todo
  list, issuing a command or producing an event.
- Screenplay **`reaction`**: the construct that models it (`when` trigger, optional `where`,
  `invokes` a command or `produces` events).
- Chronicle **reactor**: C# observer, usually for side effects; it does not by itself define
  business decisions. Stage renders supported event-triggered Automation as Chronicle reactors.
  Translate/captures and refused reactions remain gap-fill. Mixed events and commands
  in one handler remain `STAGE-ESM-037`: Arc supports the return shape, but redelivery
  after a failed command duplicates events. Keep the reaction whole and prove retry safety.
  Source boundary: Stage skill `references/admission.md`.

## Scenario vs specification
"Scenario" is the Event Modeling example and the scenario *types* `cratis-screenplay-scenario-coverage` uses for
coverage. "Specification" is the Screenplay construct that states one scenario in
given/when/then. Report coverage in scenarios, author specifications.

## State Change vs `StateChange`
Prose: "state change" (the pattern). Code: `StateChange` (the slice kind). Same for
State View/`StateView`, Translation/`Translate`.
