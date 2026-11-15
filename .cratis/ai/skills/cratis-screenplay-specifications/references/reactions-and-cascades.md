<!-- cratis-ai-managed: skills/cratis-screenplay-specifications/references/reactions-and-cascades.md -->
## Contents

- Reactions and cascades (ESM v6)
  - An automation states the commands it invokes (ESM v34)
  - The public event is stated in the publishing slice
  - Version skew and `PLAY0285`

## Reactions and cascades (ESM v6)

From ESM v6 every action sets reactions off: after a command, an append, a clock
tick, a trigger or a capture record, each new fact runs the reactions to its event,
and what those append or invoke runs more, until nothing is left.

A specification runs only the slice that declares it (Screenplay decision 0115, every
model and version from Screenplay 4.246.0). Only that slice's reactions, captures and
outbound publications run; another slice's never do, so their facts cannot appear in
`then`, read-model or query assertions. `then` events compare the new facts of the
specification's own slice. A chain across slices is proved link by link: each slice's
specification states its own link.

```screenplay expect PLAY0469
concept InvoiceId : Uuid
module Collections
  feature Invoices
    slice StateChange SendInvoice
      command SendInvoice
        invoiceId InvoiceId identifier
        produces InvoiceSent
          for invoiceId
          invoiceId = invoiceId
      event InvoiceSent
        invoiceId InvoiceId
      specification SendingAnInvoice
        when SendInvoice
          invoiceId = "9c858901-8a57-4791-81fe-4c455b099bc9"
        then InvoiceSent
          for "9c858901-8a57-4791-81fe-4c455b099bc9"
          invoiceId = "9c858901-8a57-4791-81fe-4c455b099bc9"
    slice Automation Reminders
      reaction ReminderScheduler
        description "Schedules a reminder when an invoice is sent"
        when InvoiceSent
          invoiceId
          produces ReminderScheduled
            for invoiceId
            scheduledAt = $context.occurred
      event ReminderScheduled
        scheduledAt DateTime
      specification SendingAnInvoiceSchedulesAReminder
        given clock "2026-10-02T09:00:00Z"
        when append InvoiceSent
          for "9c858901-8a57-4791-81fe-4c455b099bc9"
          invoiceId = "9c858901-8a57-4791-81fe-4c455b099bc9"
        then ReminderScheduled
          for "9c858901-8a57-4791-81fe-4c455b099bc9"
          scheduledAt = "2026-10-02T09:00:00Z"
```

- A specification whose action is a command goes in the slice that declares that
  command, here `SendInvoice`, not in the `Automation` slice that reacts: a command
  from another slice is `PLAY0273` "command is unresolved in its slice" at binding.
  It states the command's own events. The reminder is the `Reminders` slice's link,
  stated there with `when append InvoiceSent`.
- Stating another slice's event in a command specification is `PLAY0285` (the
  outcome is unreachable from the specification's slice) and fails at run time with
  "Expected N fact(s), got M". Move the expectation to the slice that produces it;
  never delete the reaction.
- A reaction invocation without a declared actor has no caller and a gated
  command denies. Reaction `runs as system role "<Role>"` supplies the
  authenticated system actor (ESM v10); `given caller` does not. Each command is
  atomic, not the cascade. Checked actor scenario and CLI/Stage limitations:
  captures-and-reactions `references/invocation-identity.md`.
- The clock is UTC and exact. An `every` or `at` occurrence fires once when it is
  due after `given clock` and at or before `when clock`; `when clock` needs
  `given clock`, so the same instant twice fires nothing.
- A reached reaction with a code body (`file` or inline) returns unsupported, as
  does a reaction that never settles. The reference stops at 1,000 new facts per
  scenario and 10,000 due occurrences per clock advance.
- A model that uses these forms selects ESM v6 (`schemaVersion: 6`). Stage
  renders supported event-triggered Automation, but not Translate,
  captures, non-event triggers or opaque bodies (`-024`), claim-reading/opaque `runs as` gates (`-034`), legacy filters (`-035`)
  or mixed effects (`-037`). Single-hop reaction append specs render; deeper appends and
  command specs cascading into reactions remain `-011`. Keep refused scope as gap-fill against this contract: `cratis-screenplay-render-and-gap-fill`.

### An automation states the commands it invokes (ESM v34)

When a slice's reaction invokes a command another slice owns, that command is decided
against the world, so the reaction's `on refused` branches behave as before, but its
facts belong to the other slice and are not kept. The automation states the hand-over
with `then invokes`, the command and the values it passes (the complete model is the
`Reminders` slice in captures-and-reactions `references/complete-examples.md`):

```screenplay excerpt
specification ClosingAPaidInvoice
  when trigger PaymentFileArrived
    invoiceId = "9c858901-8a57-4791-81fe-4c455b099bc9"
    amount = 120
  then invokes CloseInvoice
    invoiceId = "9c858901-8a57-4791-81fe-4c455b099bc9"
    reason = "paid"
```

- When a specification states at least one `then invokes`, the invocations the slice's
  reactions made must match them exactly in count and order (any order under
  `then events in any order`), comparing only the stated values. A refused invocation
  counts too. A specification that states none does not compare invocations.
- `then invokes` selects ESM v34. The Stage spec runner compares it; the Cratis
  renderer refuses models after v33 with `STAGE-ESM-016`.
- The invoked command's own events are proved by that command's slice, in its own
  `when <Command>` specification.

### The public event is stated in the publishing slice

An outbound Translate slice that publishes a public event from private events states it
in its own `when append <PrivateEvent>` specification. The command that produces the
private event states only its own events (decision 0115 replaces the earlier rule that
the command's specification listed the public event).

### Version skew and `PLAY0285`

`PLAY0285` (a specification's expected event contradicts every possible producer of its
`when` command) follows the reactions, invocations and publications declared in the
specification's own slice. From Screenplay 4.246.0 it refuses another slice's event as
unreachable, and that is a modeling error: move the expectation to the slice that
produces the event. A compiler older than ESM v6 does not trace reactions at all and
can report `PLAY0285` on a reaction in the specification's own slice; that is tool skew.
Validate with the standalone tool or a current `cratis`, record which tool gave which
verdict, and see `cratis-screenplay-toolchain`.
