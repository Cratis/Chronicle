<!-- cratis-ai-managed: skills/cratis-screenplay-scenario-coverage/references/scenario-examples.md -->
# Scenario examples, category by category

## Contents

- Good and bad forms
- 1. Command: success, rule rejection and denial
- 2. Command: state violations
- 3. External failure: the capture, then the retry
- 4. Compensation
- 5. Views: population, update, accumulation
- 6. Views: list contents and the empty list

Compact examples for each scenario category, in one invoicing domain so the shapes compare
easily. Every excerpt below is copied verbatim from the complete document in
`invoicing-dues-example.md`, which compiles with warnings as errors and whose 23 specifications
pass in the reference evaluator; it also holds the declarations the excerpts depend on. The two
compensation excerpts in section 4 come from `invoicing-dues-design.md` instead: that document
holds the stored-state rules (`reads` + `require`), compiles but does not bind (PLAY0271,
PLAY0268), so its specifications are design mode and do not run. A second
complete example, with competing claims and denial fixtures, is `berth-reservations.md`. The category structure and the good/bad contrast follow the worked
examples in `eventmodeling-elaborating-scenarios` by TrogonStack and by Martin Dilger and Nebulit GmbH (`provenance.md`), translated to
Screenplay specifications.

## Good and bad forms
| Part | Good | Bad |
|---|---|---|
| Given | the facts that exist, each with its `for` source: `given InvoiceIssued` with `for "<invoice id>"` | "given an invoice" with no fact, no source |
| When | one action with every input stated | an action with inputs left to defaults |
| Then (success) | every expected event with payload and an explicit `for`, including events the command declares inline (the inline default applies to the `produces event` declaration, not to the expectation) | "an event is produced" |
| Then (refusal) | `then error "Invoice already settled"`, the rule's own message | `then error` with no message, or `then error ""` |
| Then (denial) | `then denied` | `then error` for a caller who is not allowed |
| Description | the behavior witnessed, or an outside condition or regulation | `description "customer already ordered twice"` with no givens (write two `given OrderPlaced` with the same `for`) |
| Values | the deciding values stated literally, others from a typed `example` | every field filled with noise, so the deciding value hides |
| Reason | slice `documentation` says why the rule exists | the rule appears only in a spec title |

## 1. Command: success, rule rejection and denial
One spec for the success, one per rule (varying only the value the rule checks), one denial.
```screenplay excerpt parent=references/invoicing-dues-example.md#1
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
```

## 2. Command: state violations
After which earlier facts must the command be refused? Establish the wrong state with its real
facts, not a description of it. Settling a voided invoice and voiding a settled one are different
questions: one spec each.
```screenplay excerpt parent=references/invoicing-dues-example.md#1
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
```
A rule over stored state that no constraint can hold (for example, "the invoice must exist") is
written as `reads <View>` + `require ... message`, marked NOT enforced with its target, given a
design-mode rejection spec and `recorded` in the matrix. See `RejectingSettlementOfAnUnknownInvoice`
in `invoicing-dues-design.md`; it is never prose and never a spec that pretends to enforce it.

## 3. External failure: the capture, then the retry
Two separate obligations. First, the **capture specification**: concrete external input in, our
facts out. The provider's refusal is a fact (never a `then error`), kept in the provider's
vocabulary and then translated.
```screenplay excerpt parent=references/invoicing-dues-example.md#1
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
```
Second, the **retry**. `PaymentRequested` only says we asked the gateway; it is not the outcome.
After the decline, requesting again is an ordinary accepted command, and its exact declared
outcome is `PaymentRequested`, not `PaymentReceived`: the eventual success is a later external
fact that arrives through the capture, not something this command produces.
```screenplay excerpt parent=references/invoicing-dues-example.md#1
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
```
A view that shows the decline (a failure view) is a third, separate specification of a read
model; it tests the projection only, never the translation.

## 4. Compensation
A reversal needs a paid history, the reversal action and the exact consequence. The command spec
lists the declared consequence (`RefundRequested`); the reaction is also specified on its own.
`RefundRequested` is a recorded request, not proof that money moved, and there is no
`then <Command>` form. What the target must do to deliver the refund is a requirement, not a spec.
From `invoicing-dues-design.md` (design mode; the specs do not run):
```screenplay excerpt parent=references/invoicing-dues-design.md#1 expect PLAY0516 test=unbound
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

specification RequestingARefundAfterAReversal
  when append PaymentReversed
    for "9c1f0a52-6a4e-4b1c-9f55-0d2c7b8e1a01"
    amount = 130
  then RefundRequested
    for "9c1f0a52-6a4e-4b1c-9f55-0d2c7b8e1a01"
    amount = 130
```
If nothing in the model can reverse a fact the business can reverse, the model lacks an event:
ask. Voiding an issued invoice that is not yet paid is the other compensation (the `VoidInvoice` rule in `invoicing-dues-design.md`; `SettleOrVoid` only keeps settle and void exclusive).

## 5. Views: population, update, accumulation
A repeated event type can matter as much as a multi-event lifecycle. Two top-ups of 40 and 70
walk a balance to 110; give the second occurrence as the action so the accumulation is what is
tested.
```screenplay excerpt parent=references/invoicing-dues-example.md#1
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

## 6. Views: list contents and the empty list
A list query asserts one `result` block per expected row: count is exact, but rows
match by read model and key regardless of order. The
empty-list intent is a separate claim from a keyed row being absent (`view-and-story-specs.md`
"Removal is a positive obligation"). The backing example proves the keyed forms only; whole-list
queries are executable too (`ListOpenInvoices` in the backing example); caller-supplied equality filters on collections bind as ESM v14.
Pass `by` and filters in query arguments; PLAY0468 reports unknown, repeated or
missing required arguments. Optional filters may be omitted, supplied null is refused.
Context-sourced/single-result filters, opaque `scoped to`/`performer`, and composite
`by` remain PLAY0268. Compiled coverage and renderer ceilings:
read-surface [equality filters](../../cratis-screenplay-read-surface/references/equality-filters.md).
