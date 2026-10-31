<!-- cratis-ai-managed: skills/cratis-screenplay-captures-and-reactions/references/refusals-and-redelivery.md -->
<!-- Copyright (c) Cratis. All rights reserved. -->
<!-- Licensed under the MIT license. See LICENSE file in the project root for full license information. -->

# Refusal handling and redelivery intent

## Contents

- Select a refusal
- Branch values and acknowledgement
- Redeliver an existing occurrence
- Recovery boundaries

## Select a refusal

Refusal branches and `$refusal` values bind and reference-execute as ESM v19.

Requires Screenplay 4.160.0 or later (`screenplay --version`) for refusal execution; older compilers report PLAY0268.

Inside an `invokes` block, ordered selectors state intended recovery:

| Selector | Matches |
| --- | --- |
| `on refused` | Validation or constraint, not authorization |
| `on refused by validation` | Input/concept/require rejection |
| `on refused by constraint` | Any constraint rejection |
| `on refused by constraint <Name>` | One unambiguously resolved applicable constraint |
| `on refused by authorization` | Unauthorized, never an opaque policy's Unsupported result |

The first matching selector wins. Repeated selectors or narrower branches after
covering branches report PLAY0540; bare refusal does not cover authorization.
Unknown named constraints report PLAY0542; known but inapplicable constraints
also report PLAY0540. Authorization recovery requires a deliberate trusted path,
not a fictional caller supplied by `given caller`. Declare the reaction's
trusted actor with `runs as` when its invoked command requires one.
Mixed refusals classify as authorization first, then constraint, then validation.

## Branch values and acknowledgement

A branch contains leaf `acknowledge` alone, or ordinary `produces <Event>` blocks
with mappings and optional `for`. Empty branches, repeated acknowledgement,
acknowledgement plus productions, operations, inline events and attachments refuse.
Acknowledgement records that a refusal was handled, not that work completed.

Branch event mappings may use String values `$refusal.reason` (validation,
constraint, authorization), `$refusal.message` (error messages joined with a newline
verbatim, including symbolic localization keys), and `$refusal.constraint` (the first
violated constraint's name) only in constraint selectors.
Unknown members, wrong scope or incompatible targets report PLAY0541. Messages
are display details, not stable identity; trigger inputs remain available, read
aliases do not become branch inputs.

A handled refusal stops remaining invocations of that trigger, while other
reactions/cascades from accepted facts continue. The refused command adds no facts;
branch appends obey normal constraints. A rejected branch append ends the scenario,
not another refusal branch. Unhandled validation/constraint refusals remain assertable
with `then error`; earlier accepted facts remain.

This complete model records a constrained duplicate as a refusal fact. Run
`screenplay test <root>`: exit 0 with one passed scenario.

```screenplay
module Billing
  feature Claims
    slice Automation Recovery
      event Approved
        invoice String
      event Claimed
      event Refused
        reason String
        constraint String
        message String
      command Approve
        id String identifier
        invoice String
        produces Approved
          for id
          invoice = invoice
      command Claim
        id String identifier
        produces Claimed
          for id
      constraint OneClaim
        unique event Claimed
        message "Already claimed"
      reaction Claimer
        when Approved
          invoice
          invokes Claim
            id = invoice
            on refused by constraint OneClaim
              produces Refused
                reason = $refusal.reason
                constraint = $refusal.constraint
                message = $refusal.message
            on refused
              acknowledge
      specification RecordingTheDuplicate
        given Claimed
          for "invoice-1"
        given Approved
          for "invoice-1"
          invoice = "invoice-1"
        when redelivered Approved to Claimer
        then Refused
          for "invoice-1"
          reason = "constraint"
          constraint = "OneClaim"
          message = "Already claimed"
```

## Redeliver an existing occurrence

Recovery redelivery and explicit no-event assertions bind and reference-execute as ESM v17.

Requires Screenplay 4.157.0 or later (`screenplay --version`); older compilers report PLAY0268.

Run `screenplay test <root>` on this complete model: exit 0 with one passed scenario.

```screenplay
module Billing
  feature Claims
    slice Automation Recovery
      event Approved
        invoice String
      event Claimed
        invoice String
      command Approve
        id String identifier
        invoice String
        produces Approved
          for id
          invoice = invoice
      reaction Claimer
        when Approved
          invoice
          produces Claimed
            for invoice
            invoice = invoice
        where invoice == "invoice-2"
      specification Recovery
        given Approved
          for "invoice-1"
          invoice = "invoice-1"
        when redelivered Approved to Claimer
          for "invoice-1"
          invoice = "invoice-1"
        then no events
```

`to <Reaction>` must resolve to one observer of the event (PLAY0544). Optional
`for`, payload values and stream/no-stream route narrow effective given occurrences;
exactly one definitely matching occurrence and no unknown candidates are required
(PLAY0543). Equal duplicate givens are still separate candidates. A locator with
no route is a wildcard; explicit stream ids compare canonically, and `no stream`
selects only unrouted givens. This is the sole action, not an extra action after
append. Givens establish state without firing reactions; delivery uses the existing
occurrence only for the named reaction, honors its `from`/`where` filters, then
settles the cascade. It never appends the input again.

Leaf `then no events` explicitly expects no new facts from the action and its
cascade. After `when append`, it excludes the input fact and asserts nothing
followed. It counts as a success outcome and may accompany view/query/response
assertions, not events, any-order events, error or denial (PLAY0545). Omitting
event expectations is not this explicit assertion.

## Recovery boundaries

Exceptions, infrastructure/contract errors, Unsupported and concurrency conflicts
are not refusals. There is no `by concurrency`: transient contention fails/retries,
not acknowledgement. Ordinary redelivery is not replay. Neither these forms nor
an observer/occurrence locator prove runtime deduplication, a cascade-wide
transaction, once-only delivery or exactly-once external effects. Specify the
actual recovery owner and target enforcement before making those claims.
