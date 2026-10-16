<!-- cratis-ai-managed: skills/cratis-screenplay-model-authoring/references/choosing-generation.md -->
<!-- Copyright (c) Cratis. All rights reserved. -->
<!-- Licensed under the MIT license. See LICENSE file in the project root for full license information. -->

# Shop generation choices

## Choosing a strategy

Follow the [threaded shop model](../../cratis-screenplay-command-surface/references/shop-generation.md):
CustomerId (`cus_` + ULID), OrderId (TypeID) and InviteCode (NanoID) are released
strategies. ApiKey stays secret without executable generation. Neither the
OrderId's Arc received-time/tenant context nor ApiKey's .NET Bytes(32) secret
factory can be promised as Screenplay generation today.

For a server-owned new value, choose implicit UUID for an ordinary identity,
UUIDv7 for time-ordered UUIDs, ULID for compact time-ordered text, TypeID for
recognizable typed text, NanoID for opaque text, or bytes for an exact random-bit
budget. Inspect entropy separately from executable admission; prefixes add no
random bits and identifiers need at least 64. Time-ordered is not strict
millisecond ordering. Use the existing [strategy and MCP reference](../../cratis-screenplay-command-surface/references/generated-values-and-responses.md),
not a per-property strategy: the concept owns `generate`, the command property
says `generated identifier` or `generated`.

## When not to

Do not replace client-minted inputs, authorization, idempotency keys or a gapless
invoice allocator with generation. Secret generation, pattern/random/rule execution
and generated-concept rule consistency are not released capabilities. Preserve
refused rules instead of deleting them or inventing a repair.
Sources: `Documentation/screenplay/generated-values/choosing.md` and `Documentation/screenplay/generated-values.md` in `Cratis/Screenplay`.
