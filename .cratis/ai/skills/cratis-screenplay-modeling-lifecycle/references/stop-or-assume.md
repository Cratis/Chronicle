<!-- cratis-ai-managed: skills/cratis-screenplay-modeling-lifecycle/references/stop-or-assume.md -->
# Stop or assume: triggers, question format and the interview protocol

## Interview protocol (attended and unattended)
Assume visibly during modeling; delivery blocks any guess that could encode a wrong rule,
authorization, money or time behavior. Contradictions, a third review round, a gate only the
user can accept, and approvals not yet given always stop.

- **When to ask.** Information this phase needs is missing or ambiguous: ask, using the phase
  skill's critical questions as the guide, and follow its listed follow-up trigger on the answer.
  The phase already has what it needs: skip straight to its work and do not ask for what you hold.
- **Unless told not to ask** (an unattended run, or the user said not to stop for questions), in
  modeling: do not block. Proceed on the most reasonable assumption for anything missing and state
  it plainly in this phase's record, visibly, so it can be corrected later. Never guess silently.
  This does not extend to delivery (P7-P9, gap-fill), where a guess that could encode a wrong rule,
  authorization, money or time behavior blocks as in "Delivery triggers" below. Modeling assumptions about such rules are
  recorded visibly with their address. Unconditionally, contradictions, a third review round, a gate only the user can accept, and approvals not yet given always stop.
- **Recording the outcome.** Under the phase's Interview Trail row in STATE.md write what was
  asked and decided, or, if nothing was asked, what was assumed and why
  (`handoff-template.md` section 1).
- Ask the one question that changes the model most; follow up on vague answers ("it depends",
  "usually", "sometimes") with "what decides it?". Do not interrogate for what a skill, the tool
  or the Screenplay docs already answer.
- Attended: ask, and record the answer in STATE.md as a decision.
- Unattended: assume visibly. Write each assumption with its address and the consequence if wrong,
  and record in the Interview Trail which items were **asked** and which were **assumed**.
- A decided rejection is a specification, not a note: when the user says "never X", add the
  `then denied` or `then error` specification in P3.
- One closed outcome per run. A run that neither progressed nor closed is a failure to report:
  say what blocked it and which question would unblock it.

## Durable question notes

Record each open modeling choice as a `question` note on its element in committed
`.screenplay/notes.json`, not only chat/STATE.md or `description` text. Include the
question, consequence and assumption in use (or blocked scope). Keep its returned
note ID and address in STATE.md/packets; before an element exists, anchor to the
application or nearest identified container and name the intended element in text.
Never put secrets or personal data in note text, authors, refs or resolutions.

Read model-authoring [model notes](../../cratis-screenplay-model-authoring/references/model-notes.md)
when recording or answering questions: discover tools, list with pinned notes
revision, `propose-model-notes`, review `read-proposal view=notes`, then authorized
`apply`. If the installed bundle lacks notes, report the capability gap and carry
an explicit note request to the owner; do not pretend a chat/description is a note.

Cite the project's own decisions with `refs: [{ "id": "D123" }]`. When D123 is
the answer, resolve with `by: "external-decision"` and required
`ref: { "id": "D123" }`, explanatory text and a team-role author. The URL is
optional and never fetched. Realize a resolved question as a specification for
behavior, a description for meaning, or a project-owned decision record for
consequential policy. Resolving is not implementation; a known refusal still
needs `then error` or `then denied`. Reopen when the evidence ceases to answer it.

Question gating is separate from V1: standalone Screenplay 4.144.0 has
`--open-questions fail`; Cratis CLI 3.53.1 does not. Read toolchain
`references/versions.md` "Model notes gate" when a phase requires no open questions.
Product evidence: Screenplay `v4.144.0:Documentation/screenplay/model-notes.md:12-77,93-121`,
`decisions/0045-model-notes-sidecar.md:40-79` and the model-authoring source table.

## Question format (STATE.md, packets, edit requests)
`Qn <declaration address>: <what is missing or contradictory, one sentence> - matters because
<consequence> - <assumption in use | blocked scope>`. Write it so a reader who has not opened
the model understands it cold. "Please clarify" is not a question. A named address beats a name
inferred from prose.

## Delivery triggers (stop that scope)
- A rule stated in a `description` has no `validate`, `require`, constraint or specification.
- A specification's `given` names an event no command, reaction or capture produces in scope.
- A `description` and the specifications disagree on behavior.
- The slice depends on an event, read model or command that is neither declared nor imported,
  and it is unclear whether it should exist.

In modeling these are findings (critic) or open questions (modeling), not stops.

## Not triggers
Naming conventions, file layout, formatting, which generated hook to use, anything the phase
skill, toolchain or Screenplay docs answer, "nice to confirm".

## Calibration
A blocked scope should reliably mean a person has to decide. A wrong guess encoded into committed,
tested code is harder to catch than a paused scope; in modeling the opposite holds, because an
assumption is cheap to revise and visible to the reviewer.

## Always stop
Contradictions, a third review round, a gate only the user can accept, and approvals not yet
given for a named target. Approval is not repeated for the same scope.

## Unblocking
A scope leaves `blocked` only when its question is answered; record the answer as a decision in
STATE.md with its note ID, resolve the question note by evidence, and realize the
answer in the model or decision record. Settled rationale may also go in fenced
`documentation`. Re-blocking a blocked scope is a no-op.
Blocked delivery scopes are reported in the packet; the main session creates any issue.
