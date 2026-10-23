<!-- cratis-ai-managed: skills/cratis-screenplay-specifications/references/query-rejections.md -->
<!-- Copyright (c) Cratis. All rights reserved. -->
<!-- Licensed under the MIT license. See LICENSE file in the project root for full license information. -->

# Query rejection versus absence

Requires Screenplay 4.161.0 or later (`screenplay --version`) for query argument validation and query `then error` (ESM v18); older runners may return no result for an invalid key.

Authorization runs first. The `by` key and each supplied filter value then pass
through their concept's `validate` rules in every ESM version. An omitted optional
filter is not validated. A violation is a Validation rejection carrying the rule's
message; a valid key with no matching instance is absence.

On a composite-keyed read model every `by` part of a single-instance query is validated
the same way; an argument that breaks a part's concept rule binds and rejects with
Validation (Requires Screenplay 4.204.0, ESM v27).

Use exactly one query request and one `then error`, bare or with a message. No
command/append/reaction action, result, `then no result` or `then denied` may
accompany it (PLAY0465). An actionless `then query` with `arguments` and no `result`
can also carry the sole error assertion. Only the error-asserting form selects v18;
ordinary argument validation does not change the model's ESM version.

Run `screenplay test <root>` on this complete model: exit 0, two passed scenarios.

```screenplay
concept InvoiceNumber : String
  validate
    min 2 message "Invoice number is too short"
module Billing
  feature Lookup
    slice StateView FindInvoice
      readmodel Invoice
        number InvoiceNumber key
      query GetInvoice => Invoice optional
        by number InvoiceNumber
      specification RejectingAShortNumber
        when query GetInvoice
          number = "A"
        then error "Invoice number is too short"
      specification MissingAValidNumber
        when query GetInvoice
          number = "AB"
        then no result
```

This proves query-boundary rejection, not event rejection by a projection.
Renderer admission is independent: a target that does not admit v18 cannot render
this specification. Keep the model's concept rules and report the target gap.
