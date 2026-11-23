<!-- cratis-ai-managed: skills/cratis-arc-command/references/read-model-injection.md -->
# Injecting a read model into a command

## Contents

- Choose the key
- Nullability is the required/optional switch
- `null` is not always what absence looks like
- Which provider owns a read model type

Source: [read models in commands](https://github.com/Cratis/Arc/blob/main/Documentation/backend/csharp/chronicle/read-models/injecting-into-commands.md).
Under read protection `Off` (Arc 22 default; on Arc 23 only an explicit, reasoned opt-out) and `Measure`, injected models and keyed
reads are plain reads, not concurrency guards. Under `On`, the Arc 23 default, a read-model parameter of
`Handle()` or a validator constructor is guarded; `IReadModels.GetInstanceById` in a
command is refused, so read another entity's model with a second parameter and
`[LookUpBy(nameof(Cmd.Prop))]`. See [decisions and routing](decisions-and-routing.md)
and, for per-command opt-in, `[ProtectedDecision]` with `DecisionRead<T>`.

A read model can be injected into a `CommandValidator<T>` constructor, into
`Provide()`, or into `Handle()`. Arc resolves it through an
`ICanResolveReadModelForCommand` provider using the selected lookup key. An
explicit command-property or caller-provider lookup is not limited to protected
commands. Without one, the default depends on the read-protection mode.

Choose the key explicitly when the default is wrong. Three common cases:

1. you need a *referenced other* entity — act on A, check B;
2. the command has more than one candidate identity property, so resolution is
   ambiguous;
3. the id is generated inside `Handle()` and does not exist yet.

Explicit lookup works in every read-protection mode. Choosing a key does not
make a plain read protected. For a key computed in `Provide()`, use
`IDecisionReads.Get` when it must guard the append; a direct by-id read under
`Off` remains unprotected.

## Choose the key

Use one explicit form on a read-model `Handle()` parameter (`Cratis.Arc.Commands`):

- `[LookUpBy(nameof(CommandName.KeyProperty))]` selects a command property.
- `[LookUpBy<CallerSubject>]`, a `CallerClaim` subclass or another
  `IReadModelKeyProvider` selects a caller-derived key. See
  [command identity](../../cratis-arc-authentication-authorization-and-identity/references/command-identity.md)
  for exact claims, immutable caller capture, denial, actor injection and audit.

Do not combine property and provider forms or specify several providers. Without
an explicit lookup, `On` uses the single command property whose type exactly
matches the model's `Id` type; several matches raise `AmbiguousReadModelLookup`;
no match falls back to the command event source id. `Off` and `Measure` retain
the legacy event-source-id default. A caller provider cannot redirect a
per-stream model away from the command route.

Ordinary `Provide()` values assignable to a `Handle()` parameter are used before
service/read-model resolution. For a caller-provider read-model parameter, a
supplied matching value is refused with `InvalidLookUpBy`, not bypassed: do not
try to replace the identity-keyed model with a provided value.

Requires Arc 22.93.0 for ARCCHR0023 (no unique key-type match), ARCCHR0025
(keyless command reads a newly generated id and finds no model) and ARCCHR0026
(command or validator reads Chronicle state through `IMongoCollection<T>`).
These diagnostics expose lookup mistakes; they do not change plain-read protection.
Check the package version, and inspect keys explicitly on older versions.

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
command-scoped read model can be missing (it does not fire for model-bound
projections). Make it nullable when absence is part
of the command's valid behavior; keep it non-nullable when absence really is a
rejection. When absence is a rejection, check the nullable parameter and return
`ValidationResult.Error(...)` through `Result<TEvent, ValidationResult>`; never
return a nullable event or `null` from `Handle()`, which Arc treats as success
with nothing appended.

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

Requires Chronicle 19.53.3 for nullable string/bool concepts in read-model schemas
to stay nullable, including unset passive properties. Check the client version;
older schemas can turn these into defaults. Event-type schemas are unaffected,
and this does not turn an absent passive projection into `null`.

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
