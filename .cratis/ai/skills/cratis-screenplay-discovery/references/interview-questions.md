<!-- cratis-ai-managed: skills/cratis-screenplay-discovery/references/interview-questions.md -->
# Interview questions by topic

Use these as a menu, not a script. Skip anything the input already answers. Ask one question
per turn and pick the one whose answer changes the model most. In an unattended run, write the
assumption you take instead of asking.

| Topic | Question | Why it matters | Follow up when... |
|---|---|---|---|
| Goal | What should be true when this works well? What would make it a failure? | Sets the feature `description` and what "done" means for the workflow | the answer is a feature list: ask which outcome the list serves |
| Scope | Which part of the business are we modeling now, and what is deliberately out? | Prevents a model of everything | "everything": ask for the one workflow that hurts most today |
| Today | How is this done now, without the system (paper, email, spreadsheet)? | Real steps reveal real facts; software-only steps are suspects | a step exists only for a tool: ask what business need it serves |
| Roles | Who takes part, and what is each one there to do or decide? | Personas need a purpose; reveals read-only roles | two roles do the same things: ask whether they differ in authority |
| Authority | Who may do this, and who must not? | Feeds policies; separates actors from observers | "anyone": ask whether anonymous callers are really allowed |
| Start | What has to happen before the first step? What sets it off? | Every timeline needs a trigger | the trigger is "the user clicks": ask what made them want to |
| Next | And after that, what happens? | Builds the plot; repeat until an end is reached | an answer skips time: ask what happened in between |
| Endings | When is this finished? In how many ways can it finish? | Terminal facts (completed, cancelled, expired) | only the happy ending is named |
| Failure | What can go wrong here? What does the other party see when it does? | Rejections, compensations and correction facts | a failure has a business consequence: ask whether it must be remembered |
| Rules | What must always or never be true? | Invariants and refusals | for each rule: what does the person see when refused, and does the business record the breach? If it depends on other records, note it for `cratis-screenplay-streams-and-consistency` |
| Time | Are there deadlines, schedules, seasons or expiry? | Clock-driven facts and automations | a deadline passes silently: ask whether the business records that it passed |
| Many | Can several of these be in progress at once for the same party? | Collections vs single values; stream identity | yes: ask what tells them apart |
| Outside | Which outside parties or systems send or receive information here? | Later translations and contracts | they send facts we rely on: ask how we recognise which of our records it concerns |
| Corrections | Can a recorded fact turn out wrong? How is it put right today? | Correction and reversal events instead of edits | "we just edit it": ask who needs to know it was changed |
| History | What would an auditor, a regulator or an angry customer ask to see later? | Finds facts that look like detail but must be kept | a calculation must be shown later: treat it as a fact |
| People's data | Which values identify or describe a person? For what use, basis, retention, recipients/transfers and erasure exceptions? | bare `pii` on concepts; purpose declarations separate from reasons | special/criminal data needs declared safeguards; two people in one fact may need separate facts |
| Volume | How often does this happen, and how long does one instance live? | Informs stream design later; never a reason to drop a fact | an instance lives for years: note it for stream review |
| Input | What do we have to go on: written rules, a rough list, word of mouth, a running system? | Sets how hard to sweep | only word of mouth: run every sweep lens |
| Who decides | Who has the final word on what this means for the business? | Separates decisions from assumptions | no one present: mark meaning as assumption to confirm |
| Hotspots | Where does this go wrong, get disputed or need workarounds today? | Focuses depth | per hotspot: what happened the last time? |
| Competition | Can two parties want the same thing at the same moment? | Uniqueness rules, competing claims | yes: who wins, and what does the other see? |
| On behalf | Does anyone do this for someone else, or override it? | Authority and denial | yes: may they, and must we record who acted? |
| In any order | After this, which next steps must wait for each other and which need not? | Ordering, later automations | they need not: say so in the feature `documentation` |
| Instead | At this point, what else could happen instead, including nothing? | Branches and endings | "nothing": does it expire or wait forever? |

## Interview flow

Skip the interview when all three hold: the input is written (stories, spec or notes) with
its rules, the people who know the domain are named and taking part, and the existing model
already lists workflows and personas. Otherwise run it in two passes:

1. **Assess the input** (Input, Who decides). Gauge how complete the input is and whether
   the people who know the business are present. Thin input or engineers only: sweep harder
   and mark business meaning as assumption.
2. **Map complexity** (Hotspots, Rules). Find where depth is needed and which rules may
   produce facts. Plan which workflow to storm first from the answers.

Record the answers in STATE.md in two lists: *Decisions* (what the user said, with who) and
*Assumptions* (what you took in their absence, with why). Only the second list is something a
reviewer should challenge.

## Opening and closing questions

- Opening a session with a brief: restate the domain and goal in two sentences and ask
  whether that is right before storming.
- Opening with no input: ask for a walk-through from the first step to the goal.
- Technique: one question; when the domain suggests likely answers, offer two to four plus
  "something else". It exposes the assumption you would otherwise make.
- Stop when the user says the workflow is complete, or both closing questions return nothing new.
- Closing a workflow: "Is there anything that happens here that we have not written down?"
  and "Which of the open questions blocks the next step?"

## Signals that discovery is not done

- A persona with nothing to do or read.
- An event nobody can say the cause of.
- A workflow with no terminal fact or no failure path.
- Answers that describe screens or tables instead of what happened.
