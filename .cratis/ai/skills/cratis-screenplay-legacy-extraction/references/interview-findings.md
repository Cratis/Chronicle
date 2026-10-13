<!-- cratis-ai-managed: skills/cratis-screenplay-legacy-extraction/references/interview-findings.md -->
# Interview findings (legacy extraction)

Adapted from TrogonStack `eventmodeling-integrating-legacy-systems` (MIT; see `LICENSE`,
`provenance.md`). Fill this in before L0 and keep it in
`.ai-work/screenplay/<model-slug>/legacy/INTERVIEW.md`. Answers also go into the Interview
Trail row of `STATE.md` (see `cratis-screenplay-modeling-lifecycle`). Never paste credentials,
connection strings or production values; record where they live instead.

## Critical questions

Ask only what is missing, one per turn.

1. **System state and documentation.** "Technology stack, age and last major change, database
   engine, size and version, current users and traffic, and what documentation or audit trails
   exist?" Follow-ups: incomplete documentation, ask for at least the schema; no audit trail,
   ask whether `updated_at` columns exist; very large database, ask about bounded scopes.
2. **Freeze agreement** (most critical when a migration or side-car is planned). "Has the business
   agreed to freeze the legacy system: no new features, only bug fixes, no schema changes?"
   Why: without a freeze the extracted events go stale and the model describes a system that no
   longer exists. Follow-ups: not frozen, ask what would win buy-in; partly frozen, record the
   exact boundary. Documentation-only extraction records "freeze not applicable".
3. **Observation and extraction feasibility.** "Which environment may be observed (a clone or
   staging by default)? Can you query the database, is there an audit log or change tracking,
   who can grant change data capture or replication, is OpenTelemetry available, what workload
   exists (test suite, scripted scenarios, mirrored traffic)?" Follow-ups: direct query, ask
   for access and schema knowledge; CDC, name the DBA; changes to the legacy system are not
   allowed under a freeze.
4. **Data sensitivity and disclosure.** "Which data is personal or sensitive, and which hosted
   models may see evidence: none, redacted locators only, or named ones? What is the Prologue
   language-model policy?" This fixes what the rest of the run may do.
5. **People and goal.** "Who are the domain experts, who decides keep / change / remove, and is
   the goal to document, modernize or migrate?" Follow-up: no expert named, record that the
   candidate cannot be accepted beyond the user's stated assumptions.
6. **Timeline, staffing and risk** (migration only). "Target date, team capacity, how critical is
   the system, acceptable downtime, rollback strategy?" Aggressive timeline: ask for the MVP
   scope; zero downtime: propose gradual rollout and reconciliation checks.

## Findings template

```markdown
# Interview findings: <system>

## System overview
- Name / purpose / glossary pointer:
- Age and last major change:
- Stack(s) and repositories:
- Scale (users, traffic, database size and engine/version):
- Documentation and audit trails that exist:

## Freeze agreement  (gate)
- Status: agreed / pending / blocked
- Boundaries: what is frozen (features, schema, APIs), what is allowed (bug fixes)
- Stakeholders who agreed: names, or "none yet"
- Not applicable? Say why (for example documentation-only extraction, no side-car planned)

## Observation and extraction
- Environment that may be observed: clone / staging / production (production needs a DBA)
- Static source available: yes / no (which solutions, branches)
- Dynamic evidence allowed: Prologue capture yes/no; who can grant CDC or replication
- Workload source: test suite / scripted scenarios / mirrored traffic / none
- OpenTelemetry available: yes / no
- Out of scope (routes, tables, tenants):
- Feasibility of extraction: high / medium / low; challenges:

## Data and privacy
- PII classes present; which hosted models may see evidence (none / redacted locators only / named)
- Prologue language-model policy (see `prologue-safety.md` section 3)

## People
- Domain experts (names and areas), who decides keep/change/remove
- Goal: document / modernize / migrate

## Timeline, staffing and risk
- Target date and phase length; team capacity
- Criticality: high / medium / low; acceptable downtime; rollback strategy

## Blockers and concerns
- ...

## Green light
- [ ] Freeze agreement obtained, or recorded as not applicable
- [ ] Extraction approach and observation scope validated
- [ ] Disclosure policy for hosted models recorded
- [ ] Experts named
- [ ] Risk mitigation accepted

RECOMMENDATION: proceed / resolve blockers first / not recommended now (why)
```
