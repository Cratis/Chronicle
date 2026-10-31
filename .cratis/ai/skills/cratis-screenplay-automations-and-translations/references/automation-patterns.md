<!-- cratis-ai-managed: skills/cratis-screenplay-automations-and-translations/references/automation-patterns.md -->
# Automation patterns

## Contents

- 1. Deciding whether it is an automation
- 2. Todo list in Screenplay
- 3. Effects: `produces` vs `invokes`
- 4. The actor behind an invoke
- 5. Termination
- 6. Clock and trigger occurrences
- 7. Shapes to question in review
- 8. Chain completion checkpoint
- 9. Fan-out
- 10. Long-running process shapes

Complete compiled example: `todo-list-example.md` (binds as ESM v6; the list query
excerpt binds too, and the retry sweep that needs code is shown as a marked excerpt).

## 1. Deciding whether it is an automation
Write the four parts in one line each:

| Part | Question | Example (`todo-list-example.md`) |
|---|---|---|
| occurrence | what sets it off? | `CoursePassed` |
| state consulted | what must it know? | the pending-certificate queue |
| condition | when does it act? | the item is still pending |
| effect | which command or fact results? | `SendCertificate` -> `CertificateSent` |

Outcomes:
- Unconditional, consults nothing, cannot fail, nothing outside: **co-production**. Add a
  second `produces` to the command (one decision may record several recognised facts).
- An internal follow-up decision that may reject: reaction `invokes` a command.
- Work that waits, can fail, is retried or must be visible: **todo list** + reaction.
- A recurring check: clock reaction (`every` / `at`), usually over a todo list.
- A signal only an integration knows: declare a `trigger` and react `when <Trigger>`; specify
  it with `when trigger`. A UI click is an interaction, not a trigger.

## 2. Todo list in Screenplay
- A `readmodel` for one pending item: the identity plus what the worker needs to act. **No
  status field**: being in the list is the state. If another consumer needs history, that is a
  different read model.
- Every pending-item field names its origin (event property or key), source and target type, and
  cardinality (single value or list). A transformation the language cannot express is a recorded
  gap with the intended behavior, never an invented mapping. The same holds for every reaction input.
- A projection that adds an item `from <OpeningFact>` and removes it with
  `remove with <ClosingFact>`. Several facts may open or close; they need not match one to one.
- A keyed query (`…ById => Item optional` + `by itemId`) for specs and executable scope; a
  list query for the visible queue (`=> Item[]` binds on Screenplay 4.105.0 or later; a snapshot list renders with Stage 4.90.0 or later; older renderers refuse STAGE-ESM-010).
- A reaction `when <OpeningFact>` that invokes the command producing the closing fact.
  The trigger may `reads` the item view to document its input; that is not protection.
- A retry sweep on a clock reads the whole view (no `by` on clock reads). Iterating its items
  declaratively (`for each <View>`) is reserved syntax; the sweep needs a code body,
  which the reference never runs. Durable fan-out is outside the language (Screenplay#286).
- Specify the list as a small story: empty -> item present (`then query …ById`) -> closed
  (`when append <Closing>` + `then no readmodel … for`), plus "not opened" for facts that
  must not queue work.

When a todo list is not needed: the effect is immediate, internal and cannot fail (a direct
`produces` or co-production), and nobody needs to see pending work. Say why in the slice
`documentation`; do not add a list just to follow a pattern.

## 3. Effects: `produces` vs `invokes`
| | `produces <Fact>` | `invokes <Command>` |
|---|---|---|
| meaning | records a fact | asks for a decision |
| can it be refused? | only by append-time constraints | authorization, validation, `require`, constraints |
| destination | `for` value; without `for`, the trigger fact's source | the command's identifier |
| with trigger `reads` | fails binding (unprotected decision) | allowed; protection is the command's job |
| caller | n/a | declared reaction `runs as` actor (ESM v10), otherwise none |

## 4. The actor behind an invoke
The actor is declared with reaction-level `runs as system role
"<Role>"` (ESM v10); without it a gated invocation has no caller and denies.
`given caller` is not an invocation actor. Role requirements remain enforced;
claim conditions for this system principal remain unknown. A compiler that stops before v10
rejects the syntax (PLAY0137). The CLI/Stage pair (toolchain
`references/renderable-subset.md`) renders role/authenticated reaction identity; claim-reading/opaque gates retain STAGE-ESM-034.
A renderer that stops before v10 refuses it at its version gate.
Use the checked model in captures-and-reactions `references/invocation-identity.md`.
Arc gap-fill uses `[ExecuteCommandsAsSystem("<role>")]` for returned commands,
not identity inferred from prose or an imperative pipeline call.

Decide and record one of:
1. **Internal ungated command** - only if the security design supports it: not reachable by
   outside callers in the target (verify; the model cannot show exposure), safe to repeat
   (guarded by `unique event`), and touching one stream. State the reasons in module
   `documentation` (the historical `todo-list-example.md` used a description) and list "not externally exposed" as a target
   requirement in `STATE.md`.
2. **Direct `produces`** - when no decision is left (the fact is already decided) and nothing
   should refuse it.
3. **Gated command + declared trusted actor** - keep the service-role policy and
   declare matching `runs as system role` on the reaction. Reference-test the
   scenario with a standalone compiler that admits v10; separately record CLI/Stage target refusal.
   A missing actor must not be excused by removing the gate or inventing a caller.
Never remove an existing gate only to make a reaction spec pass.
Omitting an HTTP route is not an authorization proof: the exposure requirement stays a target
requirement with its negative case (an outside caller is refused).

## 5. Termination
For every reaction, name the fact that ends the work and check the graph: no reaction is set
off by its own result (directly or via another reaction) unless a condition stops it. The
reference evaluator caps a scenario at 1,000 new facts and a clock advance at 10,000
occurrences; hitting that is a modeling defect, not a limit to tune.

## 6. Clock and trigger occurrences
- `every N minutes|hours|days`, `at HH:mm`, `at HH:mm on <Weekday>`, `at HH:mm on day N`.
- Occurrences carry no event source: every `produces` needs `for` (reactions cannot declare inline events) (a literal such as
  `"weekly"` for a singleton stream, or a carried value).
- `where` must be resolvable from each trigger's values; a clock has none, so a clock trigger
  in a reaction with `where` fails binding. Give the sweep its own reaction.
- Specify with `given clock` + `when clock` (state both); `$context.occurred` takes the clock
  instant. A clock-produced fact example with its spec: `cratis-screenplay-toolchain`
  `references/cheat-sheet-example.md` (`Reminder` reaction).

## 7. Shapes to question in review
- A relay that only renames an already-recorded fact: ask what local decision, obligation or
  effect it adds; otherwise project from the first fact.
- A reaction triggered directly by another context's event that also makes our decision:
  establish what the outside fact means and what we trust (translate first), then decide.
- An automation with no visible pending state where failures are likely.
- A gated command invoked by a reaction with no recorded actor decision.
- A reaction that `reads` and `produces`.

**Redundant second stage.** The tell is a follow-up fact with the same identity, no new field, no
invariant checked and no choice made. Example: an external harbor system sends `BerthAssigned`
already carrying `berthId`, `boatId` and `memberId`; the berth was chosen upstream. Translating it
into our own `BerthAssigned` is required: an outside fact must not drive a local domain decision
until it is translated and accepted (a translator reaction over an imported external event is
supported). Adding a
second queue, an `ApplyBerth` automation and a `BerthOccupied` fact on top restates the same fact
under another name. Project the "occupied berths" view from our translated fact instead. A
`...Synced` name is itself the tell. Equal payloads alone do not prove equal responsibility: keep
a worker when it adds a local obligation or effect, such as notifying the member.

## 8. Chain completion checkpoint
Walk each affected chain from occurrence to terminal outcome.

For each queue:
- Identify one work item and its key. If an entity can need several independent jobs, its id
  alone may not identify the work.
- List every opening and closing cause, including cancellation and supersession; opening and
  closing keys must address the same item.
- Trace every worker input to its source. A view describing work is not an atomic claim and
  does not authorize an external effect.
- Decide what stays visible after rejection, timeout or manual intervention.
- Specify each lifecycle path: removal of the right item, unaffected items, and later
  occurrences that legitimately create new work.

A translation queue, when needed, has the same completion duty: do not keep processed
records forever while calling the view pending work.

For a proposed worker after translation ask: what must our side still decide or perform;
which local authority or obligation makes it ours; what observable outcome vanishes if it is
removed? No distinct outcome means a projection of the translated fact. Field equality is not
proof that two systems own the same decision.

Naming: no command or event is named for a form (`Update/Edit/Save/Set/Change<Noun>`,
`<Noun>Updated/Changed/Saved`); a correction names what it corrects. Separately, a relay or
an event whose payload restates an earlier event's fields (`…Synced`) is a review signal: it
is major only when business meaning is lost, a rule is bypassed or consumers become
ambiguous. Historical event generations are exempt.

## 9. Fan-out
Record one outcome fact per item. Never record a batch completion fact while some items
failed or are unknown; partial success is failure at an effect boundary
(`cratis-engineering-effect-boundaries`).

## 10. Long-running process shapes
A durable workflow engine does not exist in the language. A process that waits, escalates,
compensates or fans out is modeled as chained Automation slices over facts and pending-work
views, never as a hidden engine. Start from the signal, take the shape, then check the shape's
specifications.

| Signal in the process | Screenplay shape |
|---|---|
| One trigger, one command | One Automation slice (§1) |
| Trigger, command, wait for a fact, command | Two Automation slices chained by facts, each with its pending-work view |
| Wait for human approval or outside confirmation | A pending view the person's screen reads, with the approval command on it; a clock reaction over the same view escalates on timeout |
| Compensate when step N fails | A failure fact and a compensating command for every completed step, each with its own view (`effects-and-idempotency.md`, option 2) |
| Timer or delay | A clock reaction over a due-items view (§6) |
| Fan out, then wait for all | One fact per item plus a completion view that counts outstanding items (§9). Durable fan-out is outside the language: say so in the verdict |
| Parent and child | The child has its own stream and completion fact; the parent's view waits for it |
| Scatter and gather | One translation per reply into a fact, and a view that decides when enough replies have arrived |

One trigger and one command is a plain automation. Anything else in the table is a process:
every step has a recorded fact, so a restart resumes from state and never from memory.
Specify two paths for each process shape: the timeout path (nothing arrives in time) and the
compensation order (undoing completed steps in reverse). A shape with no timeout path can wait
forever, which is a completion defect (§8).
