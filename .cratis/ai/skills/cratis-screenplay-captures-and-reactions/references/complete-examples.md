<!-- cratis-ai-managed: skills/cratis-screenplay-captures-and-reactions/references/complete-examples.md -->
# Complete examples for the excerpts in SKILL.md

## Contents

- Capture with `source`, `map`, `append` and `children`
- Reaction on a declared trigger with `where` and `invokes`
- Reaction with `produces` and `invokes`
- Trigger `reads` on an event and on the clock
- Complete example (ESM v6)

Each document below is complete and compiles with the standalone `screenplay` 4.68.0
(`--warnaserror`). The SKILL.md excerpts show the reaction, capture or trigger part of
these documents. Documents marked ESM v6 need Screenplay 4.61 or later: the standalone tool and `cratis` 3.28.2
 validate and bind them (probed); `cratis` 3.27.1 reported
`PLAY0268` when it bound them.

## Capture with `source`, `map`, `append` and `children`

Parent of the `LegacyInvoiceSync` excerpt. Authoring and binding both pass with the
standalone tool; a capture's `source` is realization metadata (`PLAY0270`).

```screenplay
concept InvoiceId : Uuid
concept InvoiceStatus : Enum
  draft
  sent
  paid
module Invoicing
  feature Legacy
    slice Translate LegacyInvoiceSync
      event InvoiceStatusChanged
        invoiceId InvoiceId
        status InvoiceStatus
        changedAt DateTime
      event InvoicePaidFromSent
        invoiceId InvoiceId
      event InvoiceLineItemAdded
        invoiceId InvoiceId
        lineNumber Int
      capture LegacyInvoiceCapture
        source api
          api   LegacyInvoicingApi
          route /invoices
          poll  5m
        key id
        map
          status = status translate
            "utkast" => draft
            "sendt"  => sent
            "betalt" => paid
        append InvoiceStatusChanged
          tag legacy
          when status
            invoiceId = $.id
            status    = $.status
            changedAt = $context.occurred
        append InvoicePaidFromSent
          when status from "sent" to "paid"
            invoiceId = $.id
        children lineItems identified by lineNumber
          append InvoiceLineItemAdded
            when added
              invoiceId  = $.id
              lineNumber = $.lineNumber
```

## Reaction on a declared trigger with `where` and `invokes`

Parent of the `NotifyOnBuildFailure` excerpt. A trigger value needs a type to be
executable (`PLAY0268` otherwise).

```screenplay
concept Repository : String
trigger BuildFinished
  description "CI reported a finished build on a watched repository"
  repository Repository
  outcome String
module Delivery
  feature Builds
    slice StateChange SendFailureNotice
      command SendFailureNotice
        repository Repository identifier
        outcome String
        produces event FailureNoticeSent
          outcome String = outcome
    slice Automation NotifyOnBuildFailure
      reaction NotifyOnFailure
        description "Tells the owning team when a watched build fails"
        when BuildFinished
          repository
          outcome
          invokes SendFailureNotice
            repository = repository
            outcome    = outcome
        where outcome == "failed"
```

## Reaction with `produces` and `invokes`

Parent of the `Provisioner` excerpt. The reaction declares no `runs as system`, so the invoked command runs with no caller in the
reference execution.

```screenplay
concept WorkspaceId : Uuid
module Onboarding
  feature Workspaces
    slice StateChange AcceptInvitation
      command AcceptInvitation
        invitationId Uuid identifier
        workspaceId  WorkspaceId
        produces event InvitationAccepted
          workspaceId WorkspaceId = workspaceId
    slice StateChange SendWelcomeMail
      command SendWelcomeMail
        workspaceId WorkspaceId identifier
        template String
        produces event WelcomeMailSent
          template String = template
    slice Automation ProvisionWorkspace
      event WorkspaceProvisioned
        workspaceId WorkspaceId
      reaction Provisioner
        when InvitationAccepted
          workspaceId
          produces WorkspaceProvisioned
            for workspaceId
            workspaceId = workspaceId
          invokes SendWelcomeMail
            workspaceId = workspaceId
            template    = "welcome"
```

## Trigger `reads` on an event and on the clock

Parent of the `ChaseOverdueInvoices` excerpt. This document is authoring-valid but does
**not** bind at ESM v6: the MCP server of Screenplay 4.66.0 (standalone and `cratis screenplay mcp` 3.28.2, probed) reports `PLAY0268` "reads
'InvoiceBalance' before producing directly, but ESM v6 cannot protect that decision
dependency (decision 0006)" because a `file` body is an opaque effect. Drop the `file`
lines and keep only `invokes` for an executable reaction.

```screenplay
concept InvoiceId : Uuid
module Collections
  feature Overdue
    slice StateChange SendPaymentReminder
      command SendPaymentReminder
        invoiceId InvoiceId identifier
        channel String
        produces event PaymentReminderSent
          channel String = channel
    slice StateView InvoiceBalances
      event InvoiceRegistered
        invoiceId InvoiceId
        amount Decimal
      event InvoiceFellDue
        invoiceId InvoiceId
      readmodel InvoiceBalance
        invoiceId InvoiceId
        amount Decimal
      query InvoiceBalanceById => InvoiceBalance optional
        by invoiceId InvoiceId
      projection InvoiceBalances => InvoiceBalance
        from InvoiceRegistered
          invoiceId = $eventSourceId
          amount    = amount
      readmodel OverdueInvoice
        invoiceId InvoiceId
      query OverdueInvoiceById => OverdueInvoice optional
        by invoiceId InvoiceId
      projection OverdueInvoices => OverdueInvoice
        from InvoiceFellDue
          invoiceId = $eventSourceId
    slice Automation ChaseOverdueInvoices
      reaction RemindOnDueDate
        description "Reminds the customer when an invoice falls due unpaid"
        when InvoiceFellDue
          invoiceId
          reads InvoiceBalance as balance by invoiceId
          invokes SendPaymentReminder
            invoiceId = invoiceId
            channel   = "email"
          file Reactions/RemindOnDueDate.cs
      reaction SweepOverdueInvoices
        description "Re-checks every overdue invoice each morning"
        at 08:00
          reads OverdueInvoice
          file Reactions/SweepOverdueInvoices.cs
```

## Complete example (ESM v6)

One document with every construct below: an application trigger, a reaction on an
event, on a trigger (with `where` and `invokes`), on a view read and on the clock, and
a capture with its specifications. The excerpts later in this skill have their own
complete parent documents in `references/complete-examples.md`.

```screenplay expect PLAY0469
concept InvoiceId : Uuid
trigger PaymentFileArrived
  description "The bank's payment file listed a payment for an invoice"
  invoiceId InvoiceId
  amount Decimal
module Collections
  feature Invoices
    slice StateChange SendInvoice
      command SendInvoice
        invoiceId InvoiceId identifier
        amount Decimal
        produces InvoiceSent
          for invoiceId
          invoiceId = invoiceId
          amount = amount
          sentAt = $context.occurred
      event InvoiceSent
        invoiceId InvoiceId
        amount Decimal
        sentAt DateTime
    slice StateChange CloseInvoice
      command CloseInvoice
        invoiceId InvoiceId identifier
        reason String
        validate
          reason not empty message "A reason is required"
        produces event InvoiceClosed
          reason String = reason
      constraint OnlyClosedOnce
        unique event InvoiceClosed
    slice StateView InvoiceBalances
      readmodel InvoiceBalance
        invoiceId InvoiceId
        outstanding Decimal
      query InvoiceBalanceById => InvoiceBalance optional
        by invoiceId InvoiceId
      projection InvoiceBalances => InvoiceBalance
        from InvoiceSent
          invoiceId = $eventSourceId
          outstanding = amount
    slice StateChange SendPaymentReminder
      command SendPaymentReminder
        invoiceId InvoiceId identifier
        produces event PaymentReminderSent
          reminderNumber Int = 1
    slice Automation Reminders
      reaction RemindOnDueDate
        description "Reminds the customer when an invoice falls due unpaid"
        when InvoiceFellDue
          invoiceId
          reads InvoiceBalance as balance by invoiceId
          invokes SendPaymentReminder
            invoiceId = invoiceId
      reaction ReminderScheduler
        description "Schedules a reminder when an invoice is sent"
        when InvoiceSent
          invoiceId
          produces ReminderScheduled
            scheduledAt = $context.occurred
      reaction PaymentImporter
        description "Closes an invoice the bank file shows as paid"
        when PaymentFileArrived
          invoiceId
          amount
          invokes CloseInvoice
            invoiceId = invoiceId
            reason = "paid"
        where amount > 0
      reaction WeeklyDigest
        description "Issues the collections digest every Monday morning"
        at 07:30 on Monday
          produces DigestIssued
            for "weekly"
            issuedAt = $context.occurred
      event InvoiceFellDue
        invoiceId InvoiceId
      event ReminderScheduled
        scheduledAt DateTime
      event DigestIssued
        issuedAt DateTime
      specification SchedulingAReminderWhenAnInvoiceIsSent
        given clock "2026-10-02T09:00:00Z"
        when append InvoiceSent
          for "9c858901-8a57-4791-81fe-4c455b099bc9"
          invoiceId = "9c858901-8a57-4791-81fe-4c455b099bc9"
          amount = 120
          sentAt = "2026-10-02T09:00:00Z"
        then ReminderScheduled
          for "9c858901-8a57-4791-81fe-4c455b099bc9"
          scheduledAt = "2026-10-02T09:00:00Z"
      specification ClosingAPaidInvoice
        when trigger PaymentFileArrived
          invoiceId = "9c858901-8a57-4791-81fe-4c455b099bc9"
          amount = 120
        then InvoiceClosed
          for "9c858901-8a57-4791-81fe-4c455b099bc9"
          reason = "paid"
      specification IssuingTheWeeklyDigest
        given clock "2026-10-05T07:00:00Z"
        when clock "2026-10-05T07:30:00Z"
        then DigestIssued
          for "weekly"
          issuedAt = "2026-10-05T07:30:00Z"
    slice Translate LegacyInvoiceSync
      capture LegacyInvoiceCapture
        source api
          api   LegacyInvoicingApi
          route /invoices
          poll  5m
        key id
        map
          status = status translate
            "sendt"  => sent
            "betalt" => paid
        append LegacyInvoicePaid
          tag legacy
          when status from "sent" to "paid"
            paidAt = $context.occurred
      event LegacyInvoicePaid
        paidAt DateTime
      specification SeeingALegacyPayment
        given clock "2026-10-02T12:00:00Z"
        given capture LegacyInvoiceCapture
          id     = "inv-42"
          status = "sendt"
        when capture LegacyInvoiceCapture
          id     = "inv-42"
          status = "betalt"
        then LegacyInvoicePaid
          for "inv-42"
          paidAt = "2026-10-02T12:00:00Z"
```
