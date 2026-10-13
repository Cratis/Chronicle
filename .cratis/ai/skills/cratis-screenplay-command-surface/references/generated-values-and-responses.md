<!-- cratis-ai-managed: skills/cratis-screenplay-command-surface/references/generated-values-and-responses.md -->
# Generated values and responses (ESM v7 and v15)

## Contents

- The concept declares how; the property declares that
- Inspect and edit generation through MCP
- Version and freshness
- Responses and pre-generation boundaries
- What CLI 3.57.0 / Stage 4.89.1 realizes

`generated` properties and `returns` bind/reference-execute since 4.68.0 (ESM v7).
Concept generation strategies and whole-source freshness shipped in Screenplay
4.147.0 (ESM v15); standalone pin **4.149.0** includes them. CLI **3.57.0** bundles
Screenplay **4.147.1** / Stage **4.89.1**, rendering the supported strategies,
scalar/named responses, fixtures and `then returns`. Binding is not rendering.
Historical CLI 3.41.0 STAGE-ESM-028/029 probes do not describe this bundle.

Sources: Cratis/Screenplay `v4.148.0:Documentation/screenplay/generated-values.md:6-60`,
`generated-values/strategies.md:6-111`,
`Source/DotNET/Screenplay/Semantics/SemanticModelBinder.Generation.cs:14-88`;
Cratis/cli `v3.57.0:Directory.Packages.props:8-10`; Cratis/Stage
`v4.89.1:Documentation/reference/generated-values-rendering.md:6-146`.
Decision 0054 describes later phases too; only its shipped phase 2b-1 is taught here.

## The concept declares how; the property declares that

```screenplay excerpt
concept ProjectId : Uuid
  generate uuidv7
concept ReceiptCode : String
  generate nanoid
command RegisterProject
  projectId ProjectId generated identifier
  receipt ReceiptCode generated
  name String
  produces event ProjectRegistered
    name String = name
    receipt ReceiptCode = receipt
  returns
    projectId = projectId
    receipt ReceiptCode = receipt
```

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
constraint covers the mapped event property. That suppression does not make
Stage render generated unique-constraint mappings.

`pattern`, `random`, `rule` and checksum syntax parse/print but refuse PLAY0268;
`generate sequence` is reserved (PLAY0682). Generated-concept validation rules
and compliance markers, including generated secrets, remain refused. Do not
remove classification or pretend a random String is a credential contract.

## Inspect and edit generation through MCP

Screenplay **4.149.0** adds a concept `generation` summary and entropy repairs.
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
pattern/random/rule and generated-concept rules PLAY0268. Review, run candidate
specifications where executable, then apply with revisions.

`screenplay_repair_capabilities` advertises PLAY0683/0684; workspace repairs supply
the selected `diagnosticCode` and `subject` for `screenplay_propose_repair`.
The concept subject raises NanoID to the smallest size reaching 64 random bits
for its alphabet, or bytes count to 8 (possibly in another document). PLAY0683
also offers uniqueness for a non-identifier command-property subject only when
exactly one production maps it to exactly one event property. Both require
individual review and `CanonicalizeTouchedDocuments`, with no pinned evidence.
Length changes affect all concept uses; uniqueness changes append-time behavior.
Derived regex, matches/checksum repairs and rule-proof status are not shipped.
CLI **3.57.0** / MCP **4.147.1** lacks this summary and these repairs; it still
supports existing typed concept proposals. Full fields, mapping limits and tagged
source evidence: [MCP generation guide](../../cratis-screenplay-model-authoring/references/mcp-tools.md#concept-generation-41490).
Sources: Cratis/Screenplay `v4.149.0:Documentation/screenplay/generated-values.md:62-70`,
`Source/DotNET/Screenplay.Mcp/McpGenerationDetails.cs:13-83`,
`Source/DotNET/Screenplay/Workspaces/WorkspaceGenerationRepairs.cs:29-134`.

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
- Screenplay **4.148.0** resolves response names in form submit and execute-success
  continuations without changing ESM. The CLI's **4.147.1** compiler lacks that
  resolution, and Stage **4.89.1** does not provide Scene runtime continuation
  binding. See UI composition [response continuations](../../cratis-screenplay-ui-composition/references/response-continuations.md).

## What CLI 3.57.0 / Stage 4.89.1 realizes

Stage emits strategy-specific `IGeneratable<T>.New()` factories over Fundamentals
7.24.0 (via the Arc scaffold pin in Stage's `references/rendered-application.md`). v15 generated destinations use explicit
`EventsWithConcurrencyScopes` with `BeforeFirst` whole-source scopes; Chronicle
checks atomically at append. Generated rejection specs assert
`ValidationResultReason.ConcurrencyViolation`, not Arc's message text. This is
Stage's explicit realization, not a claim that Arc #3129 is delivered.
Time-based factories use wall clock, not command-receipt timestamps.

| Refusal | Still unsupported |
| --- | --- |
| STAGE-ESM-028 | Unknown strategies, non-concepts, optional/collection generation; every explicit strategy on the legacy syntax engine |
| STAGE-ESM-011 | A v15 fixture that does not match its strategy shape |
| STAGE-ESM-038 | Routing combined with generation/responses; generated-property rules/requirements or concept rules; occurrence parameter/response/factory name collisions; unsupported response sources/composites/collections; generated unique-constraint mappings; generated identifier plus destination-less plain production |
| STAGE-ESM-038 | Unpinned generation with unordered event, read-model or query expectations |
| STAGE-ESM-004 / -006 | Multi-command StateChange slices / conditional productions |

Sequence generation is refused at binding (PLAY0682) and never reaches Stage;
clock fixtures remain unsupported.

Missing pins in generated Debug specs use `AllowGeneration()` with relational
assertions against captured values; v15 captures also check strategy shape.
The in-memory Stage executor instead requires deterministic fixtures and returns
Unsupported(IdentityAllocation) when missing. Build and run generated Debug
specifications separately; source reading is not a render or runtime result.
