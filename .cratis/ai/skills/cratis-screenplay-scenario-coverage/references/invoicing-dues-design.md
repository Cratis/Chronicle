<!-- cratis-ai-managed: skills/cratis-screenplay-scenario-coverage/references/invoicing-dues-design.md -->
# Invoicing and dues: design-mode document for the stored-state rules

**Legacy control without `command reads protected`, not binding-ready by design.**
Read [protected reads](../../cratis-screenplay-command-surface/references/protected-reads.md)
for the opt-in alternative. Do not insert the preamble blindly: every read must meet
finite event-source-keyed admission with removals. A v25 reference pass is not
rendering: Stage admits v25 and renders protected reads from 4.138.0, and older bundles refuse them with STAGE-ESM-044. The comments and unenforced markers below describe
this legacy control, not a general limit on command reads.

**Design mode.** This document states the stored-state rules of the
invoicing example as the skill requires (`reads <View>` + `require ... message`, marked NOT
enforced in the slice `description`, target named). It compiles (V1, warnings as errors) but
does not bind, and the only blockers are `reads` (PLAY0271) and `require` over
a view (PLAY0268), so no specification here runs (V3 blocked). `PaymentReceived` has its producer
here (the capture and `ReceivedTranslator`), so nothing else blocks: with every `reads` and
`require` line removed the document binds. The rules cover an unknown invoice, voiding a paid
invoice, reversing without a received payment and a second reversal (`ReceivedPayments` drops the
row on `PaymentReversed`). It is not a full coverage example: for that see `berth-reservations.md`.
Keep those lines: they state real intent and are never
replaced by prose. The runnable counterpart, which holds no stored-state rule, is
`invoicing-dues-example.md`; `scenario-examples.md` copies `ReversingAReceivedPayment` from here.

The excerpt's Stage API-availability comment describes the historical Stage 4.24.x
scaffold. Stage 4.73.1's scaffold pins an Arc/Chronicle set (see the Stage skill's `references/rendered-application.md`): protected-read APIs are
available to gap-fill (the "not in a Stage-rendered application" comment in the source is
superseded), not emitted protection or `.play` rule admission. The
capture uses separate first-sight/update appends, with its local-event follow-up in Automation.

```screenplay expect PLAY0516 test=unbound
// Scenario coverage design-mode example: stored-state rules of invoicing club dues (complete document).
// Design mode: `reads` is PLAY0271 and `require` over a view is PLAY0268 at binding (the only
// blockers), so this model compiles but is not executable; the specifications below do not run. The runnable counterpart
// is invoicing-dues-example.md.
// Target enforcement of every rule here: Arc [ProtectedDecision] with DecisionRead<T> (Arc v22.39.0
// or later, not in a Stage-rendered application), or a Chronicle DCB; Screenplay#129.

concept InvoiceId : Uuid
concept MemberId : Uuid
concept GatewayOutcome : Enum
  captured
  declined

policy IsAccounts
  require role "Accounts"

module Invoicing
  feature Dues
    authorize IsAccounts

    slice StateChange IssueInvoice
      description "Issues one invoice per invoice id (declared here so the rules below have an invoice to refer to)."
      command IssueInvoice
        invoiceId InvoiceId identifier
        member    MemberId
        amount    Decimal
        produces event InvoiceIssued
          member MemberId = member
          amount Decimal = amount
      constraint IssueOnce
        unique event InvoiceIssued

    slice StateView OpenInvoices
      description "Todo list: membership means 'still to collect'. The rules below read it (it lags: an unguarded read is not protection)."
      readmodel OpenInvoice
        invoiceId InvoiceId
        member    MemberId
        amount    Decimal
      query OpenInvoiceById => OpenInvoice optional
        by invoiceId InvoiceId
      projection OpenInvoices => OpenInvoice
        from InvoiceIssued
          invoiceId = $eventSourceId
          member    = member
          amount    = amount
        remove with PaymentReceived
        remove with InvoiceVoided
        remove with InvoiceSettled
    slice StateView ReceivedPayments
      description "Which invoices have a received, not yet reversed payment: PaymentReversed removes the row, so a second reversal finds none."
      readmodel ReceivedPayment
        invoiceId InvoiceId
      query ReceivedPaymentById => ReceivedPayment optional
        by invoiceId InvoiceId
      projection ReceivedPayments => ReceivedPayment
        from PaymentReceived
          invoiceId = $eventSourceId
        remove with PaymentReversed

    slice Translate PaymentGatewayResults
      description "Gives PaymentReceived its producer (the same capture and translator as invoicing-dues-example.md): reference -> correlation key (our invoice id); outcome -> translated to our enum. PaymentReceived is OUR fact; PaymentGatewayResultReceived is the provider's."
      capture PaymentGatewayResults
        source webhook
          path /webhooks/payments/results
        key reference
        map
          outcome = outcome translate
            "C" => captured
            "D" => declined
        append PaymentGatewayResultReceived
          when added
            outcome = $.outcome
            reason  = $.reason
        append PaymentGatewayResultReceived
          when outcome
            outcome = $.outcome
            reason  = $.reason
      event PaymentGatewayResultReceived
        outcome GatewayOutcome
        reason  String

    slice Automation RecordPaymentOutcome
      description "Gives PaymentReceived its producer: the translator reacts to the provider's external fact"
      reaction ReceivedTranslator
        when PaymentGatewayResultReceived
          outcome
          produces PaymentReceived
        where outcome == "captured"
      event PaymentReceived

    slice StateChange SettleOrVoidInvoice
      description "An invoice ends either settled or voided, never both (SettleOrVoid, enforced at append). NOT enforced in the model today: that the invoice exists and is still open (settling) and that only an open, unpaid invoice may be voided. Stated as `reads OpenInvoice` + `require` (PLAY0271/PLAY0268 at binding; Screenplay#129). Target: Arc [ProtectedDecision] with DecisionRead, or a Chronicle DCB. Requirements to test in the target: settling an unknown invoice is rejected; voiding a paid invoice is rejected; voiding an open invoice succeeds."
      command SettleInvoice
        invoiceId InvoiceId identifier
        reads OpenInvoice as invoice by invoiceId   // stated intent, unprotected (PLAY0271 at binding)
        validate
          require invoice.amount > 0
            message "Only an open invoice can be settled"   // NOT enforced: the view lags (PLAY0268 at binding)
        produces event InvoiceSettled
      command VoidInvoice
        invoiceId InvoiceId identifier
        reason    String
        reads OpenInvoice as invoice by invoiceId   // stated intent, unprotected (PLAY0271 at binding)
        validate
          require invoice.amount > 0
            message "Only an open, unpaid invoice can be voided"   // NOT enforced: the view lags (PLAY0268 at binding)
        produces event InvoiceVoided
          reason String = reason
      constraint SettleOrVoid
        unique event InvoiceSettled
        unique event InvoiceVoided

      specification SettlingAnOpenInvoice
        given caller
          authenticated
          role "Accounts"
        given InvoiceIssued
          for "9c1f0a52-6a4e-4b1c-9f55-0d2c7b8e1a01"
          member = "6f1c2a8e-0b1d-4d55-9a3e-2f6a7c1d0e11"
          amount = 130
        when SettleInvoice
          invoiceId = "9c1f0a52-6a4e-4b1c-9f55-0d2c7b8e1a01"
        then InvoiceSettled
          for "9c1f0a52-6a4e-4b1c-9f55-0d2c7b8e1a01"

      specification RejectingSettlementOfAnUnknownInvoice
        given caller
          authenticated
          role "Accounts"
        when SettleInvoice
          invoiceId = "9c1f0a52-6a4e-4b1c-9f55-0d2c7b8e1a01"
        then error "Only an open invoice can be settled"

      specification RejectingTheVoidingOfAPaidInvoice
        given caller
          authenticated
          role "Accounts"
        given InvoiceIssued
          for "9c1f0a52-6a4e-4b1c-9f55-0d2c7b8e1a01"
          member = "6f1c2a8e-0b1d-4d55-9a3e-2f6a7c1d0e11"
          amount = 130
        given PaymentReceived
          for "9c1f0a52-6a4e-4b1c-9f55-0d2c7b8e1a01"
        when VoidInvoice
          invoiceId = "9c1f0a52-6a4e-4b1c-9f55-0d2c7b8e1a01"
          reason    = "Issued in error"
        then error "Only an open, unpaid invoice can be voided"

    slice StateChange ReversePayment
      description "Compensation for a received payment. NOT enforced in the model today: that a payment was received and not yet reversed (so a second reversal is refused). Stated as `reads ReceivedPayment` + `require` (PLAY0271/PLAY0268 at binding). Target: a protected decision read of the payment (keyed by the invoice source id). RefundRequested is the request recorded by the reaction; it does not show a refund was delivered, and the gateway's own answer is a later external fact. Requirements to test in the target: reversing without a received payment is rejected; a second reversal of the same payment is rejected; a correct reversal records PaymentReversed and RefundRequested. Open question: OpenInvoices dropped the invoice on PaymentReceived and nothing re-adds it on PaymentReversed, so a reversed invoice can be neither settled nor voided and leaves the still-to-collect list; whether a reversal reopens the invoice is undecided."
      command ReversePayment
        invoiceId InvoiceId identifier
        amount    Decimal
        reads ReceivedPayment as payment by invoiceId   // stated intent, unprotected (PLAY0271 at binding)
        validate
          require payment.invoiceId == invoiceId
            message "Only a received, not yet reversed payment can be reversed"   // NOT enforced: the view lags (PLAY0268 at binding)
        produces event PaymentReversed
          amount Decimal = amount

      specification ReversingAReceivedPayment
        given caller
          authenticated
          role "Accounts"
        given InvoiceIssued
          for "9c1f0a52-6a4e-4b1c-9f55-0d2c7b8e1a01"
          member = "6f1c2a8e-0b1d-4d55-9a3e-2f6a7c1d0e11"
          amount = 130
        given PaymentReceived
          for "9c1f0a52-6a4e-4b1c-9f55-0d2c7b8e1a01"
        when ReversePayment
          invoiceId = "9c1f0a52-6a4e-4b1c-9f55-0d2c7b8e1a01"
          amount    = 130
        then PaymentReversed
          for "9c1f0a52-6a4e-4b1c-9f55-0d2c7b8e1a01"
          amount = 130
        then RefundRequested
          for "9c1f0a52-6a4e-4b1c-9f55-0d2c7b8e1a01"
          amount = 130

      specification RejectingAReversalWithoutAReceivedPayment
        given caller
          authenticated
          role "Accounts"
        given InvoiceIssued
          for "9c1f0a52-6a4e-4b1c-9f55-0d2c7b8e1a01"
          member = "6f1c2a8e-0b1d-4d55-9a3e-2f6a7c1d0e11"
          amount = 130
        when ReversePayment
          invoiceId = "9c1f0a52-6a4e-4b1c-9f55-0d2c7b8e1a01"
          amount    = 130
        then error "Only a received, not yet reversed payment can be reversed"

      specification RejectingASecondReversal
        given caller
          authenticated
          role "Accounts"
        given InvoiceIssued
          for "9c1f0a52-6a4e-4b1c-9f55-0d2c7b8e1a01"
          member = "6f1c2a8e-0b1d-4d55-9a3e-2f6a7c1d0e11"
          amount = 130
        given PaymentReceived
          for "9c1f0a52-6a4e-4b1c-9f55-0d2c7b8e1a01"
        given PaymentReversed
          for "9c1f0a52-6a4e-4b1c-9f55-0d2c7b8e1a01"
          amount = 130
        when ReversePayment
          invoiceId = "9c1f0a52-6a4e-4b1c-9f55-0d2c7b8e1a01"
          amount    = 130
        then error "Only a received, not yet reversed payment can be reversed"

      specification RefusingAReversalWithoutTheAccountsRole
        given caller
          authenticated
        when ReversePayment
          invoiceId = "9c1f0a52-6a4e-4b1c-9f55-0d2c7b8e1a01"
          amount    = 130
        then denied

    slice Automation RequestRefunds
      reaction RefundRequester
        description "Records a refund request for every reversed payment; ends at RefundRequested"
        when PaymentReversed
          amount
          produces RefundRequested
            amount = amount
      event RefundRequested
        amount Decimal

      specification RequestingARefundAfterAReversal
        when append PaymentReversed
          for "9c1f0a52-6a4e-4b1c-9f55-0d2c7b8e1a01"
          amount = 130
        then RefundRequested
          for "9c1f0a52-6a4e-4b1c-9f55-0d2c7b8e1a01"
          amount = 130
```
