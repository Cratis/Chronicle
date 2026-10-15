<!-- cratis-ai-managed: skills/cratis-screenplay-streams-and-consistency/references/boundaries.md -->
# Boundaries: modules, teams and contracts

## Contents

- Establish the boundary, not an organizational formula
- Ownership by design (optional lens)
- Ownership interview
- Ownership and interface inventory
- Cross-boundary walkthrough
- Internal and public events
- Crossing a boundary in Screenplay
- Change data capture is not a domain event
- Check coupling rather than banning every cycle
- Boundary checklist
- Review questions

Read when you split, merge or connect modules, or when a fact crosses between owners. Grammar
for reactions and translations lives in `cratis-screenplay-automations-and-translations`.

## Establish the boundary, not an organizational formula
A `module` groups business behaviour; it does not automatically define a team, deployment, event
source or transaction. Evidence: differences in language, decision authority, lifecycle, change
cadence and consistency needs (tariffs change weekly, member records yearly). One team may own
several modules; a module may be shared during a transition; record which of team and system the
model follows. Organise by business responsibility, not layer: a name like "Notifications" proves
nothing either way, so investigate the responsibility behind it.

Do not reorganise teams by fiat. When answers are unavailable, label assumptions and leave the
decision visible in the session `STATE.md`; never report independence as established. Before
splitting a boundary revisit every invariant that needs information from both sides: an event
exchange does not make that decision atomic.

## Ownership by design (optional lens)
Conway's observation is that systems come to mirror the teams that build them. Use it as a
deliberate lens when you shape boundaries, not as a law: decide the ownership you want and let the
boundaries follow it, instead of discovering later that the org chart set them.

- When one team will own a boundary end to end, treat the module as that team's system: it owns
  its commands, the events it produces, the read models it maintains and the systems it calls.
  Other teams interact only through its public events and its capture contracts, so each side can
  be built, scaled and released on its own, coordinating through events rather than meetings.
- A system this process integrates with is, by definition, owned by someone else. Record that
  owner next to the public fact or capture contract; it is the person you ask before changing it.
- Treat "team = system" as a hypothesis to test with the interview answers, not as a default. One
  team may own several modules, a module may be shared during a transition, and a team boundary
  does not make a decision atomic across it (see above). Write down which the model follows.

**Indirect producers.** When a fact reaches a receiver through an intermediate boundary (the
booking system's `BerthBooked` drives billing, whose `StayInvoiceDue` then reaches the harbour
office), attribute each public event to the boundary that decides it, and name the original
producer in the consumer's description as the indirect source. Ownership of the public contract
stays with the boundary that decides the fact; the indirect source tells a reader whom a change
upstream will surprise.

## Ownership interview
Run it when ownership, autonomy or integration decisions are missing; skip it when the user or
the model already states all three. Ask one question at a time, offer the options, and follow
the trigger. Unattended: assume the most conservative option, mark it `ASSUMED` in the slice or
module `description`, give the reason in its `documentation`, and add it to `STATE.md`.

| # | Question (options) | Decides | Follow-up trigger |
|---|---|---|---|
| 1 | Who decides these rules, and who operates them when they fail? (A one team owns everything; B teams by business domain; C teams by technical function) | how many modules and who owns each | B: what does each team own and decide? C: which business responsibility does each function really serve? Offer organising by business capability; do not prescribe a reorganisation |
| 2 | How much can each side move alone? (A very high: separate releases and outages tolerated; B moderate: coordinate through published facts; C low: coupling accepted) | contract strictness; sync versus async | A: name the contract and the tolerated delay. C: ask why the coupling is needed; it may mean one module |
| 3 | Which external systems take part? (payment, access control, accounting, none) | translation edges and adapter owners | for each: who owns the adapter, its failures and its contract changes? |
| 4 | What delay, outage or temporary disagreement can the business tolerate? | `eventual by choice` rows in the invariant table | what is the compensating fact and who sees the disagreement? |

Record the answers as: team or owner, what they decide, autonomy (high, moderate, low), external
dependencies with owners, and the facts that cross between owners (`A -> B via <Event>`).

## Ownership and interface inventory
For each affected boundary, identify:
- responsibility, lifecycle and decision owner;
- stream identities and the rules decided on them;
- commands handled and their permitted human or system origins (the owner of a command differs
  from the actors allowed to invoke it; preserve authorization);
- facts produced, contracts consumed, views provided or read;
- processors: trigger, local decision or effect, resulting command or fact, operational owner;
- external dependencies, adapter owners and known coupling.

Use declaration addresses, not copies of payloads. Per slice, also record the events it consumes
and which slice produces them, for orientation, not as a build order. A command or reaction may
consult another slice's view (`reads`; unprotected in `.play`, see `consistency-and-concurrency.md`
section 3); a slice calls another slice's command only through `invokes`.

### Responsibility matrix and ownership timeline
One compact matrix per feature shows who owns what; the timeline shows where a fact crosses.
Neither is a model element, and display order is not a global event order or a deployment
topology. Do not create a module per team name just to draw it.

```text
Boundary        Commands handled        Facts produced         Views maintained      Depends on
Harbour Office  BookBerth, CancelStay   BerthBooked (public)   BerthAvailability     none
Billing         IssueStayInvoice        StayInvoiceDue         OutstandingInvoices   BerthBooked

Time ->   BerthBooked        StayInvoiceDue       StayPaymentRefused
Harbour   [BerthBooked]
Billing                      [StayInvoiceDue]     [StayPaymentRefused]
```
Check: each fact has one producing boundary, each command one owner, and each arrow crossing a
row is a published fact or interface, not a private event.

### Boundary record (compact output, one per boundary)
```text
Boundary: <module or context>        Owner: <team/person or ASSUMED>
Decides: <rules and the streams they hold on>
Commands handled: <names>            Allowed origins: <personas / systems>
Facts produced: <events, public or internal>   Facts consumed: <event -> producing slice>
Views provided: <read models>        Views read from others: <read models>
Processors: <trigger -> decision/effect -> result; operational owner>
External: <system -> adapter owner -> contract>
Open: <unresolved decisions, each also in STATE.md>
```
Keep durable rationale in module, feature and slice `documentation` and the record's `Open` lines in
`STATE.md`. Do not build a second catalogue that duplicates the model.

## Cross-boundary walkthrough
Walk one real interaction across each affected boundary:
1. the producer and the published fact or interface;
2. the receiving boundary and any translation into its language;
3. the receiver's decision, command or effect, and the destination identity;
4. success, rejection or failure, correlation and retry behaviour;
5. what happens when the receiver is late, unavailable or sees the input again.

A reaction may produce events directly without an invented command; an email or payment stays an
effect even before any domain command exists.

### Worked walkthrough: marina berth booking
Harbour Office owns a berth booking. Billing owns invoices. Ownership interview answers: two
teams by domain (Q1 B), moderate autonomy (Q2 B), the card payment provider is external with
Billing as adapter owner (Q3), Billing may lag the Harbour Office by minutes but must never
invoice twice (Q4).
1. **Producer.** Harbour Office decides `BerthBooked` on the booking stream (`BookingId`: one
   stream per booking of one berth by one vessel; it answers "is this stay confirmed?"). Its
   description marks `BerthBooked` **public** and lists consumers: Billing.
2. **Translation.** Billing consumes `BerthBooked` through a Translate-style reaction and speaks
   its own language: a `StayInvoiceDue` fact on `InvoiceId`, with the booking id kept as a
   correlation property, not as its stream identity.
3. **Decision.** Billing decides whether an invoice is due (a tariff, a free night). Harbour
   Office never writes Billing's facts, and Billing never pretends to have produced `BerthBooked`.
4. **Failure and correlation.** The card provider rejects the charge: Billing records
   `StayPaymentRefused` and shows it in its own view. Correlation is the booking id. Whether
   Harbour Office must release the berth is a business decision (a new public fact,
   `StayPaymentLapsed`), not a hidden call.
5. **Late or repeated input.** `BerthBooked` delivered twice must not create two invoices: a
   `unique event StayInvoiceDue` on the invoice stream whose id is derived from the booking (the
   retry identity) makes the second append a rejection, which the processor treats as done.
   Billing unavailable: Harbour Office still confirms; its view shows "invoice pending".
Open: the invoice-id derivation rule and who owns the dead-letter handling are written in
`STATE.md` with Billing named as owner. This is a reasoning example, not evidence of executable
integration support.

## Internal and public events
- **Internal events** record decisions inside the module. They can be fine-grained and change
  with the module.
- **Public events** are a contract other modules or contexts build on. Keep them coarse,
  stable and complete enough that consumers do not need to call back. A summary fact
  (`EnrolmentConfirmed` carrying what downstream needs) is often better than exposing every
  internal step.
- Treat a public event like a published API: evolution follows `evolution.md` with the
  consumers named, and breaking changes become new events.
- Events are private by default. Mark a public contract with `public event X` (ESM v9, released in
  Screenplay 4.117.0); declare a foreign public event's shape locally with `event X from "store"`.
  A command never produces a public event (PLAY0607/0613); crossing the boundary is a `Translate`
  slice with an explicit direction (`cratis-screenplay-automations-and-translations`
  `references/public-events-and-direction.md`). Nothing checks that the foreign shape matches the
  other store, so also name the consumers in the event's `documentation` and keep the list in
  `STATE.md`. Stage 4.73.1 refuses public events (`STAGE-ESM-032`).

## Crossing a boundary in Screenplay
| Situation | Construct |
|---|---|
| another module in the same application | reference the event by name (cross-module resolution works within one application) |
| another bounded context's contract | `event X from "store"` (shape declared locally, ESM v9); an unquoted `import Context.Event` or `import Q.X from "store"` has no local shape and stays PLAY0268 (see `versions.md` in `cratis-screenplay-toolchain`) |
| my own files | quoted `import "glob"` |
| outside data entering | `Translate` slice: `capture`, or a translator `reaction` over an imported event (`cratis-screenplay-automations-and-translations`) |
| our fact another context needs | a public summary event; the other side translates it into its own language |

Never let an external system's ids, codes or shapes become our domain vocabulary: translate at
the edge, keep the external reference as a property when it is needed for correlation or dedup.
Stage 4.89.1 renders supported internal event-triggered Automation, but Translate,
captures and foreign/public event boundaries remain refused. The translation is gap-fill
with the model as its contract (`cratis-screenplay-render-and-gap-fill`).

## Change data capture is not a domain event
A row change captured from a database (CDC) says which columns changed, not why. Treat it as an
external record: translate it into business facts with a named reason, or keep it as evidence.
Do not publish raw row changes as public events.

## Check coupling rather than banning every cycle
An acknowledgement returning to its initiator is not automatically a bad dependency. Investigate:
- synchronous mutual dependence that stops either side progressing;
- feedback that can repeatedly produce the same work;
- private event or shared-view changes that force coordinated consumer changes;
- invariants split across owners without an agreed enforcement or compensation strategy.

Record the concrete consequence and proposed repair. Independent development, deployment and
scaling are separate claims; establish each only when the dependencies support it. A shared view
is a versioned interface and an availability dependency, not free decoupling. Team and system
are not the same boundary by law: separate teams often own separate modules, but independence
is shown by the dependencies, not assumed from the org chart.

## Boundary checklist
- [ ] every boundary has a named owner or an `ASSUMED` mark in `STATE.md`
- [ ] every command has allowed origins; ownership of a command is not permission to invoke it
- [ ] every consumed fact names its producing slice; every public event names its consumers
- [ ] every processor has a trigger, an outcome and an operational owner
- [ ] every external system has an adapter owner and a translation edge
- [ ] cycles were classified (legitimate feedback, mutual dependence, repeated work), not just counted
- [ ] one interaction was walked across each boundary including late, repeated and failed input

## Review questions
- Which facts cross this boundary, and who depends on each?
- Would a change inside this module force a change in another? Then the contract is too fine.
- Is the boundary following people who talk to each other, or the business capability?
