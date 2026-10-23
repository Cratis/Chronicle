<!-- cratis-ai-managed: skills/cratis-screenplay-legacy-extraction/references/signal-to-intent.md -->
# From technical signal to candidate meaning

## Contents

- Core question per write
- Rules
- Tests
- Targeted read paths
- Generator facts (static evidence)
- Prologue facts (dynamic evidence)
- Read models
- Realization convention

Every rule here is a **modeling default** or a **review question**, not a law. A signal
proposes a reading; the evidence row records the reading as `proposed` until an expert or a
code read confirms it.

## Core question per write
For every table change, ask: *what happened in the business that made this row change, and
who decided it?* If nobody can answer, classify the row `unknown` and ask.

## Rules

| Signal | Default reading | Typical trap | Do this |
|---|---|---|---|
| One request writes several tables | one decision; one table carries the fact, the others are consequences | one event per table (`CheckinCreated`, `HookUpdated`) | one event named for the decision (`CoatTakenIn`); consequences become projections |
| Generic update route (`PUT /members/{id}`) | several intents hidden behind one form | one `MemberUpdated` event | split by which columns change together and why (address change, suspension, renewal); ask experts. No `Update/Edit/Save<Noun>` command and no `<Noun>Updated/Changed` event; a full-payload `*Updated` event is the default generator output and a review signal |
| Request with no table write and a 4xx status | a rejected attempt; evidence of a rule | Prologue models it like a success (status codes are ignored) and the merge can strip properties | `validate`/`require`/constraint plus a `then error` spec with the exact message from code (authorization refusals are `then denied`); legacy HTTP code and check order go in slice `documentation` |
| 2xx with no table write | read, idempotent no-op, or external side effect | treated as a command | check code; classify |
| Single-column unique index | a business uniqueness rule, or a technical key | every unique index becomes a constraint, or a clear business rule is dropped to a bare `rule` | when the meaning is clear, `unique <prop> on <every event that sets it>` plus `released by <removal event>`; list collation, case and reuse questions. Fall back to a bare `rule` only when no constraint form fits, and record that as a loss. A purely technical key stays out |
| Foreign key | an identity relation between streams or read models | copying the parent id into every event | keep ids only where a consumer needs the value |
| Audit columns (`UpdatedAt`, `RowVersion`, `ModifiedBy`) | infrastructure | event or command properties | drop; `$context` carries time and caller |
| Status/enum column changes | a lifecycle; each transition is a candidate event | one `StatusChanged` event | one event per meaningful transition |
| Write with no user command | job, trigger, stored procedure, integration, manual fix | assumed to be an automation | classify: maintenance / correction / integration / scheduled business process (trigger evidenced) / unexplained |
| Inbound integration write (feed, webhook, file import) | an outside fact entering the system | modeled as our own decision | Translate candidate with correlation and dedup (cratis-screenplay-automations-and-translations) |
| Outbound call after a write (mail, payment, partner API) | a side effect of a decision | an event per call, or the outcome never recorded | reaction candidate; note what "once" must mean. When the business must know the result (sent, failed, refused), that outcome is a fact recorded by the command the reaction invokes (cratis-screenplay-automations-and-translations) |
| Table read but never written by this system | reference data owned elsewhere | an invented event that "loads" it | ask where it comes from and who maintains it; a Translate candidate or a `seed`, never a made-up event |
| Generic engine (template- or schema-driven records, EAV tables, configurable workflow definitions) | infrastructure that serves several real cases | modeling the engine (`Entity`, `Attribute`, `Value`) | model the real cases it is configured for as named events with real properties; configuration rows are evidence (`doc` or config locator) |
| Sequence between steps | code rarely shows order | an order guessed from file or method order | take it from the UI click path, a document or an expert; record the source in the feature `description` story line; an unconfirmed order is an assumption with a question |
| Who calls an entry point (a role or claim check, the page that posts it, the job that runs it) | the persona or non-human cause behind the command | inferring a generic `User` | candidate `persona` (Does, Reads, Cannot) or cause in `STATE.md`; the role check is evidence for `authorize` |
| Code comment stating a rule ("orders over 10 000 need approval") | a rule at `Read` strength, possibly stale | modeled as fact | an evidence row with the comment's locator; confirm against the code path or an expert before writing a specification |
| Visible label on a form or list | the business word | the code identifier kept as the name | a Terms row (`evidence-table-template.md` section 9) with the UI label as the business term |
| Health, simulation, test or admin routes | infrastructure or tooling | become domain modules | exclude, list in the loss report |
| Custom span or handler named for an operation (`TakeInCoat`) | the best available intent signal | ignored in favour of route names | prefer it for command names |
| Concrete ids or codes in paths (`/coats/T-551/handback`) | identifiers leaked into names | `T551` module or slice | normalise to route templates; allowlist `http.route` |
| Computed value stored (price, score, total) | a calculation | dropped as "derived" | keep as a fact when it matters historically (a quoted price); otherwise a projection |
| Soft delete flag | cancellation, archiving, or GDPR erasure | `XDeleted` | name the business reason; erasure is a PII question |

## Tests
Tests are often the densest statement of rules and messages in a legacy code base, and the
likeliest to encode accidental behavior. They are hypotheses at `Read` strength, with
classification kept separate and the expert verdict still required.

| Signal in a test | Default reading | Do this |
|---|---|---|
| Test or fixture names | business vocabulary | candidate event and command names, still `proposed` |
| Failing-path test with an asserted message | a rule with its exact message | `validate` or `require` plus `then error` with that message |
| Role or permission test | an authorization gate | `authorize` plus a `then denied` specification |
| Fixture data | realistic example shapes | shapes for specification values; invent values when fixtures hold personal data |
| Assert-only trivial input check (format, required) | a value rule | a concept `validate`, not one specification per case |
| Test of removed behavior, or a test with no production caller | unclear | an `unknown` row and a question; never modeled |

Arrange, act and assert is a hint, not the shape of a scenario. Repository seeding, mocks and setup
calls are never `given` events; derive `given` from the prior facts the rule depends on
(`cratis-screenplay-scenario-coverage`). Reading tests is allowed; running them stays
approval-gated.

## Targeted read paths
When a Pass 1 or Pass 2 feature needs code reads, follow one path end to end and read only the
files on it. Each hop has a typical yield:

| Path | Hops | What to take from it |
|---|---|---|
| Write | entry point (route, handler, form action), input model and validation, service, entity, repository, schema or migration, response and error mapping | the command name and inputs; `validate` rules with their exact messages; state-dependent checks (`reads` plus `require`, eligible protected opt-in or recorded legacy/refused shape and target gap); unique indexes; the error mapping gives the `then error` text and the legacy HTTP code for the slice `documentation` |
| Read | entry point, query or repository, response model, paging and filter conventions | the read-model shape and query arguments (`by`, `filter`); which caller may narrow what |
| Background | listeners, after-commit hooks, message consumers, schedulers, outbox relays | reaction or translation candidates; say "none found" when there is none |

Tests for the feature are read last, as hypotheses (above). To learn the realization convention
before any feature is mapped, trace one recently changed, ordinary feature
(`git -C <repo> log --stat -20`, read-only), not the oldest or the most special one: an old feature
shows how the code used to be written.

## Generator facts (static evidence)
- Exact or Configured evidence (bound symbols, attributes, registrations) is strong for
  *structure*: command and event types, properties, validators, authorization, constraints.
  It says nothing about why.
- Conventional and Heuristic evidence never establishes persistence, stream ownership or
  authorization on its own.
- An existing event-sourced system may already have business events; still check names for
  CRUD shapes and payloads for property sourcing.

## Prologue facts (dynamic evidence)
- Command names come from span names, else from `{Action}{Resource}` or
  `Create/Update/Delete/Handle{Resource}`; events are `{Table}{Created|Updated|Deleted}`.
  Treat both as placeholders.
- Command properties are the union of written columns and span attribute keys, not the
  request payload (bodies are never captured). Expect audit, foreign-key and telemetry noise.
- DB transactions correlate to a request by time window only. Under concurrency or with
  background jobs, a capture can mix several commands; check suspicious captures in code.
- PostgreSQL updates list every column of the table, not the changed ones; SQL Server lists
  changed columns.
- LLM refinement renames and describes only; it never merges, splits or removes. Renames are
  global (one key renames every occurrence) and constraint properties are not renamed, so a
  refined `.play` can be invalid.

## Read models
Source read models from screens, exports and real query usage (code, UI walk), not from table
mirrors (`All{Table}s`). Prologue never sees reads.

## Realization convention
Once a few features are evidenced, record how this system realizes each kind of behavior
(`evidence-table-template.md` section 10) and check later passes against it; deviations are
questions.
