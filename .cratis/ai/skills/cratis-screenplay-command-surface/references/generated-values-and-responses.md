<!-- cratis-ai-managed: skills/cratis-screenplay-command-surface/references/generated-values-and-responses.md -->
# Generated values and responses (ESM v7 and v15)

## Contents

- Choosing a strategy
- When not to
- The concept declares how; the property declares that
- Inspect and edit generation through MCP
- Generated-concept rules (ESM v20)
- Version and freshness
- Responses and pre-generation boundaries
- What the CLI and Stage realize

`generated` properties and `returns` bind/reference-execute (ESM v7).
Concept generation strategies and whole-source freshness bind as ESM v15. The CLI and Stage
render the supported strategies, scalar/named responses, fixtures and `then returns` when their
bundled compiler and renderer admit v15. Binding is not rendering.

Requires Screenplay 4.147.0 or later (`screenplay --version`) for concept generation strategies and whole-source freshness (ESM v15).

Sources: Cratis/Screenplay `Documentation/screenplay/generated-values.md`,
`generated-values/strategies.md`,
`Source/DotNET/Screenplay/Semantics/SemanticModelBinder.Generation.cs`;
Cratis/Stage
`Documentation/reference/generated-values-rendering.md`.
Only the released generation phase is taught here.

## Choosing a strategy

Use the [threaded shop model](shop-generation.md): CustomerId is `cus_` + ULID,
OrderId is TypeID, InviteCode is NanoID. It links the same .NET concepts but does
not claim their received-time/tenant context or secret ApiKey Bytes(32) factory
is executable in Screenplay. The table below owns exact strategy shapes.

## When not to

Client-minted values remain inputs. Generation is not authorization, idempotency,
gapless invoice allocation or a credential contract. Preserve ApiKey's `secret`
classification without generated properties; context/secret execution remains
Screenplay#616 phases 3/4, not a reason to strip protection.

## The concept declares how; the property declares that

The complete reference-executable [shop example](shop-generation.md) declares
strategies on concepts and `generated`/`generated identifier` on properties.
It includes whole-value fixture pins and returned IDs.

A generated property must be a required scalar **concept**, not a bare primitive,
optional value or collection. Only Uuid and String generation are executable.
A Uuid concept implicitly generates `uuid`; String needs an explicit strategy.
Each concept owns at most one `generate` line; there is no per-property override.
Generated identifiers need Uuid or String backing and at least 64 random bits.

| Strategy | Backing | Exact default shape / options |
| --- | --- | --- |
| `uuid` | Uuid, String | UUID v4, RFC variant, lowercase canonical D text; 122 random bits |
| `uuidv7` | Uuid, String | UUID v7, RFC variant; 74 random bits; reveals time, not monotonic within a millisecond |
| `ulid` | String | 26 uppercase Crockford symbols, first 0–7; 80 random bits; reveals time, not strict millisecond ordering |
| `typeid "user"` | String | TypeID 0.3, lowercase Crockford UUIDv7 suffix; 74 random bits; prefix 1–63 lowercase letters/underscores, no leading/trailing underscore; empty prefix omits separator |
| `nanoid [size]` | String | Default 21 symbols from `A-Za-z0-9_-`; size 1–256; entropy is size × log2(alphabet size) |
| `bytes <count> base64url` / `hex` / `base32` | String | Count 1–512; 8 × count random bits; RFC 4648 without padding; hex lowercase, base32 uppercase; unused trailing bits zero |

Modifiers occur once each, in any authored order. `prefix "text"` is printable
ASCII, at most 32 characters, on uuid/uuidv7/ulid/nanoid/bytes; UUID prefixes need
String backing. `case lower|upper` applies only to String uuid/uuidv7, ulid and
hex/base32 bytes; it never changes the prefix. `alphabet "chars"` replaces
NanoID's alphabet with 2–256 distinct printable ASCII characters (the repertoire
has only 95). TypeID has its own prefix and accepts none of these modifiers.
Time and prefixes add no entropy. Below 64 random bits is PLAY0684 (error) for an
identifier; non-identifiers warn PLAY0683 unless a single-property unique
constraint covers the mapped event property. That suppression is not a runtime collision guarantee; use the constraint when
collisions must reject the command.

`pattern`, `random`, `rule` and checksum syntax parse/print but refuse PLAY0268;
`generate sequence` is reserved (PLAY0682). Generated-concept validation rules select ESM v20; compliance markers, including
generated secrets, remain refused. Do not
remove classification or pretend a random String is a credential contract.

## Inspect and edit generation through MCP

A concept `generation` summary and entropy repairs are available.
Use `screenplay_declaration_details` with `kind: "Concept"`, `view: "summary"`:
only authored `generate` gets a block, with strategy, set modifiers,
preview-only arguments, `entropyBits`, `entropyUnknownReason` when null,
`valuesAtOnePercentCollision` and `executable.status`. Status is `admitted` only
after whole-model binding, `refused` for errors at this concept/generation, or
`notAssessed` with a reason for unrelated binding failures. Empty local codes do
not prove admission; large collision counts may exceed client numeric range.

Set/change/remove `generate` through `screenplay_propose_ast` (replace the concept's
`GenerationSyntax`, omit/null `generation` to remove) or reviewed
`screenplay_propose_source`; do not hand-edit the text. Inspect executable
diagnostics independently of authoring acceptance: sequence PLAY0682;
pattern/random/rule PLAY0268; generated-rule proofs PLAY0688/0689. Review, run candidate
specifications where executable, then apply with revisions.

`screenplay_repair_capabilities` advertises PLAY0683/0684; workspace repairs supply
the selected `diagnosticCode` and `subject` for `screenplay_propose_repair`.
The concept subject raises NanoID to the smallest size reaching 64 random bits
for its alphabet, or bytes count to 8 (possibly in another document). PLAY0683
also offers uniqueness for a non-identifier command-property subject only when
exactly one production maps it to exactly one event property. Both require
individual review and `CanonicalizeTouchedDocuments`, with no pinned evidence.
Length changes affect all concept uses; uniqueness changes append-time behavior.
PLAY0688 repair replaces a refuted rule with the strategy-derived `matches`
pattern. Review input/stored-value uses because this changes their validation,
not just generation. Checksum repairs remain unadmitted.
A CLI-bundled MCP can lack this summary and these repairs; it still
supports existing typed concept proposals.

Requires Screenplay 4.149.0 or later (`screenplay --version`) for the concept `generation` summary and entropy repairs. Full fields, mapping limits and tagged
source evidence: [MCP generation guide](../../cratis-screenplay-model-authoring/references/mcp-tools.md#concept-generation).
Sources: Cratis/Screenplay `Documentation/screenplay/generated-values.md`,
`Source/DotNET/Screenplay.Mcp/McpGenerationDetails.cs`,
`Source/DotNET/Screenplay/Workspaces/WorkspaceGenerationRepairs.cs`.

## Generated-concept rules (ESM v20)

Requires Screenplay 4.164.0 or later (`screenplay --version`) for generated-concept rules; older compilers report PLAY0268.

```screenplay
concept InviteCode : String
  generate nanoid
  validate
    matches "^[A-Za-z0-9_-]{21}$"
```

The compiler proves `not empty`, text length/min/max, equality/inequality and
`matches` against the exact strategy shape. Proven rules bind silently; Refuted
rules fail with PLAY0688 and a violating example; Unproven rules warn PLAY0689
with a reason. A warning is not proof. Regex proof uses a bounded ASCII regular
subset (classes, groups, alternation, repetition and end anchors, not lookaround,
backreferences or flags) and proves containment only for unambiguous patterns
that cannot backtrack on generated values. Passing samples alone prove nothing.
Providers validate generated values too: a failing fixture is a Contract violation,
not client rejection or a reason to redraw. Opaque rules need a target.

Rules select v20 even on implicit UUID generation. No proof status is serialized;
strategy changes do not silently change rules on stored/input values.

## Version and freshness

A serialized concept `generation` member selects **v15**. Implicit Uuid generation
and explicit unmodified Uuid `generate uuid` omit it and preserve earlier executable
bytes/revision. A v15 model guards **all** generated destinations, including its
implicit UUIDs. An append whose destination equals any value generated in that
invocation requires the **whole source** to have no existing facts, regardless
of stream or event type. Multiple initial facts in one invocation are allowed;
a skipped production makes no freshness claim. Reuse rejects the whole command
with `Concurrency`, appends nothing and does not silently regenerate or retry.
Older models are Unguarded and retain legacy fixture reuse.

Freshness is neither authorization nor idempotency. Client-minted identifiers
remain inputs; protect their invariants with constraints. Never use generation
as a deduplication key, a gapless sequence, or proof that a projection is ready.

Specifications pin values, not seeds: `for` pins a generated identifier;
`generated <name> = <value>` pins another generated property. The reference
evaluator draws no randomness. From v15 every pin must match exact strategy
encoding, size, case, UUID version/variant and zero trailing bits. Invalid pins
are Contract rejections, not client validation failures. Older models retain
canonical UUID checks without a version check. Complete executable examples:
[generated responses](../../cratis-screenplay-toolchain/references/generated-responses-example.md)
and [generation strategies](../../cratis-screenplay-toolchain/references/generation-strategies-example.md).

## Responses and pre-generation boundaries

- Generated values are not request inputs, form fields, invocation arguments or
  ordinary specification values. Authorization/validation precede generation:
  policies (including a generated identifier's implicit `subject`), rules and
  requirements cannot read them (PLAY0273). Reactions map request inputs only.
- `returns <property>` exposes a scalar; a `returns` block has ordered
  `<name> [<Type>] = <property>` fields over direct command properties. An
  annotation must match source type/optionality. Collections, read aliases,
  arithmetic and whole read models are unsupported. One unconditional response,
  as a command sibling. `returns @name` forces a response; `@returns Type` is a property.
- Only acceptance has a response. Rejection, denial or unsupported outcomes have
  none; later settlement/query failure clears it. Response-only commands record
  no facts. Field names are an external contract: renaming changes revision.
- A generated identifier is the implicit inline production destination; a plain
  destination-less `produces` retains a separate allocation channel. A fixture
  never satisfies that channel.
- Screenplay resolves response names in form submit and execute-success
  continuations without changing ESM. An older CLI compiler lacks that
  resolution, and Stage does not provide Scene runtime continuation
  binding. Requires Screenplay 4.148.0 or later (`screenplay --version`) for the resolution. See UI composition [response continuations](../../cratis-screenplay-ui-composition/references/response-continuations.md).

## What the CLI and Stage realize

Stage emits strategy-specific `IGeneratable<T>.New()` factories over Fundamentals
7.24.0 or later (via the Arc scaffold pin in Stage's `references/rendered-application.md`). v15 generated destinations use explicit
`EventsWithConcurrencyScopes` with `BeforeFirst` whole-source scopes; Chronicle
checks atomically at append: a batch's scopes are checked once against the state before
the batch, all or nothing, and a storage retry that renumbers the append checks them again. Generated rejection specs assert
`ValidationResultReason.ConcurrencyViolation`, not Arc's message text. This is
Stage's explicit realization, not a claim that Arc delivers the same guard.
Time-based factories use the command's receipt time through `IGeneratableInContext<T>`
and `GenerationContext`, the same instant as `$context.occurred`.

Requires Stage 4.94.0 or later for receipt-time generation; older factories use wall clock.

| Refusal | Still unsupported |
| --- | --- |
| STAGE-ESM-028 | Unknown strategies, non-concepts, optional/collection generation; every explicit strategy on the legacy syntax engine |
| STAGE-ESM-011 | A v15 fixture that does not match its strategy shape |
| STAGE-ESM-038 | Routing combined with generation/responses; generated-property rules/requirements or concept rules; occurrence parameter/response/factory name collisions; unsupported response sources/composites/collections; generated identifier plus destination-less plain production |
| STAGE-ESM-038 | Unpinned generation with unordered event, read-model or query expectations |
| STAGE-ESM-004 / -006 | StateChange slices without commands / expression or enum production conditions |

Sequence generation is refused at binding (PLAY0682) and never reaches Stage;
command `given clock` fixtures render when whole milliseconds at or after 1970;
`when clock` and reaction/query/direct-append clocks remain refused.

Requires Stage 4.95.0 for command clock fixtures, 4.98.0 for multiple commands and
conditional productions, and 4.100.0 for generated text unique constraints; older
renderers refuse those shapes. ESM v20 still needs a renderer that admits v20.

Missing pins in generated Debug specs use `AllowGeneration()` with relational
assertions against captured values; v15 captures also check strategy shape.
The in-memory Stage executor instead requires deterministic fixtures and returns
Unsupported(IdentityAllocation) when missing. Build and run generated Debug
specifications separately; source reading is not a render or runtime result.
