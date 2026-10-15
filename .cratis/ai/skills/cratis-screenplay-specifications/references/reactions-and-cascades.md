<!-- cratis-ai-managed: skills/cratis-screenplay-specifications/references/reactions-and-cascades.md -->
## Reactions and cascades (ESM v6)

From ESM v6 every action sets reactions off: after a command, an append, a clock
tick, a trigger or a capture record, each new fact runs the reactions to its event,
and what those append or invoke runs more, until nothing is left. `then` events
compare every new fact, the action's and the reactions'.

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
      specification SendingAnInvoiceSchedulesAReminder
        given clock "2026-10-02T09:00:00Z"
        when SendInvoice
          invoiceId = "9c858901-8a57-4791-81fe-4c455b099bc9"
        then InvoiceSent
          for "9c858901-8a57-4791-81fe-4c455b099bc9"
          invoiceId = "9c858901-8a57-4791-81fe-4c455b099bc9"
        then ReminderScheduled
          for "9c858901-8a57-4791-81fe-4c455b099bc9"
          scheduledAt = "2026-10-02T09:00:00Z"
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
```

- A specification whose action is a command goes in the slice that declares that
  command, here `SendInvoice`, not in the `Automation` slice that reacts: a command
  from another slice is `PLAY0273` "command is unresolved in its slice" at binding.
- A reaction invocation without a declared actor has no caller and a gated
  command denies. At 4.125.0 reaction `runs as system role "<Role>"` supplies the
  authenticated system actor (ESM v10); `given caller` does not. Each command is
  atomic, not the cascade. Checked actor scenario and CLI/Stage limitations:
  captures-and-reactions `references/invocation-identity.md`.
- The clock is UTC and exact. An `every` or `at` occurrence fires once when it is
  due after `given clock` and at or before `when clock`; `when clock` needs
  `given clock`, so the same instant twice fires nothing.
- A reached reaction with a code body (`file` or inline) returns unsupported, as
  does a reaction that never settles. The reference stops at 1,000 new facts per
  scenario and 10,000 due occurrences per clock advance.
- A model that uses these forms selects ESM v6 (`schemaVersion: 6`). Stage 4.89.1
  admits ESM v1-v15 and renders supported event-triggered Automation, but not Translate,
  captures, non-event triggers or opaque bodies (`-024`), claim-reading/opaque `runs as` gates (`-034`), legacy filters (`-035`)
  or mixed effects (`-037`). Single-hop reaction append specs render; deeper appends and
  command specs cascading into reactions remain `-011`. Keep refused scope as gap-fill against this contract: `cratis-screenplay-render-and-gap-fill`.

### Version skew: cascades and the false `PLAY0285` (cratis before 3.28.2)

`PLAY0285` (a specification's expected event contradicts every producer of its
`when` command) runs in the syntax pass, with no binding. Reachability through
reactions and `invokes` was added after Screenplay 4.60.1. So the example above
passes `screenplay` 4.68.0 (and is executable-ready over its MCP server) and also passes
`cratis screenplay validate` 3.28.2 (probed); it failed `cratis screenplay validate` 3.27.1 with
`PLAY0285` "Specification outcome 'ReminderScheduled' cannot be produced by 'SendInvoice'",
exit 5 (Cratis/cli#242). If an older bundle still reports it, it is a tool skew, not a
modeling error: do not delete the `then` line or move the event to make `cratis`
green. Validate with the standalone tool or a current `cratis`, record which tool gave which
verdict, and see `cratis-screenplay-toolchain`.
