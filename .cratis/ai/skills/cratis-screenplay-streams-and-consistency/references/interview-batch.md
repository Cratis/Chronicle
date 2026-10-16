<!-- cratis-ai-managed: skills/cratis-screenplay-streams-and-consistency/references/interview-batch.md -->
# Interview batch for streams and consistency

The six interview questions, with follow-ups.

1. **Instance.** "What is the real-world thing this stream is about, and when does it start and
   end?" Follow-up: if the answer needs "and" for two lifecycles, ask which facts live
   independently of the other.
2. **Competition.** "Can two people do this at once, and what must never happen then?" Follow-up:
   for each answer ask whether it is a uniqueness, a count or a state rule.
3. **Retry.** "If the reply is lost and the client sends the request again, what should the
   second request see?" Follow-up: is the identifier chosen before the first attempt?
4. **Ownership** (full protocol and options in `references/boundaries.md`). Who decides these
   rules and operates them when they fail; how much can each side move alone; which external
   systems take part and who owns each adapter; what delay or disagreement the business
   tolerates. Follow-up on technical-function teams: which business capability does each serve?
5. **Growth.** "Roughly how many events per instance per year, over how many years, and which
   commands read the stream versus a view?" Follow-up: if the estimate is large, check the
   identity before anything else (`references/stream-patterns.md`, Growth and snapshots).
6. **Change.** "Has this event ever been stored in a running system?" It decides whether an edit
   is ordinary or contract evolution.
