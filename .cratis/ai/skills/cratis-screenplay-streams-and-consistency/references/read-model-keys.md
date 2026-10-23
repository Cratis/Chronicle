<!-- cratis-ai-managed: skills/cratis-screenplay-streams-and-consistency/references/read-model-keys.md -->
<!-- Copyright (c) Cratis. All rights reserved. -->
<!-- Licensed under the MIT license. See LICENSE file in the project root for full license information. -->

# Read-model keys: one id, flat parts or a composite-typed key

Requires Screenplay 4.204.0 (ESM v27; `screenplay --version`) for a read model with two or
more `key` marks; older binders refuse it with PLAY0268. A read model with one `key` mark
keeps its earlier version and bytes.

A read model's key says which instance a lookup or projection reaches. It is not a stream
identity and not an atomic decision point: choose it after the streams, and keep invariants
in constraints and commands.

## Choose the shape

| Business shape | Key | Why |
| --- | --- | --- |
| The view is one entity's state | One natural `key` property (default) | Binds on every supported version, and the event source usually supplies it |
| Independent coordinates identify the view (resource and period, plant and field), read by filtering on some parts or by all parts | Several flat `key` properties (v27) | Each part keeps its name and type; a list narrows by any part with equality filters |
| One value object already identifies the entity everywhere (it is also a command or event value) | One `key` property whose type is a composite `type` | Executes on earlier versions; projections use a typed `key TypeName` block |

A read model has exactly one of the two composite shapes: mixing flat parts with a
composite-typed key is PLAY0660, though one application may hold read models of both.
Do not concatenate parts into a string id to avoid a composite key, and do not invent a
composite type only to carry flat parts: both hide the identity the model should state.

## What flat parts do and do not do

- **Equality is per part**, in any order. Uuid parts compare as ordinal text with no case
  folding, so write one spelling of each uuid in events, fixtures and query arguments.
- **A projection names every part** in an untyped `key` block on each root `from` and
  `remove with`. The parts come from the event payload; they need not match the event
  source. `remove with` deletes one instance and a later `from` recreates it.
- **A composite key does not enforce uniqueness.** Two events can still claim the same
  part values; use a `unique` constraint over the same properties when the store must
  refuse the duplicate (`references/consistency-and-concurrency.md`).
- **Lookups.** An optional single-instance query names every part in a `by` block; a list
  uses equality filters. A command or reaction that must read the view by parts is refused
  (PLAY0271, PLAY0781 reason `by-parts`, or PLAY0268 for a producing reaction), so decide on a stream
  the command owns instead, or restructure the read. Do not drop the read to bind.
- **Limits.** Joins, `children`, `nested`, `every`, `all`, `variant` and reducers on the
  composite-keyed view are PLAY0268. Stage 4.137.0 admits v27 but the renderer refuses it (STAGE-ESM-048 for several key parts or a projection keyed by parts,
  STAGE-ESM-047 for a single-instance query by parts), so deliver it through gap-fill with the model as the contract.

Read read-surface `references/named-keys.md` for one complete executable example and the
projections skill `references/pdl-grammar.md` for the key block syntax.
