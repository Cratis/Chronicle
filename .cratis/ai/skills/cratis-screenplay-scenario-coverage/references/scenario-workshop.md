<!-- cratis-ai-managed: skills/cratis-screenplay-scenario-coverage/references/scenario-workshop.md -->
# Scenario workshop (human facilitation guide)

## Contents

- Before the workshop
- During: one command at a time
- During: one view at a time
- Reading back
- Multi-role check (before leaving the room)
- Common mistakes
- Tips for rapid capture
- Optional pacing

Use this when scenarios are written together with business people, developers and testers
instead of by one author. The facilitation structure is adapted from the Workshop Facilitation
Guide in TrogonStack `eventmodeling-elaborating-scenarios` (MIT; see `provenance.md`) and
mapped onto Screenplay specifications and the coverage matrix. An agent can run the same
protocol as an interview: each step below is a question to put to a person, with the answer
captured as a matrix cell or a named open question.

The workshop produces **candidate obligations**, not finished specifications. A facilitator
writes them into the coverage matrix (`coverage-matrix.md`) as `open: <case>` cells; a modeler
then drafts the specifications. A scenario sketched on a card is not coverage until a
specification with that name exists in the model.

## Before the workshop
**Participants** (name the person for each role; one person cannot stand in for all four):
- Domain expert or product owner: business rules, priorities, edge cases, what is refused.
- Developer: feasibility, where a rule can be enforced, failure and concurrency boundaries.
- Tester: distinguishing fixtures, boundary values, how a wrong outcome would be noticed.
- Facilitator: keeps pace, keeps the vocabulary to the domain, records every counterexample.

**Setup:**
- The slices in scope, printed or shared: commands with their rules, effective gates,
  constraints, productions, read models and queries. Derive the obligations first
  (`coverage-matrix.md` "Derive obligations from the model") so the workshop starts from a
  draft matrix, not a blank board.
- Outcome lists from discovery and slice design (Cannot lines, "only if" rules, refusals,
  competition candidates, endings) as seeds.
- One card per command or view, with the twelve matrix columns as prompts. A timer per card is
  optional; a card is finished when every column holds a value (see Optional pacing), not when
  the time is up.

## During: one command at a time
Take each command in turn, in this order. The person who answers best is in brackets.

1. **Happy path** (domain expert): "When everything is in order, what is now true that was not
   before?" Read the production list aloud: every event, with its payload.
2. **Rule rejections** (developer, tester): "Which values would you refuse, and what would you
   tell the person?" One allowed and one refused example per rule; write the exact message.
3. **Denial** (domain expert): "Who can see this screen but must not do this?" Each answer is a
   persona with a gate; no gate means a recorded gap, not a spec.
4. **State violations** (domain expert): "After which earlier facts must this be refused?"
   (cannot cancel after departure, cannot pay twice). These are the rules most often missed.
5. **Duplicates and competing claims** (developer): "What if it arrives twice? What if two
   people try at once, and what does the loser see?" Decide which promise is needed: once-only
   fact, idempotent handling, exactly-once external effect (scenario catalogue 5).
6. **Alternative paths** (domain expert): "Which inputs change the result, and what happens
   either side of the boundary?"
7. **Ordering and external failure** (developer): "Can facts arrive late or out of order? What
   if the other party refuses or never answers?" The answer is an outcome recorded as a fact.
8. **Compensation** (domain expert): "Can this be undone? What must undoing release?" A reversal
   the business has but the model lacks is a missing event: stop and tell the modeler.
9. **Evolution** (developer): only when a stored event's contract is changing.

## During: one view at a time
For each read model, ask "what does the screen show after each fact?" and decide family or no
family (`view-and-story-specs.md`). Walk one row through its states aloud, forks included
(reserved, then departed or cancelled, never both). Ask what must disappear and which row must
survive; ask what a caller who may not look sees.

## Reading back
- The facilitator reads each captured scenario aloud in domain language: the given facts, the
  action, the outcome. "Is this right?" Everyone confirms or speaks up.
- Do not perfect a scenario in the room. Capture it with its open question and move on.
- Every "what if...?" from anyone becomes a named candidate at once.
- Record why a rule behaves as it does when the room explains it; the modeler puts it in the
  slice `documentation`.

## Multi-role check (before leaving the room)
- Domain expert: is this the right business behaviour and message?
- Developer: can each rule be enforced where we put it, or is it `recorded` with a target?
- Tester: is each fixture distinguishing (vary one thing), and can a wrong outcome be seen?
- Facilitator: does every matrix cell hold `spec`, `n/a: <domain reason>`, `recorded`, `open`
  or `question`, and is each `n/a` a domain reason?

An agent that facilitates alone must say so: one voice asking four kinds of question is not
four approvals. Record who actually answered each question.

## Common mistakes
- Perfectionism on one scenario while other commands have no card.
- A missing role (no tester, no domain expert) and the answers invented to fill it.
- Technical language in the room (stored procedures, status codes); use domain words and the
  model's names for events and fields.
- Vague givens ("an order"): state which facts exist and which source they are on.
- A scenario with no stated outcome: every card says which events follow, which message is
  shown, or that the caller is denied.
- Writing fewer scenarios to finish sooner: the matrix shows what is still open.

## Tips for rapid capture
- Start from the derived draft matrix and pre-printed cards with the column prompts.
- Parallel work: different people take different commands, then read back together.
- Reuse the existing cast of example data (`scenario-catalogue.md` "Example data") so scenarios
  read as one story.
- Point to the discovery timeline and the slice boundaries when a debate repeats.
- End with the open questions listed, owner and date, and the matrix saved as the workshop
  record.

## Optional pacing
A workshop that has to finish in a fixed time can use the source guide's pacing as a starting
point, subordinate to the matrix:
- About 15 to 20 minutes per command for the first pass, with a timer per card. A command with
  few rules is quicker; one with several rules, a gate and constraints takes longer.
- Three to five scenarios per command is a typical range for a simple command, not a quota.
- Keep the rhythm of the "During" steps: capture, read back, move on. Parallel pairs take
  different commands, then read back together.

The matrix, not the clock, decides when a card is finished. A command with three rules, a gate,
two constraints and a conditional production needs the scenarios its declarations create, and a
quota would drop cases. Time-box the discussion, never the matrix; unfinished cells stay `open`
and their owner and date go in the open-question list.
