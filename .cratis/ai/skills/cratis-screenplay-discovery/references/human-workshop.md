<!-- cratis-ai-managed: skills/cratis-screenplay-discovery/references/human-workshop.md -->
# Supporting a human workshop

## Contents

- The agent's part
- Before the workshop
- Run sheet: discovery
- Techniques
- Remote adaptations
- After the workshop
- Multi-day plan mapped to phases
- Checklists
- Success indicators
- Evidence and privacy

Adapted in part from TrogonStack `eventmodeling-brainstorming-events/references/facilitating-event-modeling-workshops.md`
(MIT; see `provenance.md`), reshaped for Screenplay and for an agent in a supporting role.

Event modeling is a collaborative workshop. Often a person facilitates a room while the
agent supports: preparing the session, writing what is said into the `.play` model as it is
said, reading the story back, and producing the follow-up. The agent never replaces the
facilitator and never runs the room.

## The agent's part

| Moment | The agent does |
|---|---|
| Before | proposes participants, an agenda and a run sheet; reads existing models and documents; lists likely hotspots and questions |
| During | scribes: turns what is said into events, personas and feature descriptions in `.play`, validates after each batch, reports changes in two or three lines; keeps the open-question and assumption lists; reads the event list back in story order on request |
| Between rounds | runs the divergent sweep lenses on what exists and hands the facilitator the three best questions |
| After | validates, writes the summary, lists open questions and assumptions, proposes the next session |

Keep what people said (decisions, with who) apart from what was inferred (assumptions).
Business meaning belongs to the domain experts in the room; the agent marks anything else
as an assumption to confirm.

## Before the workshop

### Participants

Required roles:

- Domain expert or product owner: business rules, priorities, constraints.
- Developers (two or three): feasibility and implementation concerns.
- Tester (one or two): edge cases and coverage.
- Facilitator (one): keeps pace and shared understanding.

Optional: UX designer (workflows, screens), security or compliance lead (personal data,
authority), operations, a real user. Five to eight people is the right size: fewer miss
perspectives, more cannot be facilitated.

### Invitation

```text
Subject: Event modeling session: <domain>

We are mapping how <domain> works: what happens, in what order, who is involved and what can
go wrong. Date, duration, place or link.

Please think about: the workflows that matter most; where things go wrong or get disputed
today; which outside parties are involved; and what you would want a newcomer to understand.

No event modeling experience is needed.
```

### Workspace

- In a room: a wall or whiteboard at least two and a half metres wide, sticky notes in three
  colours, markers, a visible timer, a camera for the result, and the agent's screen visible
  to everyone showing the live `.play` source or a rendered view of it.
- Remote: a shared canvas everyone can edit, a call with recording agreed in advance, a shared
  document for notes, and the live model visible by screen share. Chat stays open for side
  remarks; one person owns the canvas, one the notes.

### Note colours and what they become

| Note | Becomes in the model |
|---|---|
| Orange: event (past tense) | `event` in the `StateChange` slice that produces it |
| Yellow: role | `persona` (with Does / Reads / Cannot) |
| Blue: command (later session) | `command` (`cratis-screenplay-slice-design`) |
| Green: view (later session) | `readmodel` and `query` (`cratis-screenplay-slice-design`) |
| Pink: question or hotspot | anchored `question` note (note ID in STATE.md); hotspot in STATE.md |

Use the agreed colours consistently, whatever they are; the mapping is what matters.

## Run sheet: discovery

Times are guides for a tight group; complex domains need more. Watch the clock and keep moving.

### 1. Understand goals (5 to 10 minutes)

Someone explains the goals: what problem, who the users are, what outcomes matter. The agent
writes the module and feature candidates and the goal text for their `description`s.

### 2. Free brainstorm (15 to 20 minutes)

Facilitator: "Imagine this running for a year. What happens? When something changes in the
business, what is the event? Put down any event you think of." People call out events;
capture everything without filtering. Encourage "what about ...". Example calls:

```text
"Boat owner requests a berth"   "Berth assigned"   "Boat arrives"
"Harbourmaster checks the waiting list"   "Season fee paid"   "Boat leaves"
"Berth request withdrawn"   "Fee reminder sent"
```

The agent writes each as a candidate (past tense, business verb) in the model or the
candidate list, without lecturing on names.

### 3. Gentle filtering (10 to 15 minutes)

Introduce "something that became true and that the business must remember" conversationally:

```text
Facilitator: Does "Harbourmaster checks the waiting list" change anything?
Participants: Not really, he just looks.
Facilitator: Right, so it is a view we will need. But when he gives a berth to someone,
             that changes things. What do we call that?
Participants: BerthAssigned.
Facilitator: And "Season fee paid"? Does something change?
Participants: Yes, the request moves from unpaid to paid.
Facilitator: Then it is a fact.
```

Clarify together: logging in is not a business fact unless sign-ins are tracked; an email sent
is a notification, not a fact, unless the business must prove it was sent; a check made by the
system alone is not a fact. When unsure, keep it and refine later. The agent moves non-events to
"set aside" with the view need noted. Naming rules and repairs: `event-naming.md`.

### 4. Roles (5 to 10 minutes)

Ask who does what and who must not. The agent writes personas with Does / Reads / Cannot, and
lists non-human causes (schedules, outside parties) separately.

### 5. Plot the timeline (15 to 20 minutes)

- Establish order: "What happens first? Before a berth can be assigned, what must happen?"
- Handle branches: "Can something different happen from here? What if the harbour is full?"
  Write the branches clearly (an outcome branch or a separate story).
- Identify parallel flows: "Do these happen at the same time or one after another?"
- Ask everywhere: "What happens next instead, including nothing?"

Do not get stuck on exact timing; the point is the logical flow. The agent keeps the feature
`documentation` story line and after / caused by / only if in the slices current
(`plotting-and-handoff.md`).

### 6. Sweep and close (15 to 20 minutes)

Run the divergent sweep (`divergent-sweep.md`) as questions to the room, hotspots first. Read
the event list aloud in story order: gaps are easier to hear than to see. Close with the two
questions: "Is there anything that happens here that we have not written down?" and "Which
open question blocks the next step?"

Later sessions (commands, views, screens, scenarios) use `cratis-screenplay-slice-design`
and `cratis-screenplay-scenario-coverage`.

## Techniques

### Personalities

- **Quiet participants:** ask directly about their part of the process; do not embarrass.
- **Dominating voices:** thank them, capture the point, turn to others.
- **Sceptics:** take them seriously ("what is your concern?"); the objection is often a hidden
  failure path. Write it down as a question.
- **Idea-generators:** capture everything, sort later, do not slow the momentum.

### Conversation, not correction

Do not say "that is not an event". Say "does that actually change anything?" Use examples from
their business. Ask why an event matters and who cares about it (a view, a rule, an outside party).

### Pace and energy

Good pacing is five to seven minutes per item, quick moves between steps, a break every 45 to
60 minutes, and standing rather than sitting. Warning signs: people checking phones, someone
talking endlessly, long silences (the question is too hard), fatigue. Recovery: a ten-minute
break, a change of activity (drawing to writing), and a restatement of goals and what is left.

### Disagreement

1. If there is a clear right answer, use it.
2. If reasonable people disagree, combine both if both are valid, note both as an open question,
   and take one as the working assumption (say which).
3. If it does not matter yet, defer it. Do not stall the room; keep moving.

## Remote adaptations

Shorter sessions (90 minutes rather than four hours), a break every 30 minutes, structured
input (everyone adds ideas before discussion), clear roles (one drawing, one taking notes, the
agent writing the model), recording on, and an async follow-up so people can digest.

## After the workshop

Same day: validate the model and capture the artefacts (photographs of the wall, the exported
canvas, the recording), share the model and summary with everyone, and list gaps.

Within one or two days: send a summary of what was covered, the decisions, and the open
questions; ask for corrections; schedule the next session and name who owns what.

## Multi-day plan mapped to phases

| Session | Content | Skill |
|---|---|---|
| Day 1, morning | goals, brainstorm, filtering, roles, plot | this skill |
| Day 1, afternoon | sweep, remaining workflows, closing | this skill |
| Day 2, morning | commands, inputs, views, screens | `cratis-screenplay-slice-design` |
| Day 2, afternoon | streams, consistency, outside parties, clocks | `cratis-screenplay-streams-and-consistency`, `cratis-screenplay-automations-and-translations` |
| Day 3 | scenarios and review | `cratis-screenplay-scenario-coverage`, `cratis-screenplay-model-review` |

## Checklists

Before: invitations out a week ahead; the right people confirmed; room or tools tested; notes,
markers and templates ready; the facilitator briefed; objectives clear to all; existing model
and documents read by the agent.

During: started on time; purpose and format explained; each step has an objective; everything
captured; timing kept; everyone took part; energy held; decisions written; ended on time; the
model validated at least at each break.

After: artefacts shared; summary written; gaps listed; feedback requested; next steps
scheduled; every participant knows their deliverable.

## Success indicators

Good: everyone took part, decisions were made and written, the model compiles and tells the
story, the team understands it, next steps are clear, the energy held.

Needs work: some people silent throughout; unclear what was decided; no artefact; talk drifting
to languages and databases; people left tired.

## Evidence and privacy

Do not put production data, customer records or personal data on the shared screen, in the
recording, or into a hosted agent without the participants' approval. Use invented examples.
