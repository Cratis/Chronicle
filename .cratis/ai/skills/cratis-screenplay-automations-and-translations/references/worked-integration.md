<!-- cratis-ai-managed: skills/cratis-screenplay-automations-and-translations/references/worked-integration.md -->
# Worked example: a payment provider into invoicing

An integration walked end to end, in the order of the interview: catalog, correlation,
translation rules, failure and recovery, result. The domain is invoicing; the outside system is a
payment provider. Every provider name, field and sample below is synthetic. The structure follows
the TrogonStack translating-external-events skill (MIT; see `references/provenance.md`); the
analysis is adapted to Screenplay `capture`, translator `reaction` and `constraint`.

## 1. Catalog (interview question 1)
The user said only "the provider tells us when invoices are paid". Not enough to skip the
interview: no event types, no samples, no correlation. Asked: which systems, which record types,
what each carries, how it arrives, who authenticates it.

| Source | Record type | Channel | Business occurrence | Sample fields (synthetic) |
|---|---|---|---|---|
| PayCo | `payment.settled` | webhook | money reached our account | `id` "evt_01", `payment` "pay_77", `reference` "INV-2026-0042", `amountMinor` 15000, `currency` "NOK", `settledAt` 1790000000 |
| PayCo | `payment.failed` | webhook | attempt did not settle | `id`, `payment`, `reference`, `reason` "card_declined" |
| PayCo | `payment.refunded` | webhook | money returned | `id`, `payment`, `refund` "re_5", `reference`, `amountMinor` |
| PayCo | `dispute.opened` | webhook | cardholder disputes | `id`, `payment`, `deadline` |
| PayCo | `payout.created` | webhook | provider pays us out | `id`, `batch` |

Follow-ups asked next, each from the previous answer: which header carries the signature, and who
verifies it (answer: the gateway in front of the app: a target requirement); do we know `reference`
(yes: we send our invoice number when creating the payment: we start the interaction); what happens
if two refunds for one payment arrive (both legitimate: a refund is not a duplicate of the first). Does a refund carry its own stable id, the same on every redelivery? (yes: `refund`; confirm with the provider, because the whole dedup rests on it).
`payout.created` has no consumer: excluded explicitly with that reason, not silently dropped.

## 2. Technical analysis
| Technical fact | Problem if used directly | Decision |
|---|---|---|
| `amountMinor` integer minor units | unit and scale are implicit | translate to our `Money` with currency; unsupported currencies are a recorded failure |
| `settledAt` Unix seconds | provider time is not our occurrence time | own field `settledAt`; `$context.occurred` stays the capture time |
| `payment` "pay_77" | provider identity, not ours | kept as `paymentRef` property for refund correlation; never an event source id |
| `reference` "INV-2026-0042" | echo of our value | capture `key`: lands the record on the invoice's stream |
| `id` "evt_01" | delivery id, not the occurrence | never the dedup key: it may change on redelivery, and one payment has several occurrences |
| `refund` "re_5" | provider's identity of one refund occurrence | stable occurrence key for refunds; separate from the delivery id and from `payment` |

## 3. Correlation
We create the bridge when the payment is started: the command that starts it allocates the invoice
stream id, and the realization sends it as the provider's `reference`. The echo is the capture
`key`. The record that arrives before the invoice exists is a named case: park, reject or record an
orphan fact (decided with the expert: record a `PaymentWithoutInvoiceReceived` fact for follow-up).

## 4. Translation rules (target-field completeness)
| Our event | Field | Source | Transformation | Missing or invalid |
|---|---|---|---|---|
| `InvoiceSettled` | `invoiceId` (stream) | `reference` | key | unknown: orphan fact |
| | `amount` | `amountMinor`, `currency` | scale by currency | unknown currency: failure fact |
| | `settledAt` | `settledAt` | seconds to instant | missing: reject record, failure fact |
| | `paymentRef` | `payment` | as is | missing: failure fact |
| `PaymentAttemptFailed` | `reasonCode` | `reason` | translate known codes | unknown code: failure fact, never a default |
| `InvoicePaymentRefunded` | `refundRef` (stream) | `refund` | capture `key` of the refund capture: each refund occurrence is its own stream | missing: failure fact |
| | `invoiceNumber`, `amount`, `paymentRef` | `reference`, `amountMinor`, `payment` | as above | refund above settled: cross-stream rule, review case (see section 5) |

Every target field has an origin or a recorded blocker. Source fields not in the table are
dispositioned: `dispute.opened.deadline` is kept for a follow-up automation; `batch` is ignored.

## 5. Scenarios per record type
Happy path; arrives before the invoice; duplicate delivery of the same occurrence; two distinct
refunds; settled then failed out of order; unknown currency; unknown reason code; amount changed
with the same status; malformed record; consent or tenant mismatch (enforced at the gateway:
named, with its negative case). The full catalog is `references/cases-to-specify.md`.

**Refund dedup.** A single `unique event` on the invoice stream would refuse the second legitimate
refund, and change detection per invoice key cannot tell an identical second refund from a retry.
So the refund capture is keyed by the occurrence (`key refund`): every refund is its own stream,
`unique event InvoicePaymentRefunded` on that stream is the atomic dedup, and the invoice is
referenced by a property. Cases to specify, with identical payment and amount in both:
- `re_5` delivered twice: the second delivery appends nothing new (the first fact is already there).
- `re_5` and `re_6` for the same payment and amount: two refund facts, both kept.
The cost is that "refunds never exceed the settled amount" now spans streams; it is a rule for
`cratis-screenplay-streams-and-consistency`, not something this dedup provides. Reserve
`unique event` on the invoice stream for facts allowed once per invoice (`InvoiceSettled`).

## 6. Failure and recovery (owner per class)
| Class | Visible as | Owner | Resumes by |
|---|---|---|---|
| unknown correlation | orphan fact in a review list | finance clerk | linking the invoice, then re-translating |
| unknown currency or code | failure fact | integration owner | adding the mapping; the record is replayed from the failure fact |
| duplicate delivery | nothing new | none | occurrence-keyed stream (settlement: invoice stream) plus `unique event` |
| signature invalid | refused before the app | gateway owner | operational alert |

Logging alone is not recovery: each row has an owner and a way back.

## 7. Result as the skill reports it
Hypothetical packet: no PayCo model exists in this example, so no verdict below is evidence.
```text
Integration: PayCo -> invoicing    Types cataloged: 5 (4 translated, 1 excluded: payout.created, no consumer)
payment.settled    complete: correlation (own reference, echoed), target fields traced, 6 cases named (3 specified)
payment.failed     open: reason codes beyond card_declined unknown (question for the integration owner)
payment.refunded   open: refund-above-settled rule undecided
dispute.opened     blocked: no consumer decided
Target requirements: signature verified before the app; idempotent webhook acknowledgement
Verdicts: V1 not run (no PayCo model written), V3 not run (same), V4 not run: no route
```
The model for the settled record is `translate-example.md` in miniature: correlation key, an
external fact in provider vocabulary, translator reactions that record our facts, a constraint on
our fact.
