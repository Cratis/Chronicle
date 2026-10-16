<!-- cratis-ai-managed: skills/cratis-screenplay-scenario-coverage/references/review-questions.md -->
# Review questions

Questions to ask when reviewing scenario coverage.

- Where is this invariant enforced, and what does the loser of a race see (enforced or declared)?
- What does a client that lost the acknowledgement do with the retry's answer?
- Can facts from other event sources arrive before ours? Does the view tolerate it?
- After a correction or backdated fact, does the view still say something true?
- Cross-context views: are the real source contracts resolved, or is missing evidence a gap?
