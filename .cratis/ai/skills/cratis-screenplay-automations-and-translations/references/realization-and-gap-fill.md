<!-- cratis-ai-managed: skills/cratis-screenplay-automations-and-translations/references/realization-and-gap-fill.md -->
# Realization and gap-fill

The `.play` revision is the contract; this file says what that means once code is needed. It is
the automation-and-translation addendum to `cratis-screenplay-render-and-gap-fill`; read that
skill for the admission, publication and ownership rules.

## Stage rendering versus gap-fill
CLI 3.57.0 / Stage 4.89.1 render supported event-triggered Automation slices as Chronicle
reactors. Translate/captures, non-event triggers and opaque bodies remain `STAGE-ESM-024`;
Claim-reading/opaque `runs as` gates (`-034`), legacy `from` (`-035`), mixed effects (`-037`), unsupported mappings (`-039`)
and protected destinations (`-040`) also remain refused. Command specs that cascade into
reactions remain `-011`; supported single-hop append specs now render, as do semantic
reaction `from` and role/authenticated system identities. Only refused scope is hand-written delivery with the model as
its contract. Source: Stage `v4.89.1:Documentation/reference/reaction-rendering.md:6-98`;
full admission: `cratis-stage-rendering-and-sandbox` `references/admission.md`. (History: cratis
3.28.2 and 3.28.3 refused such a model whole with `STAGE-ESM-016`, and 3.27.1 stopped earlier with PLAY0268.)

A reaction that both `produces` events and `invokes` commands stays one modeled reaction. Hand-write one
retry-safe reactor: the scaffold already includes Arc's mixed-result handler, but it appends
events before running commands, so a denied/rejected command fails the partition and
redelivery duplicates the events. `[OnceOnly]` prevents replay, not retries. Prove the
recovery behavior rather than assuming this handler supplies all-or-nothing settlement. Never split the reaction to dodge `STAGE-ESM-037`. A `Customizations/` file never makes a
rejected model renderable, and never claim a whole-application V5 from a subset. Choose the route
with `cratis-screenplay-render-and-gap-fill`.

## Where the pieces go (hand-written)
| Model element | Realization | Skill |
|---|---|---|
| reaction `when <Event>` with `invokes` or `produces` | Chronicle reactor returning commands or events | `cratis-chronicle-reactor` |
| trusted actor for `invokes` | `[ExecuteCommandsAsSystem("<role>")]` on the reactor plus a policy requiring the role (Arc, since v20.56.0; available in Stage 4.51.3's rendered Arc 22.50.5) | `cratis-chronicle-reactor` |
| clock trigger, application trigger | scheduler or host signal that raises the occurrence | `cratis-engineering-effect-boundaries` |
| call to an outside system | adapter behind an effect boundary, keyed by the idempotency key in the model | `cratis-engineering-effect-boundaries` |
| capture source (webhook, poll, topic) | adapter that authenticates, parses and appends | `cratis-engineering-effect-boundaries` |
| replay versus recovery redelivery | `[OnceOnly]` for replay; a receipt keyed by `ReactorDelivery.Id` for recovery | `cratis-chronicle-reactor` |

Stage 4.51.3 rendered apps pin Arc 22.50.5/Chronicle 19.32.0, so
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
