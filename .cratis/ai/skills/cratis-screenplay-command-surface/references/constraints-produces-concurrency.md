<!-- cratis-ai-managed: skills/cratis-screenplay-command-surface/references/constraints-produces-concurrency.md -->
# Constraints, produces mappings and concurrency

## Contents

- `constraint` — uniqueness at append time
- Refuse a command on an unknown id
- `concurrency`
- `produces`

This reference holds the detailed forms for append-time `constraint` uniqueness, `produces` mapping sources and conditions, and the `concurrency` block.

## `constraint` — uniqueness at append time

Chronicle's constraints enforce **uniqueness only**. Excerpt: the events are
declared in the same model.

```screenplay excerpt
constraint UniqueInvoiceNumber
  unique invoiceNumber on InvoiceRegistered
  released by InvoiceCancelled
  ignore casing
  message "That invoice number is already in use"

constraint OneRegistrationPerInvoice
  unique event InvoiceRegistered
```

- `unique <p>[, <p>…] on <Event>` — a value (or composite value, in order) held by
  one event source is unavailable to every other. Repeat the line for other
  events sharing the claim. The same event source may re-claim its own value; a
  null value claims nothing. The property must be declared directly on the event
  (`PLAY0391`).
- `unique event <Event>` — the event occurs at most once per event source.
- `released by <Event>` (repeatable) frees the claim; `ignore casing` applies to
  property rules only (`PLAY0393`); `message` replaces the default violation text —
  never put the colliding value in it.
- **The name is the identity.** It is unique across the application (`PLAY0392`),
  and renaming a constraint starts a new, empty index.
- Chronicle updates the uniqueness index after the append commits; do not describe
  it as an atomic index-and-append guarantee.

⚠️ **Do not use `constraint … file <Path>` for other rules.** It can only name a
hand-written Chronicle `IConstraint`, which can only declare uniqueness; it warns
(`PLAY0396`) and the executable model rejects it. A state transition rule
belongs in `validate`/`require`. Use a constraint when two concurrent appends must
not both win — a `validate` rule cannot do that. Stage renders only single-text-property and
`unique event` constraints (`STAGE-ESM-014`; `cratis-stage-rendering-and-sandbox` `references/admission.md`).

## Refuse a command on an unknown id

Without a guard, a command naming an id that has no state is accepted and appends its event. Guard
existence with a protected read: after authorization and property rules, and before any `require`
or mapping runs, it rejects with `<ReadModel> '<key>' does not exist.` The preamble goes in the root document, the read model's key property is
named `id` (`STAGE-ESM-044` otherwise, found by `cratis render --check`), and the view needs a
`remove with` event. Complete model; it validates with `--executable`, renders with `--check`,
and the specification passes in `screenplay test`:

```screenplay
domain Tasks
command reads protected
module Work
  feature Closing
    slice StateView TaskStatus
      event TaskOpened
        title String
      event TaskDeleted
      readmodel Task
        id String key
        title String
      projection TaskProjection => Task
        from TaskOpened
        remove with TaskDeleted
    slice StateChange CloseTask
      event TaskClosed
        reason String
      command CloseTask
        taskId String identifier
        reads Task by taskId
        produces TaskClosed
          for taskId
          reason = "done"
      specification ClosingAnUnknownTask
        when CloseTask
          taskId = "missing"
        then error "Task 'missing' does not exist."
```

Admission, races and refused read shapes: [protected reads](protected-reads.md). A `validate` rule or a
screen guard cannot check existence, and a `unique` constraint only guards duplicates.

## `concurrency`

Five dimensions, each at most once, and at most one `concurrency` block per
command. It mirrors Chronicle's `ConcurrencyScope` for a target implementation;
the executable model does not bind it (`PLAY0271`), and it does not protect a
`reads` decision.

| Dimension | Scopes the check to |
| --- | --- |
| `eventSource` | the command's own event source id |
| `sourceType <Name>` | an event source type |
| `streamType <Name>` | an event stream type |
| `streamId <Name>` | an event stream id |
| `events <A>, <B>` | the listed event types |

An empty block or an unknown dimension is an error. Omitting `concurrency` does
not mean unchecked appends: Chronicle's default optimistic concurrency applies
to the routed scope. This does not make command `reads` protected.

## `produces`

- **Mapping sources** that bind to the executable model: a command property
  (`= invoiceNumber`), a literal (`= "draft"`, `= 0`), `$context.occurred`, and
  the caller's audit identity (`$identity.id`/`.name`/`.userName`, also supported
  as `$context.identity.id`/`.name`/`.userName`, the same values as `$context.causedBy.*`). The last two select ESM v2.
  `$context.tenant`, claims, roles, causation, `$env.` values, templates and
  computed expressions parse but block binding (`PLAY0268`).
- **`for <identifier>`** on an indented line names the event source the event is
  appended to. At most one per `produces`. To bind, it must name the command's
  `identifier` property (`PLAY0273` otherwise; a `generated identifier` command, ESM v7, may name another required scalar property). From ESM v30 (decision 0098)
  a command may append to several event sources: each `produces` may name any required scalar command property, and the events of one accepted command are appended
  together. Optional and collection properties still cannot be destinations. A model using it selects v30; state each destination with `for` on the matching `then` event.
- **`tag`** lines apply literal tags to this append. Tags also exist on the
  `event` declaration, where they apply to every append of that type.
- **Several unconditional `produces` blocks** are allowed — that is co-production,
  and it is what an automation is *not*.
- **`produces when <condition>`** takes the event name on the next indented line.
  Conditions over command properties and constants bind; each is evaluated
  independently, and when all are false the command is accepted with no events.
  Compare an enumeration with a member bare (`status == sent`) or quoted
  (`status == "sent"`); the bare form binds, so quote it when a model
  must bind on an older version.
  Excerpt:

```screenplay excerpt
produces when isProForma == true
  ProFormaInvoiceIssued
    for invoiceId
```
