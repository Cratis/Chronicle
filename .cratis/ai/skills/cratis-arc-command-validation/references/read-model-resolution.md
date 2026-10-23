<!-- cratis-ai-managed: skills/cratis-arc-command-validation/references/read-model-resolution.md -->
<!-- Copyright (c) Cratis. All rights reserved. -->
<!-- Licensed under the MIT license. See LICENSE file in the project root for full license information. -->

# Read-model resolution for validation

Resolution does not key off `[ReadModel]`: a model becomes injectable because a
Chronicle projection or reducer, an EF Core `DbSet<T>`, or a MongoDB collection
backs it. Registration is global by type; keeping a model in the slice is a
convention, not a resolver restriction.

The default lookup uses the command's resolved event-source id. It does not
infer a key from the read-model type or an arbitrary similarly named property.
Use `[LookUpBy(nameof(Command.Property))]` for a command-property key; for a
computed key use `IDecisionReads.Get` in `Provide()` under read protection `On`.
An ordinary `IReadModels.GetInstanceById` there is refused under `On`; outside
protected work it is an advisory read, not a consistency guard.

A missing instance resolves to `null`, not a default object. A nullable parameter
receives that null; a non-nullable one produces a validation failure (400).
`ARC0006` warns on a non-nullable command-scoped read-model parameter. Reject a
missing nullable model through a validator, `Provide()` or
`Result<TEvent, ValidationResult>`; do not return a nullable event.

Read **cratis-arc-command** for the full resolution and nullability reference,
including passive projections where absence is not null.
