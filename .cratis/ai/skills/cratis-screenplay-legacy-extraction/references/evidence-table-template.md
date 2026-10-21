<!-- cratis-ai-managed: skills/cratis-screenplay-legacy-extraction/references/evidence-table-template.md -->
# Evidence table template

## Contents

- 1. Coverage (from L0)
- 2. Evidence rows
- Traceability reconciliation
- 3. Read log
- 4. Unknowns and questions
- 5. Sign-off
- 6. Where each feature lives
- 7. Analysis history
- 8. Decisions
- 9. Terms
- 10. Realization convention
- 11. As-is slice map

Keep one file per model: `.ai-work/screenplay/<model-slug>/legacy/EVIDENCE.md`. It is the single place
where as-is facts, their sources and the proposed target decisions meet. It is a work record: at
acceptance the history, where each feature lives and the open questions are promoted into the model
as anchored notes (`re-entry-and-passes.md` section 4); this file stays the home of the rows and
the line-level locators. Agents and reviewers
cite row ids (`E12`) instead of pasting code or captures.

Treat the file as confidential when it holds production data. Locators should be route
templates, table and column names, and path:line, not values.

## 1. Coverage (from L0)

| Entry-point family | Count found | Covered by | Not covered because | Route for the gap |
|---|---|---|---|---|
| HTTP write routes | 23 | generate (Arc), Prologue HTTP | PATCH and non-`/api` posts are not proxied | targeted reads E40-E44 |
| Scheduled jobs | 2 | none | no provider reads Hangfire jobs | targeted reads |
| DB triggers | 1 | Prologue (as an uncorrelated transaction only) | trigger body not captured | targeted read of the trigger DDL |
| After-commit hooks and outbox relays | 1 | none | no provider reads them | targeted reads |

## 2. Evidence rows

| Column | Content |
|---|---|
| `id` | `E<n>`, stable; never renumber |
| `source` | static (generate) / dynamic (Prologue) / code / test / UI / runtime (Chronicle) / doc / expert |
| `locator` | Repository-backed code, test and schema claims: the inspected repository-relative `path:start-end`, with the file name exactly as given (`CheckinService.cs`, never shortened; for a test, the test method's range), bound to the source identity. A locator that cannot be found is recorded as `not found` and the claim stays `unknown`; it is never rewritten as "to build". Runtime (Chronicle): target, event store, namespace, schema identity + generation or bounded event range, observation time. Other sources: capture file + record, redacted UI route + observation id, document section/version, generated declaration address + diagnostic, or person + date. Never substitute one source type's locator for another's |
| `observed fact` | what was seen, in neutral words ("POST /checkins inserts Checkins and updates Hooks") |
| `technical shape` | method + route, tables (op, columns), status-code mix, frequency, span names |
| `strength` | Exact / Configured / Conventional / Heuristic (generator ladder), Observed (capture), Read (code), Stated (person) |
| `classification` | intent / consequence / infrastructure / noise / unknown; for writes without a user command add maintenance / correction / integration / scheduled / unexplained |
| `generator disposition` | Lowered / ProvenanceOnly / OmittedWithDiagnostic (code) / Conflicted / n/a |
| `candidate element` | declaration address in the candidate (`Cloakroom/Intake/TakeInCoat`, event `CoatTakenIn`) or `none` |
| `target decision` | keep / change / remove / undecided |
| `decided by` | user, expert name, or `proposed` |
| `expert verdict` | confirmed / corrected / rejected / not asked |
| `notes` | links to other rows, open question ids |

Example rows:

| id | source | locator | observed fact | technical shape | strength | classification | gen. disposition | candidate | decision | decided by | verdict |
|---|---|---|---|---|---|---|---|---|---|---|---|
| E1 | dynamic | capture 0007, `POST /api/checkins` | taking in a coat writes two tables | insert `Checkins`; update `Hooks(occupied)`; 201 x41, 409 x3 | Observed | intent (take in) + consequence (hook) | n/a | `TakeInCoat` -> `CoatTakenIn`; occupancy via projection | change | proposed | not asked |
| E2 | code | `CheckinService.cs` | 409 when the hook is already free, message "That coat is not on a hook" | throws `ConflictException` | Read | intent (rule) | n/a | `reads CoatStatus` + `require`, NOT enforced today (target: protected decision or Chronicle `concurrency` scope); no executable error spec, recorded in LOSS.md | keep | proposed | not asked |
| E3 | static | `static.play`, SP0016 at `CheckinValidator` | validation rule held to a condition is emitted unconditionally; the condition is lost | `When(x => x.IsVip)` | Exact (loss) | unknown | Lowered without its condition (semantic mismatch, reported by SP0016) | none yet | undecided | - | question Q4: when does the rule really apply? read `CheckinValidator` and ask an expert |
| E4 | dynamic | uncorrelated txn, `Hooks` nightly 02:00 | hook flags reset without a command | update `Hooks(*)` daily | Observed | unexplained | n/a | none | undecided | - | question Q5 |

## Traceability reconciliation

Record the examined source identity once per repository: the commit, plus the digest from
the source-identity helper (`cratis-screenplay-modeling-lifecycle` `references/verdicts-and-modes.md` "Source identity"); a dirty flag alone is not enough.
Keep it apart from any tool revision (for example the MCP `modelRevision`).

Before handoff, for every candidate slice and rule:
1. Resolve each cited evidence id to its row.
2. Check that each code or schema claim (authorization, rejection, branching, unique index,
   deletion, background work) has an inspected `path:start-end` that actually shows it. A
   route, table name or generated declaration alone does not substantiate a code claim.
3. Keep inferred business meaning and target decisions apart from the observed behavior.
4. Mark unsupported claims `unknown` and add the question that would settle them.

Put an exact `file:line` locator for every evidence-backed claim (`CheckinService.cs`)
inline in the final report itself, with the file name exactly as given; a pointer to this table
or another file is not enough, and opaque ids alone do not count. List unresolved gaps. Do not manufacture references to meet
a count. A UI-only or document-only extraction may have no code locators; say so and limit
its claims accordingly.

## 3. Read log
Every code or capture read gets one line so it is traceable and reusable. Reread when the
source changed or rows disagree; note why.

| when | file:lines or capture | resolves | result row(s) |
|---|---|---|---|
| L3 | `Jobs/HookReset.cs` | E4 | E9 (maintenance job, clears stale flags) |

## 4. Unknowns and questions
List rows still `unknown` or `undecided` with the question that would settle them, who can
answer, and what the candidate assumes meanwhile. Unknowns are kept, not dropped. A question
that affects a declaration also becomes an anchored `question` note once the candidate is in the
model root (`re-entry-and-passes.md` section 4); the row keeps the evidence, the note keeps the question.

## 5. Sign-off
Per slice: expert, date, verdict, open items. Capture cleanup confirmation (who ran which
statements, verification query output summary) or "no capture run".

## 6. Where each feature lives
One table per feature, written at Pass 0; re-entry refreshes it (`re-entry-and-passes.md`).

| Area | Folder or package | Key symbols |
|---|---|---|
| API | `src/Checkins/` | `CheckinService`, `CheckinsController` |
| Persistence | `db/checkins/` | `Checkins` table, `Hooks` table |
| Tests | `tests/Checkins/` | `CheckinServiceTests` |
| UI | `web/checkins/` | check-in form |

## 7. Analysis history
Per feature, append-only; a re-entry that finds nothing new still gets a row.

| # | Date | Source identity | Pass | Scope (folders) | Result |
|---|---|---|---|---|---|
| 1 | 2025-03-02 | commit and digest (helper) | 0 | whole system | 4 features, milestone events only |
| 2 | 2025-03-09 | same commit | 1 | `src/Checkins/` | 6 slices, 2 questions open |
| 3 | 2025-03-30 | newer commit | re-entry | `CheckinService.cs`, `db/checkins/` | one rule changed, rows E8 and E9 reread |

## 8. Decisions
One row per decision, written in the same batch as the model edit that applies it.

| # | Kind | Declaration address | Why | Evidence ids | Confirmed by |
|---|---|---|---|---|---|
| D1 | merge | `Cloakroom/Intake/TakeInCoat` | the insert and the hook update are one decision | E1 | code |
| D2 | order | `BookingAVisit` | a visit needs a registered pet | E2 | expert |
| D3 | not modeled | none | nightly hook reset has no known owner | E4 | assumed |

Kinds: element mapping, order, term, override (an expert outranks the code), merge or split, not
modeled, reference data, assumption. Record every decision, not only the hard ones, and an expert's
answer is one too. `Confirmed by` is `code`, `expert` or `assumed`; an assumed row
also has a question in section 4. Decisions about the target (keep, change, remove) stay in the
evidence rows, not here.

## 9. Terms
The model carries the business word; this table carries the code word, so code readers and business
readers find each other.

| Code term | Business term | Where used | Confirmed by |
|---|---|---|---|
| `PetType` | Species | `Pets` table, `PetService` | expert |

## 10. Realization convention
After the first few features are evidenced, write down how this system realizes each kind of
behavior, citing evidence ids: creation is an insert into the entity table, a transition is a status
column update, a read is a repository method plus DTO, background work is the named scheduler. Use
it to speed up later passes. A feature that deviates from it is a question, because deviations are
where hidden rules live. Where the code realizes features in two styles (an old and a newer module),
record both and which features use which; read a later feature against its nearest neighbour. A
pass that finds the convention wrong corrects it in the same batch, citing the evidence. To learn
the convention before any feature is mapped, trace one recently changed, ordinary feature
(`signal-to-intent.md`, "Targeted read paths").

## 11. As-is slice map
One row per candidate slice, written from Pass 1 on and refreshed by re-entry. It says which code
realizes the slice today. It is evidence, not a delivery record: the delivered code of the target
is in the render skill's fallback record. Three later steps use it: re-entry (which folders to
diff per slice), the hand-written-change check (which slice a changed file belongs to) and a
side-car migration (which legacy route to divert per slice, which columns feed which event
properties).

| Slice address | Kind | Entry point | Writes (table, columns, status value) or published message | Read (query method, DTO, route) | Trigger (job, listener, hook) | Tests | Evidence ids |
|---|---|---|---|---|---|---|---|
| `Cloakroom/Intake/TakeInCoat` | StateChange | `POST /api/checkins` to `CheckinService.TakeIn` | insert `Checkins`; update `Hooks.occupied = 1` | none | none | `CheckinServiceTests.TakesInACoat` | E1, E6 |
| `Cloakroom/Collection/CoatStatus` | StateView | none | none | `HookRepository.ByCoat` to `HookDto`, `GET /api/hooks/{id}` | none | none found | E1, E7 |

Locators are `path:start-end` at the recorded source identity. A slice whose code cannot be found
is `not found`, never "to build", and no location is invented. A candidate slice with no as-is row
is new behavior: an expert-approved target requirement, and the row says so.

**Field chain** (Pass 2). Per event property, the path it travels in the legacy code: request
field to entity property to column (`ticketNumber`, `Checkin.TicketNumber`,
`Checkins.ticket_number`). An event property with no chain is a question. A column in no chain is
"not extracted, because ...". Together the chains are the column-to-property map a historical
extraction needs.
