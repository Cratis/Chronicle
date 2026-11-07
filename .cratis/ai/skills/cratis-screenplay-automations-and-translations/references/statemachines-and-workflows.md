<!-- cratis-ai-managed: skills/cratis-screenplay-automations-and-translations/references/statemachines-and-workflows.md -->
# State machines and workflows (ESM v36)

## Contents

- Which construct
- Complete example
- Specifications
- Diagnostics and targets

Requires Screenplay 5.1.0 or later (`screenplay --version`). Sources: Screenplay
`Documentation/screenplay/statemachines.md`, `workflows.md` and decision 0124.

## Which construct

| The domain says | Model |
| --- | --- |
| "When X happens, do Y", one step | An `Automation` slice with a reaction |
| Where one thing is in its lifecycle, and what moves it on | A `statemachine` that follows the thing's read model |
| A longer process: several commands, a choice, a wait for a fact or a person, a deadline, a fan-out | A `workflow` with one run per subject |

A state machine never decides on commands: it follows facts that already happened. Entering
a state may `send` a command or `start` a workflow; both may run again on redelivery, so the
command or workflow must be safe to repeat. A workflow is how a process waits, retries, asks
for approval and gives up; do not chain reactions across slices to imitate one.

Both are top-level declarations, siblings of `trigger` and `policy`. They name commands,
events, read models and personas declared in slices of any module. A model that declares one
selects ESM v36.

## Complete example

This model compiles with `--warnaserror` and its three specifications pass with
`screenplay test`:

```screenplay
concept InvoiceId : Uuid
persona FinanceController
module Billing
  feature Invoices
    slice StateChange SendInvoice
      command SendInvoice
        invoiceId InvoiceId identifier
        produces event InvoiceSent
    slice StateChange MarkOverdue
      command MarkOverdue
        invoiceId InvoiceId identifier
        produces event InvoiceMarkedOverdue
    slice StateChange PayInvoice
      command PayInvoice
        invoiceId InvoiceId identifier
        produces event InvoicePaid
    slice StateChange SendReminder
      command SendReminder
        invoiceId InvoiceId identifier
        produces event ReminderSent
    slice StateChange WriteOffInvoice
      command WriteOffInvoice
        invoiceId InvoiceId identifier
        produces event InvoiceWrittenOff
    slice StateView InvoiceStatus
      readmodel Invoice
        invoiceId InvoiceId key
        status String
      projection Invoices => Invoice
        from InvoiceSent
          invoiceId = $eventSourceId
          status = "sent"
        from InvoiceMarkedOverdue
          status = "overdue"
        from InvoicePaid
          status = "paid"
      query InvoiceById => Invoice optional
        by invoiceId InvoiceId

statemachine InvoiceLifecycle
  description "Where an invoice is, from sent to settled"
  follows Invoice
  state Sent initial
    on InvoiceMarkedOverdue -> Overdue
    on InvoicePaid -> Paid
  state Overdue
    entering start CollectOverdue
    on InvoicePaid -> Paid
    on InvoiceWrittenOff -> WrittenOff
  state Paid final
  state WrittenOff final

  specification GoingOverdueStartsCollection
    given InvoiceSent
      for "9c858901-8a57-4791-81fe-4c455b099bc9"
    when append InvoiceMarkedOverdue
      for "9c858901-8a57-4791-81fe-4c455b099bc9"
    then is Overdue
    then starts CollectOverdue

workflow CollectOverdue
  description "Reminds the customer, waits for payment and asks finance before writing off"
  for Invoice
  trigger when InvoiceLifecycle enters Overdue
    -> Remind
  action Remind sends SendReminder
    next -> AwaitPayment
  wait AwaitPayment for InvoicePaid expires after 14 days
    expired -> ApproveWriteOff
  approval ApproveWriteOff by FinanceController expires after 7 days
    approved -> WriteOff
  action WriteOff sends WriteOffInvoice

  specification AnUnpaidInvoiceIsWrittenOff
    given InvoiceSent
      for "9c858901-8a57-4791-81fe-4c455b099bc9"
    when append InvoiceMarkedOverdue
      for "9c858901-8a57-4791-81fe-4c455b099bc9"
    outcome AwaitPayment expired
    outcome ApproveWriteOff approved
    then sends SendReminder
    then sends WriteOffInvoice
    then completes

  specification TheRunWaitsForPayment
    given InvoiceSent
      for "9c858901-8a57-4791-81fe-4c455b099bc9"
    when append InvoiceMarkedOverdue
      for "9c858901-8a57-4791-81fe-4c455b099bc9"
    then sends SendReminder
    then waits at AwaitPayment
```

- `follows Invoice` / `for Invoice`: the read model whose key is one instance (one run per
  invoice). A transition condition (`on E when <condition> -> S`) reads that read model after
  the event; a condition over an optional property is refused at binding.
- Exactly one `initial` state; a `final` state has no transitions. An event takes the first
  transition on it, in declaration order across the whole machine, whose condition holds and
  whose target the current state allows. Anything else is ignored, not an error.
- Workflow nodes: `trigger` (`on <Event>`, `when <StateMachine> enters <State>`, `every`,
  `at`, `manual`), `action <Name> sends <Command> [attempts n] [timeout] [retry after]`,
  `decide <Name> when <condition>` with exactly two outlets, `wait` for an event or a
  duration, `approval <Name> by <Persona or Policy>`, `foreach <Name> over <collection>` and
  `join all|any|at least n`. Edges are `<outlet> -> <Node>` under their node.
- An unconnected negative outlet (`failed`, `rejected`, `expired`) fails the run; an
  unconnected positive outlet ends that branch. Give every event wait and approval an
  `expires after` (PLAY0849 warns otherwise).

## Specifications

| Construct | Lines |
| --- | --- |
| `statemachine` | `given <Event>`, `when append <Event>`, `then is <State>`, `then sends <Command>`, `then starts <Workflow>` (every command and workflow, in order) |
| `workflow` | `given <Event>`, `when append <Event>` or `when start <key>`, `outcome <Node> <outlet>`, `then sends <Command>` (every one, in order), `then completes`, `then fails at <Node>`, `then waits at <Node>` |

The reference evaluator steps the run deterministically. A wait or approval with no stated
`outcome` suspends the run (`then waits at`); an action defaults to `next`; a wait for a
duration resumes at once.

## Diagnostics and targets

- State machines: PLAY0834-PLAY0842 (missing or duplicate `initial` is PLAY0835, an
  unreachable state warns PLAY0836, an undeclared reference is PLAY0840).
- Workflows: PLAY0843-PLAY0851 (no trigger is PLAY0844, a bad outlet PLAY0847, a decision
  without exactly two outlets PLAY0848, an undeclared reference PLAY0851); PLAY0839 and
  PLAY0841 are shared.
- Stage refuses ESM v36 models with `STAGE-ESM-016`; every other model renders as
  before. The durable engine is Cratis.Orleans; its hosting and adapters are outside this
  reference.
