<!-- cratis-ai-managed: skills/cratis-arc-command/references/shop-generation.md -->
<!-- Copyright (c) Cratis. All rights reserved. -->
<!-- Licensed under the MIT license. See LICENSE file in the project root for full license information. -->

# Shop commands: generation stays out of requests

## Contents

- Choosing a strategy
- When not to generate
- Sources

## Choosing a strategy

Use the [same shop concepts](../../cratis-fundamentals-concept/references/shop-generation.md):
CustomerId is `cus_` + ULID; OrderId is context-aware TypeID; InviteCode is NanoID;
ApiKey is secret `Bytes(32)` with `[NotAudited]`. InvoiceNumber is a date/checksum
pattern and TicketNumber an inclusive random Int64 pickup ticket. The concept owns
the factory, not a command property or a per-call random implementation.
Requires Fundamentals 7.25.0 for Pattern/Integer factories; check `dotnet list package --include-transitive`, upgrade or retain existing factories on older versions.

The following complete command declarations depend on those Shop concepts and
`Cratis.Arc.Core` / `Cratis.Arc.Chronicle`. They allocate response values only, not persisted entities or events;
they are not production authentication endpoints. Protect any credential-issuing
endpoint independently before hosting it.

```csharp
// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands.ModelBound;

namespace Shop;

[Command]
public record AllocateCustomerId
{
    public CustomerId Handle([GeneratedValue("customerId")] CustomerId customerId) => customerId;
}

[Command]
public record AllocateOrderId
{
    public OrderId Handle([GeneratedValue("orderId")] OrderId orderId) => orderId;
}

[Command]
public record AllocateInviteCode
{
    public InviteCode Handle([GeneratedValue("inviteCode")] InviteCode inviteCode) => inviteCode;
}

[Command]
public record AllocateApiKey
{
    public ApiKey Handle([GeneratedValue("apiKey")] ApiKey apiKey) => apiKey;
}
```

Requires Arc 22.83.0 for `IGeneratableInContext<TSelf>`: it maps captured
`CommandContext.ReceivedAt` to `Now` and `ITenantIdAccessor.Current` to `Tenant`.
NotSet/Default tenants become the zero-GUID default; named tenants retain their
identifier. TypeID uses time only; the context carries the tenant but the built-in
factory does not encode it. Keep tenant authorization/storage scope independent.

Arc prefers context if both generation interfaces are implemented. An unpinned
context factory with default ReceivedAt throws `MissingGenerationTime`, even
when custom code ignores time; it never reads a replacement UtcNow. Pins bypass
generation, not concept validation. Validate-only requests do not allocate or
reserve the generated value for a later command.

For a command pipeline specification, these setup calls are released:

```csharp excerpt
using var scenario = new CommandScenario<AllocateOrderId>();
scenario.Clock(DateTimeOffset.Parse("2026-10-05T08:00:00Z"));
scenario.Tenant(new TenantId("shop-eu"));
scenario.PinGeneratedValue("orderId", new OrderId("order_0000000000e008000000000000"));
```

Imports: `System`, `Shop`, `Cratis.Arc.Testing.Commands`, `Cratis.Arc.Tenancy`.
Configure before Execute/Validate; Clock also precedes initialization. Clock fixes
receipt/UTC wall time, not randomness; Tenant changes the scenario's executions,
not its caller. A pin fixes the whole OrderId and bypasses its factory. To inspect
an unpinned timestamp contract, opt in with `AllowGeneration()` and assert shape
and captured-time semantics, never one exact random suffix.

## When not to generate

- Existing/client-owned IDs stay command inputs; services/read models remain
  ordinary dependencies. Generation is not authorization or tenant isolation.
- Randomness is not idempotency, storage freshness or a gapless invoice allocator.
  Stage's explicit whole-source scopes protect generated destinations, not
  every generated payload code; enforce payload uniqueness separately.
- ApiKey is a credential, never a Chronicle stream ID: event-source IDs are plaintext
  metadata and cannot be encrypted. Identify the key with a separate `ApiKeyId` and keep
  ApiKey a secret value property. `[NotAudited]` protects
  causation only. Never log captures, return secrets on public endpoints, or
  append plaintext authentication credentials. Retain a hash; for retained
  operational secrets use `[Encrypted]` plus `[NotAudited]`.
- Readable patterns are not gapless/sequential invoice allocators or secrets;
  check digits catch typos, not forgery. Short codes need uniqueness handling;
  generation alone does not reserve a code.
- Screenplay admits attached generation rules with Now/Tenant context, but its
  reference evaluator only consumes pinned results. Generated secrets remain
  refused. A compiled Arc factory does not imply that it can recover or render
  as `.play`. Read [generated-value recovery](../../cratis-screenplay-legacy-extraction/references/generated-recovery.md)
  for Arc.Screenplay prerequisites and refusal boundaries, and
  [Stage admission](../../cratis-stage-rendering-and-sandbox/references/admission.md)
  for checksum, pattern/random and `generate rule` rendering prerequisites (Stage 4.136.0 renders
  `generate rule`; the Host and legacy engine still refuse it with STAGE-ESM-028).

## Sources

`Source/DotNET/Arc.Core/Commands/GeneratedValueResolution.cs`,
`GeneratedValueFactories.cs`, `CommandContext.cs`;
`Source/DotNET/Testing/Commands/CommandScenarioGeneratedValueExtensions.cs`;
`Source/DotNET/Chronicle/Commands/NotAuditedAttribute.cs`,
`CommandCausationValues.cs` (command causation exclusion, not event/log protection);
`Documentation/backend/csharp/commands/generated-values.md`.
Factory/context signatures are in the linked shop concept reference.
