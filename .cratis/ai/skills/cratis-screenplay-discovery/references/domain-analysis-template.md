<!-- cratis-ai-managed: skills/cratis-screenplay-discovery/references/domain-analysis-template.md -->
# Domain analysis note

Use this when requirements arrive as prose (a brief, tickets, a recorded session, an interview
transcript) and you want one readable summary before or alongside the first `.play` edits. The
`.play` model is the source of truth; this note is a work record that points to it. It lives in
`.ai-work/screenplay/` (session state), never in the model folder or `.cratis/`, and is replaced
by the model once the model says the same things. Adapted from Martin Dilger and Nebulit GmbH's
brainstorming-events domain analysis (see `provenance.md`), reshaped for Screenplay.

## When to write one
- The input is long or scattered and a person must confirm your reading before you model.
- A workshop produced notes that need turning into candidate events, roles and rules.
- A reviewer or domain expert needs one page to answer, without reading `.play`.

Skip it when the input is already short and structured; go straight to `references/storming-loop.md`.

## Working it out
1. Read the requirements and list the processes mentioned: who starts them, what steps follow,
   where decisions and integrations occur.
2. Extract the state changes. Each completed change is a candidate event ("Customer places order"
   becomes `OrderPlaced`): "payment processed" becomes a confirmation fact; "item shipped" becomes a
   shipment fact.
3. Write the rules and invariants as stated ("can only ship once paid", "cancel within 24 hours").
   Do not decide where they are enforced here.
4. Name the entities as event timelines: each is the story its events tell, with a business
   identity. These are groupings of events, not attribute lists.
5. Note the questions the requirements do not answer.

## Template
```markdown
# Domain analysis: <domain>

Source: <documents, session, date>      Participants: <roles, if a workshop>

## Roles
Human roles, then system actors (internal or external; what sets them off).
- <Role>: <description>. Does: <key actions>. Reads: <what it sees>. Cannot: <boundary>.
- <System actor> (internal | external): <description>. Triggers: <what it starts>.

## Entities (event timelines)
- <Entity> (identity: <business identity>)

## Business processes
1. <Process>: initiated by <actor>; steps <1 -> 2 -> 3>; outcome <what changes>.

## Candidate events
- <Entity>: <PastTenseEvent>: when, why, who or what causes it.

## Rules and constraints
- <Rule>: condition and consequence, as stated in the source.

## Questions for the domain expert
- <Unclear process, missing actor, boundary condition>.

## Where this went
<Pointers to the `.play` declarations once modeled, or "not yet modeled">
```

## Requirements that make a good source
Ask for, or write down as assumptions, each of these:
- **Specific scope.** "Orders have items, pricing, a delivery address and can be cancelled within
  24 hours", not "handle orders".
- **Domain language**, not technical wording: "Customer places order with products", not "obj1
  references obj2".
- **Explicit constraints.** "Authorize payment before confirming the order; refund if shipment
  fails", not "process payments".
- **A complete role list.** Every actor an event or process mentions is in Roles, and every
  event traces to one of them ("orders can be created" by whom?).
- **Edge cases.** What if payment is declined? Can an order change once shipping starts? What
  triggers cancellation?

## Questions for the domain expert
Keep them concrete and one decision each; send the three that unblock the most. Examples:
- "When a payment is declined, does the order stay open, or end?"
- "Who may cancel after shipping starts, and what happens to the goods?"
- "Is a refund a separate step someone decides, or automatic?"

Answers become events, rules or specifications; unresolved ones stay in STATE.md as open
questions with the assumption in use (`cratis-screenplay-modeling-lifecycle` `references/stop-or-assume.md`).

## Not every failure is an event
A declined payment may be a refusal (nothing recorded) or a recorded fact that others react to
(`PaymentDeclined`). Ask which; do not turn every error condition into an event. Most rejections
are specifications (`then error`), recorded facts are the exception. See `references/event-naming.md`.

## Quality checks before sharing
- [ ] Every candidate event is past tense, one purpose, and names a completed change.
- [ ] No generic update or delete events (`RecordUpdated`); events describe business moments.
- [ ] Every role is distinct, and each event traces to one of them.
- [ ] No two candidate events mean the same thing.
- [ ] Rules are stated as the source states them, with unknowns left as questions.
- [ ] Work is not wasted on attributes: fields wait until commands and views are designed.
