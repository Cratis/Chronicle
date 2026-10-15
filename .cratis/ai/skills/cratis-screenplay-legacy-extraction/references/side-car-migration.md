<!-- cratis-ai-managed: skills/cratis-screenplay-legacy-extraction/references/side-car-migration.md -->
# Side-car migration planning

## Contents

- The pattern
- Freeze agreement (the gate)
- Extracting events from legacy data
- Routing user traffic (Y-valve)
- Integration patterns
- Anti-patterns
- When a side-car is right
- Quality checklist

Adapted from TrogonStack `eventmodeling-integrating-legacy-systems` (MIT; see `LICENSE`,
`provenance.md`). Use it only when the goal is **migrate or modernize** and a side-car is on
the table. Documenting an inherited system needs none of it; stop after the Screenplay model
and the loss report.

Extraction (this skill) answers *what does the old system do*. This page answers *how do new
features live next to it while it is frozen*. It does not change the extraction rules: the
candidate model is still authored from evidence and decisions, never from table shapes.

## The pattern

Instead of rewriting the legacy system, freeze it and build new behaviour alongside it.

```text
Rewrite (risky):  legacy -> rewrite everything -> new system
                  lost behaviour, long timeline, no fallback

Side-car (safer): legacy (frozen: bug fixes only)  --events-->  new event store (Chronicle)
                                                                    |
                                          new modeled slices: commands, projections,
                                          screens, integrations, running next to legacy
```

- The legacy system keeps serving existing users and features; it gains no new features and
  no schema changes.
- Events reach the new store from the legacy data (see the extraction options below), or the
  new slices own their own facts from day one when they are new behaviour.
- New behaviour is modeled model-first (`cratis-screenplay-modeling-lifecycle`), reviewed
  (`cratis-screenplay-model-review`) and rendered or hand-delivered
  (`cratis-screenplay-render-and-gap-fill`).

## Freeze agreement (the gate)

Without an explicit freeze, teams keep modifying the legacy system, the extracted events go
stale and the side-car becomes wrong. Get these agreed before design work:

- Not changed: legacy UI, schema, business logic, APIs. Allowed: bug fixes and support.
- Who signed off, and what each side owns (legacy team maintains legacy; side-car team owns
  the new slices).
- A timeline with phases (new behaviour in the side-car; gradual migration; phase-out).

If the freeze is only partly agreed, record the exact boundary. A dual-write exception is
described under *Integration patterns*.

## Extracting events from legacy data

Tables hold the *consequences* of events that happened. Reverse-engineering state changes
back into events is a proposal, not a fact (`signal-to-intent.md`): a status column changing
from `draft` to `confirmed` suggests an `OrderConfirmed` candidate; an expert confirms it.

| Option | How | Strength | Watch for |
|---|---|---|---|
| Historical extraction (catch-up) | Query legacy data, infer the events that must have happened (creation at `created_at`, confirmation at `updated_at`), append them to the new store, then keep polling for changes | Gives history | Inferred timestamps and ordering are guesses; mark inferred events as such in the evidence table |
| Real-time sync | Database trigger or change data capture feeds events to the new system as the legacy system writes | No delay; cleaner | Needs infrastructure and DBA approval; capture setup changes the target (see `prologue-safety.md`) |
| Hybrid | Historical extract once, then real time | Smooth onboarding | Two code paths to verify against each other |
| Audit-log parsing | Parse an existing audit log into events | Complete history, less guesswork | Log formats drift; check coverage |
| Scheduled sync | Every N minutes, query changes and generate events | Simple | Eventual consistency; missed intermediate states |

Rules for any option:

- Loading history into Chronicle and keeping it in sync is application or infrastructure
  code, not part of a Screenplay model. The model states which events exist and what they
  mean; the loader is delivered separately and verified against the evidence table.
- An event extracted from a table write is `proposed` until confirmed. Do not load inferred
  events as if they were observed facts without recording their provenance.
- Map every in-scope legacy table to extractable events or to an explicit "not extracted"
  row with a reason. An unmapped table is an unknown.

## Routing user traffic (Y-valve)

Move users gradually, never all at once:

1. New features only in the side-car; everything else in legacy.
2. New read models and dashboards shown next to legacy screens.
3. New features available; users may opt in to the new UI.
4. Deprecation: legacy becomes read-only, then is phased out.

A routing rule decides per request: a new feature goes to the side-car; a user who opted in
goes to the side-car; everything else goes to legacy. Users should always know which system
they are in.

## Integration patterns

- **Read models from legacy**: query legacy state to build a read model for the new UI, so it
  can show unified data. Source the read model from screens and real query usage, not from a
  table mirror.
- **Event stream from audit logs**: see the table above.
- **Scheduled sync**: see the table above.
- **Mirror-write through an integration boundary (use with caution).** Default: avoid
  dual-write; keep the side-car on its own store and sync from legacy events. A dual-write is
  allowed only with an approved freeze exception and all of these controls: every write goes
  through one controlled boundary (no direct database access); every write is idempotent
  (safe to replay); a reconciliation check and a documented rollback plan exist; the owner,
  the sunset date and the exception approval are recorded. Without them, stay side-car only.

## Anti-patterns

- Modifying legacy and building a side-car at the same time (confusion about which one is
  authoritative).
- Forcing every user to switch at once.
- Incomplete event extraction: important legacy data missing from the events.
- Side-car code that reads the legacy database directly (defeats the separation).
- Users not knowing which system they are using.
- Treating the side-car as a duplicate of legacy: it builds new behaviour.

## When a side-car is right

Use it when the legacy system works but is hard to change, the business needs new behaviour
quickly, a rewrite would take months and carries high risk, users can live with two systems
for a while, and old and new behaviour separate cleanly. Do not use it when legacy is
broken and needs fixing, when complete integration or a single unified system is required, or
when legacy holds critical behaviour that cannot be replicated (then extraction and expert
verification come first).

## Quality checklist

- [ ] Freeze agreement documented and signed off by the business
- [ ] Extraction strategy defined for all in-scope legacy data; every table mapped or listed as unmapped
- [ ] Event capture or load pipeline designed and owned
- [ ] New behaviour scoped as modeled slices with their own acceptance
- [ ] Accepted model and the new side-car code compared structurally with `cratis screenplay conform <model-root> --project <path>` (CLI 3.42.0 or later; exit 0 no blocking findings, 1 defects, 2 could not run or incomplete; same MSBuild trust caveat as `generate`, so use a trusted clone; structural address matching only, never a behavior or equivalence verdict)
- [ ] Traffic routing designed; users can tell which system they are in
- [ ] Parallel-operation test plan and reconciliation check
- [ ] Deprecation timeline agreed
- [ ] Rollback plan in place
