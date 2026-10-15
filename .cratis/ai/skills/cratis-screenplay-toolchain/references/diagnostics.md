<!-- cratis-ai-managed: skills/cratis-screenplay-toolchain/references/diagnostics.md -->
# Diagnostics: meaning -> fix

## Contents

- Warnings that hide defects (always validate with `--warnings-as-errors`)
- Source reference and identifier checks
- Public events and translation direction
- Explicit enum member numbers
- Named keys, production routes and observer filters
- Caller identity, compliance and routes
- Specification metadata, callers, cases and locators
- Guarded interaction diagnostics
- Structural completeness and timeline checks
- Editor versus C# verdicts
- Consistency errors (V1: the model contradicts itself)
- Binding (V2 and V3) - see `executable-subset.md` for the full table
- Frequent errors
- Render admission (V5): `STAGE-*`, `CLI-RENDER-*`, `STAGE-CRATIS-*`

Authority: Screenplay `Documentation/screenplay/diagnostics.md` and
`Source/DotNET/Screenplay/Diagnostics/DiagnosticCodes.cs`. Codes are permanent; match the code,
never the message. Severities: E (does not compile), W (compiles; usually an unresolved name),
I (never fails, even with `--warnings-as-errors`). `PLAY` = compiler, `SP` = Arc generator,
`STAGE-*`/`CLI-RENDER-*` = renderer.

Where each layer appears: `screenplay <folder>` / `cratis screenplay validate` = syntax and
consistency (V1). MCP `executable-diagnostics` = binding (V2 reads them, V3 needs none:
PLAY0268-0274, 0350-0352, 0389, 0449). `cratis render` = binding with the bundled compiler,
then STAGE (V5 admission). Tool differences: `versions.md`.

## Warnings that hide defects (always validate with `--warnings-as-errors`)
| Code | Meaning | Fix |
|---|---|---|
| PLAY0029 | unknown word in a slice body: the construct is **skipped** | fix the keyword; `type`/`concept` belong at top level |
| PLAY0165/0166/0167 | unknown type / event / policy (0167 is E inside a persona) | declare it, or validate the **folder** (cratis file mode ignores imports) |
| PLAY0177 | `reads` a read model no projection builds | add the builder or fix the name |
| PLAY0248 | reaction `when` names no event/trigger | declare it |
| PLAY0195 | `invokes` an unknown command | declare it |
| PLAY0381 | projection-level `key` routes nothing | put `key` on each `from` |
| PLAY0455 | quoted import glob matches no file | fix the glob |
| PLAY0196/0197/0198 | unknown query/screen; ambiguous bare name | declare or qualify `Slice.Name` |

## Source reference and identifier checks

At Screenplay 4.127.0 these are V1 source checks, not binding-only gaps:

| Code | Severity | Meaning |
| --- | --- | --- |
| PLAY0514 | W | Projection mapping, children or nested target absent from the declared read-model/element shape; unknown/imported shapes are not guessed |
| PLAY0166 | W | Unknown event, including `remove with`, `remove via join on` and capture `append` |
| PLAY0515 | E | Personal/secret concept in a command identifier, typed `for` destination, event-source identity, stream-id type/part or route mapping source (including nested paths); keep a surrogate identity and protected data in payload |

Legacy compliance spellings (`@pii`/`@sensitive`) do not evade PLAY0515.

## Public events and translation direction

Source checked at Screenplay 4.135.0; all are errors. These constructs bind as released
ESM v9, except public/foreign imports without local shapes (PLAY0268).

| Code | Meaning / fix |
| --- | --- |
| PLAY0607 | A command produces a public event; move publication to outbound Translate |
| PLAY0608 | Local public production outside explicit outbound Translate |
| PLAY0609 | Outbound translation has zero or several local public event types; repeated instances of one type count once |
| PLAY0610 | Foreign public consumption outside explicit inbound Translate |
| PLAY0611 | Outbound consumes a public or foreign event; consume private local facts |
| PLAY0612 | Inbound produces a public or foreign event; produce private local facts |
| PLAY0613 | Foreign public event produced locally; translate to a private event |
| PLAY0614 | Public metadata without explicit direction; typed repair declares inbound/outbound only when exactly one direction fits |
| PLAY0615 | Outbound contains an external-data capture |
| PLAY0616 | Explicit inbound consumes private or local events instead of public foreign events |
| PLAY0617 | Outbound produces a private event instead of its one local public type |
| PLAY0618 | Event-target projection/reducer outside explicit outbound Translate |
| PLAY0619 | `source events` capture outside explicit inbound Translate |
| PLAY0620 | Missing, malformed or repeated `from <Event>` in `source events` |

Only PLAY0614 in this range has a repair. Legacy directionless translations
without public metadata stay inbound. PLAY0027 also covers malformed, repeated
or non-Translate direction. Repair discovery uses `repair-capabilities`, workspace
`repairs`, then revision-checked `propose-repair`; do not guess a direction.
See automations-and-translations `references/public-events-and-direction.md`.

## Explicit enum member numbers

Source errors from Screenplay 4.137.0; ESM v12 admission from 4.139.0:

| Code | Severity | Meaning / fix |
| --- | --- | --- |
| PLAY0667 | E | Mixed numbered/unnumbered members; number all or none |
| PLAY0668 | E | Duplicate enum number; aliases are not supported |
| PLAY0669 | E | Number outside int32 (-2147483648 through 2147483647) |
| PLAY0670 | E | Malformed number: decimal digits, optional minus, no leading zero, fraction, exponent or plus |
| PLAY0671 | I | Opt-in `enum-pinning` completeness hint for an unnumbered enum used by an event property; ordinary compilation does not report it |

Numbering errors have no automatic repair. PLAY0671 suggests the opt-in
`propose-number-enum-members` refactoring, not a correctness repair. Member names
remain specification values. Non-positional pins select v12; positional pins keep
older executable bytes/version. Consumer admission is separate (`versions.md`).
Source: `v4.139.0:Documentation/screenplay/diagnostics.md`, decision 0065.

## Named keys, production routes and observer filters

| Code | Severity | Meaning / fix |
| --- | --- | --- |
| PLAY0660 | E | Invalid top-level read-model key owner/modifier/type; multipart parts must be scalar; no repair |
| PLAY0661 | E | Missing single-instance lookup parts; repair **command reads only** if every missing part has one same-named compatible source, preserving authored mappings and refusing ambiguity |
| PLAY0662 | E | Invalid block/shape, duplicate or unknown part, incompatible/optional/collection/literal read source; block needs at least two distinct parts; no repair |
| PLAY0663 | E | Route on reaction/refusal-branch production; only command event productions override routes |
| PLAY0664 | W | Override repeats complete command route; typed repair removes the redundant override |
| PLAY0665 | E | Invalid/duplicate/child-bearing/unresolved source/stream filter or filtered reaction with a non-event trigger |
| PLAY0666 | W | Filter excludes all statically known effective producer routes; C# only, handlers/foreign origins suppress it |

Queries and trigger reads have no PLAY0661 repair. Composite view keys, query
blocks and their fixtures remain PLAY0268 (#599), while one explicit key binds
byte-identically to inference. Fixtures missing any part report PLAY0351.
Overrides/filters select released ESM v10; malformed routes also use PLAY0504.
Read read-surface `references/named-keys.md` and
[production routes and filters](production-routes-and-filters.md).

## Caller identity, compliance and routes

Source authority: v4.127.0 `diagnostics.md`, `identity.md`, `concepts.md`,
`purposes.md`, `events.md`, `event-sources.md`, `reactions.md`.

| Code | Severity | Meaning / fix |
| --- | --- | --- |
| PLAY0152 | E | Bare `$identity` needs a property path |
| PLAY0155 | W | A `$identity` or `$context.identity` path names a property the caller does not carry; correct the built-in/detail name (claims suffixes are opaque) |
| PLAY0633 | E | Invalid identity header; write bare `identity` |
| PLAY0634 | E | Second identity block in one document; a second file uses the folder singular-declaration diagnostic |
| PLAY0635 | E | Invalid typed detail or escape source; exactly one source per detail |
| PLAY0636 | E | Unknown source kind; use claim, query, inline code or file |
| PLAY0637 | E | Detail has no source/implementation |
| PLAY0638 | E | Claim/query source has a body; refresh/cache belongs to runtime |
| PLAY0639 | E | Duplicate detail name |
| PLAY0640 | E | Detail redeclares a token built-in |
| PLAY0641 | E | Unknown/ambiguous source query |
| PLAY0642 | E | Source query is unkeyed or returns a list |
| PLAY0643 | E | Query source lacks `by <expression>` |
| PLAY0644 | E | Query key reads more than built-ins, claims or literals |
| PLAY0645 | E | Source query's effective gate depends on details |
| PLAY0646 | E | Result type/optionality differs from detail; optional result needs optional detail |
| PLAY0565 | I | Deprecated `@pii`, `sensitive`, `@sensitive`; repair to bare `pii`/`secret`, preserving notes/trivia |
| PLAY0566 | E | Unknown concept marker |
| PLAY0567/0568 | E | Invalid secret scope / repeated scope |
| PLAY0569 | W | Explicit secret scope ignored on `pii secret` because only `[PII]` renders |
| PLAY0570/0571 | E | Invalid personal-data qualifier/category / repeated special category |
| PLAY0653 | W | Repeated concept-header compliance marker; `pii personal` is one marker repeated. Keep the first; body settings stay attached; canonical printing writes one marker |
| PLAY0590 | E | More than one event subject; choose one, no safe automatic repair |
| PLAY0591 | E | Subject not required scalar String/Uuid or a String/Uuid/Int-backed concept |
| PLAY0592 | E | Protected subject concept; EventContext.Subject is plaintext, use a surrogate |
| PLAY0593 | E | Subject modifier outside event properties |
| PLAY0594 | E | Read-model subject mark is unsupported |
| PLAY0595 | E | Repeated/out-of-order subject modifier; write it last |
| PLAY0596/0597/0598/0599 | E | Malformed purpose declaration/reference/field; unknown closed value; repeated singleton; duplicate purpose name, respectively |
| PLAY0600/0601 | W | Unknown purpose reference / inconsistent legitimate-interest declaration |
| PLAY0602–0606 | W | Opt-in purposes coverage: uncovered pii, missing Art. 9 condition, Art. 10 authorization, basis, or unused purpose |
| PLAY0647 | E | Malformed/repeated/misplaced `runs as`, empty/duplicate role; one reaction-level line with quoted roles (zero allowed) |
| PLAY0648 | W | Gated invocation without declared identity; an authorization refusal branch gets PLAY0557 instead, once per invocation |
| PLAY0649 | W | Identity declared without invokes or inline/file implementation |
| PLAY0650 | W | C# bound-gate check: role needed by no invocation; opaque gates/implementation bodies suppress it |
| PLAY0651 | W | C# bound-gate check: declared actor definitely cannot satisfy gate; unknown is not denial |
| PLAY0652 | W/I | Opt-in `privilege`: trigger producer lacks every declared actor role (W), or has opaque gates that cannot be compared (I); clock/application triggers are silent |
| PLAY0504 | E | Missing/ambiguous stream, invalid/mismatched route parts, nominal source identifier/destination type mismatch or invalid stream-id literal; never a warning |
| PLAY0549 | E | Invalid specification route/mapping, including invalid stream-id literals |

Stream-id text must be nonempty, well-formed Unicode NFC: refuse, never normalize.
Integer ids in Double mode are within ±9007199254740991; Exact mode has no such
bound but remains unadmitted. Specification contradictions compare canonical ids,
so UUID casing does not distinguish routes. Composite encoding is ordered and
escapes `%`/`|`, not an inferred split of legacy text.

Identity declarations and event `subject` are metadata; reading a declared
`$identity` detail in executable clauses is PLAY0268. `runs as` is admitted as
ESM v10 in the current standalone, not a current PLAY0268 gap. At standalone
4.142.0, concept compliance binds as v13 (since 4.141.0); enum/identity compliance and protected
events with explicit subject marks still refuse PLAY0268. CLI 3.57.0's 4.147.1
bundle also binds concept compliance; purpose metadata is PLAY0270. See command-surface
`references/compliance.md` and `references/context.md` for syntax and limits.

`screenplay_repair_capabilities` and `propose-repair` advertise PLAY0653: a
concept-subject, line-only `PreserveTrivia` repair, no document-root or pinned
repair evidence. PLAY0565 supports one discovered `line` or a document-root
migration. Preview every repair; legal notes never move automatically.

## Specification metadata, callers, cases and locators

Authority: Screenplay v4.127.0 `diagnostics.md`, `specifications.md`, `slices.md`.

| Code | Severity | Meaning / fix |
| --- | --- | --- |
| PLAY0558 | E | Module/feature/slice/command/read-model/reaction documentation must be one nonempty fenced Markdown block; remove repeats or fix the fence. Events retain PLAY0477. |
| PLAY0559 | W | Conflicting module/feature documentation across files; first wins. Choose one owning text; identical copies are accepted. |
| PLAY0572 | E | Malformed/unknown persona caller or body lines; use a declared top-level persona with no body. |
| PLAY0573 | E (binding) | Persona synthesis refused (no policies, negation, required non-literal/role-URI claim, opaque or unresolved policy); inspect persona/policy/reason and use explicit `given caller`. |
| PLAY0574 | W | Opt-in `personas`: persona gates nothing. |
| PLAY0575 | W | Opt-in `personas`: gate denies every synthesized persona; unknown is not denial. |
| PLAY0576 | I | Opt-in `personas`: multiple unpinned buildable `or` alternatives; inspect chosen witness or pin a policy. |
| PLAY0577/0578 | E | Malformed parameter/case header; typed parameter has no body/default, case has a name and at most one inline assignment. |
| PLAY0579/0580 | E | Duplicate parameter/case name. |
| PLAY0581/0582 | E | Need parameters and cases; every case assigns each parameter exactly once, no extra names. |
| PLAY0583 | E | Case value is not concrete or cannot normalize to its type. |
| PLAY0584 | E | Unknown parameter or excluded reference position (callers/redelivery locators/clocks/names/nested literal members). |
| PLAY0585 | E | Optional parameter feeds required target. |
| PLAY0586 | E | Derived `<Specification>_<Case>` collides in scope. |
| PLAY0587 | E | Unknown or incompatible parameter/target type. |
| PLAY0588 | W | Unused parameter; remove it or use it intentionally. |
| PLAY0589 | E | Singular effective expansion cannot handle a table; use `ExpandAll`. |
| PLAY0526 | E | Route in command/read-model example; only event examples carry routes. Top-level `streamId = value` is payload. |
| PLAY0543 | E | Malformed redelivery or not exactly one definite match with no undecided candidate; narrow `for`, values, `stream` or `no stream`. |
| PLAY0548 | E | Route on `when <Command>` only; put it on the command declaration. Does not fire on redelivery locators. |

Descriptions/documentation are report-only PLAY0270, not executable conditions.
Case tables and persona callers expand before binding without a new ESM version.
Routes do not make redelivery executable: it still reports PLAY0268.

## Guarded interaction diagnostics

| Code | Severity | Meaning / fix |
| --- | --- | --- |
| PLAY0560 | E | Alternatives on a non-item trigger; only click/double click/select admit them. |
| PLAY0561 | E | Alternatives mixed with plain actions or `where`; choose one form. |
| PLAY0562 | E | Empty branch; supply a nonempty action list. |
| PLAY0563 | E | One-line labeled-action `when … execute …` in an interaction; use an indented list. C# offers a typed repair. |
| PLAY0564 | W/I | Item-trigger opaque `where` deprecated: a strict item condition warns with a C# repair to one `when`, no fallback; other text is information with no repair. Other triggers unchanged. |

PLAY0563/0564 repairs need individual review and canonical formatting, refuse
comment loss and do not support pinned evidence. TypeScript diagnoses but does
not offer these repairs. Preview through MCP `propose-repair`, never text-patch
around a refusal. Condition/subject errors also use guarded-action PLAY0341–0348.

## Structural completeness and timeline checks

Opt in with `--check <names>` or MCP diagnostics `checks`, then treat warnings
as errors if gating. Ordinary compile success does not imply these ran:

| Family | Codes | Checks |
| --- | --- | --- |
| data-bindings | PLAY0530/0531 | Conflicting visible bindings; binding/query read-model or cardinality mismatch |
| input-surfaces | PLAY0532/0533 | Missing command input form/issuing screen; missing reachable issuer |
| field-origins | PLAY0534 | Missing read-model builder or top-level field origin (AutoMap, identity and variants considered) |
| query-keys | PLAY0535 | Query argument not representable by known identity/fields |
| event-consumers | PLAY0536 | Latest local event generation lacks an operational consumer; a terminal fact can legitimately be reported |
| navigation | PLAY0537 | Unreachable screens; unreachable cycles do not establish an entry point |
| privilege | PLAY0652 | Elevated event-trigger producers must require every actor role; opaque gates are information, clock/application triggers exempt |
| purposes | PLAY0602–0606 | Personal-data coverage, special/criminal safeguards, basis and unused-purpose prompts; union of ancestor/direct references |
| personas | PLAY0574–0576 | Unused personas, unreachable gates and ambiguous buildable policy alternatives; unsynthesizable/undecidable is unknown |

Whole-source errors skip completeness checks. Unknown/opaque coverage is not
invented: report skipped status/coverage and walk field lineage manually.

PLAY0516/0517 are **information**, so never fail `--warnaserror`. They review
presentation order, not execution: a backward event/read-model dependency or a
mutually dependent sibling group. A cycle group replaces internal individual
backward findings. Ordering roots are the sole source document, an importing
`application.play`, or a unique importing root; without one, folder timeline
checking is skipped. Safe PLAY0516 repairs move siblings/imports or pin files
before a retained glob; cycles and own-sub-feature findings have no repair.

## Editor versus C# verdicts

Monaco/VS Code use the TypeScript syntax/authoring compiler. They share projection
and protected-identifier checks (PLAY0514/0515), typed-example syntax (0518/0519),
guarded actions (0341–0348, including item paths/input bindings 0345–0348),
identity details (0633–0646), compliance/subject checks, reaction identity
(0647–0649), and reaction-refusal syntax/checks (0538–0545).
A shared catalog code is not proof that a check is implemented in both compilers.

C# CLI/MCP owns whole-application scoped diagnostics, opt-in completeness
(including PLAY0530–0537, PLAY0602–0606 and PLAY0652), bound reaction gate
checks PLAY0650/0651, semantic binding and PLAY0546 (a negated claim targets optional,
unavailable-subject or non-text data). The VS Code repair bridge is not a general
C# validator. A clean editor is not V1 for the complete application, V3 binding
or V4 execution. Run the CLI or MCP for those verdicts. Authority:
`v4.127.0:Documentation/screenplay/editor-diagnostics.md`, `diagnostics.md`.

## Consistency errors (V1: the model contradicts itself)
| Code | Meaning |
|---|---|
| PLAY0282 | `validate` rule targets a field absent from the command |
| PLAY0283 | `reads View by field` type matches no `by` param of the view's queries |
| PLAY0284 | `children`/`nested` never populates a declared element field |
| PLAY0285 | a spec's `then` event contradicts every possible declared producer of its `when` command (decidable literals, property copies, equality conditions). Fix the model or the spec deliberately. **False positive** on reaction cascades with the compiler bundled in cratis before 3.28.2 (4.60.1; cli#242; reachability through reactions and `invokes` was added later); gone in 4.66.0 and cratis 3.28.2 (probed). On an older bundle confirm with the standalone tool, record a tool gap, do not "fix" a correct model |
| PLAY0286 | spec value is not a member of the enum |
| PLAY0287 | producer/capture/spec assigns a field the event lacks |
| PLAY0291-0294 | bad single-line JSON / unknown key / wrong shape / duplicate key |

## Binding (V2 and V3) - see `executable-subset.md` for the full table
| Code | Meaning | Fix |
|---|---|---|
| PLAY0268 | construct the ESM cannot represent; read the message for the construct (`executable-subset.md` lists the common ones: handler, list or observable query, `pii`, `produces when` over read models, v6 constructs on cratis before 3.28.2) | stay in design mode, or change the construct if the domain allows; never strip `pii`/authorization |
| PLAY0269 / 0270 | UI deferred / authoring metadata (I) | none |
| PLAY0271 | legacy `reads` or a `concurrency` block keeps its legacy meaning and cannot bind (error) | design gap; a constraint for uniqueness; protected decisions are a target requirement |
| PLAY0273 | incoherent: ambiguous `for`, property not on event revision, operand type mismatch, derived `$eventContext` path | fix the reference |
| PLAY0350 | `null` in command/event value | Chronicle semantics (nullable event properties are CHR0012; an optional fact is a separate event), not only a binder limitation: specs cannot state an absent command/event value. Omit the value or record a capability gap; whether the optional detail is really a separate fact is a review question, not this code's fix |
| PLAY0351 | given/then readmodel lacks the key property | add the keyed query's `by` property |
| PLAY0352 | when-less spec asserting events/errors | add a `when` or assert readmodel/query |
| PLAY0389 | gated command/query exercised without `given caller` | add `given caller` (empty block = anonymous) |

## Frequent errors
| Code | Meaning / fix |
|---|---|
| PLAY0001 | unknown top-level word (e.g. `slice` at top level of an un-imported file; `numbers` on a compiler older than 4.64.0) |
| PLAY0007 | a `concept` line is not `concept <Name> : <Type>` |
| PLAY0042 | a `produces` line is neither `produces <EventType>` nor `produces when <condition>` (the event goes on its own line and `for` on the line below) |
| PLAY0048 | a query body line opens with a word a query does not declare by (for example `observable` as a body line; it belongs in the return position) |
| PLAY0295/0297 | `$eventContext` path starts with an unknown member (W) / continues below `causation` or `tags` (E) |
| PLAY0357 | a `$strings.` message without a valid dotted key (blocks binding) |
| PLAY0508-0513 | `numbers exact` preamble errors (syntax only; a valid exact document still cannot bind, PLAY0268) |
| PLAY0006 | tab indentation (W) |
| PLAY0017/0019 | `identifier` outside a command / on an event property |
| PLAY0058 | unexpected token in projection (often a `#` comment) |
| PLAY0114/0117/0120 | policy without `require`; claim needs `claim "x" matches "v"` |
| PLAY0141 | unreadable rule (e.g. `length 3 to 10`; use `min`/`max`/`length ==`) |
| PLAY0191 | more than one builder for a read model |
| PLAY0193 | more than one `for` in a production |
| PLAY0358/0097 | more than one `when` action |
| PLAY0386-0389 | `given caller` shape / duplicates / `then denied` mixed / missing |
| PLAY0390-0393 | constraint: unknown event, property not on event, duplicate name, `ignore casing` on `unique event` |
| PLAY0410/0411/0412 | same view read twice / duplicate alias / alias collision |
| PLAY0453 | `then no readmodel` needs `RM for "<key>"` |
| PLAY0461 | clock instant not ISO 8601 with `Z`/offset |
| PLAY0468 | `when query` argument is not a `by`/`filter` param |
| PLAY0474 | `produces event` (inline) outside a command, e.g. in a reaction |
| PLAY0478 (I) | plain `produces` without `for`: add `for <identifier>` |
| PLAY0469 (I/W) | command identifier copied into the payload while targeting it |
| PLAY0479 (I) | legacy `?` optional |
| PLAY0480/0484 | modifier order `Type optional generated identifier` |
| PLAY0682 | `generate sequence` is reserved; keep the intended sequence as a capability gap |
| PLAY0683/0684 | PLAY0683: generated non-identifier below 64 random bits warns (a single-property unique constraint suppresses it); PLAY0684: generated identifier below 64 random bits is an error |
| PLAY0685 | own response name used under execute failure; outer successful responses remain available. Screenplay 4.148.0, UI composition `references/response-continuations.md` |
| PLAY0289 | (binding) no documents: empty root |

Fix order: E at V1 -> W at V1 -> silent-gap check -> V2 codes (if the mode needs V3).
Report each as `code file:line one-line meaning`; never paste more than about 30 lines.

## Render admission (V5): `STAGE-*`, `CLI-RENDER-*`, `STAGE-CRATIS-*`
Full table with causes: `renderable-subset.md`. The Stage 4.24.x rows below
retain historical source/probe evidence. The pinned CLI/Stage pair (`versions.md`) admits
through v15 to construct checks; above it is CLI-RENDER-004 (direct Stage:
STAGE-ESM-016). Publication STAGE-ESM-032, foreign facts/event captures 033,
claim-reading/opaque reaction gates 034, legacy filters 035 and production overrides 036 remain
refused; reducer filters render. Previous CLI 3.46.1 admitted through v7; its
v7 corpus received STAGE-ESM-028/029, not the older v7 version gate.
Codes in the historical snapshot:
| Code | Meaning |
|---|---|
| STAGE-ESM-016 | model above the admitted ESM schema (v4 on Stage 4.24.2: v5 absence and v6 constructs; 4.24.1 admitted v3, so a v4 model with generations was refused): whole model refused |
| STAGE-ESM-025 | Stage 4.24.2: a typed-context member references a historical event revision or property identity; only the current revision renders |
| STAGE-ESM-026 | Stage 4.24.2: a selected event, or an event a selected scope depends on, is above its initial revision; Stage cannot render event-type migrations yet (Stage#204, needs Screenplay#71); gap-fill and hand-write the migrations |
| STAGE-ESM-024 | ledger-only (Stage 4.24.1 and later): the ESM v6 members are dispositioned Rejected; a v6 model is still refused whole by STAGE-ESM-016, so this is not the code a user sees |
| CLI-RENDER-003 | historical: reported by cratis 3.28.2 for an ESM v4 model with generations; not emitted since 3.28.3, which reports an evolved event with `STAGE-ESM-026` |
| STAGE-ESM-001 | slice kind is not `StateChange`/`StateView` (Automation, Translate: Stage#79) |
| STAGE-ESM-004 / 006 / 013 | more than one command per `StateChange`; conditional or value-expression production; other context value |
| STAGE-ESM-005 / 015 | code rule or code validation; opaque policy or an ownership claim against a Uuid-backed target |
| STAGE-ESM-007 / 009 / 010 / 017 | projection shape (one per read model, cardinality, query delivery, keys, `all`, event-context values) |
| STAGE-ESM-011 | specification shape not rendered (`when append`, `then no readmodel`, composite values) |
| STAGE-ESM-014 | constraint shape (multi-claim in one command, intra-command multi-event change) |
| STAGE-ESM-019/022 | reducer body outside the pure allowlist |
| STAGE-ESM-020 | a body the model requires is missing or changed (content hash check); blocks publication |
| STAGE-CRATIS-005 | a modeled artifact would render into `Customizations/` |
Fix by changing the model only where the domain allows it; otherwise the slice is gap-fill
with the model as its contract (`cratis-screenplay-render-and-gap-fill`).
