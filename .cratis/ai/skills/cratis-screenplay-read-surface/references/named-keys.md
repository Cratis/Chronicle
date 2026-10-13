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
inferred identifier. Several marks declare a composite key. At 4.135.0, identity
selection is:

1. Explicit `key` marks take precedence over inference.
2. Without marks, single-instance queries in the owning slice supply identity from
   their `by` property; their distinct `by` names must identify one property.
3. Only when no single-instance query supplies an identifier **and** an admitted
   collection/list query exists does the binder consider conventional `*Id` names
   (case-sensitive). It first excludes collection-query `by` properties: if exactly
   one candidate remains, that is the identifier; otherwise it uses all `*Id`
   candidates and requires exactly one. An admitted list query here is a collection
   query without context-derived `by`, filters, scope or performer.

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
to pass binding; keep the truthful model and report the capability gap. Read-model
keys do not supply projection routes: align each PDL `from ... key` mapping with
the declared view identity (projection keys are a separate contract).

## Supply every part

A scalar query keeps `by name Type` on its own body line. A composite query has a
bare `by` header and at least two distinct `name Type [from source]` children.
Do not combine scalar and block forms. A single-instance lookup must supply every
key part exactly once, with the declared types; collection queries may narrow by
a subset but still have their separate executable-query limitations.

A command or reaction trigger read uses a child `by` block of `part = source`.
Sources are required, noncollection property paths with compatible nominal types,
not literals. Trigger reads map from the trigger's declared values. The historical
single `reads View by value` form remains available for a single key.

This complete authoring-only example declares all lookup parts:

```screenplay
concept ResourceId : Uuid
concept Period : String
module Reporting
  feature Months
    slice StateView Totals
      readmodel ResourceMonth
        resourceId ResourceId key
        period Period key
        hours Decimal
      query FindMonth => ResourceMonth optional
        by
          resourceId ResourceId
          period Period
    slice StateChange CloseMonth
      command CloseMonth
        resourceId ResourceId identifier
        period Period
        reads ResourceMonth as totals
          by
            resourceId = resourceId
            period = period
        produces event MonthClosed
          for resourceId
          period Period = period
```

Screen `data ... via query ... by value`, forms and navigation remain scalar
lookups: they cannot supply a composite single-instance key (PLAY0661). Do not
invent UI tuple syntax. Context-derived query parts do not become caller inputs;
keep tenant/subject ownership at the runtime boundary, not in editable UI filters.

## Specifications and executable boundary

`given readmodel` and `then readmodel` state every key part (PLAY0351 if missing).
A composite absence fixture uses an object with every named part. Query result
assertions may omit keys when arguments already select the instance.

At Screenplay 4.135.0, composite read-model keys, query `by` blocks and fixtures
over those views remain authoring-only: binding reports PLAY0268 citing
Screenplay#599 (still open). They do not reference-execute or render. This is not
ESM v10 admission; v10 covers reaction identity, production routes and observer
filters. Command reads independently remain unprotected/unadmitted (PLAY0271).
A single explicit key does bind; Stage admission still depends on the complete
model and projection/query shape, not just the key modifier.

## Repairs and evidence

- PLAY0660: fix key owner, modifier or type; no automatic repair.
- PLAY0661: supply missing parts. The typed repair covers **command reads only**
  when each missing part has one same-named compatible source. It preserves
  authored mappings and refuses ambiguity; queries/trigger reads are not repaired.
- PLAY0662: fix shape, duplicate/unknown parts or incompatible sources; no repair.
- Read-model property rename remains unsupported until its mapping chain can be
  preserved. Use the model-authoring proposal/preview/apply contract, not a text
  shortcut around an identity-preserving refusal.

Source: Screenplay `v4.135.0:Documentation/screenplay/{readmodels,queries,commands,reactions,specifications}.md`,
`fixtures/named-read-model-keys.play`, decision 0060, and
`Source/DotNET/Screenplay/Semantics/SemanticModelBinder.ReadModels.cs` (identity inference).
Its authoring delivery shipped
in 4.129.0; that release did not deliver #599's executable composite-key contract.
