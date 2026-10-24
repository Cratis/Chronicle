<!-- cratis-ai-managed: skills/cratis-screenplay-read-surface/references/named-keys.md -->
<!-- Copyright (c) Cratis. All rights reserved. -->
<!-- Licensed under the MIT license. See LICENSE file in the project root for full license information. -->

# Named read-model keys and complete lookups

## Contents

- Declare identity
- Supply every part
- Specifications and executable boundary
- Repairs and evidence

## Declare identity

A trailing `key` on a top-level read-model property declares instance identity.
One explicit mark replaces inference and binds byte-identically to the equivalent
inferred identifier. Several marks declare a composite key. Identity
selection is:

1. Explicit `key` marks take precedence over inference.
2. Without marks, single-instance queries in the owning slice supply identity from
   their `by` property; their distinct `by` names must identify one property.
3. Only when no single-instance query supplies an identifier **and** an admitted
   collection/list query exists does the binder consider conventional `*Id` names
   (case-sensitive). It first excludes collection-query `by` properties: if exactly
   one candidate remains, that is the identifier; otherwise it uses all `*Id`
   candidates and requires exactly one. An admitted list query here is a collection
   query without context-derived `by`, filters or scope.

Two `*Id` properties (`orderId`, `customerId`) with only a list query are ambiguous
(PLAY0268). Add one keyed query, whose `by` names the identity:

```screenplay excerpt
readmodel Order
  orderId String
  customerId String
query OrderById => Order optional
  by orderId String
```

A projection-only/queryless read model needs an explicit `key`: a lone `*Id`
property alone does not infer identity. Never use the command's `identifier`
modifier on a read model.

A key part is required and noncollection; it cannot also be `optional`, `generated`,
`identifier` or `subject`. A single key may have a composite type, but parts of a
multipart key must be scalar. Declaration order is display order, not identity:
Chronicle's object keys are not ordered, encoded event stream ids.

Choose a composite key when independent coordinates identify the business view,
such as resource and reporting period. Prefer the natural single id when the view
is one entity's state. Do not concatenate parts into an invented string id merely
to pass binding; keep the truthful model. Read-model keys do not supply projection
routes: align each PDL `from ... key` block with the declared parts (several flat
parts take an untyped `key` block naming every one; a single key of composite type
is a different shape, and one read model has exactly one of them).

## Supply every part

Requires Screenplay 4.204.0 (ESM v27; `screenplay --version`) to bind a read model with
two or more `key` marks; older binders refuse it with PLAY0268. A model with one `key`
mark keeps its earlier ESM version and bytes, so only models that use several parts
select v27.

A scalar query keeps `by name Type` on its own body line. Each lookup shape for a
composite-keyed read model is separate:

| Shape | Form | Result |
| --- | --- | --- |
| Optional single-instance query | bare `by` header, then every key part once as `name Type` | binds; the caller supplies every part |
| List query | equality `filter name Type [optional]` lines (one filter per narrowed part) | binds; a row matches when every supplied filter is equal |
| List query, one scalar `by` | `by name Type` on one line | binds as for a single key |
| List query with a `by` block | bare `by` header over a collection | PLAY0268: use filters |
| Non-optional single return, or a part with a `from` source | any `by` block | PLAY0268 |
| Missing part | any single-instance lookup | PLAY0661 |
| Unknown, optional, collection or ill-typed part; fewer than two parts | `by` block | PLAY0662 |

Do not combine scalar and block `by` forms. A caller-supplied part is never a trusted
tenant or subject: keep ownership at the runtime boundary.

Each `by` part runs its concept rules at execution, like a single `by` argument. A
specification argument that violates a rule still binds, so a Validation rejection
can be specified.

Command and trigger reads use a child `by` block of `part = source`, with required,
noncollection sources of compatible nominal types, not literals. Neither executes
over a composite view in the usual way:

- A command `reads … by` without the application-root `command reads protected`
  preamble is PLAY0271. With the preamble it is PLAY0781 (reason `by-parts`), PLAY0782
  first when the command has no `identifier`, and PLAY0273 when the key is not an
  input property.
- A trigger `reads … by` binds when the reaction only invokes commands (information
  PLAY0270); a reaction that produces events directly or runs code is PLAY0268.

Screen `data ... via query ... by value`, forms and navigation remain scalar
lookups: they cannot supply a composite single-instance key (PLAY0661). Do not
invent UI tuple syntax.

One executable example: a composite-keyed read model, a projection whose root `from`
and `remove with` name every part in an untyped `key` block, a list query with
equality filters, an optional single-instance query with a `by` block, and
specifications that run.

```screenplay
concept InvoiceId : String
  validate
    min 3 message "Invoice id is too short"
concept PaymentMethod : Enum
  card
  cash

module Billing
  feature Payments
    slice StateView Totals
      event PaymentReceived
        invoice InvoiceId
        method PaymentMethod
        amount Int
      event PaymentRemoved
        invoice InvoiceId
        method PaymentMethod
      readmodel PaymentTotals
        invoiceId InvoiceId key
        method PaymentMethod key
        total Int
      query TotalsForInvoice => PaymentTotals[]
        filter invoiceId InvoiceId
        filter method PaymentMethod optional
      query Total => PaymentTotals optional
        by
          invoiceId InvoiceId
          method PaymentMethod
      projection PaymentTotals => PaymentTotals
        no automap
        from PaymentReceived
          key
            invoiceId = invoice
            method = method
          total = amount
        remove with PaymentRemoved
          key
            invoiceId = invoice
            method = method
      specification TwoMethodsOneInvoice
        given readmodel PaymentTotals
          invoiceId = "INV-1"
          method = "card"
          total = 12
        given readmodel PaymentTotals
          invoiceId = "INV-1"
          method = "cash"
          total = 8
        when query Total
          invoiceId = "INV-1"
          method = "cash"
        then result
          total = 8
      specification FilterOnePart
        given readmodel PaymentTotals
          invoiceId = "INV-1"
          method = "card"
          total = 12
        given readmodel PaymentTotals
          invoiceId = "INV-2"
          method = "card"
          total = 3
        when query TotalsForInvoice
          invoiceId = "INV-1"
        then result
          invoiceId = "INV-1"
          method = "card"
          total = 12
      specification RemoveOneMethod
        given readmodel PaymentTotals
          invoiceId = "INV-1"
          method = "card"
          total = 12
        given readmodel PaymentTotals
          invoiceId = "INV-1"
          method = "cash"
          total = 8
        when append PaymentRemoved
          invoice = "INV-1"
          method = "card"
        then no readmodel PaymentTotals for {"invoiceId":"INV-1","method":"card"}
        then readmodel PaymentTotals
          invoiceId = "INV-1"
          method = "cash"
          total = 8
      specification RecreateRemovedInstance
        given PaymentReceived
          invoice = "INV-1"
          method = "card"
          amount = 12
        given PaymentRemoved
          invoice = "INV-1"
          method = "card"
        when append PaymentReceived
          invoice = "INV-1"
          method = "card"
          amount = 20
        then readmodel PaymentTotals
          invoiceId = "INV-1"
          method = "card"
          total = 20
```

Projection rules for the same shape are in the projections skill (`SKILL.md`, keys).
Verify with `screenplay <folder> --warnaserror` (0 errors, 0 warnings) and
`screenplay test <folder>` (all passed, none Unsupported).

## Specifications and executable boundary

`given readmodel` and `then readmodel` state every key part (PLAY0351 if missing;
a repeated part is PLAY0519). `then no readmodel` takes an object naming every part
(missing PLAY0661, unknown PLAY0662, repeated member PLAY0294). Parts match one by
one, in any order; Uuid parts compare as ordinal text with no case folding. Query
arguments supply every `by` part (single) or the filters (list); a missing, unknown
or repeated argument is PLAY0468. A single-instance query result may omit parts the
arguments supply; list-query result rows state every part. A stated part that
differs from the argument makes the row fail to match at run time, not at binding.

Projections on a composite-keyed read model admit only root `from` and `remove with`.
Joins, `children`, `remove via join`, `every`, `all`, `nested`, `clear with`,
`variant` and reducers are PLAY0268. This is not ESM v10 admission.

Requires Stage 4.137.0 for v27 admission (older Stage refuses the whole model with
`STAGE-ESM-016`). The semantic Host runs composite-keyed read models and the CLI's `cratis run`
default engine runs them (Requires Cratis CLI 3.75.0); the legacy `eventmodel` engine and the specification
executor refuse them. Rendering refuses a read model with several key parts or a projection keyed by
parts (`STAGE-ESM-048`) and a single-instance query by parts (`STAGE-ESM-047`, pending Chronicle#4265),
and publishes nothing, so `cratis render` cannot deliver it. Verify and reference-test the model with the
standalone compiler, then hand the delivery to gap-fill
(`cratis-screenplay-render-and-gap-fill`) with the model as the contract. Composite-keyed read models get
no conventional `Get<ReadModel>ById` query.
Eligible command reads separately opt into v25 with `command reads protected`;
a composite lookup there is still refused (PLAY0781 reason `by-parts`). See command-surface
[protected reads](../../cratis-screenplay-command-surface/references/protected-reads.md).

## Repairs and evidence

- PLAY0660: fix key owner, modifier or type; no automatic repair.
- PLAY0661: supply missing parts. The typed repair covers **command reads only**
  when each missing part has one same-named compatible source. It preserves
  authored mappings and refuses ambiguity; queries/trigger reads are not repaired.
- PLAY0662: fix shape, duplicate/unknown parts or incompatible sources; no repair.
- Read-model property rename remains unsupported until its mapping chain can be
  preserved. Use the model-authoring proposal/preview/apply contract, not a text
  shortcut around an identity-preserving refusal.
