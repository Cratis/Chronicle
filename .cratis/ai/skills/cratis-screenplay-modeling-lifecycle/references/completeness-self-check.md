<!-- cratis-ai-managed: skills/cratis-screenplay-modeling-lifecycle/references/completeness-self-check.md -->
# Do not cut corners to save tokens or effort

A correct model is the point of the workflow. It is not optional scope that can be traded away
when a step gets expensive or a rule turns out to be inconvenient. If satisfying a rule needs
more slices, events, read models, translation steps or specifications than expected, that is a
signal to write them, not to remove the requirement or invent an exemption. Budget and token cost
are never a valid reason to:

- **Delete a modeled reaction, event or read model** to avoid a conflict with another rule. Solve
  the conflict (split the stream, add the missing event, re-slice) instead.
- **Label a missing piece as "accepted debt" or "session context"** when no rule exempts it. Only
  a genuinely blank creation form needs no read model; every other screen that shows data needs a
  real view, even a small identity or lookup one.
- **Collapse translation and worker behavior into one step.** Outside data enters our model by
  either path: a `capture` that records it as our own event, or a translator reaction over an
  imported external event (`vocabulary-map.md`). Translating into our facts and the later work
  that acts on those facts are separate things; do not merge them, or skip the translation, because
  one step is simpler.
- **Drop protection or a refusal** (`authorize`, `pii`, a `validate` rule, a `then denied` or
  `then error` specification) so a tool passes or the model looks smaller.
- **Cover a command with "one success plus one failure"** when the domain has competing claims,
  branches, removals, retries or denials.
- **Use one generic `XUpdated` event** because separate past-tense facts are more writing.

## Self-catch prompt (ask it before every gate)
If you catch yourself reasoning "this would need N more slices, specifications or read models, let me
simplify instead", that is exactly the moment to stop and write the correct, larger version. A
model with more declarations that is right beats a smaller one that skips required elements.
Ask: "am I leaving something out because it is expensive, not because the domain says so?"

## Boundaries
- Completeness applies inside the scope you were given; it never licenses widening it. Gaps seen
  outside the scope go in STATE.md or the packet.
- Flag a genuine scope trade-off to the user explicitly. Never resolve it alone by cutting the
  model, and never hide it behind a stated assumption.
- Reduction is legitimate only with a domain reason written next to the declaration ("n/a:
  berths are never un-assigned in this process"), never an effort reason.
