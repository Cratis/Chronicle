<!-- cratis-ai-managed: skills/cratis-screenplay-discovery/references/worked-session.md -->
# A short discovery session, end to end

Marina berths, attended, with a thin input (a three-line brief). It shows the opening, two
storming turns, the sweep, plotting, the report and the resulting model.

## Opening

Brief: "Boat owners ask for a seasonal berth. The harbourmaster gives out berths. Fees are
checked every quarter."

Agent (restates, confirms):
> So we are modeling how seasonal berths are requested and given out in one marina, and the goal
> is that every owner gets an answer and every berth is used. Is that right? And who has the
> final word on what these steps mean?

User: "Yes. I do, I am the harbourmaster."

Decisions: scope = seasonal berths; meaning decided by the harbourmaster. Assumption: one
marina, one season at a time.

## Turn 1: storming

User: "An owner asks for a berth with the boat's name and length. I check the waiting list,
then either give a berth or say no."

The agent writes into the model, then reports:

```text
Added:     BerthRequested, BerthAssigned, BerthRequestDeclined (SeasonalBerths)
Personas:  BoatOwner, Harbourmaster
Set aside: "check the waiting list" (a view, not a fact)
Question:  When you say no, does the owner get told why, and must the marina remember why?
```

User: "Yes, we keep the reason." (so `BerthRequestDeclined` carries `reason`).

## Turn 2: the story

User: "Once a berth is given, the boat arrives some day, and leaves at season end. Sometimes
the owner cancels before arriving."

```text
Added:     BoatArrived, BoatDeparted, BerthAssignmentWithdrawn
Caused by: BoatDeparted <- schedule (season end)
Question:  If a boat never arrives, does the berth stay assigned? Until when?
```

User: "Ten days after the start date, then I free it." The agent records an open question: is
that a fact (`BerthForfeited`) caused by a deadline? Assumption (not confirmed): yes, treated as
a clock-driven fact for the automations phase; noted in STATE.md.

## Sweep (lenses answered)

| Lens | Outcome |
|---|---|
| Change of mind | modeled: `BerthAssignmentWithdrawn` before arrival |
| Waiting | open question: forfeit after ten days (clock cause) |
| Competition | candidate: two owners want the last berth; the harbourmaster decides (no race modeled) |
| Hand-over failure | does not apply: no outside party in scope |
| Endings | modeled: departure; declined; withdrawn |
| Proof later | modeled: the decline `reason`; the fee is calculated (a view), unless the season fee is fixed at assignment (open question) |
| On behalf | Cannot: owner may not assign; office staff cannot request for an owner (open question) |
| Hotspots | the waiting list order: a view need |

## Plot and handoff

The plot, the packet and the Cannot resolution table follow `plotting-and-handoff.md` and
`personas-and-causes.md`.

STATE.md:

```text
Decisions:   scope seasonal berths; harbourmaster decides meaning; decline reason is kept
Assumptions: one marina; forfeiture after ten days is a clock-driven fact (to confirm)
Rules:       AssignBerth only if a free berth is at least as long as the boat (intent)
Denial candidates: BoatOwner cannot assign berths -> gate IsHarbourmaster + denial spec
Competition: last free berth wanted by two owners
Open:        forfeit rule; season fee fixed or recalculated; office staff on behalf
Verdicts:    V1 passed (screenplay 4.66.0, --warnaserror, exit 0); V2 to V5 not run (discovery)
```

## Resulting model

The discovery skeleton in `storming-loop.md` is this model before the open questions are
resolved (there is no `BerthForfeited` until the forfeit rule is confirmed); the denial gate in
`personas-and-causes.md` is the first resolution. The Gate in SKILL.md is then checked one
bullet at a time before handing over to `cratis-screenplay-slice-design`.
