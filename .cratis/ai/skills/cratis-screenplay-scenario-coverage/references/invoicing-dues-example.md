<!-- cratis-ai-managed: skills/cratis-screenplay-scenario-coverage/references/invoicing-dues-example.md -->
# Invoicing and dues: worked example for the scenario-examples excerpts

## Contents

- Notes

Complete executable model (harbor club dues) holding every declaration the excerpts in
`scenario-examples.md` and `view-and-story-specs.md` rely on, except the stored-state rules. Each
of those excerpts is copied verbatim from the fence below (names are the specification names),
except `ReversingAReceivedPayment` and `RequestingARefundAfterAReversal`, which come from
`invoicing-dues-design.md` (`RefundRequester` stays with `ReversePayment` there to keep that example's source declaration and
destination-type information; a reaction can also consume events from a capture or another reaction).

**Scope.** This document is the runnable demonstration only. It deliberately holds no rule that
depends on stored state (the invoice must exist, only an open invoice may be voided, a reversal
needs a received payment, `reads` + `require`): the legacy design control lacks
`command reads protected` and retains PLAY0271/PLAY0268. Eligible reads can opt into
v25; see command-surface protected-read admission and Stage refusal. The rules stay
in the design-mode document `invoicing-dues-design.md`,
never in prose here. It shows the category shapes; for a coverage example see `berth-reservations.md`.

```screenplay
// Scenario coverage worked example: invoicing club dues (complete document).
// Backs the excerpts in scenario-examples.md and view-and-story-specs.md: issue an invoice,
// request payment, an external gateway result captured and translated, an open-invoices todo list family and an accumulating credit balance.
// Scope: executable demonstration only. No stored-state rule is declared or pretended here; those
// (invoice exists, void only if open, reversal needs a received payment) live in the design-mode
// document invoicing-dues-design.md.
// The external payment outcome is a fact (PaymentGatewayResultReceived); a refund request is a
// fact of intent, never proof that money moved.

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
      description "Issues one invoice per invoice id. IssueOnce refuses a retried issue with the same id. Why an amount above zero: a zero invoice cannot be collected or voided meaningfully."
      command IssueInvoice
        invoiceId InvoiceId identifier
        member    MemberId
        amount    Decimal
        validate
          amount > 0 message "An invoice needs an amount above zero"
        produces event InvoiceIssued
          member MemberId = member
          amount Decimal = amount
      constraint IssueOnce
        unique event InvoiceIssued

      specification IssuingAnInvoice
        given caller
          authenticated
          role "Accounts"
        when IssueInvoice
          invoiceId = "9c1f0a52-6a4e-4b1c-9f55-0d2c7b8e1a01"
          member    = "6f1c2a8e-0b1d-4d55-9a3e-2f6a7c1d0e11"
          amount    = 130
        then InvoiceIssued
          for "9c1f0a52-6a4e-4b1c-9f55-0d2c7b8e1a01"
          member = "6f1c2a8e-0b1d-4d55-9a3e-2f6a7c1d0e11"
          amount = 130

      specification RejectingAnInvoiceWithoutAnAmount
        given caller
          authenticated
          role "Accounts"
        when IssueInvoice
          invoiceId = "9c1f0a52-6a4e-4b1c-9f55-0d2c7b8e1a01"
          member    = "6f1c2a8e-0b1d-4d55-9a3e-2f6a7c1d0e11"
          amount    = 0
        then error "An invoice needs an amount above zero"

      specification RefusingACallerWithoutTheAccountsRole
        given caller
          authenticated
        when IssueInvoice
          invoiceId = "9c1f0a52-6a4e-4b1c-9f55-0d2c7b8e1a01"
          member    = "6f1c2a8e-0b1d-4d55-9a3e-2f6a7c1d0e11"
          amount    = 130
        then denied

    slice StateChange SettleOrVoidInvoice
      description "An invoice ends either settled or voided, never both: SettleOrVoid declares the exclusivity per invoice, enforced at append. Rules about the invoice's stored state are in invoicing-dues-design.md."
      command SettleInvoice
        invoiceId InvoiceId identifier
        produces event InvoiceSettled
      command VoidInvoice
        invoiceId InvoiceId identifier
        reason    String
        produces event InvoiceVoided
          reason String = reason
      constraint SettleOrVoid
        unique event InvoiceSettled
        unique event InvoiceVoided

      specification SettlingAnInvoice
        given caller
          authenticated
          role "Accounts"
        when SettleInvoice
          invoiceId = "9c1f0a52-6a4e-4b1c-9f55-0d2c7b8e1a01"
        then InvoiceSettled
          for "9c1f0a52-6a4e-4b1c-9f55-0d2c7b8e1a01"

      specification VoidingAnInvoice
        given caller
          authenticated
          role "Accounts"
        when VoidInvoice
          invoiceId = "9c1f0a52-6a4e-4b1c-9f55-0d2c7b8e1a01"
          reason    = "Issued in error"
        then InvoiceVoided
          for "9c1f0a52-6a4e-4b1c-9f55-0d2c7b8e1a01"
          reason = "Issued in error"

      specification RefusingSettlementWithoutTheAccountsRole
        given caller
          authenticated
        when SettleInvoice
          invoiceId = "9c1f0a52-6a4e-4b1c-9f55-0d2c7b8e1a01"
        then denied

      specification RefusingAVoidWithoutTheAccountsRole
        given caller
          authenticated
        when VoidInvoice
          invoiceId = "9c1f0a52-6a4e-4b1c-9f55-0d2c7b8e1a01"
          reason    = "Issued in error"
        then denied

      specification RejectingSettlementOfAVoidedInvoice
        given caller
          authenticated
          role "Accounts"
        given InvoiceVoided
          for "9c1f0a52-6a4e-4b1c-9f55-0d2c7b8e1a01"
          reason = "Issued in error"
        when SettleInvoice
          invoiceId = "9c1f0a52-6a4e-4b1c-9f55-0d2c7b8e1a01"
        then error "Constraint 'SettleOrVoid' is violated: the event source already has the constrained event."

      specification RejectingTheVoidingOfASettledInvoice
        given caller
          authenticated
          role "Accounts"
        given InvoiceSettled
          for "9c1f0a52-6a4e-4b1c-9f55-0d2c7b8e1a01"
        when VoidInvoice
          invoiceId = "9c1f0a52-6a4e-4b1c-9f55-0d2c7b8e1a01"
          reason    = "Issued in error"
        then error "Constraint 'SettleOrVoid' is violated: the event source already has the constrained event."

    slice StateChange RequestPayment
      description "Starts a payment attempt. PaymentRequested says only that we asked the gateway; it is NOT the outcome. The outcome arrives later as an external fact (slice PaymentGatewayResults). A request may be repeated after a decline, so there is deliberately no once-only constraint here."
      command RequestPayment
        invoiceId InvoiceId identifier
        produces event PaymentRequested

      specification RequestingPayment
        given caller
          authenticated
          role "Accounts"
        given InvoiceIssued
          for "9c1f0a52-6a4e-4b1c-9f55-0d2c7b8e1a01"
          member = "6f1c2a8e-0b1d-4d55-9a3e-2f6a7c1d0e11"
          amount = 130
        when RequestPayment
          invoiceId = "9c1f0a52-6a4e-4b1c-9f55-0d2c7b8e1a01"
        then PaymentRequested
          for "9c1f0a52-6a4e-4b1c-9f55-0d2c7b8e1a01"

      specification RetryingPaymentAfterADecline
        given caller
          authenticated
          role "Accounts"
        given InvoiceIssued
          for "9c1f0a52-6a4e-4b1c-9f55-0d2c7b8e1a01"
          member = "6f1c2a8e-0b1d-4d55-9a3e-2f6a7c1d0e11"
          amount = 130
        given PaymentRequested
          for "9c1f0a52-6a4e-4b1c-9f55-0d2c7b8e1a01"
        given PaymentDeclined
          for "9c1f0a52-6a4e-4b1c-9f55-0d2c7b8e1a01"
          reason = "Card declined"
        when RequestPayment
          invoiceId = "9c1f0a52-6a4e-4b1c-9f55-0d2c7b8e1a01"
        then PaymentRequested
          for "9c1f0a52-6a4e-4b1c-9f55-0d2c7b8e1a01"

    slice Translate PaymentGatewayResults
      description "Per external field: reference -> correlation key (our invoice id, echoed by the gateway); outcome -> translated to our enum; reason -> kept; cardholder -> ignored (personal data, no consumer). Authenticity of the webhook is a target requirement, not in the model. PaymentReceived and PaymentDeclined are OUR facts; PaymentGatewayResultReceived is the provider's."
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

      specification TranslatingADeclinedCardResult
        when capture PaymentGatewayResults
          reference = "9c1f0a52-6a4e-4b1c-9f55-0d2c7b8e1a01"
          outcome   = "D"
          reason    = "Card declined"
        then PaymentGatewayResultReceived
          for "9c1f0a52-6a4e-4b1c-9f55-0d2c7b8e1a01"
          outcome = "declined"
          reason  = "Card declined"
        then PaymentDeclined
          for "9c1f0a52-6a4e-4b1c-9f55-0d2c7b8e1a01"
          reason = "Card declined"

    slice Automation RecordPaymentOutcome
      reaction ReceivedTranslator
        when PaymentGatewayResultReceived
          outcome
          produces PaymentReceived
        where outcome == "captured"
      reaction DeclinedTranslator
        when PaymentGatewayResultReceived
          outcome
          reason
          produces PaymentDeclined
            reason = reason
        where outcome == "declined"
      event PaymentReceived
      event PaymentDeclined
        reason String

    slice StateView OpenInvoices
      description "Todo list: membership means 'still to collect'; there is no status flag."
      readmodel OpenInvoice
        invoiceId InvoiceId
        member    MemberId
        amount    Decimal
      query OpenInvoiceById => OpenInvoice optional
        by invoiceId InvoiceId
      query ListOpenInvoices => OpenInvoice[]
      projection OpenInvoices => OpenInvoice
        from InvoiceIssued
          invoiceId = $eventSourceId
          member    = member
          amount    = amount
        remove with PaymentReceived
        remove with InvoiceVoided
        remove with InvoiceSettled

      specification OpenInvoicesLifecycle1Empty
        given caller
          authenticated
          role "Accounts"
        then query OpenInvoiceById
          arguments
            invoiceId = "9c1f0a52-6a4e-4b1c-9f55-0d2c7b8e1a01"

      specification OpenInvoicesLifecycle2Issued
        given caller
          authenticated
          role "Accounts"
        given InvoiceIssued
          for "9c1f0a52-6a4e-4b1c-9f55-0d2c7b8e1a01"
          member = "6f1c2a8e-0b1d-4d55-9a3e-2f6a7c1d0e11"
          amount = 130
        then query OpenInvoiceById
          arguments
            invoiceId = "9c1f0a52-6a4e-4b1c-9f55-0d2c7b8e1a01"
          result
            invoiceId = "9c1f0a52-6a4e-4b1c-9f55-0d2c7b8e1a01"
            member    = "6f1c2a8e-0b1d-4d55-9a3e-2f6a7c1d0e11"
            amount    = 130

      specification OpenInvoicesLifecycle3Paid
        given caller
          authenticated
          role "Accounts"
        given InvoiceIssued
          for "9c1f0a52-6a4e-4b1c-9f55-0d2c7b8e1a01"
          member = "6f1c2a8e-0b1d-4d55-9a3e-2f6a7c1d0e11"
          amount = 130
        when append PaymentReceived
          for "9c1f0a52-6a4e-4b1c-9f55-0d2c7b8e1a01"
        then no readmodel OpenInvoice for "9c1f0a52-6a4e-4b1c-9f55-0d2c7b8e1a01"
        then query OpenInvoiceById
          arguments
            invoiceId = "9c1f0a52-6a4e-4b1c-9f55-0d2c7b8e1a01"

      specification OpenInvoicesLifecycle4Voided
        given caller
          authenticated
          role "Accounts"
        given InvoiceIssued
          for "9c1f0a52-6a4e-4b1c-9f55-0d2c7b8e1a01"
          member = "6f1c2a8e-0b1d-4d55-9a3e-2f6a7c1d0e11"
          amount = 130
        when append InvoiceVoided
          for "9c1f0a52-6a4e-4b1c-9f55-0d2c7b8e1a01"
          reason = "Issued in error"
        then no readmodel OpenInvoice for "9c1f0a52-6a4e-4b1c-9f55-0d2c7b8e1a01"
        then query OpenInvoiceById
          arguments
            invoiceId = "9c1f0a52-6a4e-4b1c-9f55-0d2c7b8e1a01"

      specification OpenInvoicesLifecycle5Settled
        given caller
          authenticated
          role "Accounts"
        given InvoiceIssued
          for "9c1f0a52-6a4e-4b1c-9f55-0d2c7b8e1a01"
          member = "6f1c2a8e-0b1d-4d55-9a3e-2f6a7c1d0e11"
          amount = 130
        when append InvoiceSettled
          for "9c1f0a52-6a4e-4b1c-9f55-0d2c7b8e1a01"
        then no readmodel OpenInvoice for "9c1f0a52-6a4e-4b1c-9f55-0d2c7b8e1a01"
        then query OpenInvoiceById
          arguments
            invoiceId = "9c1f0a52-6a4e-4b1c-9f55-0d2c7b8e1a01"

      specification OpenInvoicesListHoldsTheIssuedInvoice
        description "The unkeyed list query returns one row per open invoice; each row states the read-model identifier."
        given caller
          authenticated
          role "Accounts"
        given InvoiceIssued
          for "9c1f0a52-6a4e-4b1c-9f55-0d2c7b8e1a01"
          member = "6f1c2a8e-0b1d-4d55-9a3e-2f6a7c1d0e11"
          amount = 130
        then query ListOpenInvoices
          result
            invoiceId = "9c1f0a52-6a4e-4b1c-9f55-0d2c7b8e1a01"
            member    = "6f1c2a8e-0b1d-4d55-9a3e-2f6a7c1d0e11"
            amount    = 130

      specification OpenInvoicesListIsEmptyAfterPayment
        description "A paid invoice leaves the list: a query with no result block is the empty list."
        given caller
          authenticated
          role "Accounts"
        given InvoiceIssued
          for "9c1f0a52-6a4e-4b1c-9f55-0d2c7b8e1a01"
          member = "6f1c2a8e-0b1d-4d55-9a3e-2f6a7c1d0e11"
          amount = 130
        when append PaymentReceived
          for "9c1f0a52-6a4e-4b1c-9f55-0d2c7b8e1a01"
        then query ListOpenInvoices

    slice StateChange TopUpCredit
      description "Adds credit to a member. Why an amount above zero: a zero top-up changes nothing and hides a mistake."
      command TopUpCredit
        member MemberId identifier
        amount Decimal
        validate
          amount > 0 message "A top-up needs an amount above zero"
        produces event CreditToppedUp
          amount Decimal = amount

      specification ToppingUpCredit
        given caller
          authenticated
          role "Accounts"
        when TopUpCredit
          member = "6f1c2a8e-0b1d-4d55-9a3e-2f6a7c1d0e11"
          amount = 40
        then CreditToppedUp
          for "6f1c2a8e-0b1d-4d55-9a3e-2f6a7c1d0e11"
          amount = 40

      specification RejectingATopUpWithoutAnAmount
        given caller
          authenticated
          role "Accounts"
        when TopUpCredit
          member = "6f1c2a8e-0b1d-4d55-9a3e-2f6a7c1d0e11"
          amount = 0
        then error "A top-up needs an amount above zero"

      specification RefusingATopUpWithoutTheAccountsRole
        given caller
          authenticated
        when TopUpCredit
          member = "6f1c2a8e-0b1d-4d55-9a3e-2f6a7c1d0e11"
          amount = 40
        then denied

    slice StateView MemberCredit
      description "Running credit balance per member: every top-up adds to it."
      readmodel Credit
        member  MemberId
        balance Decimal
      query CreditByMember => Credit optional
        by member MemberId
      projection MemberCredit => Credit
        from CreditToppedUp
          member = $eventSourceId
          add balance by amount

      specification MemberCreditAccumulatesTopUps
        given caller
          authenticated
          role "Accounts"
        given CreditToppedUp
          for "6f1c2a8e-0b1d-4d55-9a3e-2f6a7c1d0e11"
          amount = 40
        when append CreditToppedUp
          for "6f1c2a8e-0b1d-4d55-9a3e-2f6a7c1d0e11"
          amount = 70
        then query CreditByMember
          arguments
            member = "6f1c2a8e-0b1d-4d55-9a3e-2f6a7c1d0e11"
          result
            balance = 110
```

## Notes
- The command that reverses a payment, the refund reaction and their specifications
  (`ReversingAReceivedPayment`, `RequestingARefundAfterAReversal`) are in
  `invoicing-dues-design.md`, because the command needs a stored-state rule.
- The query answers "absent" with no `result` block. The `ListOpenInvoices` list query and its two
  specifications bind and run (list queries bind on Screenplay 4.105.0 or later; they compiled but did
  not bind); only `filter`, `scoped to` and `performer` queries stay design-mode: see `view-and-story-specs.md`.
