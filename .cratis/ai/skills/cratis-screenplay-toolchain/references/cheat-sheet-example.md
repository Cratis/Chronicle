<!-- cratis-ai-managed: skills/cratis-screenplay-toolchain/references/cheat-sheet-example.md -->
# Cheat-sheet example (design mode)

One complete document with every construct family. Design mode, not executable. Prose rules: [cheat-sheet.md](cheat-sheet.md); what binds: [executable-subset.md](executable-subset.md).

Binding blockers: unquoted import, `reads` (PLAY0271), `starts with`. Caller-supplied collection equality filters bind as ESM v14; context and single-result filters remain blocked. Executable shapes are in the sibling examples. `RecordingAPayment` lists the `InvoiceClosed` cascade; an older bundled compiler rejected it with a false PLAY0285 ("outcome cannot be produced by 'RecordPayment'").

Concept compliance itself binds as ESM v13; reasons remain report-only PLAY0270. Stage renders v13 concept
protection. Binder-only PLAY0268 refusals and subject-lineage limits: command-surface `references/compliance.md`.

```screenplay test=unbound
// Design-mode cheat-sheet: every construct family; compiles warning-free on the standalone tool.
domain Acme.Invoicing                      // optional; first line when present
import Customers.CustomerRegistered        // unquoted = another bounded context's contract
// import "Shared/*.play"                  // quoted = my own files (PLAY0455 W if the glob matches nothing)

concept InvoiceId : Uuid                   // primitives: Uuid String Int Decimal Bool Date DateTime Enum
concept PaymentId : Uuid
concept Email : String pii                 // concept compliance binds as ESM v13
  pii reason "Identifies a person"
  validate
    not empty  message "An email is required"
concept Status : Enum
  draft
  sent

type Line
  sku      String
  quantity Int optional                    // Type[] / Type optional / Type[] optional

policy IsAuthenticated
  require authenticated
policy CanManage
  require role "Manager" or (role "Accountant" and claim "dept" matches "Finance")
persona Manager
  description "Runs invoicing"
  policy IsAuthenticated
  policy CanManage
authentication
  provider EntraId
  provider OpenId name Partner
trigger NightlySync
  batch Int

// ---------- structure ----------
module Billing
  description "Invoices and payments"
  authorize IsAuthenticated
  feature Invoices
    authorize CanManage
    slice StateChange RegisterInvoice
      description "Register a new invoice"

      command RegisterInvoice
        invoiceId InvoiceId identifier
        number    String
        contact   Email
        lines     Line[]
        reads InvoiceRow as existing by invoiceId   // legacy, unprotected (PLAY0271 at binding)
        authorize CanManage
        validate
          number not empty                    message "A number is required"
          number matches "^INV-[0-9]{6}$"     severity warning message "Unusual number"
          require number != "INV-000000"
            message "Reserved number"
        produces InvoiceRegistered
          for invoiceId
          tag audit
          number       = number
          registeredAt = $context.occurred
        produces when number starts with "INV-9"
          SpecialInvoiceRegistered
            for invoiceId

      event InvoiceRegistered
        number       String
        registeredAt DateTime

      event SpecialInvoiceRegistered

      constraint UniqueNumber
        unique number on InvoiceRegistered

      specification RegisteringAnInvoice
        given caller
          authenticated
          role "Manager"
        given clock "2026-10-02T09:00:00Z"
        when RegisterInvoice
          invoiceId = "9c858901-8a57-4791-81fe-4c455b099bc9"
          number    = "INV-000123"
          contact   = "ada@example.com"
          lines     = [{"sku": "A-1", "quantity": 2}]
        then InvoiceRegistered
          for "9c858901-8a57-4791-81fe-4c455b099bc9"
          number       = "INV-000123"
          registeredAt = "2026-10-02T09:00:00Z"

      specification RejectingAReservedNumber
        given caller
          authenticated
          role "Manager"
        when RegisterInvoice
          invoiceId = "9c858901-8a57-4791-81fe-4c455b099bc9"
          number    = "INV-000000"
          contact   = "ada@example.com"
          lines     = []
        then error "Reserved number"

      specification RefusingAGuest
        given caller
          authenticated
          role "Guest"
        when RegisterInvoice
          invoiceId = "9c858901-8a57-4791-81fe-4c455b099bc9"
          number    = "INV-000123"
          contact   = "ada@example.com"
          lines     = []
        then denied

      specification RejectingAnEmptyEmail          // one concept rejection spec, through this command
        given caller
          authenticated
          role "Manager"
        when RegisterInvoice
          invoiceId = "9c858901-8a57-4791-81fe-4c455b099bc9"
          number    = "INV-000123"
          contact   = ""
          lines     = []
        then error "An email is required"

    slice StateChange CancelInvoice
      command CancelInvoice
        invoiceId InvoiceId identifier
        authorize CanManage
        produces event InvoiceCancelled
      specification CancellingAnInvoice
        given caller
          authenticated
          role "Manager"
        when CancelInvoice
          invoiceId = "9c858901-8a57-4791-81fe-4c455b099bc9"
        then InvoiceCancelled
          for "9c858901-8a57-4791-81fe-4c455b099bc9"

      specification RefusingAGuestCancellation     // inherited gate: its own denial spec
        given caller
          authenticated
          role "Guest"
        when CancelInvoice
          invoiceId = "9c858901-8a57-4791-81fe-4c455b099bc9"
        then denied

    slice StateView InvoiceList
      readmodel InvoiceRow
        invoiceId InvoiceId
        number    String
      query InvoiceById => InvoiceRow optional        // keyed optional lookup (unfiltered lists bind too)
        by invoiceId InvoiceId
      query ListInvoices => observable InvoiceRow[]   // caller-supplied equality filter: ESM v14
        filter number String optional
      projection InvoiceList => InvoiceRow
        from InvoiceRegistered
          invoiceId = $eventSourceId
          number    = number
        remove with InvoiceCancelled
      screen InvoiceList
        title "Invoices"
        data InvoiceRow[] via query ListInvoices
        table InvoiceRow
          column number label "Number"
        action RegisterInvoice
      specification ListingAnInvoice
        given caller                                  // module + feature gates apply to the query too
          authenticated
          role "Manager"
        given InvoiceRegistered
          for "9c858901-8a57-4791-81fe-4c455b099bc9"
          number       = "INV-000123"
          registeredAt = "2026-10-02T09:00:00Z"
        then query InvoiceById
          arguments
            invoiceId = "9c858901-8a57-4791-81fe-4c455b099bc9"
          result
            number = "INV-000123"
      specification RefusingAGuestLookup           // ListInvoices is design-only: its denial spec is a recorded mode gap
        given caller
          authenticated
          role "Guest"
        then query InvoiceById
          arguments
            invoiceId = "9c858901-8a57-4791-81fe-4c455b099bc9"
        then denied
      specification RemovingACancelledInvoice
        given caller
          authenticated
          role "Manager"
        given InvoiceRegistered
          for "9c858901-8a57-4791-81fe-4c455b099bc9"
          number       = "INV-000123"
          registeredAt = "2026-10-02T09:00:00Z"
        when append InvoiceCancelled
          for "9c858901-8a57-4791-81fe-4c455b099bc9"
        then no readmodel InvoiceRow for "9c858901-8a57-4791-81fe-4c455b099bc9"

  feature Payments
    authorize CanManage
    slice StateChange RecordPayment
      command RecordPayment
        paymentId PaymentId identifier
        invoiceId InvoiceId
        amount    Decimal
        produces event PaymentRecorded
          invoiceId InvoiceId = invoiceId             // cross-stream reference is payload
          amount Decimal = amount
      specification RecordingAPayment
        given caller
          authenticated
          role "Manager"
        when RecordPayment
          paymentId = "0b6f3f7e-3c1e-4a53-9f7e-2a1c3b4d5e6f"
          invoiceId = "9c858901-8a57-4791-81fe-4c455b099bc9"
          amount    = 100
        then PaymentRecorded
          for "0b6f3f7e-3c1e-4a53-9f7e-2a1c3b4d5e6f"
          invoiceId = "9c858901-8a57-4791-81fe-4c455b099bc9"
          amount    = 100
        then InvoiceClosed                            // ESM v6: the Reconciler cascade (amount > 0)
          for "9c858901-8a57-4791-81fe-4c455b099bc9"

module Housekeeping                                   // no gates: this reaction declares no `runs as`, so its `invokes` has no caller
  description "Background follow-up"
  feature FollowUp
    slice StateChange CloseInvoice
      command CloseInvoice
        invoiceId InvoiceId identifier
        produces event InvoiceClosed
      specification ClosingAnInvoice
        when CloseInvoice
          invoiceId = "9c858901-8a57-4791-81fe-4c455b099bc9"
        then InvoiceClosed
          for "9c858901-8a57-4791-81fe-4c455b099bc9"

    slice StateChange MarkOverdue
      command MarkOverdue
        invoiceId InvoiceId identifier
        produces event InvoiceMarkedOverdue
          overdueAt DateTime = $context.occurred
      // No command spec here: under ESM v6 it must list the ReminderSent cascade. The file is
      // RecordingAPayment's cascade: an older bundled compiler reported a false PLAY0285 here.
      // The cascade form: automation-translate-example.md (SubscribingAMember).

    slice Automation RemindOverdue
      reaction Reminder
        when InvoiceMarkedOverdue
          overdueAt
          produces ReminderSent                           // no `for`: lands on the trigger's source
            sentAt = $context.occurred
        at 08:00 on Monday
          produces WeeklyDigestIssued
            for "weekly"                                  // clock trigger needs an explicit `for`
            issuedAt = $context.occurred
      reaction Reconciler
        when PaymentRecorded
          invoiceId
          amount
          invokes CloseInvoice
            invoiceId = invoiceId
        where amount > 0                                  // one per reaction; resolves from EVERY trigger's values
      event ReminderSent
        sentAt DateTime
      event WeeklyDigestIssued
        issuedAt DateTime
      specification RemindingAnOverdueInvoice
        given clock "2026-10-05T08:00:00Z"
        when append InvoiceMarkedOverdue                  // v6: `then` lists only what followed the append
          for "9c858901-8a57-4791-81fe-4c455b099bc9"
          overdueAt = "2026-10-05T08:00:00Z"
        then ReminderSent
          for "9c858901-8a57-4791-81fe-4c455b099bc9"
          sentAt = "2026-10-05T08:00:00Z"
      specification ReconcilingAPayment
        when append PaymentRecorded
          for "0b6f3f7e-3c1e-4a53-9f7e-2a1c3b4d5e6f"
          invoiceId = "9c858901-8a57-4791-81fe-4c455b099bc9"
          amount    = 100
        then InvoiceClosed
          for "9c858901-8a57-4791-81fe-4c455b099bc9"

    slice Translate LegacySync
      capture LegacyInvoices
        source api
          api LegacyApi
          route /invoices
          poll 5m
        key id
        map
          status = status translate
            "sendt" => sent
        append ExternalInvoiceStatusObserved
          when status
            status = $.status
      event ExternalInvoiceStatusObserved
        status Status

seed
  for "9c858901-8a57-4791-81fe-4c455b099bc9"
    InvoiceRegistered
      number       = "INV-000042"
      registeredAt = "2026-10-01T00:00:00Z"
```
