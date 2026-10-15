<!-- cratis-ai-managed: skills/cratis-screenplay-streams-and-consistency/references/chronicle-boundaries.md -->
# Chronicle boundaries

Read when consistency, tenancy or personal data decides where a boundary falls. These are
target facts; the `.play` model declares only part of them (`versions.md` in
`cratis-screenplay-toolchain`).

## Pins
Chronicle **v19.50.0** (first read at v19.32.0 `f17a2ff`): CHR0012 (nullable event properties) and CHR0034 (PII on an
event-source id) are present. Three runtime defects are **open at v19.50.0**: #3744 (SQL and
InMemory providers do not settle unique claims), #4123 (constraint index updates run after commit
and fail silently) and #4131 (composite unique constraints collide when a component contains the
separator). Arc **v22.72.0** has `[ProtectedDecision]` (since v22.39.0, needs Chronicle 19.23 or
later); Stage-rendered apps pin an Arc and Chronicle client/kernel release
(see the Stage skill's `references/rendered-application.md`) that include them, so the APIs are available to gap-fill, not automatically emitted or
protected (see `consistency-and-concurrency.md` section 3). Re-check the issues before relying on a
constraint in a new release.
A concurrency scope's first append is unchecked by default at v19.50.0 (the default is planned to become checked in the next major, `Documentation/events/concurrency.mdx`); see
SKILL.md, ‘First append into a scope’.

## Identity, routing and claims
Name four things separately: the event store/namespace, the business event-source identity, any
process streams that route work, and the scope of each invariant. Use source/stream
classification to separate genuinely independent work inside one identity; do not split a
business identity just to reduce contention. Tags classify occurrences; they do not isolate
tenants.

A value constraint protects a claim across sources within its configured scope; an event-type
constraint protects one occurrence per source. List every event that claims, changes or
releases the value, including correction and import paths. Post-commit index maintenance is not
atomic durability (Chronicle#4123); composite separator collisions are tracked in #4131; the
SQL and InMemory providers do not settle unique claims (Chronicle#3744).

## State used for decisions
Materialized views may lag. A passive read folds events on demand but does not protect the
interval between reading and appending. A guarded decision read adds that protection for an
admitted projection and key shape (including absence) when the dependency is enrolled in the
same protected commit. Name the actual target mechanism and deployment assumptions; do not
invent guards from sequence numbers, and do not assume joins, reducers, other namespaces or
external services qualify. Current `reads`/`concurrency` declarations supply no such runtime
protection: keep the invariant and its race outcome as explicit target requirements.

## Tenancy and personal data
A namespace scopes event positions, claims, observer progress and encryption keys. State
whether uniqueness and completion are per tenant or genuinely cross-tenant.

Decide the data subject before the event/view shape: one person per event or
read-model instance, not necessarily the stream's identity. At Screenplay 4.127.0
trailing event `subject` is report-only lineage, not runtime propagation or C#
`[Subject]`; read-model marks remain unsupported. Use bare `pii` for personal
values and `secret` for operational secrets on concepts, never on identities or
subjects. Current PLAY0515 is a source error, including destinations, stream-id
parts and nested route sources (Chronicle CHR0034/0052 likewise reject protected
source ids). Use a surrogate identity and protected payload. Bearer tokens, magic links and
signed URLs are never facts; record a keyed hash or a reference.

Redaction (payload replacement keeping the sequence slot) and crypto-erasure serve compliance
only; they are asynchronous, namespace-local and do not erase external copies. Model erasure as
an owned workflow, not as a flag or a claim that history is undeletable. Domain correction is a
new fact.
