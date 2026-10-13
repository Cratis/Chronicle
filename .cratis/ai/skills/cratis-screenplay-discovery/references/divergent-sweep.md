<!-- cratis-ai-managed: skills/cratis-screenplay-discovery/references/divergent-sweep.md -->
# The divergent sweep

Purpose: find the facts nobody mentioned because everyone described the good day. Run it after
the first story of a workflow exists. List first, judge second: write every candidate down,
then run each through `event-naming.md`.

Picture the business running this workflow for a full year, then walk these lenses over each
stage of the story:

| Lens | Ask | Usually yields |
|---|---|---|
| Other starts | Can this begin another way (in person, by phone, by an outside party, on a schedule)? | extra causes; sometimes a separate story |
| Change of mind | At this stage, can the initiator or the business call it off or change it? Until when? | cancellation/withdrawal facts; "only if" limits (state-violation specs) |
| Waiting | What if nobody acts? Does it expire, get chased, escalate? | deadline facts; clock causes |
| Hand-over failure | Where we depend on someone else: refusal, no answer, late answer, two answers? | outside facts; compensation; duplicate candidates |
| Competition | Can two parties want the same scarce thing at once? Who wins, and how does the loser learn? | competition candidates (competing-claim specs); uniqueness rules |
| Repetition | Can the same request arrive twice? Several in progress for one party? | duplicate/retry candidates; subject identity |
| Correction | A recorded fact is wrong: how is it put right, who must know? | correction facts instead of edits |
| Endings | Every way this finishes, by each party; what depended on it and must now go? | terminal facts; removal from lists (removal specs) |
| Afterwards | Renew, reopen, roll over, appeal, refund? | follow-on stories or branches |
| On behalf | Can someone act for another, or override? | Does/Cannot lines (denial specs) |
| Proof later | What will an auditor, regulator or angry customer ask to see? | facts that looked like detail |
| Without software | How would this run on paper, with people? Who acts, what triggers each action, what must they know first? Which step exists only because of the system (a loading state, a cache refresh, a session check)? | the real business steps; steps to drop; missing human hand-overs |
| Hotspots | For each hotspot named in step 2: what goes wrong there today? | the deepest branch set |

Outcome per lens and stage: modeled fact, set-aside (with why), open question, or "does not
apply, because ...". The sweep is done when every lens has an outcome; do not invent facts to
fill a lens. Unattended: model a lens fact only when the input implies it; otherwise it is an
open question with the assumption in use.

## Handoff seeds for later skills
| Discovery item | Becomes (not decided here) |
|---|---|
| Cannot line | a gate and `then denied` spec, or a recorded gap |
| "Only if" | a rule layer / `require` / constraint, or a recorded target requirement |
| Competition candidate | a competing-claim spec; consistency design |
| Duplicate candidate | a duplicate/retry spec |
| Ending | removal from affected views |
