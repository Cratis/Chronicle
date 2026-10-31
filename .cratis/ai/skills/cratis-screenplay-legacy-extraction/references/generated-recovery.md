<!-- cratis-ai-managed: skills/cratis-screenplay-legacy-extraction/references/generated-recovery.md -->
<!-- Copyright (c) Cratis. All rights reserved. -->
<!-- Licensed under the MIT license. See LICENSE file in the project root for full license information. -->

# Generated-value recovery: adapter version matters

## Contents

- Version gate and sources
- Choosing a strategy
- When not to
- Recover what is proven
- Pins are not local generation
- Incomplete recovery is a loss report

## Version gate and sources

These forms are not promised for the CLI's
adapter. **The CLI can bundle an older Arc.Screenplay adapter** (compare `cratis --version` and the generator package actually loaded) even though its Screenplay
compiler and Stage admit v15 strategies. A standalone compiler upgrade
cannot add recovery to that adapter. Check the resolved generator before using
this guidance; do not bump managed product pins to hide the difference.

Each recovery below has its own gate; use the adapter that has the one you rely on:

- Requires Arc.Screenplay 22.78.0 or later for `[GeneratedValue]` Handle parameters whose `New()` is a UUID v4 form, for the `generated identifier` and response they produce, and for scenario pins (`for` / `generated`). Before it none of this is recovered. From 22.78.0 until 22.86.0 any other strategy keeps its source form and reports SP0073.
- Requires Arc.Screenplay 22.86.0 or later to recover UUIDv7, ULID, TypeID, NanoID, `Bytes` and hex strategies as concept `generate` lines (ESM v15) and a passed-through `GenerationContext`.
- Requires Arc.Screenplay 22.101.0 for generated-concept validator recovery (ESM
  v20); check `dotnet list package --include-transitive`. Older adapters withhold
  generation on rule-bearing concepts. Recovery covers only a fully recovered,
  non-opaque default validator: Proven rules emit, Unproven rules remain with
  SP0078, Refuted rules withhold generation with SP0073 and a counterexample.
  Generation is also withheld with SP0073 (rules kept) when the validator is
  unreadable, conditional or names a predicate (`rule <Name>`), when a UUID
  concept has any validator, when the concept carries `pii` or `secret`, and
  below a v20 cap.
- Requires Arc.Screenplay 22.107.0 for pattern/random, checksum validation and
  self-contained String generation-rule recovery (ESM v21); older adapters leave
  those factories in code with SP0073. A v20-or-lower cap withholds generation
  and dependent values/mappings/responses/destinations/scenarios, preserving
  checksum validation as a source attachment rather than dropping it.

Tagged evidence:

- Arc `Documentation/backend/csharp/generating-a-screenplay.md`.
- Arc `Source/DotNET/Screenplay/Analysis/Commands/GenerationStrategyReader.cs`;
  `GenerationCallReader.cs`;
  `GeneratedParameterReader.cs`.
- Arc `Source/DotNET/Screenplay/Analysis/Specifications/GeneratedValuePins.cs`.

## Choosing a strategy

Use the [same shop vocabulary](../../cratis-screenplay-command-surface/references/shop-generation.md):
CustomerId (`cus_` + ULID), OrderId (TypeID), InviteCode (NanoID), ApiKey (secret
Bytes(32) with .NET `[NotAudited]`). Do not claim exact recovery of every factory:
a custom `cus_` interpolation may recover as a self-contained String rule, not as
an inferred built-in prefix strategy. TypeID recovers a passed-through context's
strategy, not proof of equivalent receipt-time/tenant behavior. Context-member
String bodies can recover under the v21 gate; generated secrets stay in code.
Recovered rule fixtures supply a String result without reference execution of the body. Preserve ApiKey's protection and source code, not an unprotected
replacement String. The intentional candidate can author the released customer
prefix strategy with an explicit target decision, separate from as-is proof.

Recover the strategy the source proves; do not choose a replacement because a
factory is named `New()`. Use the existing [strategy reference](../../cratis-screenplay-command-surface/references/generated-values-and-responses.md)
for meaning and shapes (including readable InvoiceNumber patterns and random Int
TicketNumber values), then the forms below for parameter factories, literal pins
and retained source pointers.

## When not to

Do not infer generation for client-supplied IDs, deduplication keys or gapless
invoice numbers, or turn an observed secret into a random String. Preserve
protection and unrecovered code. A pattern is not a gapless/sequential allocator
or secret; check digits detect typos, not forgery. Low-entropy payload codes need
uniqueness separate from source freshness. Arc recovery does not imply rendering:
Requires Stage 4.118.0 for checksum validation and 4.119.0 for pattern/random;
older renderers refuse them. `generate rule` renders from Stage 4.136.0; the Host and legacy syntax engine still refuse it with STAGE-ESM-028.
Report missing pins, withheld productions/scenarios and version caps
as losses, not successful recovery. Sources: Arc
`Documentation/backend/csharp/generating-a-screenplay.md`;
Screenplay `Documentation/screenplay/generated-values/choosing.md`.

## Recover what is proven

The UUID v4 forms, pins and the identifier/response rules in this section need Arc.Screenplay 22.78.0 or later; the other strategies in the list below need 22.86.0 or later.

A `[GeneratedValue]` Handle parameter becomes a generated command property only
for a required scalar Uuid/String or signed Int32/Int64 random concept with a proven strategy and no compliance
markers. Validator rules recover under the v20 gate above, not by deleting them. Its explicit attribute name, otherwise
parameter name, becomes the property name. A directly returned event-source
identity (alone or beside an event) adds `generated identifier` and a response;
a direct `EventForEventSourceId(parameter, event)` target adds the identifier
without inventing a response. Aggregate handlers do not prove that destination.
Reassignment, by-reference use and property-name collisions prevent recovery.

Read `IGeneratable<T>.New()` or `IGeneratableInContext<T>.New(context)`, including
explicit implementations. The body must be one expression or a single return:
`new(<call>)`, `new Concept(<call>)`, or a proven unchanged implicit conversion.
Constructors/conversions must forward the primitive unchanged. Recoverable calls:

- `Guid.NewGuid()` / `GenerateValue.Uuid()` → implicit UUID v4, no strategy line.
- `Guid.CreateVersion7()` / `GenerateValue.UuidV7()` → UUIDv7.
- `GenerateValue.Ulid()`, `TypeId(...)`, `NanoId(...)`, `Bytes(...)` → the respective
  concept strategy, with literal arguments and encoding enum members.
- `RandomNumberGenerator.GetHexString(...)` → hex bytes only with an even literal
  character count; preserve its requested casing.

Under the v21 recovery gate, literal `GenerateValue.Pattern(pattern[, checksum])`
and its passed-through context overload recover `generate pattern`; dated patterns
require that context overload. `GenerateValue.Integer(min, max)` recovers inclusive
signed Int64 `random`; an Int32 concept needs exactly one `(int)` cast with both
bounds inside Int32. Invalid/computed arguments, unsigned backing and extra casts
stay in code with SP0073. Screenplay checks pattern support, proofs and entropy.

An unconditional default validator's `Must(v => GenerateValue.HasValidChecksum(v,
Checksum.Luhn))` recovers `validate checksum luhn` (also Mod11/Mod97). None,
conditional predicates and arbitrary helper predicates are not checksum rules.

A self-contained String factory can instead recover `generate rule <Concept>New`
with an inline `csharp` body, stripping only proven forwarding constructions.
Literal/primitive locals, `context.Now`, `context.Tenant`, string, Math, Convert,
Guid, RandomNumberGenerator and GenerateValue are allowlisted; multi-statement
bodies can qualify. Compound time-based calls must pass the captured context.
Private helpers, fields, DI, aliases/static imports, ambient time, raw Default
comparisons and external dependencies remain SP0073; no file fallback is invented.
For example, a proven forwarding factory returning
`new($"{context.Tenant}:{context.Now:yyyyMMdd}:{GenerateValue.Ulid(context)}")`
recovers `generate rule <Concept>New` with inline body
`return $"{context.Tenant}:{context.Now:yyyyMMdd}:{GenerateValue.Ulid(context)}";`.
The compound call remains a rule, not an inferred built-in ULID strategy.

This uses the Cratis C# binding to Fundamentals' `Cratis.Concepts.GenerationContext`,
not Screenplay's BCL-only record. Passing `context` to `GenerateValue.*` makes the
body target-bound; a `csharp` tag alone does not select that binding. `Now` and the
tenant formatted as text are the portable subset; other providers must admit the
body against their own native type. Read the command-surface [binding guide](../../cratis-screenplay-command-surface/references/generated-values-and-responses.md#generation-context-binding)
when deciding whether to retain target-bound code. Recovery preserves the body but
neither executes it nor proves another target can compile it. Opaque entropy is
unknown, so a rule cannot supply an identifier.
The concept owns `generate`; each command property only marks generation.
Use the shared [strategy reference](../../cratis-screenplay-command-surface/references/generated-values-and-responses.md)
for exact encodings, ranges, entropy and admission, rather than duplicating them.

## Pins are not local generation

Successful parameter-generation scenarios need a concrete pin for every generated
value: `for` beneath `when` for its identifier; `generated <name> = <value>` for
others. Direct concept constructions and one-hop stable held fields are readable.
UUIDs normalize to lowercase D format; text retains exact size, encoding, case
and unused trailing bits. Generic pins must target the issued command.
Missing/unreadable, nested-command, conditional/repeated, mismatched, `New()` pins
or `AllowGeneration()` omit the scenario with located SP0039, retaining an
otherwise admitted command generation contract. Denial/validation precede generation
and need no generation pin.

Legacy values created inside `Handle()` by `Guid.NewGuid()` or a proven `New()`
have no pinning seam. Successful scenarios retain legacy productions or handler
references without generated values/responses, with SP0052; explicit given sources
also preserve this fallback. Do not rewrite source code or fake a successful pin
to make recovery look complete. Recovery of responses has its own proof limits
in [generate notes](generate-notes.md#enum-and-response-recovery).

## Incomplete recovery is a loss report

Record each affected command/concept/scenario with its exact source locator,
resolved adapter version, diagnostic, retained/withheld form and open question:

| Diagnostic | Loss to reconcile |
| --- | --- |
| SP0073 | Unproven strategy/body/arguments, refuted generated-concept rule with witness, or generation above the ESM cap; no replacement strategy inferred. |
| SP0078 | Generated-concept rule proof is Unproven; preserve the rule and reason, not a claim of Proven generation. |
| SP0052 | Unadmitted generated value, dependent response or pre-generation protection stays in code; local-generation fallback is not parameter recovery. |
| SP0012 | A required payload mapping or condition needs an unavailable value/read: withhold the whole production, never prune it into an incomplete fact. An optional blocked mapping can be omitted alone. |
| SP0039 | Missing/unreadable pins, unrecoverable source identities or outcomes withhold the whole scenario. Name what was lost. |
| SP0013 | Tuple destination not proven; never guess its event source. |
| SP0054 | No retained production means its route is withheld; keep the handler pointer instead of declaring no behavior. |

`MaximumExecutableModelVersion` below v15 withholds non-default concept generation
with SP0073 and dependent values/responses/mappings with SP0052; pins against them
withhold scenarios with SP0039. Below v7 responses/return expectations are withheld.
A cap describes less behavior, not an equivalent implementation. Handler references
compile but refuse executable binding (PLAY0800 for file/inline command handlers);
record that verdict separately and gap-fill the slice with a constrained implementation
attachment, not by waiting for ESM admission.
Requires Screenplay 4.210.0 (`screenplay --version`); older binders use PLAY0268 for these handlers.
Never remove protection, required payloads or loss rows to obtain a green result.
