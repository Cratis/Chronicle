<!-- cratis-ai-managed: skills/cratis-screenplay-command-surface/references/described-handlers.md -->
<!-- Copyright (c) Cratis. All rights reserved. -->
<!-- Licensed under the MIT license. See LICENSE file in the project root for full license information. -->

# Described command handlers

Requires Screenplay 4.182.0 or later (`screenplay --version`); older compilers refuse pending handlers with PLAY0268.

Use a described command when payload computation needs words rather than an invented
expression. Prefer ordinary `produces` when the logic is declarative. Do not use a
described body to reject a command or decide how many events to emit.

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
```

A pending `implementation` has no `file` or fence. Bare `implementation` is valid;
optional `hint` lines are ordered, nonblank quoted strings, decoded without trimming.
One wrapper and at most one payload are permitted. Attaching a file or fence stops
being a described command: attached handlers fail binding with PLAY0800
(`CommandHandlerRequiresAttachment`) and cannot coexist with `produces`.
This permanent limitation requires a constrained implementation attachment:
gap-fill the slice in code, not by waiting for a supported ESM version.

Requires Screenplay 4.210.0 (`screenplay --version`) for PLAY0800 and its required typed `subject` declaration argument naming the command; older binders use PLAY0268. Hosts switch on the code and read `arguments.subject`, never parse the message.

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
| PLAY0800 (error) | File/inline handler requires a constrained implementation attachment; `subject` identifies the command to gap-fill |
| PLAY0695 (information) | Described command admitted; command and RequirementId name the missing body |
| PLAY0696 (error) | No productions, including response-only; add unconditional productions |
| PLAY0697 (error) | Conditional productions; result v1 needs unconditional events |
| PLAY0698 (error) | Pending intent plus another handler; keep one handler |
| PLAY0492–0494 | Invalid wrapper, hint or payload composition; fix the source shape |

## Execution and realization

Reference execution runs the gates, routing and generation in ordinary order, then
returns Unsupported (`ImplementationBody`) on reaching the body, naming command and
RequirementId. This also applies through an invoking reaction. No payload mapping runs
and no event is emitted. Denial and validation specifications still execute. Unsupported
specifications are counted separately; an all-Unsupported run is never green.

Stage admits v23 but refuses described handlers with STAGE-IMPL-002 in the renderer,
Host and specification executor, including Stub mode: it emits no stubs. Hand-write
the body against the declared contract as authorized gap-fill; do not claim that a
pending requirement is implementation evidence. Moving the body into `handler file`
keeps its RequirementId but does not admit that attached handler alongside `produces`.

Validate the model root with `screenplay <root> --warnaserror`; expect no errors or
warnings and PLAY0695 information. Run `screenplay test <root>` separately: body-reaching
specifications must be reported Unsupported, not passed. Target tests are separate.
