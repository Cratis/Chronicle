<!-- cratis-ai-managed: skills/cratis-screenplay-specifications/references/child-ordering.md -->
<!-- Copyright (c) Cratis. All rights reserved. -->
<!-- Licensed under the MIT license. See LICENSE file in the project root for full license information. -->

# Child-before-parent specifications

## Contents

- Product sources
- Complete model
- Known routing difference

Requires Screenplay 4.149.1 or later (`screenplay --version`) for child-before-parent arrival order in the reference evaluator.

Views combining sources must tolerate any arrival order. The reference evaluator
retains a parentless child and retries it in arrival order using its original event
values, event-source context and parent key. Resolving a child can release a
pending grandchild in the same pass. Pending children survive given/when and
accepted command commits. A parent that never arrives leaves the child absent;
an ambiguous parent still fails closed.

## Product sources

Cratis/Screenplay:
- `Source/DotNET/Screenplay/Semantics/Execution/SemanticScopedProjection.Deferred.cs`
  retains and resolves child occurrences; it applies original mappings.
- `Source/DotNET/Screenplay/Semantics/Execution/for_SemanticEvaluator/when_projecting_scoped_projections/with_deferral_across_given_and_when.cs`
  is the compiling, passing source for the model below. The extra specifications
  cover both given orders and a child whose parent never arrives.
- `Documentation/screenplay/projections/semantic-model.md` and
  `decisions/0007-affected-read-model-instances.md` retain the routing boundary.

## Complete model

The event payload's `orderId` is the parent key; the source identity is not used
by these handlers. Pin absence through the keyed query: no result means neither
parent nor child has been materialized. This is not a view rejection.

```screenplay
concept Id : Uuid
type Line
  lineNumber Int
  amount Int
module Orders
  feature Ordering
    slice StateView Lookup
      event LineAdded
        orderId Id
        lineNumber Int
        amount Int
      event OrderAdded
        orderId Id
      readmodel OrderView
        orderId Id
        lines Line[]
      query OrderById => OrderView optional
        by orderId Id
      projection Orders => OrderView
        from OrderAdded key orderId
        children lines identified by lineNumber
          from LineAdded key lineNumber
            parent orderId
            amount = amount
      specification ChildBeforeParent
        given LineAdded
          orderId = "00000000-0000-0000-0000-000000000001"
          lineNumber = 1
          amount = 10
        when append OrderAdded
          orderId = "00000000-0000-0000-0000-000000000001"
        then readmodel OrderView
          orderId = "00000000-0000-0000-0000-000000000001"
          lines = [{"lineNumber":1,"amount":10}]
      specification ChildBeforeGivenParent
        given LineAdded
          orderId = "00000000-0000-0000-0000-000000000001"
          lineNumber = 1
          amount = 10
        given OrderAdded
          orderId = "00000000-0000-0000-0000-000000000001"
        then readmodel OrderView
          orderId = "00000000-0000-0000-0000-000000000001"
          lines = [{"lineNumber":1,"amount":10}]
      specification ParentBeforeChild
        given OrderAdded
          orderId = "00000000-0000-0000-0000-000000000001"
        when append LineAdded
          orderId = "00000000-0000-0000-0000-000000000001"
          lineNumber = 1
          amount = 10
        then readmodel OrderView
          orderId = "00000000-0000-0000-0000-000000000001"
          lines = [{"lineNumber":1,"amount":10}]
      specification ParentNeverArrives
        given LineAdded
          orderId = "00000000-0000-0000-0000-000000000001"
          lineNumber = 1
          amount = 10
        then query OrderById
          arguments
            orderId = "00000000-0000-0000-0000-000000000001"
```

With the fenced model saved as `child-ordering.play`, run
`screenplay test child-ordering.play --format json` on the standalone pin;
each selected specification must pass. The corpus gate runs this complete fence.
A CLI-bundled compiler that predates child deferral does not run it; a newer
standalone result does not upgrade that bundle or prove rendered tests.

## Known routing difference

For a flat root/child shared event type, the evaluator uses the root handler's
resolved key, matching Chronicle's current-event fallback. It does **not**
implement `FindParentEvent`'s earlier-child-creation branch: a shared event updating
a child created by a different event type can route through that child's existing
location in Chronicle, while the evaluator uses the current parent handler's key.
Do not use this self-referential shared-event shape as a runtime conformance oracle.
Reference specifications are not Chronicle replay, concurrent execution or a
proof of real-kernel futures.
