<!-- cratis-ai-managed: skills/cratis-screenplay-toolchain/references/observation-comparison.md -->
<!-- Copyright (c) Cratis. All rights reserved. -->
<!-- Licensed under the MIT license. See LICENSE file in the project root for full license information. -->

# Observation comparison: model versus running store

Requires Cratis CLI 3.85.0 or later (`cratis --version`) for `cratis screenplay conform-namespace`, and
3.89.1 or later for the refused-read exit `4` and clean Ctrl+C handling described below; older CLIs lack
the verb. Without it, report "observation comparison not run" and do not substitute `conform`.

## Three comparisons, three questions

| Comparison | Compares | Answers | Does not answer |
| --- | --- | --- | --- |
| Model comparison (`semantic-diff`) | two authored models | what changed between revisions | whether anything runs |
| `cratis screenplay conform` | a model with source code | whether hand-written code realizes the slice | whether the deployed system did it |
| Observation comparison (`cratis screenplay conform-namespace`) | a compiled model with a bounded snapshot of one running Chronicle namespace | do the observed event relationships agree with the model | payload agreement, behavior, specifications passing |

Reach for observation comparison after a deploy, or when a namespace may have diverged from the
model: are the declared event types registered, were undeclared events appended, did each command
and reactor append what the model says it produces, and are the declared reactions subscribed.
It reads event metadata only, never payloads, event source ids or command values.

## Read the verdict

The host reports three verdict values and a separate completeness flag.

| `HasDrift` | Meaning | Action |
| --- | --- | --- |
| true | Known drift exists, even if other evidence is incomplete | Triage each Drift finding |
| false | The comparison was complete and found no drift | Report no drift, with the scope |
| null (omitted in JSON, `inconclusive` in human output) | Drift is neither established nor ruled out | Report inconclusive; never turn it into "no drift" |

`Complete` is true only when the scan covered the sequence fully **and** no finding is Unknown.
A report can have `HasDrift` true and `Complete` false.

## Finding outcomes

| Outcome | Reading |
| --- | --- |
| Drift | The store disagrees with the model on something the evidence establishes |
| Informational | A difference that is not a contradiction, such as a declared event that has not yet been appended |
| Unknown | The evidence cannot establish agreement or disagreement; the reason says why |
| Excluded | Outside this application, such as a foreign event or a platform consumer |

Typical Drift: a declared event type missing from the registry, an undeclared event appended,
an undeclared generation, a mapped producer appending outside its declared productions, or a
declared reaction with no subscription.

## Unknown reasons

| Reason | What to do |
| --- | --- |
| `PartialCoverage` | Widen the range (`--from`, `--to`, `--max-events`) and rerun |
| `UnattributedEvents` | Events without a command or reactor in their causation; the producer cannot be judged |
| `UnmappedProducer`, `UnmappedConstruct`, `AmbiguousIdentity` | The model name did not map to exactly one observed identity; do not guess one |
| `OpaqueReaction`, `ConditionalProduction`, `DescribedHandler`, `NonEventTrigger` | The model does not state the production unconditionally, so absence proves nothing |
| `ConsumerBehind` | The reactor has not reached the tail; wait for it and rerun |
| `ConsumerKindNotCompared` | Projections and reducers are not compared; their subscriptions are never evidence either way |

## Pitfalls

- "No matching appends within coverage" does not mean the command or reactor never ran. It is
  Informational: no attributed event shows that it ran inside the examined range.
- Scanned absence under partial coverage is Unknown, never drift. Registry checks are the
  exception: the event-type registry is read in full, so a declared event that is not registered
  (`EventNotRegistered`) is drift whatever the coverage. A missing production is drift only
  with full coverage, an unambiguous producer mapping, another event attributed to that producer,
  and no unattributed appends of the expected type.
- Presence is stronger than absence: an undeclared event that was appended is drift even under
  partial coverage.
- A clean result proves only the relationships it checked. Read the report's not-established
  statements; they list the limits.
- Event generations are approximated: each appended event counts against the highest registered
  generation of its type.
- One sequence is compared per run, `event-log` by default.

## Report it

State the verdict value, `Complete`, the event store, namespace and sequence, the examined range
against the captured tail, each Drift finding with its statement, and each Unknown reason. The
post-deploy procedure and its exit codes are in
[post-deploy check](../../cratis-screenplay-render-and-gap-fill/references/post-deploy-check.md).
Run the comparison through the CLI; no MCP tool offers it.
