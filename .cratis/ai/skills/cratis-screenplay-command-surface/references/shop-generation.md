<!-- cratis-ai-managed: skills/cratis-screenplay-command-surface/references/shop-generation.md -->
<!-- Copyright (c) Cratis. All rights reserved. -->
<!-- Licensed under the MIT license. See LICENSE file in the project root for full license information. -->

# The shop in Screenplay: keep the release boundary

## Choosing a strategy

Use the same vocabulary as the [Fundamentals/Arc shop](../../cratis-fundamentals-concept/references/shop-generation.md),
but only the released language realization: CustomerId is `cus_` + uppercase
ULID, OrderId is `typeid "order"`, and InviteCode is NanoID. Their strategies
belong to concepts; command properties only say `generated`/`generated identifier`.
The existing [strategy reference](generated-values-and-responses.md) owns exact
encodings, entropy, MCP edits, fixtures and freshness.

**Context-dependent generation is not released in Screenplay.** The OrderId below
is time-ordered TypeID, not Arc's received-time/tenant factory contract; Stage
emits a wall-clock `New()`. Passed-through GenerationContext in Arc source
recovery describes that strategy shape, not equivalent clock/tenant semantics.
Context-member/rule recovery stays in code.

The shop's ApiKey remains a **secret**. In .NET its factory is `Bytes(32)` and its
concept is `[NotAudited]`. It is never an identifier (event-source IDs are plaintext
metadata); a separate `ApiKeyId` identifies the key. Executable secret generation is **not released in
Screenplay**. The declaration below preserves classification without
adding `generate bytes 32` or a generated command property. No API-key issuance
behavior is claimed; retain it as an explicit gap/owning implementation contract.

This complete reference-executable model compiles with the standalone tool:

```screenplay
concept CustomerId : String
  generate ulid prefix "cus_"
concept OrderId : String
  generate typeid "order"
concept InviteCode : String
  generate nanoid
concept ApiKey : String secret
module Shop
  feature Customers
    slice StateChange Register
      command RegisterCustomer
        customerId CustomerId generated identifier
        inviteCode InviteCode generated
        produces event CustomerRegistered
          inviteCode InviteCode = inviteCode
        returns
          customerId = customerId
          inviteCode = inviteCode
      specification RegistersWithPinnedValues
        when RegisterCustomer
          for "cus_01ARZ3NDEKTSV4RRFFQ69G5FAV"
          generated inviteCode = "abcdefghijklmnopqrstu"
        then CustomerRegistered
          for "cus_01ARZ3NDEKTSV4RRFFQ69G5FAV"
          inviteCode = "abcdefghijklmnopqrstu"
        then returns
          customerId = "cus_01ARZ3NDEKTSV4RRFFQ69G5FAV"
          inviteCode = "abcdefghijklmnopqrstu"
  feature Orders
    slice StateChange Place
      command PlaceOrder
        orderId OrderId generated identifier
        produces event OrderPlaced
        returns orderId
      specification PlacesWithPinnedValue
        when PlaceOrder
          for "order_0000000000e008000000000000"
        then OrderPlaced
          for "order_0000000000e008000000000000"
        then returns "order_0000000000e008000000000000"
```

Each property is server-owned, not a client-minted input. The ApiKey declaration
is not permission to replace its secret contract with an ordinary random String.
No `given clock` is used: pin the whole value. A seed/clock does not draw a
portable ULID or TypeID fixture. A v15 duplicate-source specification should
expect Concurrency with no new facts or response, not a regenerated ID.

## When not to generate

Client-minted customer/order IDs remain inputs. Fresh random values are not
idempotency keys, authorization, gapless invoice numbers or proof of projection
readiness. A reference InviteCode is not automatically a credential; if possession
confers access, protect it as a secret and do not claim this generated String
example realizes that credential behavior. Never drop ApiKey's secret marker to
make its generation bind/render. Pattern/random/rule execution, context rules,
sequence allocation and secret generation stay explicit upstream gaps.

## Tagged evidence

Screenplay `Documentation/screenplay/generated-values/choosing.md`,
`Documentation/screenplay/generated-values.md`,
`Documentation/screenplay/concepts.md` (header secret classification),
`Source/DotNET/Screenplay/Semantics/SemanticModelBinder.Generation.cs`.
Stage `Documentation/reference/generated-values-rendering.md`.
Arc `Documentation/backend/csharp/generating-a-screenplay.md`.
Sources for the released .NET context/secret helpers are in the linked shop reference.
