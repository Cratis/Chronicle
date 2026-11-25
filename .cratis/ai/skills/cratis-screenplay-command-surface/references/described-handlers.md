<!-- cratis-ai-managed: skills/cratis-screenplay-command-surface/references/described-handlers.md -->
<!-- Copyright (c) Cratis. All rights reserved. -->
<!-- Licensed under the MIT license. See LICENSE file in the project root for full license information. -->

# Described command handlers

## Contents

- Body-decided events: `may produce`
- Result contract v1
- Execution and realization

Requires Screenplay 4.182.0 or later (`screenplay --version`); older compilers refuse pending handlers with PLAY0268.

Use a described command when payload computation needs words rather than an invented
expression. Prefer ordinary `produces` when the logic is declarative. A body cannot reject a
command. Fixed v1 productions cannot vary event count; use v2 possible events below for that.

Complete model:

```screenplay
domain Billing
module Invoices
  feature Fees
    slice StateChange ApplyLateFee
      command ApplyLateFee
        invoiceId String identifier
        daysOverdue Int
        validate
          daysOverdue >= 1 message "The invoice must be overdue"
        handler
          implementation
            hint "Calculate the late fee from overdue days using the agreed daily rate"
        produces LateFeeApplied
          for invoiceId
      event LateFeeApplied
        amount Decimal
      screen ApplyLateFee
        action ApplyLateFee for screen
```

A pending `implementation` has no `file` or fence. Bare `implementation` is valid;
optional `hint` lines are ordered, nonblank quoted strings, decoded without trimming.
One wrapper and at most one payload are permitted. Attaching a file or fence makes it
an attached handler. Beside unconditional `produces`, with a resolved, nonempty,
language-tagged body, it binds with the same v23 result contract (Screenplay 4.250.0,
decision 0117): the productions fix event types, count, order and destinations, and
the body owns property values. The standalone runner has no provider: reaching the body is
Unsupported (`ImplementationBody`) and appends nothing. A host may execute verified result-v1
or result-v2 bodies through `HandleAsync`; see
[host body execution](../../cratis-screenplay-specifications/references/host-body-execution.md).
An attached handler with neither unconditional
`produces` nor a `may produce` block, with `produces ... when`, or with an empty or
unresolved body fails binding with PLAY0800 (`CommandHandlerRequiresAttachment`): declare
unconditional `produces`, declare a `may produce` block, split variable outcomes into
separate commands, or attach a resolved body.

A resolved, nonempty, language-tagged handler can instead declare a `may produce` block:
result v2, ESM v35. The syntax requires Screenplay 4.256.0, the provider seam requires 4.257.0,
and source binding requires 5.4.0 (`screenplay --version`). These are separate gates, not
proof that a target executes the body. A pending `handler implementation` remains a described command.

## Body-decided events: `may produce`

Inside `handler`, the block lists permitted events and destinations. The body returns an ordered
subset or multiset, including an empty result; repetitions and returned order are meaningful.
A scalar `for batchId` uses that command property; `for one of parcelIds` requires an explicit
returned destination belonging to that command collection. Destinations must be required:
scalar properties may be generated only when they are the identifier; one-of collections cannot
be generated. Omitting `for` requires exactly one identifier. Declare each event identity once;
this does not prohibit repeated returned items. Invalid sets/destinations are PLAY0823.
The block cannot coexist with `produces` (PLAY0822/PLAY0823). An attached empty/unresolved body
still fails PLAY0800; with no body or implementation declaration at all, the set is invalid
(PLAY0823). Pending `implementation` is described intent, not execution permission.
Conditional `produces` is not an alternative. A handler cannot read views: put state-dependent
decisions in declarative `require`, not an invented read API.

````screenplay test=unbound
domain Dispatching

module Parcels
  feature Shipping
    slice StateChange Dispatch
      command DispatchParcels
        batchId String identifier
        parcelIds String[]
        dispatch Bool
        handler
          may produce
            BatchDispatched for batchId
            ParcelDispatched for one of parcelIds
          ```csharp
          if (Dispatch)
          {
              result.Add(new BatchDispatched(context.Occurred));
              foreach (var id in ParcelIds)
                  result.Add(id, new ParcelDispatched(context.Occurred));
          }
          ```

      event BatchDispatched
        dispatchedAt DateTime

      event ParcelDispatched
        dispatchedAt DateTime

      screen DispatchParcels
        action DispatchParcels for screen

      specification DispatchRequiresAHost
        when DispatchParcels
          batchId = "B1"
          parcelIds
            item "P1"
          dispatch = false
        then no events
````

All three models are source-valid; `test=unbound` pins the standalone verdict, not a passing
execution. The first binds but needs a provider. The next two fail executable binding with PLAY0800
(the focused spec checks the structured diagnostics). Conditional productions cannot replace
possible events:

````screenplay test=unbound
domain ConditionalDispatch

module Parcels
  feature Shipping
    slice StateChange Dispatch
      command DispatchParcels
        batchId String identifier
        enabled Bool
        produces when enabled == true
          BatchDispatched
            for batchId
        handler
          ```csharp
          return new BatchDispatched(context.Occurred);
          ```

      event BatchDispatched
        dispatchedAt DateTime

      screen DispatchParcels
        action DispatchParcels for screen

      specification ConditionalHandlerIsUnbound
        when DispatchParcels
          batchId = "B1"
          enabled = false
        then no events
````

An attached empty body does not establish an executable handler (PLAY0800):

````screenplay test=unbound
domain EmptyDispatchBody

module Parcels
  feature Shipping
    slice StateChange Dispatch
      command DispatchParcels
        batchId String identifier
        handler
          may produce
            BatchDispatched for batchId
          ```csharp
          ```

      event BatchDispatched
        dispatchedAt DateTime

      screen DispatchParcels
        action DispatchParcels for screen

      specification EmptyHandlerIsUnbound
        when DispatchParcels
          batchId = "B1"
        then no events
````

Requires Screenplay 4.250.0 (`screenplay --version`) for attached handlers beside `produces` to bind; 4.210.0 to 4.249.x refuse every attached handler with PLAY0800, and older binders use PLAY0268. Hosts switch on the code and read `arguments.subject`, never parse the message.

## Result contract v1

| Part | Owner |
| --- | --- |
| Events, count, order | Declarative; each unconditional `produces` means exactly one event |
| Destination, routes, allocation, tags and metadata | Declarative |
| Generated values and response | Declarative; generation precedes the body |
| Authorization and validation | Declarative; denial or refusal never reaches the body |
| Event payload values | Body; mappings are non-normative hints, required properties may be unmapped |

Mapping type checks still apply. Neither a renderer nor reference execution evaluates
these payload mappings. Conditional productions, repeated/occurrence productions,
body-decided cardinality and body rejection are outside this contract.

The command selects ESM v23 with `handler: { requirementId, resultVersion: 1 }`.
The requirements sidecar carries a Pending CommandHandler requirement with ordered
hints, context/result versions 1, and provisional/ambiguous flags. Consumers must
explicitly admit this version, never silently drop the body.

| Diagnostic | Meaning and action |
| --- | --- |
| PLAY0800 (error) | File/inline handler with neither unconditional `produces` nor a `may produce` block, or without a resolved, nonempty body; `subject` identifies the command |
| PLAY0695 (information) | Described command admitted; command and RequirementId name the missing body |
| PLAY0696 (error) | Pending v1 without productions, including response-only; add unconditional productions or declare v2 possible events |
| PLAY0697 (error) | Conditional productions; result v1 needs unconditional events |
| PLAY0698 (error) | Pending intent plus another handler; keep one handler |
| PLAY0492–0494 | Invalid wrapper, hint or payload composition; fix the source shape |

## Execution and realization

Without both a resolved body and a host provider, reference execution runs the gates, routing and
generation in ordinary order, then returns Unsupported (`ImplementationBody`) on reaching the body,
naming command and RequirementId. This also applies through an invoking reaction. No payload
mapping runs and no event is emitted. Denial and validation specifications still execute.
Unsupported specifications are counted separately; an all-Unsupported run is never green.
Host execution does not authorize invented context values or domain logic.

Stage admits v23 but refuses described handlers with STAGE-IMPL-002 in the renderer,
Host and specification executor, including Stub mode: it emits no stubs. Hand-write
the body against the declared contract as authorized gap-fill; do not claim that a
pending requirement is implementation evidence. Moving the body into `handler file`
keeps its RequirementId; beside unconditional `produces` it binds like any resolved
attached handler (above), while with neither `produces` nor a `may produce` block, with
`produces ... when`, or with an empty or unresolved body (even beside a `may produce` block)
it still fails PLAY0800. Stage renders a body resolved in C#, but
still refuses a pending body or one resolved in another language with STAGE-IMPL-002.

Validate the model root with `screenplay <root> --warnaserror`; expect no errors or
warnings and PLAY0695 information. Run `screenplay test <root>` separately: body-reaching
specifications on the standalone runner must be reported Unsupported, not passed. Target tests are separate.
