<!-- cratis-ai-managed: skills/cratis-screenplay-projections/references/secret-concepts.md -->
# Secret concepts in read models (`PLAY1142`)

Read this when a projection or reducer builds a read model from events that carry a `secret` (or `pii secret`) concept.
Requires Screenplay 6.28.0 or later (`screenplay --version`).

A `secret` value belongs on the event, not in a read model. Copying it into a read-model property makes it part of the read model and of every query result over it, so the compiler warns with `PLAY1142` (`SecretConceptProjected`).

## What is reported

- An explicit mapping whose target property or source event property has a secret concept.
- A property that AutoMap fills from a same-named secret event property, including `children` and `nested` scopes that inherit AutoMap from the root.
- A reducer that builds a read-model property with a secret concept.

Metadata, literal and aggregate sources are not reported. The message names the projection or reducer, the concept and the read-model property path.

## Fix

There is no automatic repair; decide whether the read model needs the value at all.

- Leave the property out of the read model (and use `no automap` or a differently named property where AutoMap would fill it).
- Or map a non-secret derived value, for example a flag that the value is set, or a masked form computed elsewhere.

The code is a warning, so `--warnings-as-errors` turns it into a failure. See the toolchain diagnostics reference for the full warning list.
