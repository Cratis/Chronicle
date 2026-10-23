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
encoded bytes make an exact entropy budget explicit. InvoiceNumber uses a readable
pattern with Luhn check digit; TicketNumber uses an inclusive random Int64 range
for pickup tickets. Prefixes, dates and checksums add no entropy.
UUIDv7/ULID/TypeID reveal time and are not monotonic within a millisecond.
An invitation reference here is not by itself permission to join the shop; if
possession is the authorization mechanism, treat it as a protected credential too.

This complete concept excerpt requires `Cratis.Fundamentals` 7.25.0 or later and,
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

public record InvoiceNumber(string Value) : ConceptAs<string>(Value), IGeneratableInContext<InvoiceNumber>
{
    public static InvoiceNumber New(GenerationContext context) =>
        new(GenerateValue.Pattern("INV-{yyyy}{MM}-#{7}", context, Checksum.Luhn));
}

public record TicketNumber(long Value) : ConceptAs<long>(Value), IGeneratable<TicketNumber>
{
    public static TicketNumber New() => new(GenerateValue.Integer(100000, 999999));
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
Fundamentals' `Cratis.Concepts.GenerationContext` is a readonly record struct with
one captured `DateTimeOffset Now` and a String `Tenant`; `DefaultTenant` is the
zero-GUID string, never a display name or empty string. The shop passes time and
tenant together, for example:

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

For Screenplay `generate rule`, this Fundamentals type is the **Cratis C# target's**
binding of the language-neutral Now/Tenant descriptor, not the universal runtime
type. `Cratis.Screenplay.Contexts.GenerationContext` remains a BCL-only record with
portable `TenantId Tenant`, not a Fundamentals alias. The selected provider, not
`csharp` or a file reference, chooses the binding. Portable C# uses `context.Now`,
BCL operations and tenant text (for example `$"{context.Tenant}"`, compared ordinally).
Passing the whole context to `GenerateValue.*`, naming/constructing GenerationContext,
using `DefaultTenant` or reading `Tenant.Value` is target-bound. Never add a bridge
or claim Stage renders a rule merely because its Cratis binding is defined; read
[generation context binding](../../cratis-screenplay-command-surface/references/generated-values-and-responses.md#generation-context-binding)
for the compiler prerequisite and renderer refusal.

Use the context form only for time/tenant-dependent generation. If both interfaces
are implemented, callers choose context; the interfaces themselves do not dispatch.
Time-ordered built-in context overloads reject default Now (`MissingGenerationTime`)
and time before the Unix epoch. A fixed context fixes the timestamp, not random suffixes.

Requires Cratis.Fundamentals 7.25.0 for `Pattern`, `Integer`, `Checksum` and
`HasValidChecksum` (`dotnet list package --include-transitive`); older packages
lack these helpers. Upgrade deliberately or retain the existing factory.

`GenerateValue.Pattern(string pattern, Checksum checksum = Checksum.None)` is the
date-free overload; `Pattern(string pattern, GenerationContext context,
Checksum checksum = Checksum.None)` uses `context.Now` in UTC for date segments.
A date pattern without context throws `PatternRequiresGenerationContext`;
default Now on the context overload throws `MissingGenerationTime`.
Use `#` for digits, `?` for uppercase letters, `*` for letters/digits, positive
ASCII classes and repeats up to 64. UTC segments are `{yyyy}`, `{yy}`, `{MM}`, `{dd}`.
The appended checksum uses every preceding ASCII digit, including date digits:
`Checksum.Luhn` and `Mod11` append one digit, `Mod97` two; Mod11 excludes payloads
that would need digit 10. Output including suffix is limited to 256 characters.

Generation does not add a validator. Use
`GenerateValue.HasValidChecksum(value, Checksum.Luhn)` in the concept's validator
when supplied/stored values must obey the arithmetic too; combine it with shape
validation. `Integer(long minimum, long maximum)` returns `long`, includes both
endpoints and supports the full Int64 range with unbiased secure randomness.
Enforce unique short payload codes at append: randomness alone does not reserve
one, and event-source freshness does not protect a different payload value.

## When not to generate

- Client-minted customer/order IDs stay inputs. Do not replace them on retry.
- Random values and readable patterns supply neither idempotency nor gapless/sequential
  invoice allocation. A legally gapless invoice number needs an allocator.
- Check digits detect transcription mistakes, not forgery or authorization; patterns
  are not secrets. Low-entropy codes need explicit collision/uniqueness handling.
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
`GenerateValue.RandomStrings.cs`, `GenerateValue.Patterns.cs`,
`GenerateValue.Numbers.cs`, `Checksum.cs`; at Fundamentals v7.25.0,
`Documentation/csharp/generating-values/patterns.md`,
`Documentation/csharp/generating-values/reference.md` and
`Documentation/csharp/generating-values/context.md`.
Arc `Source/DotNET/Chronicle/Commands/NotAuditedAttribute.cs`,
`CommandCausationValues.cs` (command/type/property exclusion only, never
payload encryption or log protection);
`Documentation/backend/csharp/commands/generated-values.md`.
The latter attribute belongs to the host integration, not Fundamentals.
