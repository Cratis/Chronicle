---
name: cratis-screenplay-projections
description: Use when declaring Screenplay event-to-state mappings, a graph projection onto nodes and edges, or diagnosing a .play projection's keys, joins, children or lifecycle variants. Not for hand-written C# Chronicle projections or read-model query contracts.
license: MIT
---
<!-- cratis-ai-managed: skills/cratis-screenplay-projections/SKILL.md -->

# Projections — the Screenplay PDL

Read [preferred syntax](../cratis-screenplay-toolchain/references/preferred-syntax.md) when writing or reviewing `.play`.

A `projection` folds events into a read model. Its body is the **Projection
Declaration Language**, an embedded sub-grammar with its own parser — not free
text. This is half of every event model, and the half most easily got wrong.

## Locate the model

The folder holding the project's existing `.play` files is the model; code-only
`Source/` or `src/` does not qualify. Otherwise use repository-root `Screenplay/`,
never `.cratis/`. Read [discovery/version gates](../cratis-screenplay-event-modeling/references/file-layout.md#locate-the-model)
and do not invent another location.

`cratis ai install` manages `.cratis/ai/`, never model files. Never hand-copy
Screenplay source between repositories; the `.play` source is the single flow model.

## Product sources

Screenplay `Documentation/screenplay/projections/` covers grammar and semantics,
`readmodels.md` targets and `diagnostics.md` errors.

**Chronicle decides what a projection means.** Screenplay owns the grammar; which instance an
event affects, what a join may create and what a removal removes follow Chronicle's lowering.
Syntax that compiles can still mean something else: read the warnings.

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

**A builder never names a sink.** A projection or reducer writes to its read model's sink, and a
graph projection to its graph's sink; only the `readmodel` or `graph` body carries `sink <Name>`
(read surface [sinks](../cratis-screenplay-read-surface/references/sinks.md)).

## Public-event targets and observer filters

In an explicitly outbound Translate slice, `projection P => PublicEvent` or
`reducer P => PublicEvent` targets the slice's one local public event (ESM v9).
Read [public-event folds](../cratis-screenplay-automations-and-translations/references/public-events-and-direction.md)
for publication rules and specifications. A reducer can filter `from Source[.Stream]`
(ESM v10); a projection cannot: [observer filters](../cratis-screenplay-toolchain/references/production-routes-and-filters.md).

## Projection-level directives

Read [projection-level directives](references/pdl-grammar.md#projection-level-directives)
when using descriptions, `sequence`, `file` or dynamic dictionary-key targets.

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
| Flat composite parts | untyped `key` block naming every key part (Requires 4.204.0; ESM v27) |
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
`references/admission.md`). An event declared by `produces event` is a named
slice-owned contract: projections subscribe to it like a standalone event.
Removing a payload property needs consumer/persistence review.

Key errors (`PLAY0060`, `PLAY0073`, `PLAY0074`, `PLAY0661`/`PLAY0662`, `PLAY0268`) are in
[key diagnostics](references/mapping-sources.md#key-diagnostics).

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

⚠️ A projection that targets a public event and combines `from` with `every` over different
producible event types is refused as `InvalidSemanticContract` (no `PLAY` code). Read
[outbound `every`](references/outbound-every.md) when writing one.

## One event per level

One event type is used once per projection level (`PLAY0801`): two `from` blocks
cannot update two rows at that level, even with different keys. Combine mappings
for one row; model a genuine second domain fact as its own event. Never invent
events only to satisfy the read side. Joins use a separate set; each children,
nested and variant block starts fresh sets. See [event scope](references/pdl-grammar.md#event-scope).

## Property mapping and AutoMap

The order of a read model's properties is not a projection input; only a C# host's opt-in ESM 37.1
metadata records it ([property order](../cratis-screenplay-toolchain/references/read-model-property-order.md)).

⚠️ **AutoMap is on by default.** Matching property names are copied before your
explicit mappings run, and explicit mappings win. Turn it off with `no automap` at
**projection**, `every`/`all`, `join`, `children` or `nested` level — it
**cannot** be toggled inside an individual `from` block. An `automap`/`no automap`
under a joined event (`with …`) decides auto-mapping for that event alone;
without one, the joined event follows its level's auto-map.

Requires Screenplay 4.212.4 (`screenplay --version`) for joined-event auto-map
overrides in executable binding; older binders replace the setting with the level's.

Mapping sources are a property path, a literal, `$eventSourceId` and `$eventContext.<path>`.
`$eventContext.occurred` in a property assignment of `from`, `every` (including `all`) and
`children` runs in the reference runner as UTC text. Keys, joins, arithmetic, `every` with joins,
nested-object scopes and other paths (`sequenceNumber`, `correlationId`) still bind but their
specifications cannot run there. A missing occurrence fails the projection with no partial state:
supply it with `given clock` or an explicit fact timestamp.
`clear <property>` removes a value; prefer it to `= null`. Read
[mapping sources](references/mapping-sources.md) for the catalog, `PLAY0295` and `clear` paths.

## Arithmetic

`count`/`increment` add one, `decrement` subtracts one, `add`/`subtract <prop> by <expr>` change by an expression; the target must be numeric. Read [references/joins.md](references/joins.md) for the full table.

## Joins

`join <label> on <property>` followed by `with <EventType>` blocks; **A join never creates an instance** (`PLAY0063`, `PLAY0064`). Read [references/joins.md](references/joins.md) when writing a join, including inside `children` or `nested`.

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
- **Ordering, `set once` and dictionary keys**: a `children` block may carry `insert first` or `sort by <property>`; `set once` keeps a first value; a target may end in `.$eventContent.<path>`. See [dictionaries and ordering](references/dictionaries-and-ordering.md).

## Removal

Excerpt, inside a `projection`:

```screenplay excerpt
remove with InvoiceCancelled key invoiceId
remove via join on CustomerAccountClosed
```

`remove with <Event> [key <expr>]` removes the instance the event identifies.
`remove via join on <Event> [key <expr>]` removes instances reached through a
join; on a projection's own level (second line above) it binds but the reference
execution plan refuses it, so use it inside `children`. There both forms take an
indented `parent <expr>` and **only** `parent` (`PLAY0069`).

## Variants — one identity, mutually exclusive read models

Each `variant` is its own read model created only by its `enters on` events, and mutual exclusion works through the event source identity: keep the event source equal to the entity identifier or the issue stays in both variants. Read [references/variants.md](references/variants.md) when modeling an entity with exclusive lifecycle shapes or fixing `PLAY0382`–`PLAY0385`.

## Graph targets — `projection X => graph G`

A `graph` declares node labels (one `key` each) and edge types; `projection X => graph G`
builds it with `node` and `edge` blocks. Nothing writes to a graph except events.
Requires Screenplay 5.7.0 (`screenplay --version`).

Choose a graph for traversals and a document read model for lookups by key. Design by
**reading the graph backwards**: write each question as a path, make every hop an edge type
and every stop a node label, then name the event that creates and removes each. Read
[references/graphs.md](references/graphs.md) for syntax, operations, a graph in a named sink
(`sink People for graphs`), graph queries (`match`/`return`), `foreach` over an event's list and `PLAY0862`–`PLAY0886`.

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
fence still parses but warns (`PLAY0397`). Built-in inline languages are `csharp`,
`typescript`, `react`, `html`, `sql` and `java`; the body is opaque and travels unchanged
to the target, and the Cratis render target refuses `java` bodies.
Requires Screenplay 4.244.0 (`screenplay --version`) for that refusal. Reach for a reducer only when the next
state depends on the current one — a running balance, a state machine.
`context.State` is **null for the first event** and is the only nullable member;
`Event`, `Key`, `Tenant`, `Occurred`, `SequenceNumber` and `IsFirst` are always
there.

`context.Key` is text; read it typed with `context.KeyAs<T>()` (Requires Screenplay 5.2.0).

Key facts: every `on` rule needs a body, inline or `file` (`PLAY0398`/`PLAY0399`); the key is
**always the event source id**, and a query `by` another identity warns `PLAY0690`
([reducer identity](references/reducer-identity.md)); the reference runner cannot compute
reducer state, so a specification reading a reducer-built read model never passes. Read
[reducer details](references/reducers.md) for `KeyAs<T>()`, the executable-model rules and Stage
admission.

**Prefer a projection where one will do.** A reducer is code, and code is the part
of a document a reader cannot check at a glance.

Read [pdl-grammar.md](references/pdl-grammar.md) when checking exact syntax, interpreting a projection diagnostic code, or needing a worked example.

## Secret concepts and read models

A `secret` concept copied into a read-model property (explicit mapping, AutoMap or reducer) raises warning
`PLAY1142`. Read [secret concepts](references/secret-concepts.md) before projecting a protected event value.

## `STAGE-ESM-017` passes `validate` and fails at `render`

`validate --executable` binds only Screenplay's model; Stage's admission runs only in `render`.
Read [Stage admission](references/stage-admission.md) when `cratis render` refuses a projection
with `STAGE-ESM-017` after `validate` exits 0.

## Verify

- [ ] Standalone `screenplay <model> --warnaserror` or `cratis screenplay validate --warnings-as-errors`
      reports zero errors and warnings (an older bundled compiler can stop at a lower ESM version).
      Name the tool; fix and rerun, and report unresolved diagnostics as blockers.
- [ ] Each read model has **exactly one** builder.
- [ ] No projection-level `key`; every `from` that must address the same
      instance is keyed on the same identity.
- [ ] Every read-model property traces back to an event that carries it.
- [ ] `every` was intended where `every` is written — not `all`.
- [ ] AutoMap's default-on behavior is intended, or `no automap` is declared at the
      right level.
- [ ] Every `children` block's `from` declares `parent`.
- [ ] Every `nested` block contains at least one `from`.
- [ ] Where the model must run: no `$causedBy`, template or `sequence`; no `$eventContext`
      path other than `eventSourceId` or an `occurred` property assignment, no projection-level `remove via join`, no
      `all` beside removals, `children` or `nested`, and no `join`, `children` or
      `remove via join` inside `nested`.
- [ ] Variant entering events use the event source identity as the entity's
      identifier.
- [ ] A reducer is present only because a projection genuinely could not express it,
      and every rule has a body in a tagged fence or a `file`.

Read `cratis-screenplay-toolchain` when selecting a tool/version or checking tool capabilities and the executable and renderable subsets. Where a construct sits in the
method: `cratis-screenplay-modeling-lifecycle` and `cratis-screenplay-slice-design`.

## Route near misses

- Deciding which read models exist at all: `cratis-screenplay-event-modeling`.
- Read-model shape, queries and screens: `cratis-screenplay-read-surface`.
- Specifying projection behavior, including `given graph` and `then graph`: `cratis-screenplay-specifications`.
- Reading a graph from Arc code: `cratis-arc-neo4j`.
- Hand-written C# Chronicle projections: `cratis-chronicle-projection`.
- Hand-written C# Chronicle reducers: `cratis-chronicle-reducer`.
