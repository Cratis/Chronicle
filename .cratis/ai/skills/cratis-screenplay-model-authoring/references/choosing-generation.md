<!-- cratis-ai-managed: skills/cratis-screenplay-model-authoring/references/choosing-generation.md -->
<!-- Copyright (c) Cratis. All rights reserved. -->
<!-- Licensed under the MIT license. See LICENSE file in the project root for full license information. -->

# Shop generation choices

## Choosing a strategy

Follow the [threaded shop model](../../cratis-screenplay-command-surface/references/shop-generation.md):
CustomerId (`cus_` + ULID), OrderId (TypeID) and InviteCode (NanoID) are released
strategies. InvoiceNumber uses a readable pattern/checksum and TicketNumber an
Int pickup-ticket range. ApiKey stays secret without executable generation.
An attached generation rule can use Now/Tenant, but the reference runner consumes
pinned Strings rather than executing its implementation.

For a server-owned new value, choose implicit UUID for an ordinary identity,
UUIDv7 for time-ordered UUIDs, ULID for compact time-ordered text, TypeID for
recognizable typed text, NanoID for opaque text, or bytes for an exact random-bit
budget. Choose `pattern` for readable references (dates/check digits add no
entropy), `random` for bounded Int values, `rule` only when an opaque implementation
is needed.

Requires Screenplay 4.172.0 (`screenplay --version`) for these v21 constructs; older compilers refuse PLAY0268.

Inspect entropy separately from executable admission; prefixes add no
random bits and identifiers need at least 64. Time-ordered is not strict
millisecond ordering. Use the existing [strategy and MCP reference](../../cratis-screenplay-command-surface/references/generated-values-and-responses.md),
not a per-property strategy: the concept owns `generate`, the command property
says `generated identifier` or `generated`.

## When not to

Do not replace client-minted inputs, authorization, idempotency keys or a gapless
invoice allocator with generation. Patterns are not sequential/gapless allocators
or secrets; check digits detect typos, not forgery. Use a unique constraint for
short payload codes, and retain whole-source freshness for generated destinations.
Secret generation and sequence allocation remain refused. Inspect authored-order
MCP `rules[]` and `derivedPattern` for rule proofs and reviewed PLAY0688 repair;
never delete a business invariant to silence PLAY0689. Read
[Stage admission](../../cratis-stage-rendering-and-sandbox/references/admission.md)
for checksum validation and pattern/random rendering prerequisites.
`generate rule` renders only from Stage 4.136.0 (STAGE-ESM-028 before it, and still in the Host), so model admission does not promise rendering.
Sources: `Documentation/screenplay/generated-values/choosing.md` and `Documentation/screenplay/generated-values.md` in `Cratis/Screenplay`.
