<!-- cratis-ai-managed: skills/cratis-screenplay-reverse-engineering/references/intake-sources.md -->
# Intake sources

Each source writes rows into the same evidence file (`evidence-format.md`). Record what was
used, what was missing and what each source cannot show. Order: the running system and its
features first, then the shipped executable, source code last.

## Contents

- Choosing sources
- Running app (UI walk)
- HAR captures
- OpenAPI documents
- Screenshots
- Product documents
- Shipped executable
- Source code

## Choosing sources

| Source | Shows well | Cannot show |
|---|---|---|
| UI walk | screens, journeys, visible rejections, what a view lists | server rules, authorization, events, idempotency |
| HAR | exact requests and responses, status codes, field names, ordering | intent, rules that did not trigger, anything not exercised |
| OpenAPI | the declared surface: routes, inputs, response shapes, auth schemes | behavior, rules, whether a route is used or accurate |
| Screenshots | what a person saw when no live app is available | structure; treat as weak and say so |
| Documents | stated purpose, vocabulary, roles, policies | what the system actually does |
| Executable | exposed commands, menus, routes, strings, embedded schemas, files written, endpoints | whether users reach a feature, rules; see `executable-intake.md` |
| Source (last, optional) | rules, branches, jobs, schemas | what is deployed or used; see legacy extraction |

Combine at least two kinds before claiming a rule. One source alone gives a candidate, not a fact.

## Running app (UI walk)

Follow `../../cratis-screenplay-legacy-extraction/references/ui-observation.md`: capability check,
two questions at most, approved test identity, bounded and state-aware walk, before/after
states, grouping into flows. Differences for this skill:

- Observation ids use the `OBS-` prefix and are the locators for evidence rows (the state id).
- Keep page text and the visible rejection text exactly; screenshots only on a state change that
  text does not show, saved under `captures/screens/` with the state id in the file name.
- Writes need approval naming the target and action. A read-only walk covers navigation, lists
  and details; submit forms only with approved example values.
- Log out and destructive actions are skipped unless the brief names them.

## HAR captures

- Obtain the HAR from an approved session (browser devtools export) or from the walk.
- **Scrub first**: remove cookies, `Set-Cookie`, `Authorization`, API keys, CSRF and session
  tokens, and personal values from request and response bodies, before extraction or any hosted
  model. Verify by searching the scrubbed file for the header names and for token-shaped
  strings. Keep only the scrubbed copy under `captures/`; delete the raw export.
- Digest with a script, not the model: group entries by method plus route template, then record
  status-code mix, request field names, response field names, call order and timing. Drop static
  assets, analytics and third-party hosts.
- Locator: `har:<file>#<entry index>` or a request id from the digest.
- Only claim an effect the capture confirms (a 201 plus a later read showing the item). A 200
  with no follow-up read is a request, not a confirmed change.

## OpenAPI documents

- Treat the document as a claim about the surface. Each operation is an L0 inventory row.
- Locator: `openapi:<operationId or METHOD path>`. Strength: `Declared`.
- Mutating operations are command candidates; `GET` operations are query candidates. Names are
  hints only. Parameters and schemas give field names, types, required flags and enums.
- Cross-check with the HAR or walk: unused operations and undocumented calls are both findings.

## Screenshots

Use when no live app exists. Locator: file name plus a short description of the region. Strength
`Stated` at best; read visible labels as business terms, and record anything legible only by
inference as `unknown`. Do not send screenshots with personal data to a hosted model without
approval.

## Product documents

Locator: document title plus section heading or page. Strength `Stated`. Documents give
vocabulary, roles and intended rules; where they disagree with an observation, keep both rows
and mark the conflict.

## Shipped executable

A native binary, bundle, .NET assembly, APK, CLI tool, installer or container image. Read
[executable-intake.md](executable-intake.md) for safe handling, what to inventory and locators.

## Source code

Last and optional: when offered, use `cratis-screenplay-legacy-extraction` (locator `path:start-end`, strength `Read`).
