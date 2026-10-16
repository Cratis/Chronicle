<!-- cratis-ai-managed: skills/cratis-arc-command/references/product-sources.md -->
# Product sources

This reference holds the packages and types the command guidance relies on.

| Package | Purpose |
| --- | --- |
| `Cratis.Arc.Core` | `Cratis.Arc.Commands.ModelBound.CommandAttribute`, `CommandResult`, `CommandValidator<T>` |
| `Cratis.Arc.Chronicle` | event append handling, `Cratis.Arc.Chronicle.Commands.NotAuditedAttribute` |
| `Cratis.Arc.ProxyGenerator.Build` | `CratisProxiesOutputPath` MSBuild integration |
| `Cratis.Fundamentals` | `Cratis.Monads.Result<TResult, TError>` |
| `Cratis.Chronicle` | `EventTypeAttribute`, `EventForEventSourceId`, `ICanProvideEventSourceId` |
| `@cratis/arc` | `ICommand`, `CommandResult`, `ValidationResult` |

Fundamentals:
[`Result<TResult, TError>`](https://github.com/Cratis/Fundamentals/blob/main/Source/DotNET/Fundamentals/Monads/Result%7BTResult,TError%7D.cs):
success is first, error second, with implicit conversions from both types.
