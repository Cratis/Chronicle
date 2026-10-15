<!-- cratis-ai-managed: skills/cratis-screenplay-discovery/references/facilitation.md -->
# Facilitation

## Stance

- Help people say what happens in their business; do not teach event modeling unless asked.
- Use their words in names and descriptions. Correct a name by writing the better one into the
  model and mentioning it in the change report, not by explaining why theirs was wrong.
- Keep the conversation about the business. Databases, APIs and frameworks are out of scope
  during discovery; park them in STATE.md if they come up.
- Breadth before depth: a rough map of the whole domain first, then one workflow at a time.

## Filtering without friction

When someone offers a non-event, ask what changed because of it. "When the harbourmaster
looks at the waiting list, does anything get decided?" If the answer names a decision, model
that decision. If not, note the need as a view and move on.

## Disagreement

- When people describe the same step differently, first check whether they describe different
  situations (two branches, two roles). Often both are right.
- If it is a real disagreement, record both versions in an anchored `question`
  note; carry its note ID and working assumption in STATE.md. Say which assumption
  you use and continue. Do not stall the session on it.
- Domain experts decide business meaning; engineers decide feasibility. When only engineers
  are present, mark business-meaning decisions as assumptions to confirm.

## Group sessions

- Ask the quieter participants directly about their part of the process.
- Treat a sceptic's objection as a likely failure path: write it down as a question, not a disagreement.
- Let idea-generators run, capture everything, and sort afterwards; do not slow the momentum
  with filtering in the middle of their stream.
- Someone who dominates: thank them, capture the point, and turn to the others for theirs.
- For a live workshop with people in a room, use `human-workshop.md`.
- Timebox broad discovery; deep dives happen per workflow.
- Read the event list aloud in story order at checkpoints; gaps are easier to hear than see.

## Unattended runs

- Never block on a question during discovery. Take a visible working assumption
  and record it with its reason in an anchored `question` note, not chat or
  `description` text. STATE.md carries the note ID.
- Prefer assumptions that keep the model honest (fewer, clearly named facts) over ones that
  invent detail.

Read `plotting-and-handoff.md` "Durable discovery questions" when recording or
resolving a note with project decision `refs`. Never put secrets or personal data
in notes. See "Evidence and privacy" in SKILL.md.

## Closing a session

1. Validate the model (`--warnings-as-errors`).
2. Summarise: workflows covered, events added or renamed, personas, what was set aside.
3. List open questions with the one that blocks progress first.
4. List assumptions taken.
5. Per workflow give the happy path, the decision points and the terminal facts in a few lines.
6. Propose the next step: the next workflow, or `cratis-screenplay-slice-design` for this one.
7. Update STATE.md (phase, status, carry-forward); keep durable rationale in fenced Markdown
   `documentation`, summaries in `description`. At 4.127.0 canonical printing retains
   documentation on its owner; MCP exposes it, but it changes no `modelRevision` and
   reaches no rendered code. Inspect dropped comments after canonicalizing edits.
