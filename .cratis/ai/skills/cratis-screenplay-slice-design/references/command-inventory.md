<!-- cratis-ai-managed: skills/cratis-screenplay-slice-design/references/command-inventory.md -->
# Command design and the per-command refusal inventory

## Contents

- Command card
- Values a rule reads
- The inventory
- Filled for `worked-example.md`
- Common mistakes
- Interview batch for slice design

For every command, record what it needs, where each input comes from, how it can succeed and
every way it can be refused. The inventory is the design output that
`cratis-screenplay-scenario-coverage` consumes: each refusal becomes a specification there,
and each persona that must be denied becomes a `given caller` fixture. Build it from the
`.play` files, not from memory.

## Command card

Fill one card per command before (or while) writing the slice. Keep it in the phase report;
durable rationale goes in fenced Markdown slice `documentation`.

```text
Command:        <ImperativeBusinessName>  (one decision)
Origins:        <screen action / form / reaction invokes (event, clock or application trigger)>,
                persona, gate chain; classify each as [USER] (a person on a screen) or [AUTO]
                (a reaction; a capture only appends facts, a later command is reached through
                a reaction)
Inputs:         <property: Concept - who supplies it - why the caller decides it>
Identifier:     <property naming the instance, or "allocation" with the reason>
Reads:          <per rule: each value it reads, as input | state | context>
Rules by layer: <concept validate | command validate/require | authorize | claim policy
                 | constraint | stored-state (NOT enforced, target named)>
Success:        <event(s) with for <identifier>, payload mapping>
Refusals:       <see the inventory table>
```

Questions that fill it (ask them of the domain expert, or assume visibly when unattended):

1. Who or what makes this happen: a person on a screen, a reaction, an outside system? Is it
   always the same persona? (Several origins are fine; each must be reachable and authorized.)
2. What does the caller decide or supply, and what do they merely see? Anything they only see
   is a read-model value, not an input; anything that is who they are comes from context.
3. Does one input only matter for certain values of another? That is a granularity question:
   two decisions are two commands; one decision with a dependent rule is an implication
   (`require a == false or b > c`). Never leave a conditional input implicit.
4. What must be true for this to be allowed, and where does each condition live? Walk the
   layers in `rule-layers.md` from the lowest up.
5. What can go wrong that the business names? Each distinct failure is a row below, with its
   message in the user's words.
6. What new fact becomes true on success, and who reacts to it? If nobody can say, the event
   may be a form in disguise (`slicing.md` *Generic edits*).

Group the commands of a slice set by origin: user-issued (each attributed to a named persona,
never a generic "user") and automation-issued (attributed to the reaction or integration, with
the trusted actor behind it). A command with no screen is started by a clock or application
trigger (a reaction trigger), or by a reaction over any modeled event: an ordinary local event,
an imported event or a translated event. A capture starts a process by appending facts and
issues no command; any later command is reached through a reaction. Design
these in `cratis-screenplay-automations-and-translations`. Every automation-issued command states
what happens when it fails or is retried.

## Values a rule reads

Before placing a rule in a layer, list every value it reads and classify each one:

- **input**: a command property the caller decides.
- **state**: a `reads <View>` path, or the prior facts of the stream.
- **context**: the time the decision occurs, the caller, the tenant.

Decision state is the part of *state* a rule actually branches on. Derive it from the
specifications, not from the event's shape: a "given no prior activity" and "given already
{X}" pair means one flag for {X} (a presence check is a flag or a nullable instance); a
scenario that discriminates on a value means that value. A `reads` view carries exactly the facts the command's specifications branch on, one field
per fact. Add nothing because an event happens to carry it.

A value that fits none of the three is a gap: a missing input, a missing view or a hidden
dependency. Do not close it with prose in `description` or `handler`.

Context is never a command property. The pipeline supplies who is calling and when, because a
caller-supplied "who am I" or "what time is it" cannot be trusted, and a field the caller
fills is still an input. A date the person chooses (a start day, a requested slot) is an
input; "now" is context. A declarative `require` reads only command properties and read state,
so a rule over context is a named `rule` with a code body (its `RuleContext` carries
`Occurred`, `CausedBy` and `Tenant`), or an `authorize` policy when it concerns roles or
claims. The reference runner cannot execute an opaque body: record that, do not delete the
rule. When a rule depends on a local calendar day ("before midnight", "a business day"), state
whose timezone applies and where it comes from (an input, a setting read through a view, or an
open question).

Give each context-reading rule its own inventory row, and have its specification state the
context it relies on: `given clock` for time, `given caller` or `given caller as <Persona>`
for the actor. A time rule without `given clock` is not pinned.

~~~screenplay test=unbound
domain Bikeshare.Rentals

concept ReservationId : Uuid
concept BikeId : Uuid

module Rentals
  feature Reservations
    slice StateChange ReserveBike
      command ReserveBike
        reservationId ReservationId identifier
        bikeId BikeId
        startTime DateTime
        validate
          startTime rule StartsAfterRequest message "The start time has already passed"
            ```csharp
            return (DateTimeOffset)context.Value > context.Occurred;
            ```
        produces event BikeReserved
          bikeId BikeId = bikeId
          startTime DateTime = startTime
          reservedAt DateTime = $context.occurred

      specification RejectingAStartTimeInThePast
        given clock "2026-06-01T09:00:00Z"
        when ReserveBike
          reservationId = "6b2a5f0e-6d1c-4f4e-9f57-3a9a6d3c8b10"
          bikeId = "0d7c3a34-5a43-4c1a-8f7e-9a1c2b3d4e5f"
          startTime = "2026-06-01T08:00:00Z"
        then error "The start time has already passed"
~~~

## The inventory

One row per outcome. The layer says where the refusal is enforced; the spec column says
which specification pins it (or `gap`, handed to `cratis-screenplay-scenario-coverage`).

| Command | Outcome | Layer | Message / denied persona | Spec |
|---|---|---|---|---|
| a command | success event, or a refusal | `authorize`, concept `validate`, command `validate`/`require`, `constraint`, stored-state (NOT enforced), claim policy | exact message, or the caller fixture that must be denied | name, or `gap` |

Rules for filling it:

- Every command has a success row and at least one refusal row, or a stated reason for none.
- Every gated command and query has its own `then denied` row, even when the gate is
  inherited from the module or feature (an ownership gate needs a second caller with a
  different claim value).
- Every persona `Cannot` line resolves to a row with a gate and a denied spec, or is recorded
  as a gap. Persona description text is intent only.
- A refusal that depends on stored state is a row marked **NOT enforced in the model today**
  with its target (Arc `[ProtectedDecision]` + `DecisionRead<T>` where Arc 22.39.0 or later is
  available, including Stage 4.73.1's rendered package set for gap-fill, not emitted
  protection; Chronicle DCB, or a
  constraint). It still gets a spec row, because the intended behaviour is the contract.
- Every rule row names what the rule reads (input, state or context); a context read needs a
  `given clock` or `given caller` in the spec that pins it.
- A decided refusal never goes to open questions: it goes here, then to scenarios.
- A refusal message is a fact of the design; keep the wording identical in the rule, the
  inventory and the spec.

## Filled for `worked-example.md`

| Command | Outcome | Layer | Message / denied persona | Spec |
|---|---|---|---|---|
| InstallLocker | LockerInstalled for the locker id | | | InstallingALocker |
| InstallLocker | caller is not an attendant | `authorize IsAttendant` | denied: Customer (authenticated, role "Customer") | RefusingACustomerInstallingALocker |
| InstallLocker | caller is anonymous | module `authorize IsAuthenticated` | denied: no caller | gap (inherited gate; the Customer row proves the command is gated) |
| InstallLocker | locker number empty | concept `LockerNumber` | "A locker needs a number" | gap |
| InstallLocker | volume below one litre | concept `Litres` | "A locker must hold at least one litre" | gap (one spec per concept, through one command) |
| InstallLocker | number already used by another locker | constraint `UniqueLockerNumber` | "That locker number is already installed" | gap |
| InstallLocker | the same locker installed twice | constraint `LockerInstalledOnce` | "The locker is already installed" | gap |
| RequestLocker | LockerRequested for the request id | | | RequestingALocker |
| RequestLocker | preferred volume below one litre | concept `Litres` | "A locker must hold at least one litre" | covered by the shared concept rejection spec through InstallLocker (one spec per concept) |
| RequestLocker | caller is anonymous | module `authorize IsAuthenticated` | denied: no caller | RefusingAnAnonymousRequest |
| AssignLocker | LockerAssigned for the locker id | | | AssigningALocker |
| AssignLocker | caller is anonymous | module `authorize IsAuthenticated` | denied: no caller | gap (inherited gate; the Customer row proves the command is gated) |
| AssignLocker | caller is not an attendant | `authorize IsAttendant` | denied: Customer | RefusingACustomerAssigningALocker |
| AssignLocker | locker already has a request | constraint `OneRequestPerLocker` | "The locker is already assigned" | AppendingASecondAssignmentOfALocker (`when append`: the command's own `require` would reject first) |
| AssignLocker | request already has a locker | constraint `OneLockerPerRequest` | "The request already has a locker" | gap |
| AssignLocker | locker is not free | `reads` + `require`, NOT enforced today | "The locker is not free"; target: `[ProtectedDecision]` (Arc 22.39.0+; available to Stage 4.73.1 gap-fill, not emitted protection) or DCB on the locker stream | RejectingASecondAssignmentOfALocker (pins the message; blocked until the target exists) |
| AssignLocker | request is not waiting | `reads` + `require`, NOT enforced today | "The request is not waiting for a locker" | gap (same) |
| LockerById, ListLockers | caller is anonymous | module `authorize IsAuthenticated` | denied: no caller | RefusingAnAnonymousLockerLookup, RefusingAnAnonymousLockerList |
| RequestById | caller is anonymous | module `authorize IsAuthenticated` | denied: no caller | RefusingAnAnonymousRequestLookup |

The gaps are the hand-off: most are cheap rejection specs, and two are blocked on a target the
model cannot express yet. A model whose inventory shows no gaps at design time has probably
not been asked the refusal questions.

## Common mistakes

- Naming the command for the form (`UpdateLocker`) instead of the decision.
- A boolean input that attests a rule (`confirmsLockerIsFree == true`) instead of a modeled
  value or a recorded stored-state rule.
- Copying state into the command so `require` can read it, instead of `reads <View>`.
- An origin that is neither reachable (no screen, form or reaction leads there) nor authorized
  for the persona who should act.
- A reaction invoking a gated command without `runs as system role "<Role>"`: the invocation has
  no caller and is refused; declare the trusted path with the role the gate admits
  (`cratis-screenplay-automations-and-translations`), never delete the gate.
- A time- or actor-dependent rule written as if it only read inputs: its specification passes
  for the wrong reason, or cannot be reproduced.
- Refusals recorded as prose in the `description` with no layer and no spec row.
- Personal data or secrets as command input: values are by default recorded in the causation chain, so treat them as permanent
  (retention can be configured in newer Chronicle; verify it, never audit secrets).
  Mark concepts bare `pii` for personal data or `secret` for operational secrets,
  never on the identifier; declare use/basis/retention in a purpose, not a reason.

## Interview batch for slice design

1. **Origin of each action.** Is it (A) user-initiated only, (B) automated, (C) a mix, or
   started by a clock or outside system (schedule, webhook, feed)? Impact: separates screen
   commands from automation (and a trusted path); clocks and outside records are designed in
   `cratis-screenplay-automations-and-translations` (trigger, capture, reaction over imports).
   Follow-up: if a mix, which user actions start automation, and what does it decide on its
   own? If outside systems, which one and what data does it send?
2. **Critical fields.** Which data matters most on each screen, and what decision does the
   person make from each field? Impact: fields with no decision behind them are questions, not
   requirements. Follow-up: for a field nobody can justify, ask whether it is history worth
   keeping or can go.
3. **Freshness and cost.** How stale may each view be, and what does a stale answer cost?
   Impact: picks projection versus reducer, and one-shot versus `observable`. Follow-up: if a
   wrong answer is expensive (a booking, a payment), that decision needs a protected read
   (`references/rule-layers.md`), not a view. For every calculated or aggregated figure
   (a total, a count, an average) ask whether it recalculates as source data changes ("so it
   belongs in a view - does that match your expectation?") or whether someone agrees or
   certifies a value (a fact, step 5).
4. **Granularity.** When someone changes these values, why? Impact: separates decisions from
   forms. Follow-up: when a screen edits several values, ask for each who changes it and why.
5. **Existing UI or constraints.** Is there a screen or system to match? Impact: origins and
   screen data. Follow-up: ask to see it; styling belongs to `cratis-screenplay-ui-composition`.
