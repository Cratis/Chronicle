---
title: Query open alert incidents
description: Look up recorded open observer incidents within an affected event store, with bounded paging and sampled materialization health.
---

Use the alert incident queries to inspect recorded open incidents without reading the full System event sequence. Every query requires the **affected event store**, even though Chronicle stores the incident rows alongside the transition history in the System store's Default namespace.

These queries use the kernel's normal authenticated query surface. They do not send notifications, expose an unscoped enumeration, or add incident gauges. Recorded incidents are neither a notification outbox nor a complete failure audit; see [recorded incidents](/chronicle/hosting/alerting-on-observer-failures/#understand-recorded-incidents).

## Query surface

| Query | HTTP route | Arguments | Result |
| --- | --- | --- | --- |
| `GetOpenIncidents` | `/api/alerts/get-open-incidents` | Required `eventStore`; optional `namespace`, `observerId`, `condition`, `minimumSeverity`, `limit`, and paired continuation fields | `status`, `items`, `next` |
| `GetOpenIncident` | `/api/alerts/get-open-incident` | Required `eventStore` and `incidentId`; optional `namespace` | `status`, `incident` |
| `GetOpenIncidentCounts` | `/api/alerts/get-open-incident-counts` | Required `eventStore`; optional `namespace` | `status`, `counts` |

Omitting `namespace` includes all namespaces of the affected store. Supplying `Default` selects only that namespace. Scope names match case-sensitively. Chronicle applies scope and filters in storage before limiting a page, looking up an identity, or counting rows.

An incident contains its identity, affected target, recorded condition, severity and evidence, persisted raise and last-change times, and raise and last-transition sequence numbers. The target's partition is preserved verbatim: a partition named `[none]` remains a partition alert when the recorded condition is partition-specific. Unknown condition strings are retained.

Counts cover the **complete** matching open population, grouped by affected namespace, condition and severity. They are not counts of the current page. A Ready lookup with `incident` absent is normal not-found, including an incident that has cleared.

## Read the status before interpreting an empty result

| Status | Meaning |
| --- | --- |
| `CatchingUp` | The incidents reactor is not Active, has catch-up or replay work, or its next position has not passed the sampled alert-transition tail. |
| `Ready` | The reactor is Active, has no catch-up, replay or failed partitions, and its next position has passed the sampled transition tail (or there is no transition history). |
| `Degraded` | The incidents reactor has a failed or quarantined partition. |

CatchingUp and Degraded responses can still contain partial or stale rows. Do not interpret an empty non-ready response as proof that no incidents exist. Storage and observer-state read failures fail the query; they do not produce a zero count or a Ready-empty result.

Ready is a **sampled health check**, not a contiguous checkpoint. Normal in-flight live batches do not prevent Ready. The sample reads the transition tail, reactor state, and reactor failures in that order, and concurrent events can arrive between those reads.

## Page with both continuation fields

Pages sort ascending by `(raisedSequenceNumber, incidentId)`. The identity tie-breaker uses its canonical lowercase, 32-character string in ordinal order. The default limit is 100; supplied limits are bounded to 1–500.

When `next` is present, pass its `raisedSequenceNumber` as `afterRaisedSequenceNumber` and its `incidentId` as `afterIncidentId` on the next request, keeping the scope and filters unchanged. Both fields must be supplied together. A half-specified continuation or sentinel sequence is rejected rather than restarting traversal. Sequence numbers follow the existing numeric client representation; JavaScript callers must account for its integer precision limit.

There is no snapshot across pages or between pages and counts. A later raise can reopen a closed incident and reset its raise position, moving it to another page. Concurrent changes can therefore make an incident appear again or disappear during traversal.

## Consistency and retention

Incident materialization follows the recorded raise, escalation and clear events asynchronously through a kernel reactor. Independent observer partitions, including targets in different stores, can be at different progress points. A page combines those points; a count sampled separately can differ from the page.

Clearing retains a closed tombstone with its last transition position. Closed rows are excluded from every public read, but prevent a delayed raise from resurrecting an incident. Tombstone retention is deliberate and currently unbounded; there is no compaction API. Incident rows, System transition history and reactor state share namespace storage so kernel-state reset and backups keep their scope together.

Native catch-up materializes existing history on first deployment. A rebuild or repair operation is not included; that work is tracked in [#4507](https://github.com/Cratis/Chronicle/issues/4507).
