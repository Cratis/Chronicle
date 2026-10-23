<!-- cratis-ai-managed: skills/cratis-screenplay-captures-and-reactions/references/atomic-batches-and-source-mappings.md -->
<!-- Copyright (c) Cratis. All rights reserved. -->
<!-- Licensed under the MIT license. See LICENSE file in the project root for full license information. -->

# Atomic reaction batches and event-source invocation mappings

## Choose the batch boundary

Requires Screenplay 4.181.0 or later (`screenplay --version`) for atomic batches; older compilers cannot admit the declaration.

Start new models with application-root `reaction batches atomic`, before declarations.
Declare it once per composed application; imports inherit it. It selects ESM v22 or
later even before the first reaction. Duplicate, conflicting or invalid declarations
report PLAY0693. Do not automatically repair by removing it: that changes semantics.

One trigger occurrence's direct `produces` list is one all-or-none batch. Each selected
invocation-refusal branch's productions form another batch. Invoked commands retain
their own atomic appends; neither separate triggers nor occurrences are combined.
This is not a transaction around the cascade.

Candidates are materialized and append-validated before private projections and
publications. Primary facts are adopted first, then derived publications; only adopted
facts start cascades. The complete expanded result must fit the 1,000-new-fact budget
before adoption; given history does not count. A failed batch leaves its pre-batch
facts, read models and pending children unchanged. Earlier accepted batches/commands
remain. Violation order is authored production order, then ordinal constraint name;
SemanticRejected.Production carries the zero-based index, event contract and route.
Property-value reservations within a batch are not freed when a source changes value;
the same source may repeat its own claim.

Without the preamble, per-fact production remains legacy behavior, even at a higher
ESM version. Recompiling never opts an existing model in. Review migration explicitly.
Atomic batching does not admit direct-production stream routes or closing constraints,
deduplicate redelivery, promise atomic observer snapshots or resolve unknown storage
outcomes. `then error` alone is insufficient: also assert no facts from the failed
batch and unchanged read models with evaluator world/read-model assertions; earlier
accepted work can remain. `then no events` means accepted zero outcomes and cannot
accompany `then error`. Do not invent a mixed success/error specification form.

Requires Stage 4.139.0 for ESM v22 admission; older Stage refuses STAGE-ESM-016.
Atomic mode is still refused with STAGE-ESM-043 by renderer, Host and specification executor.
Keep the declaration and report authorized gap-fill; do not promise a render date or
strip atomicity to lower the version.

MCP `describe-application` reports `reactionProductionMode` and
`reactionProductionReadiness`; ApplicationSyntax.reactionProductionMode is Atomic.
Source/AST proposals preserve it; readiness is not target realization.

## Map the triggering event source

Requires Screenplay 4.197.1 or later (`screenplay --version`) for event-source `invokes` mappings; older compilers refuse them with PLAY0268.

Use context identity rather than duplicating an event source in event payload.
Complete model (atomic mode deliberately retained despite Stage's refusal):

```screenplay
domain Orders
reaction batches atomic
module Fulfillment
  feature Packing
    slice StateChange PackOrder
      command PackOrder
        orderId String identifier
        produces OrderPacked
          for orderId
      event OrderPacked
    slice Automation PackPlacedOrder
      event OrderPlaced
      reaction PackPlacedOrder
        when OrderPlaced
          invokes PackOrder
            orderId = $eventSourceId
```

`$eventContext.eventSourceId` is equivalent. The source also works in reaction
`produces` mappings. The target must be a required scalar; reference execution checks
the value against that target type without conversion. Clock/application triggers
have no event source. This is neither clock context nor caller/audit identity and
does not select a new ESM version: it uses the reaction v6 mapping contract.
Keep authorization and a trusted reaction `runs as` where the invoked command is gated.

Run `screenplay <root> --warnaserror` and `screenplay test <root>`; expect source
validation and admitted scenarios to pass separately. A Stage refusal remains a
separate result even when both Screenplay checks pass.
