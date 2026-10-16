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
the custom `cus_` interpolation is not a recognized single factory call;
TypeID can recover a passed-through context's strategy, not equivalent receipt-time/
tenant behavior. Context-member bodies and generated secrets remain unreleased
Screenplay scope. Preserve ApiKey's protection and source code, not an unprotected
replacement String. The intentional candidate can author the released customer
prefix strategy with an explicit target decision, separate from as-is proof.

Recover the strategy the source proves; do not choose a replacement because a
factory is named `New()`. Use the existing [strategy reference](../../cratis-screenplay-command-surface/references/generated-values-and-responses.md)
for meaning and shapes, then the forms below for parameter factories, literal pins
and retained source pointers.

## When not to

Do not infer generation for client-supplied IDs, deduplication keys or gapless
invoice numbers, or turn an observed secret into a random String. Preserve
protection and unrecovered code; rule/context-member bodies are not executable
strategies. Report missing pins, withheld productions/scenarios and version caps
as losses, not successful recovery. Sources: Arc
`Documentation/backend/csharp/generating-a-screenplay.md`;
Screenplay `Documentation/screenplay/generated-values/choosing.md`.

## Recover what is proven

The UUID v4 forms, pins and the identifier/response rules in this section need Arc.Screenplay 22.78.0 or later; the other strategies in the list below need 22.86.0 or later.

A `[GeneratedValue]` Handle parameter becomes a generated command property only
for a required scalar Uuid/String concept with a proven strategy and no validator,
validation rules or compliance markers. Its explicit attribute name, otherwise
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

A passed-through `GenerationContext` parameter is readable for UUIDv7/ULID/TypeID;
reading `context.Now` or `context.Tenant` is not. Helper calls, multi-statement
bodies, computed/invalid arguments and rule/pattern/random execution stay in code
with SP0073. Never invent a rule body or implementation-file reference.
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
| SP0073 | Unproven strategy/body/arguments, or non-default strategy above the ESM cap; no replacement strategy inferred. |
| SP0052 | Unadmitted generated value, dependent response or pre-generation protection stays in code; local-generation fallback is not parameter recovery. |
| SP0012 | A required payload mapping or condition needs an unavailable value/read: withhold the whole production, never prune it into an incomplete fact. An optional blocked mapping can be omitted alone. |
| SP0039 | Missing/unreadable pins, unrecoverable source identities or outcomes withhold the whole scenario. Name what was lost. |
| SP0013 | Tuple destination not proven; never guess its event source. |
| SP0054 | No retained production means its route is withheld; keep the handler pointer instead of declaring no behavior. |

`MaximumExecutableModelVersion` below v15 withholds non-default concept generation
with SP0073 and dependent values/responses/mappings with SP0052; pins against them
withhold scenarios with SP0039. Below v7 responses/return expectations are withheld.
A cap describes less behavior, not an equivalent implementation. Handler references
compile but refuse executable binding (PLAY0268); record that verdict separately.
Never remove protection, required payloads or loss rows to obtain a green result.
