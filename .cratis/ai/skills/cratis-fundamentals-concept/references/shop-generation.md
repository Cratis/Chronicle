<!-- cratis-ai-managed: skills/cratis-fundamentals-concept/references/shop-generation.md -->
<!-- Copyright (c) Cratis. All rights reserved. -->
<!-- Licensed under the MIT license. See LICENSE file in the project root for full license information. -->

# Small-shop generation: choose the value's job

## Contents

- Choosing a strategy
- When not to generate
- Sources

## Choosing a strategy

A shop can use CustomerId (`cus_` + ULID) for
recognizable time-ordered customer references, OrderId (TypeID) for typed order
references, InviteCode (NanoID) for opaque invitation references, and ApiKey
(`Bytes(32)`) for a 256-bit credential. UUID remains the ordinary identity choice;
encoded bytes make an exact entropy budget explicit. Prefixes add no entropy.
UUIDv7/ULID/TypeID reveal time and are not monotonic within a millisecond.
An invitation reference here is not by itself permission to join the shop; if
possession is the authorization mechanism, treat it as a protected credential too.

This complete concept excerpt requires `Cratis.Fundamentals` 7.24.0 or later and,
for the ApiKey audit attribute, `Cratis.Arc.Chronicle`. These are
non-stream references; use `EventSourceId<string>` instead only if a concept
actually identifies a Chronicle event source.

```csharp
// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.Commands;
using Cratis.Concepts;

namespace Shop;

public record CustomerId(string Value) : ConceptAs<string>(Value), IGeneratable<CustomerId>
{
    public static CustomerId New() => new($"cus_{GenerateValue.Ulid()}");
}

public record OrderId(string Value) : ConceptAs<string>(Value), IGeneratableInContext<OrderId>
{
    public static OrderId New(GenerationContext context) => new(GenerateValue.TypeId("order", context));
}

public record InviteCode(string Value) : ConceptAs<string>(Value), IGeneratable<InviteCode>
{
    public static InviteCode New() => new(GenerateValue.NanoId());
}

[NotAudited]
public record ApiKey(string Value) : ConceptAs<string>(Value), IGeneratable<ApiKey>
{
    public static ApiKey New() => new(GenerateValue.Bytes(32));
}
```

`CustomerId` has 80 random bits; the custom prefix is not a TypeID. `OrderId`
has 74 random bits and the TypeID `order_` prefix. Default NanoID is 21 symbols
with 126 bits; bytes use unpadded base64url (43 symbols for 32 bytes).

`IGeneratableInContext<TSelf>` requires `static abstract TSelf New(GenerationContext)`.
`GenerationContext` holds one captured `DateTimeOffset Now` and a portable String
`Tenant`; `DefaultTenant` is the zero-GUID string, never a display name or empty
string. The shop passes time and tenant together, for example:

```csharp excerpt
var context = new GenerationContext(
    DateTimeOffset.Parse("2026-10-05T08:00:00Z"), "shop-eu");
var orderId = OrderId.New(context);
```

This excerpt assumes `System`, `Cratis.Concepts` and the Shop declarations above.
TypeID consumes `Now`, **not Tenant**. Passing a tenant does not encode it or
provide isolation; retain tenant scope independently in the host/storage.
A domain factory can read Tenant, but arbitrary tenant IDs cannot be inserted
into a TypeID prefix (only lowercase letters/underscores, letter endpoints).
Do not present a tenant-prefixed custom string as standard TypeID.

Use the context form only for time/tenant-dependent generation. If both interfaces
are implemented, callers choose context; the interfaces themselves do not dispatch.
Built-in context overloads reject default Now (`MissingGenerationTime`) and time
before the Unix epoch. A fixed context fixes the timestamp, not random suffixes.

## When not to generate

- Client-minted customer/order IDs stay inputs. Do not replace them on retry.
- Random values supply neither idempotency nor gapless invoice allocation.
- An ID is not an authentication token or authorization policy.
- ApiKey is a **secret**, not an event-source ID: event-source IDs are plaintext metadata
  and cannot be encrypted. Use a separate `ApiKeyId` as the identity and keep ApiKey a
  secret value property. `[NotAudited]` withholds command
  causation values; it does not protect logs, responses or storage. Deliver a
  credential only over an authorized secure channel and retain its hash, not
  plaintext. For a retained operational integration secret instead, use
  `[Encrypted]` plus `[NotAudited]` as described by `cratis-arc-command`.
- Never log generated values/captures or seed production randomness. Use known
  values in specs; test shape/time separately when randomness is the behavior.

## Sources

Fundamentals `Source/DotNET/Fundamentals/Concepts/GenerationContext.cs`,
`IGeneratableInContext.cs`, `GenerateValue.TimeOrderedStrings.cs`,
`GenerateValue.RandomStrings.cs`; `Documentation/csharp/generating-values.md`.
Arc `Source/DotNET/Chronicle/Commands/NotAuditedAttribute.cs`,
`CommandCausationValues.cs` (command/type/property exclusion only, never
payload encryption or log protection);
`Documentation/backend/csharp/commands/generated-values.md`.
The latter attribute belongs to the host integration, not Fundamentals.
