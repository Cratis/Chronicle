<!-- cratis-ai-managed: skills/cratis-screenplay-read-surface/references/equality-filters.md -->
<!-- Copyright (c) Cratis. All rights reserved. -->
<!-- Licensed under the MIT license. See LICENSE file in the project root for full license information. -->

# Caller-supplied equality query filters (ESM v14)

## Contents

- Refusals kept intact
- Specifications that run
- Argument diagnostics
- Rendering is a different verdict
- Product sources

Screenplay binds `filter <name> <Type> [optional]` on a collection query when:

- there is no `from` source;
- the read model has a same-named non-collection primitive, concept or enum property;
- the filter has the same nominal type as that property, ignoring optionality;
- filter names are distinct and none repeats the `by` name.

Each supplied value matches that property by equality. `by` and all supplied
filters combine with AND; a row missing the property does not match. A required
filter must be supplied. An optional filter may be omitted, in which case it
adds no restriction. **Supplied `null` is refused**, even for an optional filter;
it is not omission or a request to match null properties.

Filters select ESM v14. Models without filters retain their bytes, version and
revision. `observable` collections may use filters too, but the reference runner
answers a snapshot; change sets, paging, sorting and non-equality predicates
are not admitted by this increment. Query policies still see only `by`, not filters.
A caller-supplied filter is not a trusted tenant or authorization boundary.

## Refusals kept intact

Each binding refusal reports PLAY0268 with its query/filter and reason:

| Shape | Why it remains unadmitted |
| --- | --- |
| `filter ... from ...` or context-derived `by` | Typed query context sources are not admitted; a caller argument must not stand in for trusted context |
| Filter on a single-result query | Equality filters are admitted only for collections |
| `scoped to` | The named scope has no portable executable scope contract |
| `performer` | Query implementation code is opaque, not a portable declarative lookup |
| Composite `by` block | Multipart lookup admission remains deferred (Screenplay#599) |
| Filter on a collection/composite property, missing property or mismatched nominal type | The v14 filter contract requires a same-named scalar primitive, concept or enum property with the same type |

Keep those intents when the domain needs them; never drop a context source or
scope to make a query bind.

## Specifications that run

Both `then query … arguments` and `when query` name `by` and filter arguments.
Unknown, repeated or missing required arguments report **PLAY0468** in either
form. The query, its key and supplied filter values identify a call, so one
specification may assert two calls with different filters.

Collection results compare **without regard to row order**, matched by read
model and instance key. Row count is exact; asserted properties use subset
matching unless `exactly` is requested. This does not relax ordering of event
expectations or collection-valued properties inside one row.

This complete model checks omission, supplied filters, AND with `by`, both query
specification forms and reversed result rows:

```screenplay
concept InvoiceId : String
concept CustomerId : String
concept InvoiceStatus : Enum
  issued
  paid
module Invoicing
  feature Overview
    slice StateView Invoices
      readmodel InvoiceRow
        invoiceId InvoiceId key
        customerId CustomerId
        status InvoiceStatus
      query ListInvoices => InvoiceRow[]
        filter status InvoiceStatus
        filter customerId CustomerId optional
      query InvoicesById => observable InvoiceRow[]
        by invoiceId InvoiceId
        filter status InvoiceStatus optional
      example FirstIssued : InvoiceRow
        invoiceId = "inv-1"
        customerId = "customer-a"
        status = issued
      example SecondIssued : InvoiceRow
        invoiceId = "inv-2"
        customerId = "customer-b"
        status = issued
      example PaidInvoice : InvoiceRow
        invoiceId = "inv-3"
        customerId = "customer-a"
        status = paid
      specification OmittingAnOptionalFilter
        given readmodel FirstIssued
        given readmodel SecondIssued
        given readmodel PaidInvoice
        then query ListInvoices
          arguments
            status = issued
          result
            invoiceId = "inv-2"
          result
            invoiceId = "inv-1"
      specification SupplyingBothFilters
        given readmodel FirstIssued
        given readmodel SecondIssued
        then query ListInvoices
          arguments
            status = issued
            customerId = "customer-a"
          result
            invoiceId = "inv-1"
      specification CombiningByAndFilter
        given readmodel FirstIssued
        given readmodel SecondIssued
        given readmodel PaidInvoice
        when query InvoicesById
          invoiceId = "inv-3"
          status = issued
        then no result
      specification OmittingTheFilterBesideBy
        given readmodel FirstIssued
        given readmodel SecondIssued
        given readmodel PaidInvoice
        when query InvoicesById
          invoiceId = "inv-3"
        then result
          invoiceId = "inv-3"
      specification DistinguishingCallsByFilterValues
        given readmodel FirstIssued
        given readmodel PaidInvoice
        then query ListInvoices
          arguments
            status = issued
          result
            invoiceId = "inv-1"
        then query ListInvoices
          arguments
            status = paid
          result
            invoiceId = "inv-3"
```

## Argument diagnostics

This deliberately invalid fence pins PLAY0468 for missing, unknown and repeated
arguments in both specification forms. These are source errors, not passing
reference scenarios or empty-result assertions.

```screenplay expect PLAY0468
concept ItemId : String
module FilteringExamples
  feature Arguments
    slice StateView Items
      readmodel Item
        itemId ItemId key
        status String
      query ListItems => Item[]
        filter status String
      specification MissingThenArgument
        then query ListItems
      specification MissingWhenArgument
        when query ListItems
        then no result
      specification UnknownThenArgument
        then query ListItems
          arguments
            status = "open"
            search = "anything"
      specification UnknownWhenArgument
        when query ListItems
          status = "open"
          search = "anything"
        then no result
      specification RepeatedThenArgument
        then query ListItems
          arguments
            status = "open"
            status = "open"
      specification RepeatedWhenArgument
        when query ListItems
          status = "open"
          status = "open"
        then no result
```

## Rendering is a different verdict

A CLI whose bundled Screenplay and Stage admit **ESM v14** or later renders equality filters on observable collections, with or without
`by`, combined by AND.`by`, combined by AND. Optional omission is preserved. Snapshot lists, DateTime
filters and required filter parameters after optional ones refuse `STAGE-ESM-010`;
parameter name collisions refuse `-012`. Generated query specifications pass named
arguments and check result counts. The semantic Host executes filters, but the
independent specification executor refuses filtered assertions with `-010`
(and retains the GivenReadModel gap). V3/V4 is still not a render/build/test result;
never erase filters to obtain a render.

## Product sources

- Screenplay: `Documentation/screenplay/queries.md` (Executable equality filters) and
  `specifications.md`.
- Stage: `Source/Rendering.Cratis/Semantics/SemanticVersionFeatures.cs` and
  `SemanticCratisAdmission.StateView.cs` check filters/order; `Documentation/docker/index.md`,
  `docker/spec-runner.md` distinguish Host and independent executor.
- CLI: `Source/Cli/Commands/Render/RenderedSemanticVersions.cs` lists the admitted ESM versions.

Requires Screenplay 4.143.0 or later (`screenplay --version`) for caller-supplied equality filters (ESM v14).
