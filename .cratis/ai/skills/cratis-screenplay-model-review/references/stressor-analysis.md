<!-- cratis-ai-managed: skills/cratis-screenplay-model-review/references/stressor-analysis.md -->
# Stressor analysis (optional resilience pass)

The critic review asks whether the model is correct and complete. This pass asks how the model
behaves when the world changes: an outside partner disappears, a rule of law changes, a person
leaves. Run it on request, or for business-critical scope, after the critic review and before
acceptance. It reads the model and proposes edit requests; it never edits. The method is
residuality theory (Barry O'Reilly), applied to event models in the Eventmodelers work of
Martin Dilger and Nebulit GmbH, which this reference adapts closely (`provenance.md`).

The aim is a model whose every residue is acceptable to the business. A design shaped by many
unrelated stressors tends to survive ones nobody listed, which is why variety matters more than
likelihood.

## Contents

- Procedure
- Incidence matrix
- Redesign proposals
- Output

## Procedure

1. **Scope, read once.** One feature or module at a fixed source revision. Inventory its slices,
   commands, events, read models, reactions, captures and personas from the `.play` files or the
   MCP views, never from memory.
2. **Stressors.** About fifteen, deliberately wide, in business words and without mechanics. Draw
   at least a third from outside the model (an office flooded, a new privacy law, the company
   acquired, the key administrator leaves, a competitor goes free, the main customer goes
   bankrupt) and the rest from the domain's own business. Alternate the two. One stressor per item,
   eight words or fewer, no sub-lists, no field names, races or API mechanics. Their value is
   variety, not likelihood.
3. **Residue per stressor.** One sentence each: which slices still work, which facts are still
   recorded, which views go stale or wrong, who is not told. A residue is observed, not
   designed. A fix is not a residue. Name slices and declarations, never fields.
4. **More stressors.** Before deciding, the person may ask for more. Keep every earlier stressor
   with its number and residue and add about ten new ones aimed at the categories and declarations
   the earlier list hit least (nature, law, people, market, data). Never replace the list.
5. **The person decides** which residues are unacceptable, because that is a business decision.
   Unattended, record each as a question with an assumed default (lifecycle
   `references/stop-or-assume.md`) and say so in the report. Deciding is the go-ahead for the
   matrix: with fewer than five unacceptable residues, build it anyway and say the coupling it
   shows is still thin.
6. **Incidence matrix** (below). Name the shared cause of every cluster.
7. **Redesign proposals** for one cluster at a time (below). State the new residue for each
   stressor.
8. **Validate with fresh stressors** not used before. Report the share of acceptable residues
   before and after. Stop when new stressors stop hitting declarations that were not hit before.

Do not invent requirements. A stressor is a question about the business, answered by the person,
not a feature to add.

## Incidence matrix

A Markdown table in the review report: one row per declaration a stressor hits, one column per
unacceptable stressor, sorted by how many stressors hit the row. Over ten stressor columns, use one "Hit by" column
(`S1, S4, S9`) and give the legend under the table. Declarations hit by the same
stressors are hidden coupling: name the shared cause (one outside system, one persona, one
clock). A cluster that crosses modules is boundary evidence for
`cratis-screenplay-streams-and-consistency`.

| Declaration | Partner outage | Law change | Key person leaves | Hits |
|---|---|---|---|---|
| `ConfirmPayment` | x | x | | 2 |
| `PaymentStatus` | x | | x | 2 |

## Redesign proposals

Each proposal is an edit request, written like every other review fix (address, change,
reason). Typical shapes, most of them Translate or Automation slices
(`cratis-screenplay-automations-and-translations`):

- an outside dependency: a `capture` and a Translate slice, so outside facts arrive as our own
  events instead of being read live;
- reconciliation: a todo-list view plus a `reaction`, so work that could not finish is visible
  and retried;
- a grace period or expiry: an event raised on a clock trigger;
- a blocked actor: a second command origin or persona;
- an undo: a compensating command.

Persisted events and accepted slices are not changed. New slices go beside them, and identity
ownership follows the lifecycle (`cratis-screenplay-modeling-lifecycle`).

## Output

The matrix plus the edit requests, in the review report. Every finding is pinned to a
declaration like any other finding.
