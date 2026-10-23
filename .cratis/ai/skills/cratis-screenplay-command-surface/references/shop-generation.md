<!-- cratis-ai-managed: skills/cratis-screenplay-command-surface/references/shop-generation.md -->
<!-- Copyright (c) Cratis. All rights reserved. -->
<!-- Licensed under the MIT license. See LICENSE file in the project root for full license information. -->

# The shop in Screenplay: generated values

## Contents

- Choosing a strategy
- When not to generate
- Sources

## Choosing a strategy

Use the same vocabulary as the [Fundamentals/Arc shop](../../cratis-fundamentals-concept/references/shop-generation.md),
but only the released language realization: CustomerId is `cus_` + uppercase
ULID, OrderId is `typeid "order"`, and InviteCode is NanoID. InvoiceNumber uses
a readable date/checksum pattern; TicketNumber is an Int pickup-ticket value. Their strategies
belong to concepts; command properties only say `generated`/`generated identifier`.
The existing [strategy reference](generated-values-and-responses.md) owns exact
encodings, entropy, MCP edits, fixtures and freshness.

Requires Screenplay 4.172.0 or later (`screenplay --version`) for pattern/random/rule
and checksum validation (ESM v21); older compilers refuse them with PLAY0268.
`generate rule Name` can attach code with Now/Tenant context, but the reference
runner never executes it: pin the whole String result. TypeID itself has no
tenant slot. Context-member factory recovery still stays in code. Stage's
time-ordered factories use the command receipt, not an invented wall-clock value.

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
concept InvoiceNumber : String
  generate pattern "INV-{yyyy}{MM}-#{7}" checksum luhn
  validate checksum luhn
concept TicketNumber : Int
  generate random 100000..999999
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
      constraint UniqueInvoiceNumber
        unique invoiceNumber on OrderPlaced
      constraint UniquePickupTicket
        unique pickupTicket on OrderPlaced
      command PlaceOrder
        orderId OrderId generated identifier
        invoiceNumber InvoiceNumber generated
        pickupTicket TicketNumber generated
        produces event OrderPlaced
          invoiceNumber InvoiceNumber = invoiceNumber
          pickupTicket TicketNumber = pickupTicket
        returns orderId
      specification PlacesWithPinnedValue
        given clock "2026-10-02T09:00:00Z"
        when PlaceOrder
          for "order_0000000000e008000000000000"
          generated invoiceNumber = "INV-202610-00000004"
          generated pickupTicket = 123456
        then OrderPlaced
          for "order_0000000000e008000000000000"
          invoiceNumber = "INV-202610-00000004"
          pickupTicket = 123456
        then returns "order_0000000000e008000000000000"
```

Each property is server-owned, not a client-minted input. The ApiKey declaration
is not permission to replace its secret contract with an ordinary random String.
The invoice date segments use the pinned UTC clock; it does not draw a portable
ULID or TypeID fixture. Checksum validation is authored separately from generation.
Dates/check digits add no entropy. The short invoice and ticket values use
single-property unique constraints; neither is a generated identifier. Whole-source
freshness protects the OrderId destination, not every generated payload value.
A duplicate-source specification should
expect Concurrency with no new facts or response, not a regenerated ID.

## When not to generate

Client-minted customer/order IDs remain inputs. Fresh random values are not
idempotency keys, authorization, gapless invoice numbers or proof of projection
readiness. A reference InviteCode is not automatically a credential; if possession
confers access, protect it as a secret and do not claim this generated String
example realizes that credential behavior. Never drop ApiKey's secret marker to
make its generation bind/render. A pattern is not gapless/sequential numbering;
check digits catch typos, not forgery. ESM v21 binds and reference-executes with
pins. Requires Stage 4.118.0 for checksum validation and 4.119.0 for pattern/random
rendering; older renderers refuse those constructs. `generate rule` renders from Stage 4.136.0 (the Host
and legacy syntax engine still refuse it with STAGE-ESM-028). Sequence allocation and secret generation stay explicit gaps.

## Sources

Screenplay `Documentation/screenplay/generated-values/choosing.md`,
`Documentation/screenplay/generated-values.md`,
`Documentation/screenplay/concepts.md` (header secret classification),
`Source/DotNET/Screenplay/Semantics/SemanticModelBinder.Generation.cs`.
Stage `Documentation/reference/generated-values-rendering.md`.
Arc `Documentation/backend/csharp/generating-a-screenplay.md`.
Sources for the released .NET context/secret helpers are in the linked shop reference.
