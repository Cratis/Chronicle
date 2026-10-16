<!-- cratis-ai-managed: skills/cratis-chronicle-event-constraints/references/closing-streams.md -->
<!-- Copyright (c) Cratis. All rights reserved. -->
<!-- Licensed under the MIT license. See LICENSE file in the project root for full license information. -->

# Closing scope and recovery

Source: `Cratis/Chronicle`:
`Source/Clients/DotNET/EventSequences/{IEventSequence,ClosedStreamScope,CompleteStreamError}.cs`,
`Events/Constraints/ClosesStreamAttribute.cs`,
`Documentation/closing-streams/` and `Documentation/scenarios/close-the-books.mdx`.

## Choose the scope before closing

`ClosedStreamScope` has optional `EventSourceId`, `EventSourceType`,
`EventStreamType`, `EventStreamId`. Unset dimensions are wildcards. All set
values must match for an append to be refused.

- Source id alone closes **all** its streams.
- Stream type/id alone closes that pair for **all** sources.
- Source id plus type/id closes just that source's stream.
- Source type alone closes every source of that type.

`ForEventSource(id)` and `ForStream(type, id)` are convenience factories.
`All` / `Default` are real stream values, not unset dimensions. Closing the
catch-all default without a specific source or other specific dimension returns
`DefaultStreamCannotBeCompleted`. Empty scope returns `EmptyScope`.

Pass `expectedTailSequenceNumber` when a manual close must refuse if another
event landed after the last fact you read. Mismatch returns `ExpectedTailMismatch`.
Manual coverage returns `AlreadyCompleted`, before the expected-tail check:
retrying does not duplicate the closure. Coverage by an event-owned closure
alone still writes a manual closure; a later reopening event cannot undo it.
Use a kernel that supports scoped closing; an older server must not be allowed
to ignore dimensions and close a wider scope.

## Close the books with a fact

For a completed accounting period, carry the period identifier in the closing
fact and bind it with `ClosesStreamAttribute.EventStreamIdFrom`. Explicitly
choose `Dimensions`: decide whether the closure is one account's period or all
accounts' period. Route normal appends to that same period stream. The closing
event is accepted; later matching appends get `StreamClosed`. Continue new work
in the next period, rather than blindly retrying into the closed one.

`ReopenedBy` names domain events that remove **that constraint's exact closure**.
They never remove manual closures. Closing/reopening run in the append's grain
turn, not crash-atomically with durable append. Constraint reindex repairs
missing event-owned closures and never changes manual ones. Registration or
removal of a declaration does not automatically reindex historical facts.
Redacted property-sourced closing/reopening facts can leave the owning index
untouched because their scope is unrecoverable; inspect the warning rather than
assuming a complete rebuild. Read the appended generation when evolving a
property-sourced scope.

## Inspect and repair

`IsStreamCompleted(scope)` checks coverage; `GetClosedStreams(within)` lists
matching persisted scopes. Workbench's namespace **Closed streams** view shows
source/type/stream, origin, closing owner, sequence number and time; it is
read-only, not a reopen button.

Kernel `ReopenStreamScope` is an administrator repair path for a mistaken manual
close, not a normal business transition. Name exact scope, reason and target;
get authorization, re-read, then act. It appends `StreamScopeReopened` before
removing the manual closure; failed audit append leaves it closed. It refuses
an event-owned closure (`ClosedByEvent`); use the modeled reopening event there.

## Specify both sides

Use `EventScenario` for completing a scope and asserting a later append's
`StreamClosed` constraint violation. Include an unrelated source/period still
open, expected-tail mismatch, repeated completion and, where declared, reopening.
Keep integration proof for client/kernel compatibility and recovery. Closing
preserves history: observers and projection replay still consume prior events.
