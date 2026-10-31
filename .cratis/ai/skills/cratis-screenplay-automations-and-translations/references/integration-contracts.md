<!-- cratis-ai-managed: skills/cratis-screenplay-automations-and-translations/references/integration-contracts.md -->
# Integration contracts

## Contents

- Resolve only the unknowns
- Trust and meaning
- Trace both directions
- Separate the identities
- Publish the inbound contract
- Recoverable failure
- Faithful realization
- Per-record-type contract

Use before completing a new or changed translation. Keep accepted decisions in the relevant
`.play` `documentation` and specs; open questions and target requirements go to `STATE.md`. Do
not create a second canonical model.

## Resolve only the unknowns
For each in-scope source and record type:
| Question | Why it matters |
|---|---|
| Who sends it, through which channel? | Names the external authority and trust boundary. |
| What does a sanitized sample contain? | Fields, types, cardinality, missing values. |
| What business occurrence does it report? | Fact versus snapshot or transport status. |
| Which identifier finds our entity? | Correlation and event destination. |
| Is mapping direct, aggregated or enriched? | Reveals dependencies and incomplete-input cases. |
| What if required context arrives later? | Pending work, rejection or reconciliation. |
| Who handles records we cannot accept? | Failure recovery needs an owner. |
Ask the next question from the previous answer. Use synthetic samples; no credentials or
production personal data. Cite existing answers instead of re-asking. Resolve every cataloged
type or exclude it explicitly.

## Trust and meaning
Record which component verifies source authenticity, tenant/entity ownership, schema and
business preconditions. A `capture` source declaration proves none of these. If Screenplay
cannot express an acceptance rule, keep it as a named target requirement with its negative
case; do not invent syntax or call it enforced.

Per technical field: identity scope and provider vocabulary; units, currency, precision;
absent, null, malformed and unknown-code behavior; provider time versus receipt time versus
model occurrence time; personal-data purpose, consent and retention. `$context.occurred` in a
capture is the capture/scenario time: give a provider timestamp its own field and mapping.

## Trace both directions
Classify source fields (map, enrich, infer, ignore), then walk every required field of the
intended event and command:
| Target field or destination | Source | Transformation | Missing/invalid outcome |
|---|---|---|---|
Every target needs an available origin or a recorded blocker. A reaction's selected values
must exist on its trigger; `reads` does not make view data a portable mapping source; a clock
has no selected values. Inference needs a business-accepted rule, never a convenient default.
Prefer enrichment from established local facts; an outside lookup is its own dependency with
failure and freshness terms, not an imagined capture operation.

## Separate the identities
Name separately when they differ: our entity/event source; the provider's object reference;
the logical occurrence or revision; the delivery id; workflow correlation and immediate
cause. Create the correlation bridge when starting the outside interaction, or define how an
unsolicited record gets one; cover a record that arrives before the bridge. Define duplicates
by business occurrence, not by provider object id alone; state dedup scope, retention and
atomic enforcement (a preliminary lookup is not a concurrency guarantee).

## Publish the inbound contract
An external integrator cannot read this codebase, so publish the inbound endpoint's contract for
the provider's integrator: the path, the payload fields mapped from the capture, the
authentication header (the provider's signature or shared secret, not an application user
token), and the responses for a duplicate delivery and for each refusal. Answer a duplicate
as already handled, and keep the provider's delivery or occurrence id as the retry identity
rather than generating a new id per delivery, which would defeat deduplication.

## Recoverable failure
Per failure class choose: ignore, reject, defer, terminal failure or reconcile; say what is
visible, who acts, how processing resumes. Cover unknown correlation, unavailable enrichment,
invalid input, partial progress and uncertain external results. Logging is not recovery. A
resumed attempt keeps the logical work identity and idempotency key and does not repeat
effects that already succeeded.

## Faithful realization
The accepted `.play` revision is the behavioral contract. A realization may choose reactor,
scheduler or queue mechanism but may not drop pending visibility, add filters, guess fields
or weaken authorization. Specify event-to-command mapping at the reaction boundary; a
command-only spec does not exercise it. Unsupported realization is a gap routed to delivery,
not a silent model change.

## Per-record-type contract
For each external record type write one compact contract; accepted behavior then lives in the
`.play` descriptions and specifications, and checks Screenplay cannot express stay named target
requirements.
| Part | Content |
|---|---|
| Trigger | what arrives, over which channel |
| Acceptance preconditions | what must hold before it becomes our fact (correlation known, state allows it, consent, tenant) |
| Ordered translation steps | extract, correlate, validate, map, append |
| Success | exactly which fact of ours is appended, with which key |
| Failure handling | per failure class: reject, defer, failure fact or reconcile, who acts, how it resumes; never log-and-drop |

Filled example (invoicing, synthetic provider): `payment.settled`.
- Trigger: provider webhook with `paymentRef`, `amountMinor`, `currency`.
- Preconditions: a stored reference maps `paymentRef` to an invoice number; the invoice is issued and unpaid.
- Steps: extract `paymentRef`; look up the invoice; check currency and amount against the invoice;
  append `InvoicePaymentSettled` keyed by the payment occurrence.
- Success: one `InvoicePaymentSettled`; a second delivery of the same occurrence appends nothing new
  (atomic `unique event`, not a preliminary lookup).
- Failures: unknown `paymentRef` -> failure fact `PaymentReferenceUnknown`, finance reviews and
  resumes with the same occurrence id; invoice not issued -> deferred until the later `InvoiceIssued`
  for that number retries the same occurrence, and finance reconciles it if no invoice is issued
  within the agreed window; currency mismatch -> failure fact `PaymentCurrencyMismatch`, finance
  decides and resumes with the same occurrence id; amount mismatch on an issued, unpaid invoice (underpayment, partial payment or overpayment)
  -> failure fact `PaymentAmountMismatch` carrying expected and received minor units, finance
  decides (request the balance, accept as a partial payment against the invoice, or refund the
  excess) and resumes with the same occurrence id, so a settled amount is never guessed; invoice already paid (a distinct payment occurrence, double payment or overpayment) ->
  failure fact `PaymentReceivedForSettledInvoice`, finance decides on a refund and resumes with the
  same occurrence id; signature check -> target requirement at the gateway with its negative case.
