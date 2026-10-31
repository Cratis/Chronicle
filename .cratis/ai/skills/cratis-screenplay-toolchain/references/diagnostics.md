<!-- cratis-ai-managed: skills/cratis-screenplay-toolchain/references/diagnostics.md -->
# Diagnostics: meaning -> fix

## Contents

- Warnings that hide defects (always validate with `--warnings-as-errors`)
- Source reference and identifier checks
- Public events and translation direction
- Explicit enum member numbers
- Reserved stored names and event pins
- Named keys, production routes and observer filters
- Ambiguous match patterns (PLAY0803)
- Projection event scope (PLAY0801)
- Reducer instance identity (PLAY0690)
- Caller identity, compliance and routes
- Specification metadata, callers, cases and locators
- Guarded interaction diagnostics
- UI profile shell: root template, options, appearance
- Structural completeness and timeline checks
- Editor versus C# verdicts
- Consistency errors (V1: the model contradicts itself)
- Batches, described handlers, text rules and protected reads
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
then STAGE (V5 admission). Tool differences: `../SKILL.md` ("Tool versions and the contract").

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

These are V1 source checks, not binding-only gaps:

| Code | Severity | Meaning |
| --- | --- | --- |
| PLAY0514 | W | Projection mapping, children or nested target absent from the declared read-model/element shape; unknown/imported shapes are not guessed |
| PLAY0166 | W | Unknown event, including `remove with`, `remove via join on` and capture `append` |
| PLAY0515 | E | Personal/secret concept in a command identifier, typed `for` destination, event-source identity, stream-id type/part or route mapping source (including nested paths); keep a surrogate identity and protected data in payload |

Legacy compliance spellings (`@pii`/`@sensitive`) do not evade PLAY0515.

## Public events and translation direction

PLAY0607–0620 are errors. These constructs bind as ESM v9, except public/foreign imports without local shapes (PLAY0268).

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

| Code | Severity | Meaning / fix |
| --- | --- | --- |
| PLAY0694 | W | An unrouted reaction in a directionless Translate consumes an event its own capture appends (root, children or nested). Keep the capture here and move the reaction to a separate Automation slice with its specs. No automatic repair; `--warnaserror` fails. Other slices' captures and reactions with a `from` route do not trigger this check; explicit inbound local-event consumption is PLAY0616. |
| PLAY0700 | E | Event-capture `key` is not required on every `from` event, or uses a `$` context key. Choose a required content property or deliberately change the source contracts; no repair. |
| PLAY0701 | E | `given capture` / `when capture` names a capture over events. Present `given <FromEvent>` / `when append <FromEvent>`. A compiler-authored repair exists only for exactly one `from` event; several are ambiguous. Review the specification repair individually; no fix-all or pinned evidence. |

Requires Screenplay 4.171.0 or later (`screenplay --version`) for PLAY0700/0701
and event delivery to captures; older runners do not execute that delivery.
Requires Screenplay 4.176.0 or later for PLAY0694; older compilers leave the
legacy directionless co-location unchecked. Full capture execution and ordering:
automations-and-translations `references/public-events-and-direction.md`.

## Explicit enum member numbers

Source errors; the pins bind as ESM v12:

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
older executable bytes/version. Consumer admission is separate.

## Reserved stored names and event pins

| Code | Layer / severity | Meaning / fix |
| --- | --- | --- |
| PLAY0802 | Authoring error (`ReservedStoredName`) | A stored event-source name exactly `Default`, or stored event-stream name exactly `All`, is reserved; the stored name is the `id` pin when present, else the declaration name. Rename the stream with discovered `propose-rename`; keeping `id "All"` cannot fix it, while a pin away from the reserved name (`stream All` with `id "Archive"`) is valid. Comparison is ordinal: streams `all`, `ALL` and `Default` remain valid. Old `All` events read as source-only appends; moving history needs a Chronicle migration, not a language repair. Requires Screenplay 4.213.3 (`screenplay --version`); earlier compilers report these names only at binding, as PLAY0273. |
| PLAY0273 | Binding error | Executable binding keeps refusing a stored source `Default` or stored stream `All` (name or `id "All"` pin) as a backstop to PLAY0802. |
| PLAY0472 | Binding error | An event pin is not a valid event-name identifier, effective stored names differ across generations, or two events share a stored name. Use a valid, application-unique stored name shared by all generations; never rename stored history merely to hide the error. |
| PLAY0686 | Retired | Differing event pins travel as `storedName` in ESM v16. MCP's compatibility `eventPinWarnings` array is empty. Renderer admission is separate: a renderer that does not admit v16 must refuse the model rather than render it under the new name. |

PLAY0802 appears in authoring diagnostics and MCP readiness; match its reserved-name
message before choosing the rename. It adds no ESM version.
Source/stream pins already travel in ESM v8; differing event pins require v16.
Equal-name event pins are no-ops; PLAY0471 removal preserves canonical bytes.
Target evidence: a renderer must admit ESM v16 and preserve `storedName`, or refuse it.

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

Queries and trigger reads have no PLAY0661 repair. Flat composite view keys bind as v27;
projection or removal key blocks missing a part report PLAY0661 and bad parts PLAY0662.
One explicit key binds byte-identically to inference. Fixtures missing any part
report PLAY0351. Joins, children, `every`, `all`, `nested`, `variant`, reducers and
list-query `by` blocks over a composite-keyed view are PLAY0268.
Overrides/filters select released ESM v10; malformed routes also use PLAY0504.
Read read-surface `references/named-keys.md` and
[production routes and filters](production-routes-and-filters.md).

## Ambiguous match patterns (PLAY0803)

Requires Screenplay 4.218.2 (`screenplay --version`); older compilers do not report it.

| Code | Severity | Meaning / fix |
| --- | --- | --- |
| PLAY0803 | W | `AmbiguousMatchPattern`: a `matches` pattern in the portable dialect, such as `(a+)+$`, `(a\|a)*` or `(a*)*`, can backtrack exponentially in a browser engine. Reported by both compilers and editors with the ambiguous subexpression; very large patterns are not analyzed. Rewrite it unambiguously (`a+`, `[ab]*`); see command-surface `text-and-match-rules.md`. |

## Projection event scope (PLAY0801)

Requires Screenplay 4.212.3 (`screenplay --version`) for this ordinary compilation
check in both C# and TypeScript; older executable binding rejected these duplicate
uses later with PLAY0273.

| Code | Severity | Meaning / fix |
| --- | --- | --- |
| PLAY0801 | E | `DuplicateEventInProjectionScope`: an event name occurs more than once in one projection level's event set or separate join set. Reports the second and each later reference, even with identical or different keys. Combine same-row mappings into one block; one event cannot update two rows of that level through two `from` blocks. |

A level is the root, one `children` block, one `nested` block or one variant.
`from` (including each event in `from A, B` lists), `remove with`, `remove via join`
and `clear with` share one set of event names. `join` events have their own set:
once in `from` and once in a join is allowed. Each children, nested and variant
block starts fresh sets; separate projections are independent. Names compare
ordinally without resolving qualifications or generations. If the domain has a
genuine second fact, model it as its own event; never invent events only to
satisfy the read side. Full scope guidance: projections `references/pdl-grammar.md`.

## Reducer instance identity (PLAY0690)

| Code | Severity | Meaning / fix |
| --- | --- | --- |
| PLAY0690 | W | A reducer instance is keyed by the event-source id, so a reducer-backed scalar query `by` another nominal type, or a reducer whose complete producers use several identity types, warns. Reducers cannot be re-keyed: use a projection with a property key on each `from`, or a join for existing instances |

Fails `--warnaserror`. Compares declared nominal types only where producers are known (command `for`, implicit or generated identifiers, reaction `for`); plain `produces` without `for`, reactions without `for`, captures, seeds and code are not compared. Native C# only: a clean editor proves nothing. Example and limits: projections [reducer identity](../../cratis-screenplay-projections/references/reducer-identity.md).

Requires Screenplay 4.170.0 or later (`screenplay --version`); an older compiler does not report it, and `cratis screenplay validate` reports it only when its bundled compiler is that new.

## Caller identity, compliance and routes

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
ESM v10, not a PLAY0268 gap. Concept compliance binds as v13; enum/identity compliance and protected
events with explicit subject marks still refuse PLAY0268. Purpose metadata is PLAY0270. See command-surface
`references/compliance.md` and `references/context.md` for syntax and limits.

`screenplay_repair_capabilities` and `propose-repair` advertise PLAY0653: a
concept-subject, line-only `PreserveTrivia` repair, no document-root or pinned
repair evidence. PLAY0565 supports one discovered `line` or a document-root
migration. Preview every repair; legal notes never move automatically.

## Specification metadata, callers, cases and locators

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
Routes do not by themselves select v17: redelivery executes as v17 (Requires Screenplay 4.157.0), and older compilers report PLAY0268 for it.

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

## UI profile shell: root template, options, appearance

Requires Screenplay 4.200.0 or later (`screenplay --version`); 4.197.0 reads the syntax without the checks. The fix for each is in
`../../cratis-screenplay-ui-composition/references/ui-profile-and-theme.md`.

| Code | Severity | Meaning / fix |
| --- | --- | --- |
| PLAY0788 | E | A `root template` / `new screens use template` line is malformed, or pins a version on an own template. |
| PLAY0789 | E | A malformed or repeated `identity` or `appearance` line, or a second `identity` or `appearance` block. |
| PLAY0790 | E | The template is not declared (or the blueprint does not ship it). Declare it or take it from a blueprint. |
| PLAY0791 | W | The profile selects its shell with the legacy `layout <Name>`. Write `root template <Name>`; the quick fix rewrites it (or removes the ignored line beside a `root template`). |
| PLAY0792 | E | The blueprint is not in the profile's `packages` (or not installed). |
| PLAY0793 | W | The pinned `version` range is not satisfied by the installed blueprint (catalog hosts only). |
| PLAY0794 | E | The root template cannot be the shell (not a layout or application-scope template, fits a slot, `scopes` without `application`). |
| PLAY0795 | E | The root template has no `content` slot. |
| PLAY0796 | W | An `option` the shell does not expose. |
| PLAY0797 | E | Color scheme is not `light`, `dark` or `system`, the accent is not a hex colour, the corners are not `sharp`, `default` or `round`, or an `identity` value is outside Scene's rules (initials longer than 2, unknown icon, image that is not a PNG/JPG/SVG/WebP data URI of at most 400 KB). |
| PLAY0798 | E | More than one `root template` in a profile. |
| PLAY0799 | E | `new screens use template` cannot start a screen in a feature. |

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

For `query-keys` (PLAY0535), a collection `by` can match a same-named compatible
read-model field or composite key part, or the identity. A single-result `by`
still matches the identity, not an arbitrary field. Parameter optionality is
ignored for compatibility; `optional` alone does not establish a match. Identity
inference uses only single-result keyed queries, not collection queries, with
structurally resolved projection keys as fallback. Performer-served views,
tenant-context parameters and unknown identity types are skipped.
Requires Screenplay 4.212.1 (`screenplay --version`) for this check alignment;
older tools can warn on valid collection fields or optional parameters and infer
identity from collection queries. Upgrade before using those warnings as a gate.

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
(including PLAY0530–0537, PLAY0602–0606 and PLAY0652), reducer identity
PLAY0690, bound reaction gate checks PLAY0650/0651, semantic binding and PLAY0546 (a negated claim targets optional,
unavailable-subject or non-text data). The VS Code repair bridge is not a general
C# validator. A clean editor is not V1 for the complete application, V3 binding
or V4 execution. Run the CLI or MCP for those verdicts.

## Consistency errors (V1: the model contradicts itself)
| Code | Meaning |
|---|---|
| PLAY0282 | `validate` rule targets a field absent from the command |
| PLAY0283 | `reads View by field` type matches no `by` param of the view's queries |
| PLAY0284 | `children`/`nested` never populates a declared element field |
| PLAY0285 | a spec's `then` event contradicts every possible declared producer of its `when` command (decidable literals, property copies, equality conditions). Fix the model or the spec deliberately. **False positive** on reaction cascades with an older bundled compiler. On an older bundle confirm with the standalone tool, record a tool gap, do not "fix" a correct model |
| PLAY0286 | spec value is not a member of the enum |
| PLAY0287 | producer/capture/spec assigns a field the event lacks |
| PLAY0291-0294 | bad single-line JSON / unknown key / wrong shape / duplicate key |

## Batches, described handlers, text rules and protected reads

Use code and typed subjects from [diagnostic arguments](diagnostic-arguments.md), not
message parsing, for codes that declare arguments. Other messages still explain
refusal reasons. Read [language admission](language-admission.md) before changing modes.

| Code | Meaning / fix |
| --- | --- |
| PLAY0693 | Duplicate/conflicting/invalid atomic batch declaration; keep one application-root declaration, do not remove intended atomicity |
| PLAY0695 (I) | Described handler admitted; a target must supply its payload body |
| PLAY0696 | Pending handler without productions, including response-only; declare unconditional productions |
| PLAY0697 | Conditional productions under pending intent; result v1 needs unconditional events |
| PLAY0698 | Pending intent plus another handler; retain one |
| PLAY0699 | Pattern outside portable dialect or over 1,000 UTF-16 units / 3,000 expanded nodes; rewrite the refused construct |
| PLAY0780 | Invalid placement/repetition of `command reads protected`; declare once at root |
| PLAY0781 | Protected read shape/key refused; read the typed `reason` argument (see `diagnostic-arguments.md` for the compiler prerequisite), never downgrade to an unguarded read |
| PLAY0782 | Omitted `by` without command identifier; declare the lookup input |
| PLAY0783 | Command combines reads and concurrency; choose the intended boundary |
| PLAY0784 | Unknown read/property path; reference a declared read or alias |
| PLAY0785 | Ambiguous view path; use distinct aliases on repeated reads |
| PLAY0786 (I) | Eligible legacy model; preview typed preamble repair, individually review absence/conflict semantics |
| PLAY0787 (W) | Exact deprecated Studio text-rule pattern; repair to literal rule or equality, preserving message/severity |
| STAGE-ESM-043 | Atomic mode not realized in admitted v22–v27; retain declaration and gap-fill (v22 admission Requires Stage 4.139.0; older Stage reports -016) |
| STAGE-ESM-047 | Single-instance query by composite key parts (v27) in the renderer, until Chronicle can look up composite keys (Chronicle#4265); keep the query, gap-fill |
| STAGE-ESM-048 | Read model with several key parts, or projection keyed by parts (v27), in the renderer or legacy syntax engine; keep the model, gap-fill |
| STAGE-ESM-044 | Renderer refuses a protected read despite v25 admission: any protected read in a bundle that predates protected-read rendering, afterwards only shapes it cannot preserve (constrained events, read paths in `produces when`, optional/collection/composite/mistyped mapping targets, freshness scopes), naming the reason; the legacy Host/structural runner refuse the preamble. Semantic Host/executor support is separate; keep protection and gap-fill generated-app scope |
| STAGE-ESM-045 | Non-Error protected requirement; semantic Host/executor refuse before execution, including reaction-invoked commands; keep severity and report Unsupported |
| STAGE-IMPL-002 | Described handler body not realized by Stage, even Stub mode; no stubs emitted |
| STAGE-IMPL-003 | Reached reducer or command/concept validation body in semantic Host/executor; owner and RequirementId named, unrelated behavior remains admitted (Requires Stage 4.145.0; older engines refuse unrelated behavior too) |
| STAGE-PROFILE-001 to -006 | Shared target profile-input findings: unknown package, missing layout, incompatible packages, missing theme, invalid root template, unresolved blueprint template (Requires Stage 4.143.0; older consumers may check only in the CLI) |

Invalid match syntax remains PLAY0367. Property/concept text rules bind v24;
`contains`/`starts with` in conditions only still fail PLAY0268. Stage rejects
v28 and mixed pairs with STAGE-ESM-016; v1–v27 admission does not implement
bodies or batches. The renderer refuses protected reads it cannot preserve (STAGE-ESM-044; every
protected read in an older bundle), unsupported date-comparison shapes (STAGE-ESM-046;
all date comparisons before Stage 4.141.0) and composite read-model keys (STAGE-ESM-048/-047).
Read [language admission](language-admission.md) for Host outside-append commit behavior.
Requires Stage 4.129.0 for semantic Host/executor protected reads; older engines refuse -044.

## Binding (V2 and V3) - see `executable-subset.md` for the full table

Requires Screenplay 4.210.0 (`screenplay --version`) for PLAY0800 and its required typed `subject`; older binders attribute attached command handlers to PLAY0268. Other PLAY0268 refusal families are unchanged.

| Code | Meaning | Fix |
|---|---|---|
| PLAY0268 | construct not representable in a supported ESM version yet (`executable-subset.md` lists unsupported query shapes, protected enum/identity concepts, read paths without opt-in or in `produces when`, Uuid ordering, different Uuid concepts and Uuid rules, and older bundled-compiler limits) | stay in design mode, or change the construct if the domain allows; never strip `pii`/authorization |
| PLAY0800 | error `CommandHandlerRequiresAttachment`: a file/inline command handler requires a constrained implementation attachment; required typed `subject` identifies the command | permanent limitation: gap-fill the slice in code, not by waiting for ESM admission; hosts switch on the code and read `arguments.subject`, never match the message |
| PLAY0269 / 0270 | UI deferred / authoring metadata (I) | none |
| PLAY0271 | legacy `reads` or a `concurrency` block keeps its legacy meaning and cannot bind (error) | opt eligible command reads into v25 with reviewed preamble repair; `concurrency` remains unbound; uniqueness uses a constraint, target realization stays separate |
| PLAY0273 | incoherent: ambiguous `for`, property not on event revision, operand type mismatch, derived `$eventContext` path, unparsable date literal or offset-less DateTime literal in an admitted v26 comparison, or a Uuid comparison literal that is not lowercase hyphenated (v28) | fix the reference or literal; DateTime needs offset/Z; Uuid literals are lowercase hyphenated |
| PLAY0350 | `null` in command/event value | Chronicle semantics (nullable event properties are CHR0012; an optional fact is a separate event), not only a binder limitation: specs cannot state an absent command/event value. Omit the value or record a capability gap; whether the optional detail is really a separate fact is a review question, not this code's fix |
| PLAY0351 | given/then readmodel lacks the key property | add the keyed query's `by` property |
| PLAY0352 | when-less spec asserting an action outcome, except a sole query Validation error (v18) | add an action or assert readmodel/query |
| PLAY0389 | gated command/query exercised without `given caller` | add `given caller` (empty block = anonymous) |

## Frequent errors
| Code | Meaning / fix |
|---|---|
| PLAY0001 | unknown top-level word (e.g. `slice` at top level of an un-imported file; `numbers` on an older compiler) |
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
| PLAY0465 | query action needs result/no-result/denial or one Validation error; a query error cannot mix with results/no-result/denial or more than one query |
| PLAY0468 | `when query` argument is not a `by`/`filter` param |
| PLAY0474 | `produces event` (inline) outside a command, e.g. in a reaction |
| PLAY0478 (I) | plain `produces` without `for`: add `for <identifier>` |
| PLAY0469 (I/W) | command identifier copied into the payload while targeting it |
| PLAY0479 (I) | legacy `?` optional |
| PLAY0480/0484 | modifier order `Type optional generated identifier` |
| PLAY0682 | `generate sequence` is reserved; keep the intended sequence as a capability gap |
| PLAY0683/0684 | PLAY0683: generated non-identifier below 64 random bits warns (a single-property unique constraint suppresses it); PLAY0684: generated identifier below 64 random bits is an error |
| PLAY0687 | capture `source events` spans multiple origins; split into one capture per origin (event store) |
| PLAY0688 | generated strategy refutes a concept rule; read its violating example, align the rule/strategy or review the derived-`matches` repair |
| PLAY0689 (W) | generated-concept rule proof is undecided; reason names regex fragment/budget/opaque limits or ambiguity. Only deterministic matches patterns prove containment; passing samples prove nothing |
| PLAY0691 | finite pattern support cannot be counted within the generation budget; simplify the strategy, never treat it as an Unproven rule warning or redraw |
| PLAY0692 | pattern support is empty for some valid UTC date in the compiler's next-100-year horizon; repair the pattern/checksum contract, never sample around it |
| PLAY0685 | own response name used under execute failure; outer successful responses remain available. UI composition `references/response-continuations.md` |
| PLAY0289 | (binding) no documents: empty root |

Requires Screenplay 4.164.0 (`screenplay --version`) for PLAY0688/0689 (ESM v20),
4.172.0 for finite generation PLAY0691/0692 and checksum validation (v21); older
compilers refuse those constructs with PLAY0268. A refuted rule's PLAY0688 repair
uses the strategy-derived matches pattern (and declared checksum validation if
absent). Inspect MCP `rules[]` for status/witness/reason and only use a repair
when diagnosticCode/repair is reported. Pattern/random/rule no longer refuse
PLAY0268; generated secrets still do. Requires Stage 4.109.0 for v20, 4.115.0
for v21 admission/all-Proven unpinned specs, 4.117.0 for warning/info rules,
4.118.0 for checksum validation and 4.119.0 for pattern/random. Older renderers
refuse those constructs or require pins even for Proven rules. Bad pins fail
STAGE-ESM-011; unpinned Unproven/Refuted rules fail -038; opaque rules fail -005
before pin evaluation. `generate rule` renders in a new enough bundle (bodies refuse -019/-022/-020; the Host and legacy syntax engine keep -028); checksum remains -042 only
on legacy syntax paths. Tool-author checks: [generation checks](generation-checks.md).

Fix order: E at V1 -> W at V1 -> silent-gap check -> V2 codes (if the mode needs V3).
Report each as `code file:line one-line meaning`; never paste more than about 30 lines.

## Render admission (V5): `STAGE-*`, `CLI-RENDER-*`, `STAGE-CRATIS-*`
Full table with causes: `renderable-subset.md`. The bundled renderer admits through a fixed
ESM version to construct checks; above it is CLI-RENDER-004 (direct Stage: STAGE-ESM-016).
Publication STAGE-ESM-032, foreign facts/event captures 033, claim-reading/opaque reaction gates
034, legacy filters 035 and production overrides 036 remain refused; reducer filters render.

| Code | Meaning |
|---|---|
| STAGE-ESM-016 | model above the admitted ESM schema: whole model refused |
| STAGE-ESM-025 | a typed-context member references a historical event revision or property identity; only the current revision renders |
| STAGE-ESM-026 | a selected event, or an event a selected scope depends on, is above its initial revision; Stage cannot render event-type migrations; gap-fill and hand-write the migrations |
| STAGE-ESM-001 | unsupported slice kind; `StateChange`, `StateView` and event-triggered `Automation` render, Translate is STAGE-ESM-024 |
| STAGE-ESM-004 / 006 / 013 | a `StateChange` without commands; `produces` that cannot render (expression/enum conditions, literal or computed mappings, duplicate tags); event-context value other than `$context.occurred` (reactions: `$context.causedBy.*`, `$context.eventSourceId`) |
| STAGE-ESM-005 / 015 | code rule or code validation bodies; authorization Stage cannot render exactly (role-claim URIs, nested or unresolved opaque policies, opaque-policy subjects that are not text, Uuid, Date or DateTime). Role-only, authenticated, opaque pure policies and Uuid/Date/DateTime/text claim targets render |
| STAGE-ESM-007 / 009 / 010 / 017 | projection shape (one projection or reducer per read model, affected-instance key, unsupported query shapes, keys, unsupported `all` mappings, event-context values) |
| STAGE-ESM-011 | specification shape not rendered (`when append` outside the single-hop reaction subset, command specifications that cascade into reactions, composite values, an unauthenticated caller carrying roles or claims, generated fixtures that do not match their strategy shape or concept rules). Command scenarios and single-hop reaction appends render |
| STAGE-ESM-014 | constraint shape (multi-claim in one command, optional or composite targets) |
| STAGE-ESM-019/022 | reducer or opaque pure policy body outside the pure allowlist; a `generate rule` body that does not compile or warns (019) or shadows a type or namespace (022) |
| STAGE-ESM-024 | interval, schedule, application, startup and shutdown triggers, opaque reaction bodies, captures, Translate, clock/trigger/capture specifications |
| STAGE-ESM-020 | a body the model requires, including a `generate rule` attachment, is missing or changed (content hash check); blocks publication |
| STAGE-ESM-039 | unsupported refusal branch conditions/routes/mappings, same-event multi-trigger refusal groups, localized `$refusal.message` or ambiguous multi-constraint `$refusal.constraint` |
| STAGE-EVENT-002 / CLI-RENDER-016 | existing destination event type ids changed or unreadable; pin stored identity, never bypass with `--force` |
| STAGE-BASELINE-001–004 | whole-application warnings: changed/missing admitted event revision, unreadable baseline, unavailable or stale assessment; keep `baseline check` as the independent CI gate |
| CLI-RENDER-017 / 018 | persisted PATH root cannot open / `--name` differs from saved application name; resolve root state, never discard identities |
| STAGE-CRATIS-005 | a modeled artifact would render into `Customizations/` |
Fix by changing the model only where the domain allows it; otherwise the slice is gap-fill
with the model as its contract (`cratis-screenplay-render-and-gap-fill`).
