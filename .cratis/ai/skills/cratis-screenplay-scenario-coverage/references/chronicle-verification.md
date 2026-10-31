<!-- cratis-ai-managed: skills/cratis-screenplay-scenario-coverage/references/chronicle-verification.md -->
# Chronicle verification obligations

Logical `.play` outcomes, in-process Chronicle scenarios and real-kernel tests prove different
things; do not substitute one for another. Match each guarantee you claim to evidence of that
kind, and add only the applicable cases below to the coverage matrix (as `spec`, `open`, `gap`
or `question` cells).

- **Claims**: different-source collision, same-source reclaim, change or release, a legitimate
  repeat.
- **Decision reads**: never-created absence, removal then recreation, a relevant concurrent
  change, an irrelevant one, rejected or no-fact outcomes, refusal of an incompatible
  projection or key.
- **Namespaces**: identical business keys in different tenants, background target selection,
  independent observer progress, every namespace covered by erasure.
- **Compliance**: subject resolution, unreadable values after erasure, fenced new writes,
  partial erasure, deliberate re-enrollment, disposal of external copies.
- **Evolution**: historical generations replay through the real migrations into current views.
- **Projections and reducers**: AutoMap collisions, join ordering, child removal, variant entry,
  update-before-create, stale attempts, late events after completion.
- **Reactors**: normal delivery, replay, recovery redelivery, uncertain external outcome,
  duplicate-result handling, failure becoming visible.

A competing-claim spec pins the loser's outcome after an earlier holder. It does not run
commands in parallel and does not prove index-and-append behaviour under failure.
