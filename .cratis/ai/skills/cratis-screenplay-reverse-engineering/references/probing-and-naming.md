<!-- cratis-ai-managed: skills/cratis-screenplay-reverse-engineering/references/probing-and-naming.md -->
# Probing, time, naming and defects

## Contents

- Boundary and rejection probes (required per command)
- Confirm surprises and enumerate the query surface
- Time-based views
- Client-side or server-side
- Naming fidelity
- Observed defects
- Queries the renderer can serve

What a walk must exercise beyond the happy path, and how to label what it cannot observe. Rows
follow [evidence-format.md](evidence-format.md). Probes are writes: they need the write approval
of the walk, on a target the user owns.

## Boundary and rejection probes (required per command)

Rules that were never submitted are never seen. For EVERY command in scope, send the probes
directly to its endpoint (bypass the UI; take the route from the OpenAPI, `/stage/status` or your
own traffic) with fresh marker ids, and record one row per probe (accepted or rejected, plus the
exact response and `validationResults[].message`). The eight probe classes:

- a valid baseline;
- empty required text and whitespace only, each required field alone, and all empty together;
- over-long text, past any limit the form or schema hints at (the limit and limit + 1; 255 and 256);
- zero, negative and the largest plausible number plus one;
- an invalid enum value, format or date;
- an unknown, never-registered id, for every command that references an existing aggregate;
- a duplicate create: the same id twice, and the same natural value under a second id;
- an invalid state transition (archive twice, restore a non-archived item, complete a completed
  one, move to the current stage);
- **limit + 1**: when a limit is suspected, probe the boundary and one past it (100 and 101 for a
  100 limit), and the common sibling limits (50, 100, 200, 255) when a sibling field has one;
  "100 accepted" alone proves no limit;
- **field combinations**: a rule that fails only when several fields are empty together
  (at-least-one-of, mutually dependent fields) never shows in per-field probes. Probe all
  fields empty, each one empty alone, and all filled;
- a duplicate is a second, distinct id with the same value, sent sequentially; read the body.
  Do this for every name-like field of a create command: a uniqueness rule is confirmed or
  refuted only by this probe, never by a UI success;
- **toggle commands** (complete and reopen, archive and restore, assign and unassign): probe
  the inverse and read back the whole read-model row before and after, field by field, not only
  the headline flag; a timestamp or reason the inverse should clear is a rule.

Budget: about ten probes per command, written as a script that sends the whole batch for one
command and prints one table (`evidence-format.md`, probe table); the batch counts as one walk
state. Classify each result of a free-form probe as **intent** (a consistent, deliberate
rejection or message), **defect** (coercion, crash, HTTP 500, silent wrap) or `unconfirmed`
(one observation) before it becomes a row; defects follow "Observed defects" below.

Read the command schema as well as the form: concept names (`x-concept`), enums, required flags
and formats show value types the screen hides. When the first call is unknown, request the
OpenAPI or schema endpoints (`/openapi.json`, `/swagger`, `/api/docs`) before guessing the
request envelope and auth shape; if none exists, record that and make one read call first.

**Probe coverage gate.** Keep `re/PROBES.md`: one row per command and probe class, each
accepted, rejected or `not probed: <reason>` (write scope not approved, destructive, no id
available). Synthesis does not start until every in-scope command has all eight classes
recorded or explicitly `not probed`. End the file with a coverage check by catalog class: rule,
rejection, flow (read-model effect of each command), time, authorization (each role or
anonymous, when approved) and query shape (every argument of every query). Each class lists at
least one probe row or an explicit reason it was skipped; an empty class blocks synthesis.
A command with two or more optional text fields always has a row for all fields empty together. A `not probed` class counts as unknown in the report,
never as "no rule". Ask for the write approval once, with the matrix attached. Each rule the
assemblies declare (`executable-intake.md`) gets a probe that tries to violate it; a mismatch
between assembly and server is an observed defect or dead code, recorded either way.

Copy `validationResults[].message` verbatim into the rule's `message` and note the member and
the reason: a framework typing failure is not a model rule. Use fresh ids and a marker on test
data for every run, in a sandbox instance, so repeated sweeps do not pollute read models.
An out-of-range or missing value that is coerced or crashes (missing enum defaults to the first
member, overflow wraps, missing id gives HTTP 500) is an observed defect, not intent: record
it under "Observed defects" and model the intended behavior.

## Confirm surprises and enumerate the query surface

A result that contradicts a standard pattern (a uniqueness constraint that accepts a duplicate,
"no validation at all") needs a confirming second probe, designed differently, before it
becomes a row; until then it is `unconfirmed`. A wrong `Confirmed` row is worse than a gap.

The UI shows only the queries its screens use. On a system the team owns or is authorized to
probe (the owned-only limit above holds), also enumerate the surface from your own traffic and
the schema: by-id lookups, filters and argument variants of each query, and the routes seen in
the capture. Probe every argument of every list query as true, false and omitted, and record
each one that changes the result as an optional filter (`synthesis-rules.md`, Intent in the mapping);
an argument seen in traffic but not probed is a gap, not an absent argument. Read the error text
for required arguments. A query that returns HTTP 202 or
`isReady: false` is warming up: retry after a few seconds before recording it empty.

## Time-based views

A query, screen or filter whose name or text mentions time (overdue, due today, dormant, recent)
has a threshold that only data can reveal. Seed records through the commands' own date fields
(for example occurred, due or last-activity dates) at day offsets around the suspected threshold:
0, 1, N-1, N and N+1 days, today plus and minus one, near local midnight, done and not done.
Diff the result sets to find the boundary and whether the edge is inclusive; where assemblies
are available, confirm that the decompiled threshold and the observed one agree. If the server
refuses backdated writes, record that as its own rule. Also probe an item whose reference is
undefined (a stage or status never created) to see whether it counts as open or is excluded.
How to model the result, and what the Screenplay surface cannot run, is in
`synthesis-rules.md` ("Time facts"). If a threshold could not be exercised, record the
placeholder value as an `unknown` with a question; never ship a guessed number unrecorded.
When the server clock cannot be set, seed relative dates from the host clock (`today`,
`today±N`), run well away from local and UTC midnight, and record the zone assumption and run
time in PROBES.md. Probe each state filter (completed, archived, stage open, won, lost,
undefined) as its own row.
The server's time zone and its "today" are standing unknowns: seed near midnight and with
backdated dates, and look for a zone setting or response header. A today-relative rule the
declarative dialect cannot express goes in a query performer body plus an `event-model.md` gap
row (`synthesis-rules.md`, Time facts), with specs on fixed dates. Record the zone as an
explicit assumption and add a spec near midnight. A probed time rule is never left as a `GAP`
comment or prose, and never as a guessed filter.

## Client-side or server-side

A rejection seen only in a UI form is a hypothesis: the form may validate what the server
accepts. Confirm with a direct API call (or the same field on a sibling command) before modeling
a rule. Mark each rule row `client-side`, `server-side` or `unconfirmed` in `fact` or
`unknowns`. Prefer the API response over the form when they differ; an unconfirmed rule is
modeled as a question, not a `validate` line.

## Naming fidelity

When the target exposes artifact names (command, field, concept and route names in the UI,
schema or OpenAPI), record that those names are `Observed`. Event names, streams, event payloads
and read-model, query and screen names that are not exposed are `Inferred`: label them as
inferred in model comments, and report naming fidelity separately from behavior fidelity
(`behavior-catalog.md`), because an exposed command name makes its past-tense event name look
confirmed when it is not. Names from assemblies are legitimate evidence for a run that has
them, in their own evidence class.

## Observed defects

Model behavior that looks like a defect (a stale join, a missing filter) as observed, with a
comment flagging the suspected defect, and specify it so the round-trip proves the model is
faithful. List each in a findings section of the report for the owner to decide; never fix one
silently, and never present it as intended.

## Queries the renderer can serve

When an observed rule is expressible declaratively (filter, join, sort, count), prefer a
declarative query. A code-performed query is opaque: `cratis run` cannot serve it and
specifications cannot execute it. Record each opaque query as `unverified` in the fidelity
result and the report.
