<!-- cratis-ai-managed: skills/cratis-screenplay-reverse-engineering/references/scoping.md -->
# Scoping a reverse engineering run

A system rarely needs to be modeled whole. The user names the key areas they want (for example,
from a CRM: contacts, companies and the deal pipeline, not marketing email or invoicing) and only
those become the model. Scope is decided at intake, applied to the inventory, and carried through
the walk, synthesis and fidelity check.

## Contents

- Scope statement
- Tagging the inventory
- Dependency closure
- Boundary modeling
- Walk, probes and evidence
- Growing the model
- Changing scope later

## Scope statement

Record one statement at intake: the target capabilities that are **in**, and what is explicitly
**out**. Reuse the user's words.

If the user gave no scope, do not start modeling or walking deep. Build the L0 feature inventory
first (a cheap map of everything visible), propose a scope from it, and ask once. That question
counts toward the intake question budget. With no answer, default to the smallest coherent core:
the feature the user named or the entry flow, plus its dependency closure.

## Tagging the inventory

Tag every L0 feature **in**, **out** or **boundary** (an out-of-scope feature that an in-scope one
refers to). List out-of-scope features by name and source, then stop: never walk, probe or model
them. An out-of-scope entry costs one line.

## Dependency closure

When an in-scope feature needs another (a deal needs a company), pull in the minimum: its identity
plus the fields the in-scope feature displays or uses. Record why it was pulled in, citing the
in-scope feature. Nothing of its rules, commands or lifecycle comes with it; if more is needed,
the user widens the scope.

## Boundary modeling

A reference to an out-of-scope thing becomes either:

- a typed reference id (a concept), the default; or
- a minimal lookup read model fed by a minimal command or event, when the in-scope feature
  displays fields of it.

Mark each with a comment so it can be found and replaced:

```
// boundary: out of scope (invoicing)
```

Never model out-of-scope behavior or invariants, even when the capture shows them.

## Walk, probes and evidence

The UI walk and the probes of
[probing-and-naming.md](probing-and-naming.md) cover in-scope features and their boundary
only. Spend the walk budget there. Evidence rows that fall outside scope are dropped, or kept
with a `scope: out` tag when cheap to keep; they never feed synthesis.

## Growing the model

Organize the model so scope can grow: one feature area per file or module, with boundary
references isolated at its edge. Adding an area then adds files instead of reshaping existing ones.

Record the scope (in, out, boundary and closure reasons) in the provenance or synthesis notes.
The fidelity check scores in-scope journeys only and lists the out-of-scope features as not
covered; do not report them as failed.

## Flow by flow

Break each in-scope feature into flows: one entry point to its persisted or visible effect.
Take them one at a time, each small enough for a person to read in one sitting and say "right"
or "wrong" before the next starts; a flow the reviewer cannot judge that way is split.

## Changing scope later

Narrowing removes the area's slices (a keep / change / remove decision applies) and turns
remaining references into boundary references. Widening re-runs intake for the added area only:
inventory tags, closure, walk, probes and synthesis. Do not re-walk areas whose sources are
unchanged, and keep evidence ids stable. Replace a boundary lookup with the real slice when its
area is added.
