<!-- cratis-ai-managed: skills/cratis-screenplay-event-modeling/references/nine-steps.md -->
# The nine-step workflow design process

## Contents

- Step 1 — Identify the user goal
- Step 2 — Brainstorm events
- Step 3 — Order events chronologically
- Step 4 — Create wireframes
- Step 5 — Identify commands
- Step 6 — Design read models
- Step 7 — Find automations
- Step 8 — Map external integrations
- Step 9 — Decompose into vertical slices
- Facilitation questions quick reference

Follow all nine steps for each workflow. Do not skip steps, do not combine them,
and do not start workflow 2 while workflow 1 is incomplete — **discovery is
design**. Each step has a defined Screenplay output; produce it.

## Step 1 — Identify the user goal

Phase skill: `cratis-screenplay-discovery`.

Ask until the goal is unambiguous: *"What exactly is the user trying to
accomplish? What does success look like? What would make this fail?"*

**Output:** the `feature` name and its `description`.

```screenplay
module Invoicing
  feature InvoiceManagement
    description "Registering and managing the lifecycle of invoices"
```

`description` is optional on a module, feature, slice, persona or command;
at most one, anywhere in its body. Use a fenced block tagged ` ```text ` when one line
is not enough (a bare fence warns with `PLAY0397`).

## Step 2 — Brainstorm events

Phase skill: `cratis-screenplay-discovery`.

Sticky-note style, no ordering yet. *"What facts need recording? What happened
that we care about? What would an auditor want to know?"* Keep asking *"what
else?"*

Past tense, business language, facts: `InvoiceRegistered`, `PaymentReceived`.

**Domain facts vs runtime context.** Events must be true regardless of which
machine replays them. A working directory, PID or hostname is runtime context and
does not belong in an event.

**Output:** a list of `event` names. Shapes come later.

## Step 3 — Order events chronologically

Phase skill: `cratis-screenplay-discovery`.

Arrange into the timeline — the plot. *"What happens first? And then what
happens?"* Identify the happy path **and** the alternative and error paths.

**Output:** the authored declaration/import order for the board, with causes
and branches documented. Screenplay 4.125.0 MCP layouts preserve this presentation
order with imports; an import-less `PlayFileWriter.Expand` layout uses path order.
Timeline position does not impose execution order or change identities.

## Step 4 — Create wireframes

Phase skill: `cratis-screenplay-slice-design` (screens: `cratis-screenplay-ui-composition`).

These need not be the real UI. Their purpose is a complete accounting of what a
user can **see** and what they can **do** at each interaction point.

```text
+-------------------------------+
|  Register Invoice             |
+-------------------------------+
|  Customer:  [dropdown]        |
|  Lines:     [list]            |
|  Total:     $XX.XX            |
|                               |
|  [Register]                   |
+-------------------------------+
```

Every field on the wireframe is one of two kinds. A field the person **enters** starts on the
screen and becomes a command input. A field the screen **shows** names the read model it comes
from, even one not designed yet (step 6 groups the views by these names), and that read model
traces it to an event. A shown field never takes its value from the command the screen submits.
If you cannot say which kind a field is, or where it comes from, something is missing.

**Concurrency check:** *"Can there be more than one of these in progress at the
same time?"* If yes, the wireframe shows a list or table, not a single-item view.

**Output:** the `screen` declarations, at Level 1 (intent) for now —
`data <ReadModel> via query <Query>` plus `action <Command>`. When the wireframe
says what a click, submit or screen entry *does* (confirm, refresh, navigate,
open a dialog), capture it as an `on` block or a named `behavior` attached with
`uses` — see `cratis-screenplay-ui-composition`. An `action` alone does not say
what happens afterwards.

## Step 5 — Identify commands

Phase skill: `cratis-screenplay-slice-design` (identity and consistency: `cratis-screenplay-streams-and-consistency`).

For each event: *"What triggered this? Who issued that command? What information
did they provide? Under what circumstances would this NOT happen?"*

Commands are imperative and present tense: `RegisterInvoice`, `ProcessPayment`.
**Commands can fail; events cannot.**

**Output:** the `command` declaration with its properties, its `identifier`
property (at most one; leave it out only when the runtime should allocate a new
identity), `authorize`, and `produces`. Excerpt: the complete document is
[invoicing-example.md](invoicing-example.md). Give the event a description of the
fact, and fenced Markdown `documentation` when its meaning needs elaboration.
Prefer `produces event` with typed mappings for an event owned by one command;
shared events stay standalone with explicit command destinations.

```screenplay excerpt parent=references/invoicing-example.md#1
// Parent: references/invoicing-example.md (complete document), slice RegisterInvoice
slice StateChange RegisterInvoice
  command RegisterInvoice
    invoiceId     InvoiceId identifier
    invoiceNumber InvoiceNumber
    authorize CanManageInvoice
    validate
      invoiceNumber not empty  message "Invoice number is required"
    produces event InvoiceRegistered
      invoiceNumber InvoiceNumber = invoiceNumber
      registeredAt DateTime = $context.occurred
```

The "under what circumstances would this NOT happen" answers become `validate`
rules, `authorize` policies, `constraint` declarations, and the rejection
specifications.

## Step 6 — Design read models

Phase skill: `cratis-screenplay-slice-design`.

Read models exist to support what wireframes display and what automations need.
For each actor at each point, and for each automation: *"What does this person
need to see? What information do they need to decide? What does this automation
need to determine its next action?"*

Verify **every** field traces back to an event:

```text
InvoiceListReadModel:
  invoiceId      <- InvoiceRegistered event context ($eventSourceId)
  invoiceNumber  <- InvoiceRegistered.invoiceNumber
  status         <- InvoiceRegistered, InvoiceSent, InvoicePaid
```

**Concurrency check per field:** if the domain supports concurrent instances, use
a collection type, not a singular value.

**Output:** the `readmodel` shape and the one `projection` or `reducer` that
builds it. **Exactly one thing may build a read model** — two builders is a
compile error (`PLAY0191`).

Route each `from` deliberately. When the instance is the event source, omit the
key and map its identity from `$eventSourceId` (design or executable scope; a renderable
projection leaves it unmapped, see `cratis-screenplay-slice-design`); do not duplicate it in payload.
A `from` without a key never inherits another `from`'s key, and a projection-level
`key` routes nothing (`PLAY0381`). Excerpt: the complete document is
[invoicing-example.md](invoicing-example.md); both events target the invoice's event source.

```screenplay excerpt parent=references/invoicing-example.md#1
// Parent: references/invoicing-example.md (complete document), slice InvoiceList
slice StateView InvoiceList
  readmodel InvoiceListReadModel
    invoiceId     InvoiceId
    invoiceNumber InvoiceNumber
    status        InvoiceStatus
  projection InvoiceList => InvoiceListReadModel
    from InvoiceRegistered
      invoiceId = $eventSourceId
      status = "draft"
    from InvoiceSent
      invoiceId = $eventSourceId
      status = "sent"
  query ListInvoices => InvoiceListReadModel[]
```

**Do not model infrastructure preconditions as read models.** "Does the directory
exist?" is not domain state.

## Step 7 — Find automations

Phase skill: `cratis-screenplay-automations-and-translations`.

*"Does anything happen automatically after this event? What business rules trigger
other processes? Does the system need to check anything before acting?"*

**All four components are required for a true automation:** a triggering
occurrence, state it consults, conditional logic, and a resulting command or
event. **Test:** *"Can this automatic response ever be skipped or vary based on
system state?"* If no, it is co-production — one `StateChange` slice with several
`produces` blocks, not an `Automation` slice.

**Output:** the `reaction`, in an `Automation` slice. It binds on the standalone
`screenplay` and the `cratis` bundle (ESM v6; the 3.27.1 bundle rejected the slice at binding) and
Stage 4.89.1 renders supported event-triggered Automation; refused scope stays gap-fill (versions:
`cratis-screenplay-toolchain`). A reaction that `produces` and `invokes` stays one reaction: Stage
refuses it (`STAGE-ESM-037`) but Arc 22.72.0 and later run it as one reactor (events first, then
commands; no cross-effect transaction; the scaffold includes the handler, but retries can duplicate events), so gap-fill it whole, never split it. When the automation decides from a view, declare it under
the trigger with `reads` (see `cratis-screenplay-captures-and-reactions`).

The example below is a complete document: a Monday-morning clock occurrence invokes a command
that records the digest. The occurrence is time, not an event, so the reaction fires whether
or not any invoice changed; a reaction on the event that created an invoice cannot decide
"is it overdue yet", because nothing happens at the due date. A clock occurrence carries no
event source, so the invoked command binds its own identifier (`weekly`).

**What the model does and does not do.** The view stores the deadline (`dueDate`), never an
"overdue" flag: overdue is a comparison with the clock, not a fact to materialize. The
reaction's `reads UnpaidInvoice` is report-only metadata (information `PLAY0270`, checked);
it does not make the model consult the view. Binding therefore admits only a scheduled
invocation that records a timestamp. Choosing which invoices are overdue (due date before the
clock instant) is a target-side decision: record it as gap-fill with the model as the
contract (`cratis-screenplay-render-and-gap-fill`), and say so in the slice `documentation`. Do not claim the specification proves the overdue decision.

```screenplay
domain Acme.Invoicing

concept InvoiceId : Uuid
concept InvoiceNumber : String
concept DigestPeriod : String

module Invoicing
  feature Collections
    slice StateView UnpaidInvoices
      description "Unpaid invoices with their due dates; membership means 'still unpaid'. The deadline is stored, never an 'overdue' flag: overdue is decided against the clock"
      readmodel UnpaidInvoice
        invoiceId     InvoiceId
        invoiceNumber InvoiceNumber
        dueDate       DateTime
      query UnpaidInvoiceById => UnpaidInvoice optional
        by invoiceId InvoiceId
      projection UnpaidInvoices => UnpaidInvoice
        from InvoiceRegistered
          invoiceId = $eventSourceId
          invoiceNumber = invoiceNumber
          dueDate = dueDate
        remove with InvoicePaid
      event InvoiceRegistered
        invoiceNumber InvoiceNumber
        dueDate       DateTime
      event InvoicePaid

    slice StateChange IssueCollectionsDigest
      description "Records that the weekly collections digest was issued"
      command IssueCollectionsDigest
        period DigestPeriod identifier
        produces event CollectionsDigestIssued
          issuedAt DateTime = $context.occurred
      specification IssuingTheDigest
        given clock "2026-10-05T07:30:00Z"
        when IssueCollectionsDigest
          period = "weekly"
        then CollectionsDigestIssued
          for "weekly"
          issuedAt = "2026-10-05T07:30:00Z"

    slice Automation WeeklyCollectionsDigest
      reaction DigestIssuer
        description "Every Monday morning, issue the collections digest. The view read documents intent only: choosing which invoices are overdue (due date before the clock instant) is NOT enforced or executed by the model; the target realization must do it. Ends at CollectionsDigestIssued"
        at 07:30 on Monday
          reads UnpaidInvoice
          invokes IssueCollectionsDigest
            period = "weekly"
      specification IssuingOnMonday
        given clock "2026-10-05T07:00:00Z"
        when clock "2026-10-05T07:30:00Z"
        then CollectionsDigestIssued
          for "weekly"
          issuedAt = "2026-10-05T07:30:00Z"
```

At the example's verified Screenplay version, iterating the overdue items needs code (a clock trigger reads the whole view and takes
no `by`); see `cratis-screenplay-automations-and-translations`. A reaction that `produces`
directly while it `reads` fails binding (`PLAY0268`): decide in a command instead.

`produces` and `invokes` are indented **inside** the trigger; only `description`
and `where` sit at reaction level. Outdenting an effect gives `PLAY0137`.

`produces` requests a direct fact append, still subject to append-time constraints.
`invokes` asks for a command, so the command's authorization, validation and requirements
run as well. The words are different on purpose.

Every automation needs a **termination condition**. Watch for infinite loops.

## Step 8 — Map external integrations

Phase skill: `cratis-screenplay-automations-and-translations`.

*"Does this workflow receive data from outside? Does it send data outside?"* Note
names and purposes only — no APIs, webhooks or protocols yet.

**Ask:** *"Is this integration specific to THIS workflow, or would every workflow
need it?"* If every workflow needs it, it is cross-cutting infrastructure, **not**
a `Translate` slice.

**Output:** the `capture` in a `Translate` slice, and any `trigger` declaration
for a name only an integration knows. A `trigger` is an application signal a
reaction consumes; it is not how a button click is modeled (that is a UI `on`
clause).

```screenplay
trigger BuildFinished
  repository String
  outcome    String
```

A `trigger` declares that the name exists and what an occurrence hands the
reaction — deliberately **not** what makes one occur.

## Step 9 — Decompose into vertical slices

Phase skill: `cratis-screenplay-slice-design`; then specifications in `cratis-screenplay-scenario-coverage` and review in `cratis-screenplay-model-review`.

List every slice grouped by type to review and count them. The declarations themselves stay in
story order (`cratis-screenplay-slice-design` `references/slicing.md`). A good slice is a complete interaction,
independently valuable, testable in isolation, small enough for 1–2 days.

Bad slices: *"Set up database"* (technical, no user value), *"Implement invoicing"*
(too broad), *"Create Invoice table"* (implementation detail).

**Slice independence.** Slices sharing an event schema are **independent** —
connected by the event contract, not by execution order. A `StateChange` slice is
specified by asserting on produced events; a `StateView` slice is specified with
synthetic `given` events, or with `when append <Event>` to run one event through
constraints and projections. Neither needs the other implemented first. Do not
build artificial dependency chains. An authorized command's specifications state
the caller explicitly with `given caller` and assert refusals with `then denied`
(see `cratis-screenplay-specifications`).

**Output:** the complete `feature` → `slice` tree, ready for the specifications.

## Facilitation questions quick reference

| Topic | Questions |
| --- | --- |
| Domain discovery | What does the business do? Who are the actors? What are the major processes? What external systems exist? Which workflow is most critical? |
| Events | What facts need recording? What happened here? Would the business need to know this? |
| Timeline | What happens first? And then? Can these happen in parallel? |
| Commands | Who initiates this? User-triggered or automatic? What intent does this represent? |
| Read models | What does this actor need to see? What queries do users run? Can multiple instances be active at once? |
| Automations | Does anything happen automatically? What business rules apply? Can this response ever be skipped? |
| Edge cases | What if this fails? What if the user cancels? What if the external system is down? |
