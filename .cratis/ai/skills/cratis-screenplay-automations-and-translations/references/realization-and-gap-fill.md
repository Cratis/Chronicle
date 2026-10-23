<!-- cratis-ai-managed: skills/cratis-screenplay-automations-and-translations/references/realization-and-gap-fill.md -->
# Realization and gap-fill

The `.play` revision is the contract; this file says what that means once code is needed. It is
the automation-and-translation addendum to `cratis-screenplay-render-and-gap-fill`; read that
skill for the admission, publication and ownership rules.

## Stage rendering versus gap-fill
The CLI and Stage render supported event-triggered Automation slices as Chronicle
reactors. Translate/captures, non-event triggers and opaque bodies remain `STAGE-ESM-024`;
Claim-reading/opaque `runs as` gates (`-034`), legacy `from` (`-035`), mixed effects (`-037`), unsupported mappings (`-039`)
and protected destinations (`-040`) also remain refused. Command specs that cascade into
reactions remain `-011`; supported single-hop append specs now render, as do semantic
reaction `from` and role/authenticated system identities. Only refused scope is hand-written delivery with the model as
its contract. Source: Stage `Documentation/reference/reaction-rendering.md`;
full admission: `cratis-stage-rendering-and-sandbox` `references/admission.md`.

A reaction that both `produces` events and `invokes` commands stays one modeled reaction. Hand-write one
retry-safe reactor: the scaffold already includes Arc's mixed-result handler, but it appends
events before running commands, so a denied/rejected command fails the partition and
redelivery duplicates the events. `[OnceOnly]` prevents replay, not retries. Prove the
recovery behavior rather than assuming this handler supplies all-or-nothing settlement. Never split the reaction to dodge `STAGE-ESM-037`. A `Customizations/` file never makes a
rejected model renderable, and never claim a whole-application V5 from a subset. Choose the route
with `cratis-screenplay-render-and-gap-fill`.

Application-root `reaction batches atomic` also blocks Stage delivery with
STAGE-ESM-043. Requires Stage 4.139.0 for v22 admission; older Stage reports -016. Preserve the
per-occurrence/selected-branch batch boundary and independent invoked commands.
Atomic appends do not prove redelivery deduplication or external exactly-once effects.

Requires Stage 4.141.1 for independent event-reaction specifications with `given clock`;
older executors refuse clocks beside reactions. This supplies `$context.occurred`
through cascades, invocation arguments and refusal branches, not a scheduler.
Clock/non-event triggers still refuse `STAGE-ESM-024`. Generated append/redelivery
clock fixtures retain -024 and context-dependent generated reaction specs retain -011.
Read [sandbox occurrence boundaries](../../cratis-stage-rendering-and-sandbox/references/sandbox-and-specrunner.md)
when verifying reaction time in Stage rather than reference execution.

## Where the pieces go (hand-written)
| Model element | Realization | Skill |
|---|---|---|
| reaction `when <Event>` with `invokes` or `produces` | Chronicle reactor returning commands or events | `cratis-chronicle-reactor` |
| trusted actor for `invokes` | `[ExecuteCommandsAsSystem("<role>")]` on the reactor plus a policy requiring the role (Arc; see the Requires line below) | `cratis-chronicle-reactor` |
| clock trigger, application trigger | scheduler or host signal that raises the occurrence | `cratis-engineering-effect-boundaries` |
| call to an outside system | adapter behind an effect boundary, keyed by the idempotency key in the model | `cratis-engineering-effect-boundaries` |
| capture source (webhook, poll, topic) | adapter that authenticates (the provider's signature or shared secret, never an application user token, because the provider cannot obtain one), parses and appends, keeping the provider's retry identity | `cratis-engineering-effect-boundaries` |
| replay versus recovery redelivery | `[OnceOnly]` for replay; a receipt keyed by `ReactorDelivery.Id` for recovery | `cratis-chronicle-reactor` |

Rendered apps pin their own Arc and Chronicle releases, so
`[ProtectedDecision]` and `DecisionRead<T>` are available to gap-fill, not
Stage-emitted enforcement. Admit the projection/key and enroll the read in the
active unit of work; never edit managed `Directory.Packages.props`.

## Faithful realization
A realization may choose the reactor, scheduler or queue mechanism. It may not: drop pending
visibility, add filters the model does not state, guess a field, or weaken authorization. Specify
the event-to-command mapping at the reaction boundary (a command-only spec does not exercise it).
An unsupported realization is a gap reported to delivery, never a silent change to the model.
After writing it, check it element by element against the slice with
`cratis-application-slice-conformance`.
