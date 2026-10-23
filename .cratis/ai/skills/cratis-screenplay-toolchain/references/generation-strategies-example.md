<!-- cratis-ai-managed: skills/cratis-screenplay-toolchain/references/generation-strategies-example.md -->
<!-- Copyright (c) Cratis. All rights reserved. -->
<!-- Licensed under the MIT license. See LICENSE file in the project root for full license information. -->

# Shop generation strategies example (ESM v21)

## Contents

- Choosing a strategy
- Pinned creation and freshness
- Attached generation rule
- When not to
- Verify

## Choosing a strategy

Requires Screenplay 4.172.0 or later (`screenplay --version`) for pattern/random
and checksum validation; older compilers refuse PLAY0268.
Requires Stage 4.118.0 for checksum validation and 4.119.0 for pattern/random
rendering; older renderers refuse them with STAGE-ESM-042/-028. The attached rule
below still refuses STAGE-ESM-028. The reference runner uses whole-value fixtures,
never random seeds.

OrderId stays the high-entropy source identity. InvoiceNumber is a readable
reference, not a gapless/sequential invoice allocator or secret; its check digit
catches typos, not forgery. The Int pickup ticket is a value, not an identifier.
Unique constraints protect short payload codes, while serialized generation
activates freshness for the whole generated source. Dates/check digits add no
entropy. The clock fixes every UTC pattern segment, not the random digits.

## Pinned creation and freshness

```screenplay
concept OrderId : String
  generate typeid "order"
concept InvoiceNumber : String
  generate pattern "INV-{yyyy}{MM}-#{7}" checksum luhn
  validate checksum luhn
concept TicketNumber : Int
  generate random 100000..999999
module Shop
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
      specification PlacesWithPinnedValues
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
      specification RejectsAUsedGeneratedSource
        given clock "2026-10-02T09:00:00Z"
        given OrderPlaced
          for "order_0000000000e008000000000000"
          invoiceNumber = "INV-202610-00000004"
          pickupTicket = 123456
        when PlaceOrder
          for "order_0000000000e008000000000000"
          generated invoiceNumber = "INV-202610-00000004"
          generated pickupTicket = 123456
        then error "A generated event source already has facts; retry the command with a fresh generated value."
```

The rejection appends no new facts and returns no response. Freshness guards the
whole source even if its old facts use another stream or event type. Without a
serialized strategy the model is Unguarded, even if generated-concept rules
select v20; removing strategies is not a way to repair stale-source behavior.
Pin failure against shape or authored rules is a server Contract rejection,
never a client validation failure or a reason to redraw. Without `validate
checksum`, a fixture checks suffix shape only, not arithmetic. Without `given
clock`, date segments check digit widths only, never today's date.

## Attached generation rule

An implementation-specific tenant-bearing reference can instead attach one
generation rule; its entropy is unknown, not zero. The body gets only Now and
Tenant. The reference evaluator consumes a String fixture without running code:

````screenplay
concept TenantOrderReference : String
  generate rule TenantOrder
    ```csharp
    return $"{context.Tenant}:order_{context.Now:yyyyMMdd}-{Guid.NewGuid():N}";
    ```
module Shop
  feature Orders
    slice StateChange ReserveReference
      command ReserveReference
        reference TenantOrderReference generated
        returns reference
      specification UsesThePinnedReference
        when ReserveReference
          generated reference = "shop-eu:order_example"
        then returns "shop-eu:order_example"
````

## When not to

Keep client-minted values as inputs. Generation is neither idempotency nor
authorization. Use an allocator for gapless numbering, and preserve secret
classification rather than trying to bind generated credentials. A short code's
uniqueness constraint protects its payload claim, not source freshness or retry
safety after an unknown append outcome.

## Verify

Compile with `screenplay <file> --warnaserror`; then `screenplay test <file>` must report
all selected specifications passed, none failed or unsupported. Passing reference
specifications do not prove target support or that attached generation code runs.
