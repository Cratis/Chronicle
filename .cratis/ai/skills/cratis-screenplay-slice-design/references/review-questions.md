<!-- cratis-ai-managed: skills/cratis-screenplay-slice-design/references/review-questions.md -->
# Review questions

Ask these while designing and again before the Gate. Report a finding only when it has a
domain consequence.

- Can the persona who should do this reach the command, and may they? Does every persona have
  a screen with data (and an action unless it is a read-only role with a stated purpose)? Can
  someone act for another: role policy, ownership claim, or a separate command?
- Does this command apply in every state and set most of the entity's fields? That is a form:
  which business reasons hide behind it? What does a person decide with each field?
- Does a command input carry a secret or personal value (by default the causation chain keeps it; retention is configurable, verify it)?
  Mark the concept bare `pii` for personal data or `secret` for an operational
  secret; inspect declared purpose coverage, not just the reason. Never protect an identifier.
- Does this optional event property hide two situations, or is it an optional detail?
- Does an event exist for a derived condition? Only when a domain expert names it and something
  reacts to it; merge causes only when they mean the same to the business.
- Does a read model or component draw on many events (fan-in)? Split only for a named
  consequence: different owners, change reasons or consistency needs, or a harder screen. High
  fan-in alone is not a finding.
- Does one screen fire several commands for one intent (one decision)? Can there be more than
  one of these at once (a collection)?
- Where is each invariant enforced atomically, and what happens if two people try at once or a
  command is retried (`cratis-screenplay-streams-and-consistency`)?
- Who may see these rows, and is that enforced by scope/`from`, not by a caller filter?
