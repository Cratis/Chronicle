<!-- cratis-ai-managed: skills/cratis-screenplay-legacy-extraction/references/expert-verification.md -->
# Expert verification

Owner: screenplay-modeler with the user, after screenplay-reviewer (review phase P5). The extractor prepares
the question list; experts decide. Nothing in the candidate is accepted until a person who
knows the business has confirmed it or the user has accepted the stated assumption.

## Three views, compared on purpose
Look at the same process from three directions and treat mismatches as the most valuable
findings:
- **Written intent**: requirements, tickets, manuals, contracts.
- **Lived practice**: what experts and operators say actually happens, including manual
  workarounds.
- **System behaviour**: the evidence table (code, generated model, captures).
Features that are documented but absent, practice with no system support, and code nobody
asked for each become a row or a question.

## Preparing the session
- One page per workflow: the candidate slices as a short story in business words (no
  Screenplay syntax, no table names unless the experts use them), with evidence ids.
- At most about 12 questions per session, ordered by risk. Each question names the slice, the
  evidence row and what changes depending on the answer.
- Mark which items are proposals (`decided by: proposed`) so experts know what to challenge.
- Redact production values; ask before showing captures or code to anyone outside the team.

## Questions per slice
- Is this event something you would say happened, in these words? Is it one fact or several?
- Who decides this, and can anyone act on someone else's behalf?
- What stops it from happening (rules, limits, states)? What message does the person see?
- What happens next, and who needs to know?
- Is there a manual, back-office, month-end or exception path the system does not show?
- Does time matter (deadlines, expiry, reminders, backdating, corrections)?
- Which personal or sensitive data does this carry, and how long must it be kept?
- Which team owns it? Which other part of the business consumes it?
- For a generic update (one form, many fields): which separate reasons lead someone to change
  these fields?
- For writes with no user behind them: why does this job, trigger or import run, and who
  would notice if it stopped?
- Keep, change or remove this behaviour in the target? If removed, who relies on it today?

## Recording outcomes
- Update the evidence row: `expert verdict` (confirmed / corrected / rejected), `decided by`,
  `target decision`. Never overwrite the observed fact.
- Corrections become model changes through screenplay-modeler (address-preserving text edits, or
  identity-affecting edits through the identity owner:
  the identity procedure in `cratis-screenplay-modeling-lifecycle`).
- Disagreements between experts stay open as questions with both positions recorded.
- Per slice sign-off (expert, date, open items) goes in the evidence table; the user's
  acceptance of the whole candidate is the accept phase (P6), bound to a source identity.
