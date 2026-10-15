<!-- cratis-ai-managed: skills/cratis-screenplay-legacy-extraction/references/evidence-table-template.md -->
# Evidence table template

Keep one file per model: `.ai-work/screenplay/<model-slug>/legacy/EVIDENCE.md`. It is the single place
where as-is facts, their sources and the proposed target decisions meet. Agents and reviewers
cite row ids (`E12`) instead of pasting code or captures.

Treat the file as confidential when it holds production data. Locators should be route
templates, table and column names, and path:line, not values.

## 1. Coverage (from L0)

| Entry-point family | Count found | Covered by | Not covered because | Route for the gap |
|---|---|---|---|---|
| HTTP write routes | 23 | generate (Arc), Prologue HTTP | PATCH and non-`/api` posts are not proxied | targeted reads E40-E44 |
| Scheduled jobs | 2 | none | no provider reads Hangfire jobs | targeted reads |
| DB triggers | 1 | Prologue (as an uncorrelated transaction only) | trigger body not captured | targeted read of the trigger DDL |

## 2. Evidence rows

| Column | Content |
|---|---|
| `id` | `E<n>`, stable; never renumber |
| `source` | static (generate) / dynamic (Prologue) / code / UI / runtime (Chronicle) / doc / expert |
| `locator` | Repository-backed code/schema claims: the inspected repository-relative `path:start-end`, with the file name exactly as given (`CheckinService.cs:40-54`, never shortened), bound to the source identity. Runtime (Chronicle): target, event store, namespace, schema identity + generation or bounded event range, observation time. Other sources: capture file + record, redacted UI route + observation id, document section/version, generated declaration address + diagnostic, or person + date. Never substitute one source type's locator for another's |
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
| E2 | code | `CheckinService.cs:88-94` | 409 when the hook is already free, message "That coat is not on a hook" | throws `ConflictException` | Read | intent (rule) | n/a | `reads CoatStatus` + `require`, NOT enforced today (target: protected decision or Chronicle `concurrency` scope); no executable error spec, recorded in LOSS.md | keep | proposed | not asked |
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

Put an exact `file:line` locator for every evidence-backed claim (`CheckinService.cs:88-94`)
inline in the final report itself, with the file name exactly as given; a pointer to this table
or another file is not enough, and opaque ids alone do not count. List unresolved gaps. Do not manufacture references to meet
a count. A UI-only or document-only extraction may have no code locators; say so and limit
its claims accordingly.

## 3. Read log
Every code or capture read gets one line so it is traceable and reusable. Reread when the
source changed or rows disagree; note why.

| when | file:lines or capture | resolves | result row(s) |
|---|---|---|---|
| L3 | `Jobs/HookReset.cs:12-40` | E4 | E9 (maintenance job, clears stale flags) |

## 4. Unknowns and questions
List rows still `unknown` or `undecided` with the question that would settle them, who can
answer, and what the candidate assumes meanwhile. Unknowns are kept, not dropped.

## 5. Sign-off
Per slice: expert, date, verdict, open items. Capture cleanup confirmation (who ran which
statements, verification query output summary) or "no capture run".
