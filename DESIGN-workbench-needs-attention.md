# Workbench Needs Attention — Trustworthy Operational Home

**Status:** proposed  
**Profile:** Chronicle framework  
**Related:** `PLAN-workbench-product.md`, `WORKBENCH-OPERATOR-ACTION-INVENTORY.md`,
`DESIGN-workbench-operator-actions.md`

---

## 1. Product promise

The long-term Needs Attention home answers:

- What explicitly requires attention now?
- What evidence supports that statement?
- Is the evidence fresh and complete?
- Where should the operator investigate?
- Is there a safe action, or is no action currently justified?

It does not claim general infrastructure health. It reports Chronicle-owned state.

The first screen uses:

```text
Known issue → evidence → drill-through → safe action or explicit no-action
```

---

## 2. Trust language

Workbench must distinguish:

- **Needs attention:** current source state explicitly indicates a problem.
- **In progress:** recovery/replay/Job is active.
- **Recommendation:** Chronicle suggests review or action.
- **No known issues:** current required feeds contain no active issue.
- **Insufficient data:** required evidence is absent, stale, disconnected, or unsupported.

Do not display “all systems healthy” merely because arrays are empty. Empty plus fresh/complete can
mean no known Chronicle issues; empty plus missing feeds is insufficient data.

---

## 3. Phase 1 — Failed Partition Monitor

The smallest trustworthy release is explicitly named **Failed Partition Monitor**, not Needs
Attention or System Health. It makes no completeness claim outside this one signal family and does
not start as a client-side merge of unrelated observable sources.

Use the existing namespace-scoped `AllFailedPartitions` observable; do not add a duplicative report
until multiple source families require one.

**Precondition:** repair and contract-test every provider.
At the committed baseline, `Storage.Sql/.../FailedPartitionStorage.ObserveAllFor()` emits an initial
empty value, then one asynchronous snapshot, and never publishes later changes. The uncommitted
candidate replaces it with the existing cluster-visible SQL `LiveQuery` polling mechanism and proves
save, resolve, and external-database updates. The monitor still does not ship until InMemory, MongoDB,
and SQL pass one shared initial/add/update/remove/filter/cancellation contract.

Present attention items for:

| Source state | Category | Default presentation |
|---|---|---|
| `IsQuarantined == true` | Needs attention | Critical; automatic retry has stopped |
| `IsQuarantined == false` | Needs attention | Warning; automatic recovery may still succeed |
| `IsQuarantined == null` or missing | Insufficient data | State unsupported by the connected Kernel/version |

The existing `FailedPartition.Id` is stable for the life of one failure registration and changes on
a later failure cycle after resolution. The canonical global UI identity is:

```text
failed-partition:{eventStore}:{namespace}:{failedPartition.id}
```

Event store and namespace use their canonical case-sensitive values. The raw partition value is not
part of the ID, avoiding unbounded or ambiguous identifiers. Specify and test collision behavior.

Display order is deterministic:

1. quarantined before automatically retryable, with Unknown in a separate insufficient-data group;
2. oldest source-backed last-attempt occurrence first;
3. canonical stable ID ordinal as the tie breaker.

Evidence-only updates retain the ID and do not reorder unless a declared sort key changes.

`IsQuarantined` is a nullable additive protobuf scalar. The uncommitted working-tree schema uses
proto3 `optional` field presence: omitted from an older Kernel means Unknown, while explicit `false` means automatic
retry remains allowed. Cross-version protobuf round-trip and Workbench missing-field specs gate this
behavior.

This MVP introduces no universal lag, attempt-count, client-staleness, or duration thresholds. It
does not persist dwell/hysteresis in browser memory.

After failed-partition add/update/remove/reconnect semantics are proven, add other explicit source
families incrementally:

1. quarantined observers;
2. explicitly failed/partially failed Jobs;
3. recommendations after severity/target/evidence are added;
4. in-progress recovery operations and Jobs;
5. disconnected/lag/stall conditions only after temporal semantics exist.

Severity mappings are published semantics for explicit states, not metric thresholds.

### 3.1 Backlog and lag

Workbench may calculate and display:

```text
max(0, tail sequence number - last handled sequence number)
```

as neutral evidence.

It must not classify the observer as slow, stalled, or critical without temporal progress, expected
rate/SLO, dwell, and a clear condition. A million-event backlog may be normal during a fast replay;
a hundred-event backlog may be severe for a low-volume observer that has stopped.

### 3.2 Recommendations

Recommendations remain outside the MVP severity queue because the current contract has no severity,
target resource, correlation key, trigger condition, or clearing condition.

Do not assign all recommendations Warning severity. Add those fields before merging recommendations
into the primary severity queue.

---

## 4. Information architecture

### 4.1 Header

The Phase 1 monitor header shows:

- selected event store and namespace;
- active, quarantined, and unknown-state failed-partition counts;
- first-payload/loading state;
- observable transport live/disconnected state;
- time the displayed payload was received;
- an explicit label: **Failed partitions only — not overall system health**.

It does not claim overall evidence completeness.

### 4.2 Main sections

The Phase 1 monitor has:

1. **Failed partitions needing attention** — quarantined then automatically retryable;
2. **Feed state** — loading/live/disconnected/failed;
3. **Recent failed-partition recovery operations** — added when the action history exists.

Recovery, recommendations, observer issues, and broader recent operations appear only as their
server reports and semantics mature.

### 4.3 Item anatomy

Every item contains:

- textual severity/category; color is secondary;
- stable source id;
- source kind;
- concise title;
- evidence summary;
- exact event store/namespace/resource;
- occurred/first-observed time where source-backed;
- source freshness;
- drill-through URI;
- supported action/no-action status;
- reason why an action is unavailable.

Raw stack traces and event content are progressively disclosed, permission-gated, and never placed in
list rows.

### 4.4 Navigation

Deep links preserve event store, namespace, source resource, absolute time range, and selected
evidence. A failed-partition item opens its current detail view. Observer and Job items open their
existing feature pages before a combined investigation workspace is introduced.

---

## 5. Phase 1 view model

Phase 1 keeps the generated `FailedPartition` read model as the source and adds Workbench-owned pure
presentation helpers for canonical ID, status, evidence summary, target URI, and deterministic
ordering.

Arc Delta reconciliation continues to use the generated `FailedPartition.id`. The canonical global
identity is used for view keys, links, operation targets, and future unified reports. Severity/status
are never part of either identity.

Helpers have BDD specifications and never perform commands. A server-owned `NeedsAttentionItem`
contract is introduced only when the second source family is added and a unified report provides
clear value.

---

## 6. Freshness and failure states

Phase 1 has one feed:

- **Loading:** no first payload yet.
- **Live:** payload received and observable transport is active.
- **Disconnected:** last known payload is retained and marked stale immediately when transport loss
  is known.
- **Failed:** query returned an error.

The current contract has no source timestamp or delivery SLA, so Phase 1 does not invent a
time-based stale threshold. It publishes this limitation. Direct action remains unavailable until an
authoritative server preflight succeeds.

---

## 7. Actions from the monitor

Phase 1 favors drill-through. It does not add direct mutation buttons merely because the existing
pages have them.

Direct actions are introduced only after `DESIGN-workbench-operator-actions.md` is implemented:

- effective capability available;
- current preflight available;
- structured reason captured;
- durable Operation ID returned;
- Job/verification navigation available.

“No action,” “wait for automatic retry,” “insufficient evidence,” and “escalate” are first-class
outcomes.

---

## Appendix A — deferred server-owned operational issues

Browser-local threshold state cannot become operational truth. Phase 2 adds a Kernel-owned issue
lifecycle for heuristic conditions that require dwell, hysteresis, suppression, and clearing.

### 8.1 `OperationalIssue`

A durable issue contains:

- stable id and rule id/version;
- target resource;
- severity;
- status: `Active`, `Suppressed`, `Resolved`;
- first detected, last observed, last changed, resolved timestamps;
- structured evidence;
- trigger and clear explanations;
- insufficient-data/suppression reason;
- related recommendation and operation ids.

Explicit source records such as failed partitions remain authoritative. Do not duplicate their full
attempt history into issue events.

### 8.2 Rules

A rule definition contains:

- rule id and version;
- signal kind and scope;
- minimum sample count;
- trigger condition;
- trigger dwell;
- clear condition;
- clear dwell;
- severity/elevation conditions;
- missing-data behavior;
- restart/deployment grace behavior;
- human-readable explanation template.

Narrower configured scope can override broader scope, but every supported scope level is explicit.
Do not apply partition-level settings to a store-level fact.

Hysteresis is mandatory for metric-derived issues. Trigger and clear thresholds are separate.
Grace periods expire into a visible “predates restart” issue; they never hide a persistent fault
forever.

### 8.3 Rollup

Above a configured cardinality ceiling, roll up issues by rule and parent target while retaining:

- exact affected count;
- total evaluated count;
- top bounded examples;
- link to the complete filtered list.

Do not drop the remainder or approximate totals.

---

## Appendix B — deferred additive contracts

### Observer evidence

Add source-backed fields rather than a precomputed unexplained health score:

- `SampledAt`;
- `LastProgressedAt`;
- current owner/client freshness;
- explicit subscription state;
- replay/catch-up progress where available;
- safe last-failure reference, not raw stack trace.

Keep sequence positions and handled count. Derive rates from documented windows in the Kernel/metrics
layer, not from arbitrary browser refresh intervals.

### Job evidence

Add:

- progress `LastUpdated`;
- operation id where the Job was operator initiated;
- terminal failure code distinct from display message;
- resumability and cancellability capabilities.

### Recommendation evidence

Add:

- severity;
- target resource;
- correlation/dedup key;
- trigger and clear explanation;
- supported action kind;
- evidence links.

### Unified query

After server issue semantics exist, add an observable `AllNeedsAttention` read model with stable
`id`. It combines explicit source states, operational issues, recommendations, and operation
progress while preserving source links.

This reduces multiple high-cardinality SSE streams and gives CLI/MCP the same ordered report.

---

## Appendix C — deferred metrics and OpenTelemetry

Chronicle emits Chronicle-owned metrics; Workbench does not become a time-series database.

Required instruments are driven by product questions:

- observer backlog gauge with event store/namespace/observer dimensions reviewed for cardinality;
- observer progress and last-progress age;
- observer processing duration/rate with a documented window;
- Job duration and progress age;
- active failed/quarantined partition counts;
- active operational issues by rule/severity;
- operator-action duration and verification outcome.

Never label metrics with partition values, event-source ids, actor subjects, reason text, stack
traces, or event content.

External Prometheus/APM systems own long-term time-series storage and alert delivery.

---

## 11. SSE and payload budgets

Arc observable queries use Delta transfer and require stable `id` properties. Design and tests must
measure, not estimate:

- initial snapshot bytes;
- idle bytes per minute;
- delta bytes per source/item kind;
- client reconciliation time;
- render time;
- reconnect burst behavior;
- fan-out by namespaces and open tabs.

No hardcoded 2–5 second full refresh is introduced. Observable-query delivery remains event driven.

Before Phase 1 ships:

1. benchmark 10/100/1,000/5,000 failed-partition snapshots and add/update/remove deltas;
2. measure snapshot and delta bytes, reconnect fan-out, reconciliation time, render time, and memory
   on documented baseline hardware;
3. have the product/performance owners approve budgets from the measurements;
4. implement virtualization, a paged/cursor query, or an exact bounded summary before any measured
   budget is exceeded;
5. verify reconnect does not duplicate or reorder items.

Do not invent a universal item or byte cap in the design. The selected cap is a recorded consequence
of measurements and may become configuration.

Before later adaptive cadence or coalescing:

1. identify gauges versus additive counters;
2. preserve counters exactly;
3. single-flight expensive initial snapshots;
4. reply only to the connecting client;
5. add server aggregation only where measurements justify it.

---

## 12. Accessibility and dense-data behavior

- Severity always has text and icon; color is not the only signal.
- Queue rows are keyboard navigable.
- Focus moves predictably into and out of detail panels/dialogs.
- Count changes use a polite live region without announcing every SSE delta.
- Stack traces and large evidence are collapsed by default.
- Empty, loading, stale, disconnected, partial, and error states are distinct.
- Mobile presents summary and drill-through; mutations remain in full preflight dialogs.
- Reduced-motion preferences disable attention-grabbing animation.
- Virtualization/paging is introduced before lists become unbounded.

---

## 13. Runtime scenarios

### Phase 1

1. First payload pending: Loading, never green.
2. Fresh empty failed-partition payload: `No active failed partitions`, never `System healthy`.
3. Active failed partition: Warning with attempts and drill-through.
4. Quarantined failed partition: Critical.
5. Attempt/evidence update: ID stable and no duplicate item.
6. Resolution: item removed once, with no stale duplicate.
7. Rapid add/update/remove: deterministic final state without flicker beyond the declared UI budget.
8. SSE disconnect: last state stale and actions disabled.
9. Reconnect/full snapshot: stable IDs reconcile without duplicates and retain deterministic order.
10. Cross-namespace navigation: no leaked items.
11. Keyboard-only and baseline screen-reader journey through queue and detail.
12. High-cardinality snapshot/reconnect benchmark with measured payload, reconciliation, render, and
    memory results.

### Later Needs Attention phases

1. Trigger dwell not reached: no issue.
2. Trigger dwell reached: one issue raised.
3. Oscillation between trigger and clear thresholds: no flap.
4. Clear dwell reached: issue resolved.
5. Restart grace expires while condition persists: issue appears with explanation.
6. Missing samples: Insufficient Data, never green.
7. High-cardinality namespace set: exact rollup and bounded payload.
8. Rule version changes: deterministic re-evaluation without duplicate issues.
9. Recovery operation completes: issue verifies/clears or records recurrence.
10. 2,000-namespace reconnect/load scenario with measured budgets.

---

## 14. Implementation slices

### N0 — Cross-provider live-observation contract

- shared behavioral suite for initial/add/update/remove/filter/cross-silo delivery;
- repair SQL with a cluster-visible, lifecycle-bounded mechanism;
- no initial false-empty payload before authoritative snapshot;
- verify cancellation/disposal does not leak polling/subscriptions.

### N1 — Failed-partition monitor helpers

- use existing namespace-scoped `AllFailedPartitions`;
- failed/quarantined/unknown status presentation;
- exact canonical identity and target URI;
- deterministic ordering;
- specs for add/update/remove/order/reconnect semantics.

### N2 — Live monitor shell

- one scoped feed;
- explicit failed-partition-only label;
- loading/live/disconnected/error states;
- accessible queue and detail links;
- measured snapshot/reconnect/reconciliation/render/memory budgets;
- owner-approved threshold choosing virtualization, paging, or exact overflow summary before ship.

### N3 — Failed-partition evidence and recovery links

- failed-partition deep links;
- durable recovery-operation links when available;
- no direct actions until the safety path exists;
- runtime SSE and accessibility proof.

### N4 — Additive evidence contracts and metrics

- observer timestamps/subscription/progress;
- Job progress timestamp;
- recommendation target/severity/evidence;
- protobuf/client compatibility.

### N5 — Operational issue engine

- rule definitions;
- dwell/hysteresis/grace;
- durable lifecycle;
- exact rollups.

### N6 — Unified server report

- `AllNeedsAttention` observable;
- CLI/MCP report parity;
- payload budgets and scale tests.

### N7 — Safe actions and verification

- capability/preflight/reason;
- operation/Job navigation;
- verified outcomes and recurrence.

---

## 15. Rejected alternatives

- Re-enabling the deleted mock Dashboard.
- Hardcoded attempt, lag, age, or client-count thresholds without product evidence.
- Client-only dwell/hysteresis as operational truth.
- Green status from empty arrays without freshness/completeness.
- General log or trace search inside Workbench.
- Full stack traces in list payloads.
- One direct action button per item before server safety exists.
- Treating every recommendation as Warning.
- Polling when observable state already exists.
- Unbounded one-item-per-partition rendering at high cardinality.

---

## 16. Product measures

- median open-item to causal drill-through time;
- false-positive and flap rate by rule;
- percentage of items with complete/fresh evidence;
- percentage ending in verified action, explicit no-action, or escalation;
- recurrence after recovery;
- initial snapshot and idle payload budgets;
- critical journey runtime-test success;
- accessibility defects and keyboard completion rate.

No metric becomes a target until its semantics and collection quality are published.
