<!-- cratis-ai-managed: skills/cratis-chronicle-reactor/references/slice-delivery-checklist.md -->
# Automation slice delivery checklist

## Contents

- Start from the contract
- Build order
- Handler patterns
- Redelivery against a refusing command
- What no longer applies
- Checklist
- Specifications

Use this when you implement a reactor from an existing contract: an accepted `.play`
`Automation` slice (a `reaction` with its `when`, `where`, `invokes` or `produces`), or an agreed
slice outline in a code-first repository. It is the delivery checklist for the code, and it adds
to the Verify list in `SKILL.md`; it does not replace it. Adapted from Martin Dilger and Nebulit
GmbH's build-automation (see `provenance.md`), with the event-store, processor and storage
mechanics replaced by Chronicle and Arc.

## Start from the contract
Read the slice first. It is the source of truth for three things and nothing else is assumed:
- which event (or clock or trigger) sets the automation off;
- which command it invokes, or which events it produces, and from which trigger fields each
  input is filled;
- which conditions narrow it (`where`).

If an item the reactor needs is not in the contract, it is not in the code. Ask or record an open
question; do not guess a mapping or add a filter. Notes in the slice `description` and
`documentation` are implementation hints: carry them into code comments, validation or
specifications where they add value, and do not treat them as new requirements.

## Build order
This checklist covers event-triggered reactions only. Clock and application triggers are not
`[EventType]`s and do not run through `IReactor` (Chronicle 19.50.0 discovers handlers only for
registered event types). Route them first to a scheduler or host-signal implementation as
`cratis-screenplay-render-and-gap-fill/references/gap-fill-handoff.md` describes, then apply the
remaining steps where they fit.

An automation is a state-change with a trigger in front of it. Build the command side first, then
wire the reactor.
1. The command (and its event) exists and works: `cratis-arc-command`, and its specifications
   (`cratis-application-slice-specifications`). If the automation produces events directly, the
   `[EventType]` records exist instead.
2. The trigger event is defined as an `[EventType]` in the contracts project that owns it. A
   trigger from another module or store is consumed through its public contract, never copied
   (the cross-service rules in `SKILL.md`).
3. Write the reactor: one class implementing `IReactor`, one handler per trigger event, no
   state. It is discovered by the interface; there is no registration step to add.
4. Add the specifications (below) and run the slice's gate.

The reactor has no route of its own. The invoked model-bound `[Command]` keeps its generated Arc
HTTP endpoint, so if only the automation should run it, gate it with `[Roles]` or `[Authorize]` and
give the reactor `[ExecuteCommandsAsSystem]` naming that role.

## Handler patterns
| Contract says | Handler |
|---|---|
| One trigger event, one command | One handler whose first parameter is the trigger event, returning the command built from the trigger's fields. Add `[ExecuteCommandsAsSystem]` only when the command is gated, and `[OnceOnly]` |
| One trigger event, several commands | Return the commands as a collection: Arc runs them in order, with no transaction across them, and a retry can repeat them (see "Invoking a command from a reactor") |
| One trigger, events and commands | Return one stable collection mixing registered events and commands (Arc 22.72.0 or later): the events are appended first as one batch, then the commands run in order, with no transaction across them. Add `[OnceOnly]` where a replay must not repeat it. The Stage scaffold pins an older Arc (see `cratis-stage-rendering-and-sandbox` `references/rendered-application.md`), so this needs a proven dependency override; never split the modeled reaction |
| One trigger event, events produced directly | Return the event or events; `[OnceOnly]` applies (`CHR0022`) |
| A condition narrows the trigger | Realize exactly the stated `where`. Show it with one specification where it skips and one for the neighboring case where it acts |
| A fact on another event source | `EventForEventSourceId` with the stated target id |
| The trigger's fields are not enough | Inject the read model the contract names; do not widen the mapping |
| The invoked command refuses an exact repeat | Apply the contract's redelivery disposition to that refusal only; see below |

Which return shape expresses "do nothing" for a given handler is established by a scenario that
runs it, not assumed. Use only the return shapes listed in `SKILL.md`.

## Redelivery against a refusing command
Recovery can redeliver the trigger even with `[OnceOnly]`. If the invoked command's own
"already done" rule refuses the repeat, returning that command without a refusal disposition
fails the partition. Decide from the contract, in this order:

1. A redelivery specification says the repeat is harmless (`then no events`). If the command's
   own contract says an exact repeat succeeds without new facts, realize that no-op in the
   command. Otherwise execute through the imperative pipeline (`cratis-arc-command-execution`)
   and acknowledge only the exact already-done validation result or named constraint authorized
   by the contract. Identify it from the command's rule and result, never by guessing an exception
   type or treating every validation failure as a duplicate. Preserve the modeled system identity
   when using the pipeline; a returned command's role handling is not implicit in an imperative call.
2. A refusal branch states the disposition: realize its exact selector and its acknowledgment
   or produced fact. Screenplay refusal branches reference-execute as v19 (Requires Screenplay 4.160.0);
   render admission is a separate question per target, and hand-written delivery keeps the model as the contract.
3. The redelivery specification expects an error, or no redelivery disposition is stated: do not
   acknowledge on your own, because that changes failure behavior. Report the possibility of a
   paused partition on recovery as an edit request to the model owner.

Every other refusal, denial or exception still fails the handler. Never catch broadly: it hides
mapping bugs and real validation failures. Authorization denial needs an explicit authorization
selector; concurrency conflicts are not refusals and must fail for retry.

For fan-out where the contract requires independent item progress, execute each item through the
pipeline and apply the disposition at that call, not around the loop. A returned collection stops
at the first failure, and even an acknowledged refusal can end that collection's execution; it
cannot prove later items ran. Unexpected failures still fail the handler, so completed items need
retry-safe behavior too.

## What no longer applies
Processor identity strings, consumer instance ids, lock and retry configuration, a dead-letter
store, `schema.migrate()` and a hand-written start and stop call belong to a different runtime.
Chronicle owns delivery, retry, partition failure and quarantine; see "Failure behavior". Do not
write a dead-letter table or catch-and-continue around the handler: a swallowed failure is a lost
fact, not handled (`references/domain-failures.md`).

## Checklist
- [ ] The slice contract was read first and every trigger, condition, command and field came from it
- [ ] The trigger event is an `[EventType]` from the owning contracts project
- [ ] The command (or events) it fires exists and has its own specifications
- [ ] One handler per trigger event; first parameter is the trigger; no invented `ReactorSideEffect` wrapper, no injected `IEventLog`
- [ ] Every command or event field is mapped from the trigger, an injected read model or a stated mapping; none invented or defaulted
- [ ] Every skip condition is the contract's, with a specification on each side of it
- [ ] Handlers that return events or commands carry `[OnceOnly]`; the effect is idempotent regardless
- [ ] A gated command has `[ExecuteCommandsAsSystem]` naming the role the command requires
- [ ] Repeated delivery of the same trigger is specified (what happens when it fires twice)
- [ ] If the invoked command refuses a repeat, its disposition comes from the contract and is
  realized for exactly that rule; nothing is caught broadly
- [ ] For a pending-work read model, the successful effect records the event that removes the
  selected item, directly or through the invoked command; success leaves no completed work pending
- [ ] A modeled slice matches its `.play` contract; the model was not edited to fit the code
- [ ] The reactor has no route; a command meant only for the automation is gated and the reactor runs it as that role; no processor or lock configuration, no dead-letter store
- [ ] The build has no `CHR0004`, `CHR0005`, `CHR0008`, `CHR0013`, `CHR0022`, `CHR0031` or `CHR0032` outstanding and the specifications pass

## Specifications
The trigger-to-command field mapping is part of the contract, so give it its own specification (a
direct call asserting the returned command, or a `ReactorScenario` where wiring matters). The
command's own `CommandScenario` covers the command side separately; neither replaces the other.
Details: `references/specifications.md`.
Where a pending-work view drives the reaction, its read-model specification seeds the trigger
and then the completion event and asserts that the item is absent. Link it from the reaction's
specification map: command success alone cannot show the queue will stop selecting that item.
