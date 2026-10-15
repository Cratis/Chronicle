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

At Screenplay 4.125.0 these forms are authoring-only: binding refuses the whole
model with PLAY0268, even when the source is valid. Do not execute them as append
scenarios or silently drop branches. Sources: released `reactions.md`,
`specifications.md`, decision 0030.

Inside an `invokes` block, ordered selectors state intended recovery:

| Selector | Matches at future admission |
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
not a fictional caller supplied by `given caller`. On current source a declared
reaction `runs as` affects identity diagnostics, but refusal branches themselves
remain unadmitted; do not infer their execution from that separate capability.

## Branch values and acknowledgement

A branch contains leaf `acknowledge` alone, or ordinary `produces <Event>` blocks
with mappings and optional `for`. Empty branches, repeated acknowledgement,
acknowledgement plus productions, operations, inline events and attachments refuse.
Acknowledgement states a terminal disposition, not a successful command or fact.

Branch event mappings may use String values `$refusal.reason` (validation,
constraint, authorization), `$refusal.message` (verbatim details, including symbolic
localization keys), and `$refusal.constraint` only in constraint selectors.
Unknown members, wrong scope or incompatible targets report PLAY0541. Messages
are display details, not stable identity; trigger inputs remain available, read
aliases do not become branch inputs.

The intended admitted behavior stops remaining invocations of that trigger after
handling a refusal, but lets other reactions/cascades from accepted facts settle.
The refused command adds no facts; branch appends obey normal constraints and
cannot catch their own refusal through another branch. Unhandled refusal retains
earlier accepted facts. This is a future contract, not passing runner behavior.

## Redeliver an existing occurrence

The complete authoring model below compiles without warnings on 4.125.0;
`screenplay test` intentionally returns 3/unbound with PLAY0268 for redelivery
and no-event assertion, not a recovery pass:

```screenplay test=unbound
module Billing
  feature Claims
    slice Automation Recovery
      event Approved
        invoice String
      reaction Claimer
        when Approved
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
append. Intended delivery uses the existing occurrence, never appends it again.

Leaf `then no events` explicitly expects no new facts after a non-append action.
It may accompany view/query/response assertions, not events, any-order events,
error or denial. It is invalid after `when append` (PLAY0545), and remains
unadmitted even on a model without a refusal branch.

## Recovery boundaries

Exceptions, infrastructure/contract errors, Unsupported and concurrency conflicts
are not refusals. There is no `by concurrency`: transient contention fails/retries,
not acknowledgement. Ordinary redelivery is not replay. Neither these forms nor
an observer/occurrence locator prove runtime deduplication, a cascade-wide
transaction, once-only delivery or exactly-once external effects. Specify the
actual recovery owner and target enforcement before making those claims.
