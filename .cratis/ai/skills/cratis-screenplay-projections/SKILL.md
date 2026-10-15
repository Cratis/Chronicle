---
name: cratis-screenplay-projections
description: Build a Cratis Screenplay read model with the Projection Declaration Language (PDL) — keys, `from`/`every`/`all`, property mapping and AutoMap, joins, children and nested objects, removal, arithmetic counters, and the reducer escape hatch. Use when declaring or changing how events become read-model state in a `.play` model, or when a projection does not populate what was expected. Use `cratis-chronicle-projection` instead for hand-written C# Chronicle projections.
license: MIT
---
<!-- cratis-ai-managed: skills/cratis-screenplay-projections/SKILL.md -->

# Projections — the Screenplay PDL

Read [preferred syntax](../cratis-screenplay-toolchain/references/preferred-syntax.md) when writing or reviewing `.play`.

A `projection` folds events into a read model. Its body is the **Projection
Declaration Language**, an embedded sub-grammar with its own parser — not free
text. This is half of every event model, and the half most easily got wrong.

## Locate the model

Look first for the project's existing `.play` files: the folder holding them is the
model. A new model goes under the repository's `Source/` or `src/` folder, else in a
`Screenplay/` folder at the repository root; never under `.cratis/`, which holds
configuration and the shared AI corpus only. This is the
conventional home for consumer-owned `.play` source; do not invent another
location or search the whole repository before checking it.

`cratis ai install` manages `.cratis/ai/`, never model files. Never
hand-copy Screenplay source between repositories. Keep Markdown that explains,
questions or navigates the model in the repository's documentation; the `.play`
source is the single flow model.

## Verified product sources

| Package | Version | Purpose |
| --- | --- | --- |
| `Cratis.Screenplay` | `4.31.0` | PDL parser, validator, diagnostics, semantic binder |
| `Cratis.Screenplay` | main `fd18129` | Inline event extraction and context identity guidance |
| `Cratis.Screenplay` | `4.66.0` (`c89198b`) | Reducer-body binding (`Semantics/`); no PDL syntax change affects the examples |

The update follows `commands.md`, `events.md` and `mcp/authoring-tools.md` at
that main commit (after v4.52.0); no PDL example changes in this update.

Originally checked against the Screenplay repository at tag `v4.31.0` (commit `355dffb`; the
projection grammar was re-read at v4.139.0 for headers and dynamic keys):
`Documentation/screenplay/projections/` (including `semantic-model.md`, `keys.md`
and `variants.md`), `readmodels.md`, `diagnostics.md` and decisions 0001 and 0002.
Every example below compiles with that version's compiler. Reverify before
claiming another version behaves the same.

**Chronicle decides what a projection means** (Screenplay decision 0001).
Screenplay owns the grammar; which instance an event affects, what a join may
create and what a removal removes follow Chronicle's lowering. Syntax that
compiles can still mean something other than it seems: read the warnings.

## The shape

Excerpt: the concepts and the three events (each carrying `invoiceId`) are
declared elsewhere in the model.

```screenplay excerpt
readmodel InvoiceDetailsReadModel
  invoiceId     InvoiceId
  invoiceNumber InvoiceNumber
  status        InvoiceStatus
  lastUpdatedAt DateTime

projection InvoiceDetails => InvoiceDetailsReadModel
  every
    lastUpdatedAt = $eventContext.occurred
    exclude children
  from InvoiceRegistered key invoiceId
    status = "draft"
  from InvoiceSent key invoiceId
    status = "sent"
  remove with InvoiceCancelled key invoiceId
```

**The arrow always points the same way.** A read model never declares what builds
it; the projection or reducer points at it with `=>`. **Exactly one thing may
build a read model** — two builders is error `PLAY0191`: *a projection or a
reducer builds it, and only one of them*. A slice may still declare several
projections, each building a **different** read model.

## Public-event targets and observer filters

In an explicitly outbound Translate slice, `projection P => PublicEvent` or
`reducer P => PublicEvent` targets the slice's one local public event (ESM v9).
The projection folds one instance per event source id; opaque reducers remain
Unsupported in reference execution. Read
[public-event folds](../cratis-screenplay-automations-and-translations/references/public-events-and-direction.md)
for publication rules and the `when append`/`then PublicEvent` specification.
A reducer can filter `from Source[.Stream]` (ESM v10); a projection cannot.
Read [observer filters](../cratis-screenplay-toolchain/references/production-routes-and-filters.md).

## Projection-level directives

`description "..."` (or a fenced text block) on the projection header (report-only `PLAY0270`; variants
and nested blocks take none), `no automap`, `sequence <name>`, `file <path>`. `sequence` is a realization
concern and does not bind to the executable model. A mapping target may end in a dynamic dictionary
key, `count eventCountByType.$eventContext.eventType.id` (one entry per distinct value); only
`$eventContext` resolves there, any other `$` text is a literal key with warning `PLAY0299`.

⚠️ **Do not write `key` on the projection.** It parses, but neither Chronicle nor
the executable model routes any event by it, and it is not a default for the
`from` blocks (warning `PLAY0381`). Key each `from` instead.

## Keys — eight ways, and the default

| Form | Example |
| --- | --- |
| Projection-level | `key invoiceId` — **routes nothing** (`PLAY0381`); do not use |
| From-level inline | `from InvoiceRegistered key invoiceId` |
| From-level block | indented `key invoiceId` inside the `from` |
| Per event, several events | `from A key idA, B key idB, C` |
| Composite | `key OrderKey` with indented `<property> = <expr>` parts |
| Literal (constant) | `key literal "site-stats"` — every event updates the **same** instance |
| Children identity | `children lines identified by lineNumber` |
| **Default** | **the event source id** — `from X` ≡ `from X key $eventSourceId` |

The key resolves from the event's inline key, then the `from` block's key, then
the event source id. A `from` without a key does **not** inherit another
`from`'s key: when one event is keyed by `invoiceId` and another is unkeyed, they
update the same instance only if the event source id happens to equal
`invoiceId`.

For same-source events, map the read-model identity from `$eventSourceId`
(or `$eventContext.eventSourceId`), not a duplicated payload field. That holds for
executable or design scope; in renderable scope (Stage) never map it from `$eventSourceId`:
the key of a scoped projection establishes it (`cratis-stage-rendering-and-sandbox`
`references/admission.md`). An event
declared by `produces event` is still a named slice-owned contract: projections
subscribe to it exactly like a standalone event. Verified extraction preserves
canonical ESM and identities, so it changes no projection contract. Removing a
payload property is different and requires consumer/persistence review.

A second key on one `from` is error `PLAY0060`. A template expression in a
composite key is error `PLAY0073`; an empty composite key is `PLAY0074`.

## `from` vs `every` vs `all`

| Block | Fires for | Use for |
| --- | --- | --- |
| `from <Event>` | that event type only | the actual mapping work |
| `every` | **only** the event types its level names in `from` and `join` blocks | common fields across your own events |
| `all` | **every event type in the system**; types no `from` names are keyed by their event source id | activity timestamps and counters per event source |

⚠️ `all` and `every` are not synonyms, and confusing them is the classic PDL
mistake. `every` is scoped to your level's `from` and `join` events — a joined
event also runs the `every` mappings, so a counter in `every` counts joined
events too. `all` is not scoped at all. Only a projection's own level can
subscribe to every event type: `all` inside `children` or `nested` behaves as
`every` (warning `PLAY0380`). An `all` block beside removals, `children` or
`nested` binds, but the reference execution plan refuses it.

`every` accepts `exclude children`, so child events do not bump the parent's
`lastUpdatedAt`. The `every` block **inside** a `children` block does not accept
it — it is already in a children context.

## Property mapping and AutoMap

⚠️ **AutoMap is on by default.** Matching property names are copied before your
explicit mappings run, and explicit mappings win. Turn it off with `no automap` at
**projection**, `every`/`all`, `join`, `children` or `nested` level — it
**cannot** be toggled inside an individual `from` block. An `automap`/`no automap`
under a joined event (`with …`) parses but Chronicle replaces it with the auto-map
of the level the join sits in (`PLAY0380`).

Mapping sources: a property path (`name`, `contactInfo.email`), a literal
(`true`, `"Pending"`, `42`, `null`), `$eventSourceId`, and
`$eventContext.<path>` from the event-context catalog (for example `occurred`,
`sequenceNumber`, `correlationId`, `causedBy.subject`; an unknown path warns with
`PLAY0295`). Templates (`` `${first} ${last}` ``) and `$causedBy.<…>` parse but
do not bind to the executable model; write `$eventContext.causedBy.subject`
instead of `$causedBy.subject`.

⚠️ Only the event source identity (`$eventSourceId`,
`$eventContext.eventSourceId`) runs in the reference runner. Any other
`$eventContext` path in a mapping or key — `occurred` in the shape above,
`sequenceNumber`, `causedBy.subject` — binds, but the reference execution plan
refuses it, so no specification in that model runs there. Use it when the target
needs it, and report those specifications as needing a target.

`clear <property>` removes a value and is equivalent to `= null`; prefer `clear`.
It takes a dotted path (`clear Owner.Note`) and escapes reserved names
(`clear @with`).

## Arithmetic

| Line | Effect |
| --- | --- |
| `count <prop>` | +1, expressing *counting occurrences* |
| `increment <prop>` | +1, expressing *modifying state* |
| `decrement <prop>` | −1 |
| `add <prop> by <expr>` | += expression |
| `subtract <prop> by <expr>` | −= expression |

`count` and `increment` behave identically; the difference is intent. The target
must be numeric.

## Joins

Excerpt, inside a `projection` whose read model has `customerId` and
`customerName`:

```screenplay excerpt
join customer on customerId
  with CustomerCreated
    customerName = name
  with CustomerUpdated
    customerName = name
```

`join <label> on <property>` (error `PLAY0063` otherwise), then one or more
`with <EventType>` blocks (error `PLAY0064` otherwise). **A join never creates an
instance**: a joined event updates every existing instance whose `on` property
equals the joined event's event source id. Chronicle discards the label, but
inside `children` and `nested` the compiler's completeness check (`PLAY0284`)
counts the label as the field the join fills, so name it after that field there.
Joins work at a projection's own level and inside `children`. Inside `nested`,
Chronicle's engine does not wire a join, a `children` block or a
`remove via join`; they bind, but the reference execution plan refuses them.
A join cannot declare its own key or trigger removal — that is
`remove via join on`.

## Children and nested

Excerpt, inside a `projection`:

```screenplay excerpt
children lineItems identified by lineNumber
  from InvoiceLineItemAdded key lineNumber
    parent invoiceId
    quantity = quantity
  remove with InvoiceLineItemRemoved key lineNumber
    parent invoiceId

nested billingContact
  from BillingContactSet
    email = email
  clear with BillingContactCleared
```

- **`children <collection> identified by <expr>`** — a collection with its own
  lifecycle. `parent <expr>` ties a child to its parent instance; it accepts an
  event property, `$eventContext.eventSourceId`, or a nested path. Children nest
  arbitrarily deep and may contain `join`, `remove`, `nested` and `every`.
- **`nested <property>`** — a single nullable object. It **must** contain at least
  one `from` (error `PLAY0067`), and `clear with <Event>` drops the whole object
  back to null. To clear one property instead, use a `clear <property>` mapping.

## Removal

Excerpt, inside a `projection`:

```screenplay excerpt
remove with InvoiceCancelled key invoiceId
remove via join on CustomerAccountClosed
```

`remove with <Event> [key <expr>]` removes the instance the event identifies.
`remove via join on <Event> [key <expr>]` removes instances reached through a
join. On a projection's own level, as in the second line above, it has no
verified meaning — Chronicle's engine wires it as a child removal — so it binds
but the reference execution plan refuses it. Use it inside `children`. Inside
`children`, both take an indented `parent <expr>` — and **only**
`parent`; anything else is error `PLAY0069`. Several removal conditions may
coexist.

## Variants — one identity, mutually exclusive read models

Excerpt: the events and the three read models (each with a keyed query) are
declared elsewhere. Every event about an issue is appended with the issue's
identifier as its event source.

```screenplay excerpt
projection WorkItem
  from TitleChanged
    title = title
  variant BacklogItem
    enters on IssueCreated
  variant DevelopmentItem
    enters on IssueStarted
  variant PullRequestItem
    enters on PullRequestCreated
```

Each variant is its own read model; only its `enters on` events create it.
Everything else, including a projection-level shared handler, is update-only: it
becomes a join on the variant's identifier, matched against the handler's key
(here the event source).

⚠️ **Mutual exclusion works through the event source identity.** Entering a
variant removes the entity from its siblings, and that removal is always keyed by
the entering event's source identity, whatever key the `enters on` line declares.
Keep the event source equal to the entity's identifier, as above. An
`enters on IssueStarted key issueId` whose `issueId` differs from the event
source creates the development item under `issueId` but removes the backlog item
keyed by the event source instead, so the issue stays in both variants.

Diagnostics: no `enters on` (`PLAY0382`), a shared mapping a variant's read
model lacks (`PLAY0383`), duplicate variant names (`PLAY0384`), an entering
event claimed twice (`PLAY0385`). The executable model binds variants the way Chronicle's client SDK
reclassifies them; Chronicle's hosted declaration language does not lower them
yet (Cratis/Chronicle#4109). Use variants for genuinely different shapes, not for
a single `status` field.

## When PDL is not enough — the reducer

Excerpt: the events and the `AccountBalance` read model are declared elsewhere.

````screenplay excerpt
reducer Balance => AccountBalance
  on AmountDeposited
    ```csharp
      return context.State is null
          ? new(context.Event.amount, 1)
          : context.State with { balance = context.State.balance + context.Event.amount };
      ```
  on AmountWithdrawn
    file Reducers/Withdrawn.cs
````

Inline code uses a tagged fence (` ```csharp `); a `csharp` line above a bare
fence still parses but warns (`PLAY0397`). Reach for a reducer only when the next
state depends on the current one — a running balance, a state machine.
`context.State` is **null for the first event** and is the only nullable member;
`Event`, `Key`, `Tenant`, `Occurred`, `SequenceNumber` and `IsFirst` are always
there.

What the executable model does with it (ESM v3):

- Every `on` rule needs a body, inline or `file`. Mixing bodied and body-less
  rules is `PLAY0398`; two rules for one event is `PLAY0399`; a reducer with no
  bodies is rejected with a hint to write a projection.
- The key is **always the event source id** — a reducer cannot route by an event
  property. State starts at null; returning null deletes the instance.
- The body is an opaque attachment identified by a content hash, never compiled
  or run by Screenplay. The reference runner cannot compute reducer state, so a
  specification that reads a reducer-built read model reports unsupported and
  never passes. A target must supply the transition. Stage (4.73.1) admits only pure reducer bodies
(Roslyn allowlist, `STAGE-ESM-019`/`-022`); an impure body is gap-fill code, and the model stays the contract.

**Prefer a projection where one will do.** A reducer is code, and code is the part
of a document a reader cannot check at a glance.

Read [pdl-grammar.md](references/pdl-grammar.md) when checking exact syntax, interpreting a projection diagnostic code, or needing a worked example.

## Verify

- [ ] Standalone `screenplay <model> --warnaserror` (4.68.0) reports zero errors and zero
      warnings; or `cratis screenplay validate --warnings-as-errors` on the model folder
      (3.28.2 bundles the same Screenplay 4.66.0; before 3.28.2 bundled 4.60.1, ESM v5
      or lower). Name which tool produced the result. If it reports diagnostics, correct the model and rerun the same command; report unresolved diagnostics as blockers rather than a passing verification.
- [ ] Each read model has **exactly one** builder.
- [ ] No projection-level `key`; every `from` that must address the same
      instance is keyed on the same identity.
- [ ] Every read-model property traces back to an event that carries it.
- [ ] `every` was intended where `every` is written — not `all`.
- [ ] AutoMap's default-on behavior is intended, or `no automap` is declared at the
      right level.
- [ ] Every `children` block's `from` declares `parent`.
- [ ] Every `nested` block contains at least one `from`.
- [ ] No `$causedBy`, template or `sequence` where the model must run.
- [ ] Where specifications must run in the reference runner: no `$eventContext`
      path other than `eventSourceId`, no projection-level `remove via join`, no
      `all` beside removals, `children` or `nested`, and no `join`, `children` or
      `remove via join` inside `nested`.
- [ ] Variant entering events use the event source identity as the entity's
      identifier.
- [ ] A reducer is present only because a projection genuinely could not express it,
      and every rule has a body in a tagged fence or a `file`.

Read `cratis-screenplay-toolchain` (`references/versions.md`) when selecting a tool/version or checking tool capabilities and the executable and renderable subsets. Where a construct sits in the
method: `cratis-screenplay-modeling-lifecycle` and `cratis-screenplay-slice-design`.

## Route near misses

- Deciding which read models exist at all: `cratis-screenplay-event-modeling`.
- Read-model shape, queries and screens: `cratis-screenplay-read-surface`.
- Specifying projection behavior: `cratis-screenplay-specifications`.
- Hand-written C# Chronicle projections: `cratis-chronicle-projection`.
- Hand-written C# Chronicle reducers: `cratis-chronicle-reducer`.
