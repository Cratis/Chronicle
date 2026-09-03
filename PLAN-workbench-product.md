# Chronicle Workbench Product Program — Execution Plan

> **For the contributor picking this up:** read this file and the linked design for the workstream.
> Take one bounded status-board item per branch/PR. Update evidence and status in the same PR. Do not
> reintroduce mock operational data or add mutation affordances that bypass the operator-action and
> authorization designs.

---

## 1. North star

Chronicle Workbench will be the fastest and safest way to move from an event-sourced symptom to an
explained cause, the smallest valid response, a verified outcome, and a durable operational record.

> **Signal → causal evidence → bounded action or explicit no-action → verified outcome → durable record**

Workbench owns Chronicle-specific evidence and operations:

- events, streams, sequences, revisions, generations, correlation, and causation;
- observers, positions, ownership, failed partitions, quarantine, replay, and catch-up;
- Jobs, recommendations, connected clients, and version compatibility;
- projection/read-model effects, Preview, and Time Machine verification;
- safe event repair, compliance, and operator-action history.

Workbench is not a generic broker, Orleans, Kubernetes, APM, log, trace, pager, or ticketing console.
It emits stable correlation identifiers and deep-links to those systems.

---

## 2. Program artifacts

| Artifact | Purpose |
|---|---|
| `WORKBENCH-OPERATOR-ACTION-INVENTORY.md` | Evidence-backed baseline of current mutations, safeguards, verification, authorization, and audit gaps |
| `DESIGN-workbench-authorization.md` | Roles, permissions, resource scope, identity migration, CSRF, denied-attempt telemetry, and secure rollout |
| `DESIGN-workbench-operator-actions.md` | Shared action envelope, durable ledger, idempotency, preflight, Job linkage, verification, and failed-partition pilot |
| `DESIGN-workbench-needs-attention.md` | Trustworthy live home, explicit signal semantics, freshness, health-rule lifecycle, accessibility, and scale budgets |
| `PLAN-reliability-program.md` | Repository-wide release, verification, generated-output, and hot-core constraints that this program must preserve |

Research inputs from decompiled/proprietary artifacts are excluded from implementation provenance.
Allowed comparison sources are Chronicle source, legitimate public JasperFx samples/documentation, and
public platform/security documentation.

---

## 3. Current strengths

Chronicle already has strong operational primitives:

- sequence exploration with paging, filters, histograms, exports, tabs, and saved queries;
- event content/context, correlation, causation, revisions, and generations;
- observer state, owner, position, tail, handled count, and replayability;
- failed-partition attempts with messages and stack traces;
- durable resumable Jobs with progress and status history;
- connected-client placement and version/process evidence;
- projection Preview and read-model Time Machine;
- Arc SSE observable queries;
- a general persisted recommendation framework with `Perform` and `Ignore`.

The product gap is composition and trust: these are separate resource screens with inconsistent
mutation safety rather than one incident-to-verification journey.

---

## 4. Uncommitted first-increment candidate

The main working tree intentionally contains an uncommitted candidate increment. It is not identified
by the baseline revision, released, or accepted. It currently:

- removed the disabled Dashboard prototype and all hardcoded operational data;
- removed stale commented import/menu/route wiring;
- exposed failed-partition `IsResolved` and presence-carrying nullable `IsQuarantined` additively
  through the Kernel contract, working-tree observation protobuf schema, Kernel/API/.NET converters,
  .NET client, generated Workbench proxy, table, and details; an omitted quarantine field is rendered
  Unknown instead of falsely claiming automatic retry remains active;
- preserved existing four-argument public record constructors;
- marked a resolved partition before removing it from the active collection;
- added C# and TypeScript specifications;
- uses one deterministic initial-administrator id plus a first-event concurrency expectation so
  multiple silos cannot create competing bootstrap administrators; AddUser rejects existing
  case-insensitive usernames, while an atomic provider-level uniqueness guarantee remains; initial-
  password setup is bound to the configured administrator, prevents simultaneous password appends,
  and marks newly added password-bearing users as initialized so their required first change uses
  the own-password flow; bootstrap status now carries
  the configured administrator username through generated contracts/proxies so custom administrators
  can sign in; password commands now wait for the user projection to reach the appended event before
  reporting success, eliminating immediate sign-in races; bootstrap token and rate limiting remain;
- rejects authenticated mutation principals without a stable `sub` instead of inventing a random
  actor identity, while preserving the explicit anonymous identity path;
- requires Secure cookies for Workbench authentication and pins it with a Server specification;
- issues and validates antiforgery tokens for authenticated cookie mutations; Arc commands, SSE
  control posts, and logout receive the shared request header, with Server and Workbench specs;
- removes group-wide anonymous access from ASP.NET Identity endpoints, allowing only login and token
  refresh anonymously and adding an authenticated logout endpoint, with route-policy specs;
- replaces `RedactMany`'s thrown generic 500 with an explicit 501 problem response and specs while
  retaining the current public route until next-major removal;
- replaces SQL failed-partition's one-shot false-empty observable with cluster-visible `LiveQuery`
  polling and specs for save, resolve, and external-database changes;
- issues a stable Chronicle audience on password/client-credential/refresh tokens and requires that
  audience during internal-token validation; external JWT authority, audience, and HTTPS metadata are
  now configured explicitly, with issuance/configuration specifications;
- has fresh local evidence from affected Debug specifications, clean Release builds, Workbench lint,
  TypeScript checks, focused tests, and production build; the compatibility refinement adds explicit
  Unknown/Failed/Quarantined/Resolved Workbench specs and nullable protobuf round-trip coverage.

Resolved-history visibility remains a separate retention/query decision; the current live query
returns active failures.

Latest local verification for the uncommitted candidate:

| Gate | Result |
|---|---|
| Focused backend specifications | 101 passed, 0 failed |
| Workbench specifications | 22 passed, 0 failed |
| Affected Release builds | zero warnings, zero errors |
| Workbench lint and app/spec TypeScript checks | passed |
| Workbench production build | passed |
| Primary diagnostics and `git diff --check` | clean; lens reports only generated-contract redundant-using noise |

No live browser/Kernel SSE journey has been run yet.

---

## 5. Product principles

1. **Incident-first, not page-first.** Preserve context across events, observers, failures, Jobs,
   operations, and read models.
2. **Evidence before action.** Every status names source, scope, freshness, and uncertainty.
3. **Smallest valid scope first.** Retry one failed partition before proposing observer replay.
4. **No action is valid.** Wait, insufficient evidence, escalation, and no-action are recordable.
5. **Server-enforced safety.** UI confirmation is usability, not authorization or eligibility.
6. **Verification is part of execution.** HTTP 200/202 is not recovery success.
7. **No invented precision.** Unknown rate, cost, impact, or ETA stays unknown.
8. **Progress health, not process uptime.** State, ownership, position, tail, rate, and last progress
   remain distinct.
9. **Stable lifecycle vocabulary.** Retry, quarantine, replay, catch-up, revision, and redaction are
   never collapsed into one generic retry.
10. **One contract, many callers.** Workbench, CLI, MCP, and automation share server semantics.
11. **OpenTelemetry is the external integration boundary.** Chronicle emits its own evidence; external
    systems store general telemetry.
12. **First-run value is tested.** Samples and smoke tests prove the journey.

---

## 6. Non-negotiable gates

1. No shipped Workbench view presents hardcoded operational data as real.
2. Protobuf evolution is additive; field numbers are never reused.
3. Existing public APIs remain source/wire compatible or follow an explicit semver migration.
4. Every new mutation is authorized and resource-scoped server-side.
5. Every authorized mutation writes actor, reason, target, preflight, outcome, and verification.
6. Actor identity is stable and server-resolved; no random or caller-supplied fallback.
7. Cross-event-store and cross-namespace negative tests exist.
8. No destructive action has implicit global scope.
9. Every health state is source-backed or explicitly insufficient-data.
10. Metric-derived issues have dwell, clear criteria, and hysteresis.
11. Every mutation ships with observable verification.
12. Workbench/CLI/MCP cannot bypass server rules.
13. Existing reliability, hot-core, timing, semver, build, and test gates are not weakened.
14. Critical Workbench journeys are exercised against a running stack before release.
15. Keyboard, screen-reader, focus, dense-data, stale, empty, loading, and error behavior are
    acceptance criteria.

---

## 7. Status board

States: `todo`, `scoped`, `in-progress`, `implemented-uncommitted`, `partial`, `done`, `blocked`,
`deferred`.

### Phase 0 — foundation

| ID | Initiative | State | Evidence/dependency |
|---|---|---|---|
| W0.1 | Approve product thesis, clean-room boundary, and non-goals | `done` | This plan |
| W0.2 | Inventory operator actions, contracts, safeguards, authorization, and verification | `done` | `WORKBENCH-OPERATOR-ACTION-INVENTORY.md` |
| W0.3 | Remove/development-gate mock Dashboard | `implemented-uncommitted` | Working-tree candidate; not released |
| W0.4 | Define Workbench quality/evidence gates | `done` | §6 |
| W0.5 | Approve authorization role/permission/scope vocabulary | `todo` | `DESIGN-workbench-authorization.md` |
| W0.6 | Approve action reason, retention, stabilization, and legacy policies | `todo` | `DESIGN-workbench-operator-actions.md` |
| W0.7 | Secure initial bootstrap; split own-password, admin reset, and reset-token flows; add rate/CSRF | `partial` | Admin-target binding + cookie CSRF done; bootstrap token/rate/flow split remain |
| W0.8 | Define authenticated, anonymous-read-only, and unsafe-development deployment modes | `scoped` | Security design |
| W0.9 | Make `RedactMany` explicitly unsupported now; remove proxy/route in next major | `partial` | Uncommitted 501 ProblemDetails + specs; next-major removal remains |
| W0.10 | Remove random actor fallback for mutations | `implemented-uncommitted` | Stable-subject rejection + specs |
| W0.11 | Require secure production cookies and resolve internal-token audience validation | `implemented-uncommitted` | Secure cookie + issued/validated Chronicle audience specs |
| W0.12 | Protect security/action-history event content from generic event reads | `scoped` | Security design |
| W0.13 | Enforce case-insensitive username uniqueness across all storage providers | `partial` | Deterministic bootstrap + AddUser precheck; atomic provider constraint remains |

### Phase 1 — contracts and safety

| ID | Initiative | State | Depends on |
|---|---|---|---|
| W1.1 | Add observer temporal/progress evidence contracts | `deferred` | Not required for failed-partition MVP |
| W1.2 | Add recommendation severity/target/evidence contracts | `deferred` | Not required for failed-partition MVP |
| W1.3 | Complete failed-partition operator contract | `implemented-uncommitted` | Working-tree active-state candidate; resolved-history decision remains |
| W1.4 | Define minimal issue/freshness/insufficient-data semantics | `done` | Needs Attention design |
| W1.5 | Add internal role/scope events and read model | `todo` | W0.5 |
| W1.6 | Add external issuer/claim mapping and shadow authorization | `todo` | W0.5 |
| W1.7 | Add effective-capabilities query | `todo` | W1.5–W1.6 |

### Phase 2 — operator-action pilot

| ID | Initiative | State | Depends on |
|---|---|---|---|
| W2.1 | Add action-specific failed-partition recovery concepts/contracts/reason validation | `scoped` | W0.5–W0.9 |
| W2.2 | Add target-namespace failed-partition action history and observable read model | `scoped` | W2.1 |
| W2.3 | Return Job ID from atomic failed-partition operator retry | `scoped` | W2.1 |
| W2.4 | Add idempotent coordinator/reconciliation | `scoped` | W2.2–W2.3 |
| W2.5 | Add verification and recurrence outcome | `scoped` | W2.4 |
| W2.6 | Add Workbench preflight/reason/operation journey | `scoped` | W1.7, W2.2–W2.5 |
| W2.7 | Adapt/deprecate legacy retry endpoint | `todo` | W2.6 |

### Phase 3 — Failed Partition Monitor

| ID | Initiative | State | Depends on |
|---|---|---|---|
| W3.0 | Make failed-partition observation live on every supported storage provider | `partial` | SQL LiveQuery + add/remove/external/no-change specs; shared suite remains |
| W3.1 | Add Failed Partition Monitor helpers using existing observable | `scoped` | W3.0 and current failed-partition contract |
| W3.2 | Specify/test canonical identity, deterministic order, add/update/remove/reconnect | `scoped` | W3.1 |
| W3.3 | Measure snapshot, reconnect, reconciliation, render, and memory budgets | `scoped` | W3.2 |
| W3.4 | Ship explicitly scoped Failed Partition Monitor | `scoped` | W3.2–W3.3 |
| W3.5 | Add recovery-operation/Job verification links | `todo` | W2.6, W3.4 |
| W3.6 | Add keyboard and screen-reader runtime scenarios | `todo` | W3.4–W3.5 |
| W3.7 | Decide virtualization, paging, or exact overflow behavior from measurements | `todo` | W3.3 |

### Phase 4 — investigation and recovery

| ID | Initiative | State | Depends on |
|---|---|---|---|
| W4.1 | Preserve investigation context in shareable URLs | `todo` | W3.3 |
| W4.2 | Add compact causal chronology | `todo` | W4.1 |
| W4.3 | Validate partition replay semantics | `todo` | W2 safety architecture |
| W4.4 | Add replay preflight/planner and bounded execution | `todo` | W4.3 |
| W4.5 | Promote Time Machine into recovery verification | `todo` | W4.4 |

### Phase 5 — operational intelligence and caller parity

| ID | Initiative | State | Depends on |
|---|---|---|---|
| W5.1 | Add Kernel operational-issue rule engine | `deferred` | Requires temporal evidence and post-MVP incidents |
| W5.2 | Add dwell/hysteresis/grace and exact rollups | `deferred` | W5.1 |
| W5.3 | Broaden actionable recommendation catalog | `deferred` | W1.2, W5.1 |
| W5.4 | Expand `AllNeedsAttention` beyond failed partitions | `deferred` | W5.1–W5.3 |
| W5.5 | Add read-only CLI with JSON report parity | `todo` | Shared reports/contracts |
| W5.6 | Add MCP read parity; mutation remains separately approved | `deferred` | W5.5 |

### Phase 6 — conditional platform work

| ID | Initiative | State | Trigger |
|---|---|---|---|
| W6.1 | Capability/version manifests | `deferred` | measured client/server skew incidents |
| W6.2 | Runtime-first event-flow map | `deferred` | investigation evidence gap |
| W6.3 | Adaptive observable cadence/coalescing | `deferred` | measured payload/fan-out pressure |
| W6.4 | Guided/approvable automation | `deferred` | proven auth, idempotency, ledger, verification |
| W6.5 | Historical operational intelligence | `deferred` | stable issue/action semantics |

---

## 8. Critical path

```text
Security hotfixes and deployment modes
  → approve permissions/reasons
  → internal/external capability assignments
  → failed-partition action history
  → atomic retry + durable deduplication + Job ID
  → reconciliation and verification
  → Workbench recovery journey
  → measured Failed Partition Monitor
```

The read-only Failed Partition Monitor may proceed in parallel, but it must not add direct mutation
buttons before the safety path is complete and must never imply overall system health.

---

## 9. MVP definition

The MVP is complete only when one failed partition can be handled end to end:

1. an authenticated, scoped operator sees the failure in Failed Partition Monitor;
2. unsupported/skewed quarantine state is shown as Unknown;
3. preflight explains current facts and whether manual retry is valid;
4. operator supplies a bounded structured reason;
5. the server durably records the request before dispatch;
6. duplicate Operation IDs cannot start duplicate effects;
7. the observer revalidates state and starts/joins one partition-keyed Job;
8. Workbench receives Operation and Job receipts;
9. crash reconciliation reaches an honest terminal or InDoubt state;
10. verification reports Verified, StillFailing, or Inconclusive from per-partition evidence;
11. action history remains namespace-scoped, protected, and queryable after failure removal;
12. add/update/remove/reconnect, authorization, CSRF, compatibility, and runtime tests pass.

Nothing broader—general health scoring, replay planner, generic action framework, CLI/MCP mutation, or
automation—is part of MVP.

---

## 10. Delivery workstreams

### A. Security and identity

Owns role/scope events, external mapping, shadow decisions, effective capabilities, CSRF, secure
authentication-disabled behavior, and enforcement rollout.

### B. Operation safety

Owns operation contracts, ledger, preflight, idempotency, Job linkage, verification, and one-action-
at-a-time migration.

### C. Operational experience

Owns the Failed Partition Monitor, later Needs Attention expansion, freshness, investigation links,
causal chronology, Time Machine verification, accessibility, and dense-data behavior.

### D. Evidence and scale

Owns OpenTelemetry instruments, rule semantics, payload budgets, reconnect tests, runtime smoke
scenarios, and operator documentation.

Every workstream has a product owner and technical owner. Security/public API decisions require
explicit approval rather than inference from implementation convenience.

---

## 11. First 90 days

### Days 0–30

- Complete W0.5–W0.9 security and owner decisions.
- Replace caller-selected initial-password setup with a single-use bootstrap claim; separate
  own-password change from administrative reset and define CSRF enforcement.
- Deny mutations in anonymous-read-only mode and constrain unsafe-development mode.
- Replace `RedactMany`'s generic 500 with an explicit unsupported response; schedule public
  proxy/route removal for the next major release.
- Remove random mutation-actor fallback; require secure production cookies; resolve internal-token
  audience validation.
- Add internal role/scope event model, external mapping, and shadow decisions.
- Define action-specific failed-partition recovery contracts and history events.

### Days 31–60

- Implement target-namespace failed-partition action history and read model.
- Implement atomic target-grain revalidation, durable Operation ID deduplication, and Job receipt.
- Add reconciliation and separate execution/verification state machines.
- Add effective-capabilities query.
- Add authenticated, CSRF, wrong-scope, duplicate-ID, and crash-window tests.

### Days 61–90

- Build Workbench preflight/reason/receipt/verification journey.
- Migrate Workbench failed-partition retry to the safe path.
- Ship only the explicitly scoped Failed Partition Monitor.
- Exercise the full runtime scenario, including SSE add/update/remove/reconnect and deterministic
  ordering.
- Publish measured snapshot/reconnect/reconciliation/render/memory budgets and implement the
  resulting virtualization, paging, or exact-overflow policy.
- Review evidence before approving a second action or broader home signals.

---

## 12. Recommended PR sequence

Each item is independently reviewable and keeps public/security changes explicit:

1. **Security characterization:** executable specs pin current authentication-disabled, anonymous
   password/bootstrap, actor fallback, cookie, audience, and protected-content behavior.
2. **Password/bootstrap hardening:** bound single-use bootstrap claim, own-password flow, rate limits,
   non-enumerating failures, and CSRF infrastructure.
3. **Deployment modes and protected content:** authenticated/anonymous-read-only/unsafe-development
   modes; protected security/action sequence classification.
4. **Capabilities in shadow mode:** role/scope events, external claim mapping, decision telemetry,
   effective-capabilities query; no enforcement yet.
5. **Failed-partition recovery contracts:** action-specific concepts, reason validation, receipt,
   preflight, protobuf compatibility, no generic framework.
6. **Protected action history:** OperatorActions sequence protection, action-specific events,
   projection/read model, namespace/access/retention specs.
7. **Atomic retry and Job receipt:** target-grain revalidation, partition-keyed Job deduplication,
   Operation ID correlation, explicit quarantine semantics.
8. **Idempotency/reconciliation:** request digest, concurrency expectation, duplicate/conflict,
   InDoubt, single-activation reconciler, crash-window specs.
9. **Verification:** durable stabilization check and per-partition Verified/StillFailing/Inconclusive
   evidence.
10. **Workbench safe-recovery journey:** capability, preflight, reason, receipt, Job/history,
    verification, accessibility, authenticated runtime scenario.
11. **Failed Partition Monitor:** existing observable, Unknown compatibility state, stable identity,
    deterministic order, feed state, measured scale/reconnect budgets, overflow policy.
12. **Enforcement and compatibility:** compare shadow decisions, assign users/scopes, enforce pilot,
    adapt/deprecate legacy route with semver documentation.

No PR mixes the broad protobuf-generator #3734 work or unrelated pre-existing working-tree changes
into this program.

---

## 13. Verification matrix

For every bounded implementation slice:

- primary LSP diagnostics;
- clean Debug and Release builds with zero warnings/errors;
- affected C# specifications;
- protobuf and generated-proxy compatibility/diff checks;
- Workbench lint, app/spec TypeScript checks, focused tests, and production build;
- storage semantics across InMemory, MongoDB, and at least one SQL provider where applicable;
- negative auth/scope/idempotency tests;
- runtime SSE journey where behavior crosses process boundaries;
- documentation verification;
- advisory code/security/performance review.

Generated output is never hand-edited where a working generator exists. The known #3734 protobuf
reliability dependency remains separately owned by `PLAN-reliability-program.md`; bounded additive
schema changes must not pull unrelated wire drift into this program.

---

## 14. Product measures

Measure outcomes, not page count:

- median symptom-to-causal-evidence time;
- percentage of recoveries using the smallest valid scope;
- verified recovery and recurrence rates;
- percentage of authorized mutations with complete ledger and verification;
- authorization denied/indeterminate rate by action;
- false-positive and flap rate by operational rule;
- percentage of home items with complete/fresh evidence;
- recommendation action/ignore/no-action outcomes;
- clone/start to first real operational signal;
- initial snapshot, idle bytes/minute, reconciliation, and render budgets;
- critical runtime-journey success rate.

Metric semantics, source, scope, and collection quality are published before targets are set.

---

## 15. Explicit non-goals

- fleet/microservice control plane;
- generic Orleans, broker, Kubernetes, APM, log, trace, paging, or ticket product;
- transport/vendor-specific telemetry exporters where OpenTelemetry owns integration;
- feature licensing gates;
- one-click global replay/reset/destructive actions;
- automatic recovery before classification, authorization, idempotency, ledger, and verification;
- anonymous shared-token break-glass endpoint;
- client-only authorization, dwell, hysteresis, or idempotency;
- copying proprietary implementation details;
- replacing actionable recommendations with passive alert acknowledgement.

---

## 16. Recommended defaults and owner decisions

Recommended defaults:

- use the roles/permissions in `DESIGN-workbench-authorization.md` as the initial public vocabulary;
- existing internal users receive no silent permanent administrator grant; use shadow mode and an
  explicit, expiring migration assignment where compatibility requires it;
- service principals are read-only unless an issuer/client-specific automation permission is
  explicitly configured;
- authentication-disabled production mode is anonymous read-only; unsafe mutation is development
  only;
- reason code is required, external reference is optional/bounded, and note is optional/bounded;
- application event-content permission excludes action-history and security/credential content;
- MCP is read-only through MVP;
- the next action is selected only after failed-partition evidence demonstrates a reusable shape.

Owner approval is still required for:

1. exact external issuer/claim mapping syntax;
2. temporary compatibility assignment duration for existing users;
3. stabilization-window duration and completed history/Job retention;
4. legacy retry endpoint deprecation/enforcement release;
5. action-history export/access and target-namespace deletion behavior;
6. protected-sequence naming and security-event classification;
7. measured Failed Partition Monitor performance budgets and overflow strategy;
8. which second action, if any, justifies extracting shared action infrastructure.
