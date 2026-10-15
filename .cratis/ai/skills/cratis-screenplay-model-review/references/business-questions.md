<!-- cratis-ai-managed: skills/cratis-screenplay-model-review/references/business-questions.md -->
# Business-question pass (explain brief)

## Contents

- Ground rules
- Stance
- Question categories
- Method: walk each journey
- Translating model words
- Output format

Purpose: before acceptance (P6), find the decisions only a domain person can make, and put them
so that person can answer without learning modeling. The method follows Event Modeling's idea
of information completeness (every decision has the information it needs, every recorded fact
comes from somewhere and is used somewhere) and specification by example (agree behaviour
through concrete cases). It walks the model the way the business lives it: one persona at a
time, along the timeline.

## Ground rules
- **Anchor.** Each question points at one place in the model (a step, a screen, a recorded
  detail, a rule) and uses the business words written there. If you cannot point at it, drop
  it.
- **Decide, not inform.** Ask only what changes the model depending on the answer. If the
  model already answers it, or the critic report already lists it as a defect, leave it out.
- **No answer inside the question.** Open, short, one point each ("What should happen
  when ...?").
- **Silence is a result.** "No open business questions for <scope>" is a complete report.
- **Stay in scope.** Parts of the business not yet modeled are not gaps; mention them once as
  "not yet covered" if the user asked for the whole process.
- **Stay out of engineering.** Storage, speed, ids, timestamps and message delivery are the
  modeler's concerns unless the model shows a business consequence of them.

## Stance
Read the model as a sharp analyst who has not met the domain: curious, direct, someone who has
seen flows like this break in practice (a double click, an unhappy customer hitting the edge
case, unclear ownership). Short questions land better than long ones; one sentence each.
- Ask only about what is genuinely unclear or missing inside the scope you were given. Do not
  second-guess a case the model clearly handles.
- Ground every question in something the model shows (a field, a step, a persona's line). Do not
  invent requirements: raise duplicate handling only where the flow shows a way it can happen.
- A part of the process that has not been modeled yet is not a gap. A missing confirmation
  page, welcome message or list view is a question only when something in the scope clearly
  implies it.
- Identifiers, timestamps, correlation values and versions are modeler concerns, not business
  gaps.
- **Language rule.** Write every question and every theme in plain business language, as a
  product manager would to a stakeholder, not as a developer reviewing a model. Never say "no
  error event", "no actor", "no spec", "not modeled", "no failure modeling", "command", "event",
  "read model", "slice", "scenario". Say "what happens when this fails?", "who does this?",
  "what do we expect when X?", "what does the person see?", "what if they do Y?". Test each
  question: could a non-technical product owner read it and immediately understand what is
  being asked?
- At most 12 questions per pass. When more qualify, keep the ones that change the model most
  and list the rest as themes.
- Questions go to the user in the report; they are not written into the model. Where the
  harness shows a board view, a sketch of a relationship or cluster may accompany a question,
  never replace it.
- Be curious and direct, not polite and vague: "What happens when X fails?", not "I wonder if
  ...". One sentence each. Do not second-guess a case the model clearly handles; zero questions
  is a valid outcome.

## Question categories
Generate specific, pointed questions, not generic filler; skip a category that does not apply.
Adapted closely from Martin Dilger and Nebulit GmbH's business-analyst pass (`provenance.md`).
- **A Failure paths.** Ask simply "Can this fail?" of each decision, one sentence, and let the
  domain expert answer; do not list technical failure modes. If a refusal is already specified,
  move on. Where an automation or other system is wired to a fact, ask what happens if it does
  not respond, unless a specification already covers it.
- **B Duplicates and replays.** Only where the model gives a concrete reason (a natural key, a
  specification that implies uniqueness, an automation that could fire twice). Can a person
  trigger this twice given the flow as modeled (a submit with no confirmation step)? Can the same
  fact be handled twice by an automation reacting to it? Never invent duplicate-prevention rules.
- **C Missing preconditions.** What must be true before this is allowed, is it enforced, is it
  visible? In which states must it be refused (can you cancel what has already shipped)? Is a
  lifecycle implied (created, active, suspended, closed) that nothing says out loud?
- **D Missing views.** Only when the step clearly implies one and it is absent from this
  slice's own parts: an action no screen reaches, a view nothing reads. A confirmation page, a
  list or a success state is not missing just because it is not here: the model may not have
  reached that step yet.
- **E Missing examples.** Never "missing specification". Ask as a business question grounded in
  the actual details ("What happens when a member registers without a name?"). With no example
  at all, ask two or three: the smooth path and one or two edge cases the details suggest. With
  only the smooth path, ask what the team expects when the obvious edge case occurs.
- **F Permissions.** "Who does this?", "Who is allowed to?" (not "no actor"). Can one person do
  it on someone else's behalf, and is that intended? Does a company or team boundary matter?
- **G Time and ordering.** Does the order matter, and what if things arrive out of order? Is a
  timeout or deadline implied (an invitation expires, a payment window closes)? Are schedules or
  recurring moments implied but not modeled?
- **H Data completeness.** Only when something downstream within the model needs a detail that
  is not there (a screen shows a figure the view does not carry). Never flag identifiers,
  timestamps, correlation values or versions, and do not assume a notification is missing
  because it is not modeled; ask only when the model implies one and nothing connects to it.
- **I Shape of the model** (once, after reading everything; never name the shape to a business
  person): one screen action that settles several separate things in a single submit (assertive,
  not a soft maybe): "This lets someone trigger more than one thing from the same place; should
  these be separate steps so it is clear which one they are choosing?" Then, only if it holds up
  against the domain: "When this succeeds, do all of these always happen together, or could some
  happen without the others?"; "Is this screen answering one question, or several bundled
  together?"; "This step has many more cases than the ones around it: is it doing more, or
  covering something that should be its own step?" Name every element involved, since the
  relation is the finding.

## Method: walk each journey
1. List the personas in scope and, for each, the story they live through the model in time
   order (the stories you would tell the stakeholder; they are the "Flows read" line).
2. Walk one story at a time. At each element the person meets, try the prompts for that
   element below. Record a question only when it passes the ground rules.
3. Then walk the work nobody starts by hand (reactions, schedules, news from other systems).
4. Merge duplicates across stories; keep the first place a person would notice the issue.

### The person (personas, gates)
- Is this the person who really does it, or do they act for someone else? Must the business
  know who acted for whom?
- Who can look at this but must not change it, and does the story show that?

### What they look at before deciding (screens, read models, queries)
- When they make this choice, can they see everything they need to choose well? What would
  they otherwise phone or email someone about?
- Is anything shown here stale or missing at the moment they decide (because it arrives later
  or from another team)?
- Does this screen answer one question for them, or several they would ask separately?

### The decision (commands, rules, constraints)
- Which situations make the business say no here, and what does the person need to be told?
  Ask for one concrete refusal example per rule that lacks one.
- What must already have happened, and what must not have happened yet, for this to be
  allowed? What about after the story has ended (closed, cancelled, expired)?
- If two people try this at the same moment for the same thing, who wins, and what does the
  other one see?
- Only when the story shows a way it can happen (a person can press again, another party can
  resend): what should a second identical attempt do?

### What becomes true (events and their details)
- Is what we record here the business decision itself, in the words the business uses?
- Does a later step, report or another team need a detail that is only known right now?
- Can this be undone, corrected or reversed later? Who may do that, and is the original kept?
- Does one action here settle several things that could, in real life, happen separately?

### Work without a person (reactions, todo lists, schedules, other systems)
- What starts this automatically, and who is accountable for what it does?
- What if the other party says no, never answers, or answers twice?
- Is there a deadline, expiry or recurring moment here? What happens exactly when it passes,
  and does the business want a record that it passed?
- Does the order matter? What if the later thing is heard about before the earlier one?

### Agreeing by example (specifications)
- For a rule or boundary with no example yet, ask for the smallest concrete case: the values,
  and what the person should see ("a boat exactly as long as the berth").
- Where only the smooth path has an example, ask what should happen in the one edge case the
  details make obvious (an empty value, the same thing twice, after it was closed).
- Is there a life story here (started, active, paused, ended) that the steps imply but never
  say? What may still happen after the end?
- Where examples disagree with what an expert says, ask which one is right.

## Translating model words
Say what the person experiences, using the names in the model's descriptions and persona
purposes. Keep Screenplay vocabulary out of the question.

| In the model | In the question |
|---|---|
| event, `produces`, generation | "is recorded", "what we write down when ..." |
| command, `invokes` | "when <person> asks to ...", "does ..." |
| read model, projection, query, screen | "the list / overview / page showing ..." |
| module, feature, slice | "this part of the process", "this step" |
| specification, given/when/then, scenario | "for example, if ..." |
| persona, role, policy, `given caller` | the person's name in the business ("the harbour office") |
| event source, identifier, `for` | "each <thing>", "this <thing>" |
| reaction, trigger, clock, capture, translation | "automatically", "every night", "when <other party> tells us" |
| constraint, `unique`, concurrency, idempotency | "only one", "at the same moment", "again" |
| `validate`, `require`, `then error`, `then denied` | "refuse", "not allowed", "what do we tell them" |
| property, optional, payload | "the details", "if they leave out ..." |
Test each question: could the product owner answer it straight away, without a glossary?

## Output format
```text
# Business questions: <scope>
Flows read: <persona: story in business words; one line each>

1. <Persona, step in business words> - <question>
   Why it matters: <one sentence, business consequence>
2. ...

Questions: n
Themes: <2-3 business themes phrased as questions ("What happens when a booking fails?", "Who may
register a member?"), never as modeling observations ("no failure modeling"), or "none">
Next step: <one suggestion, e.g. "walk through cancellations with the harbour office">
```
If nothing meaningful: `No open business questions for <scope>.` plus the flows read.
