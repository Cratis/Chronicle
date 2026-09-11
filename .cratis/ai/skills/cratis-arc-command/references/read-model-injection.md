<!-- cratis-ai-managed: skills/cratis-arc-command/references/read-model-injection.md -->
# Injecting a read model into a command

Verified against `Cratis.Arc.Core` `22.10.4` and `Cratis.Chronicle` `16.39.1`.

A read model can be injected into a `CommandValidator<T>` constructor, into
`Provide()`, or into `Handle()`. Arc resolves it through an
`ICanResolveReadModelForCommand` provider, **by the command's resolved key** —
not by the read-model type, and not by "the property that looks like its key".

If the instance you need is keyed by anything else, direct injection hands you
the wrong instance or nothing at all. That is a correctness bug, not a compile
error. The three shapes it takes:

1. you need a *referenced other* entity — act on A, check B;
2. the command has more than one candidate identity property, so resolution is
   ambiguous;
3. the id is generated inside `Handle()` and does not exist yet.

In all three, read by explicit key in `Provide()`:

```csharp
var other = await readModels.GetInstanceById<<Other>>((EventSourceId)<OtherId>);
```

`ReadModelKey` converts implicitly from `EventSourceId`, `Guid` and `string`, so
the cast is all that is needed.

## Nullability is the required/optional switch

| Parameter | No instance for the command's key |
| --- | --- |
| `TReadModel?` | `null` is injected — guard with `is null` |
| `TReadModel` | Rejected, not crashed: a **registered** read model with a usable resolved key raises `ReadModelDoesNotExistForCommand`, an `IValidationFailure` that surfaces as a validation failure (HTTP 400) with reason `DependencyUnavailable` and a message that does not name the type |

The server-fault path is only reached when the dependency is *not* a registered
read model, or when no usable key was resolved. Then it is
`CannotResolveCommandDependency` for a handler parameter and
`CannotResolveValidatorDependency` for a validator constructor parameter — both
of whose messages tell you to declare the parameter nullable or to inject
`IReadModels` and check existence explicitly.

A command that carries no usable key at all fails inside the resolver with
`UnableToResolveReadModelFromCommandContext`, which is also a client-input
failure rather than a server fault.

`ARC0006` warns when a read-model parameter is non-nullable, precisely because a
command-scoped read model can be missing. Make it nullable when absence is part
of the command's valid behavior; keep it non-nullable when absence really is a
rejection.

## `null` is not always what absence looks like

Chronicle's `IReadModels.GetInstanceById<T>` is declared as a non-nullable
`Task<TReadModel>` yet hands back `default!` when nothing exists, so the compiler
gives callers no signal at all.

| Backing | Never created | Removed by a `[RemovedWith<T>]` event |
| --- | --- | --- |
| Materialized projection or reducer (the default) | `null` | `null` |
| `[Passive]` **projection** | ⚠️ a **default-valued instance**, never `null` | `null` |
| `[Passive]` **reducer** | `null` | `null` |

`[RemovedWith<T>]` is not the axis — a removed instance is `null` on every
backing. The single non-`null` case is a `[Passive]` projection that was never
created: Chronicle computes it on demand and seeds the initial state from the
read model's schema, filling every non-nullable value property with its type
default. Neither the `is null` guard nor the non-nullable "must exist" switch
fires there.

⚠️ This is invisible when a status enum's `0` is a real state — absent becomes
byte-identical to freshly created, so a command reads never-invited as already
invited. Renumbering the enum from `1` is **not** the fix; the value is then
dropped from the payload and deserializes back to CLR `0` on the client anyway.
Carry an explicit existence flag instead: one
`[SetValue<TFirstEvent>(true)] bool Exists` per event that can be the first for
the stream, and check that rather than nullability.

⚠️ A specification suite will not catch this by itself. The command scenario
harness answers `null` for an unseeded event-source id, which matches production
for every row above *except* the passive projection. Cover the absent case by
seeding a default-valued instance explicitly, not only `null`.

Write your own by-id accessors as `Task<T?>` so callers get the compiler signal
Chronicle's own signature withholds.

## Which provider owns a read model type

More than one provider can resolve the same read model type, and the application
does not control registration order. `ReadModelForCommandOwnership` decides,
not order:

- `Declared` — something in the application says the provider owns the type: a
  Chronicle projection or reducer targeting it, or a `DbSet<T>` on a read-model
  `DbContext`. A declaring provider claims the type even when something else
  already resolves it.
- `Fallback` — the provider *can* resolve anything it reports, but nothing says
  it owns them. It claims only the types nothing else already resolves.
