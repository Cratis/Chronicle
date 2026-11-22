<!-- cratis-ai-managed: skills/cratis-arc-authentication-authorization-and-identity/references/product-sources.md -->
# Product sources

This reference holds the packages and types Arc identity and authorization guidance relies on.

| Package | Purpose |
| --- | --- |
| `Cratis.Arc.Core` | `Cratis.Arc.Identity`, `Cratis.Arc.Authorization`, `Cratis.Arc.Authentication`, `Cratis.Arc.Tenancy` |
| `Cratis.Arc` | ASP.NET Core hosting, `AddMicrosoftIdentityPlatformIdentityAuthentication` |
| `@cratis/arc` | `IdentityProvider`, `IIdentity` |
| `@cratis/arc.react` | `IdentityProvider` component, `useIdentity`, `RequireRole` |
| Cratis AuthProxy (`cratis/authproxy`) | the trusted ingress described in [AuthProxy](authproxy.md) |

## Command identity and audit evidence

Verified against Arc `origin/main` (`d3ecc873b`) and Chronicle `origin/main`
(`ed97fe729a`). These sources own the contracts in
[command identity](command-identity.md); check your installed version before using
caller providers. No minimum release is asserted here.

| Source | Contract verified |
| --- | --- |
| Arc `Source/DotNET/Arc.Core/Authorization/ICurrentPrincipalAccessor.cs`, `CurrentPrincipalAccessor.cs`, `AuthorizationCommandIdentity.cs` | Selected principal; AsyncLocal, execution-context and HTTP access; immutable exact-claim caller snapshot without actor-chain claims |
| Arc `Source/DotNET/Arc.Core/DependencyInjection/IAsyncParameterDependencyResolver.cs`, `ICanResolveParameterDependency.cs`, `ParameterDependencyResolution.cs`, `ParameterDependencyResolver.cs` | Async and synchronous resolver members, resolved-null versus not-resolved, awaited binding |
| Arc `Source/DotNET/Chronicle/ReadModels/ChronicleParameterDependencyResolvers.cs`, `ReadModelServiceCollectionExtensions.cs` | Chronicle-first composition, scoped application resolver discovery through `IInstancesOf<ICanResolveParameterDependency>` |
| Arc `Source/DotNET/Arc.Core/Commands/CommandProvideInvoker.cs`, `CommandHandlerArgumentResolver.cs`, `ModelBound/ModelBoundCommandHandler.cs` | `Provide()` parameter binding; ordinary provided-value precedence; a supplied matching value for a caller-provider read-model parameter is refused with `InvalidLookUpBy` |
| Arc `Source/DotNET/Arc.Core/Authorization/AuthorizationResult.cs`; `Source/DotNET/Arc.Core/Commands/CommandPipeline.cs` | Failed `AuthorizationResult` from `Provide()` stops handler execution; key-resolution denial maps to unauthorized without exception details |
| Arc `Source/DotNET/Arc.Core/Commands/LookUpByAttribute.cs`, `IReadModelKeyProvider.cs`, `ReadModelKeyContext.cs`, `ReadModelKeyLookup.cs`, `CallerClaim.cs`, `CallerSubject.cs`, `ReadModelKeyProviders.cs`, `ReadModelKeyResolutionDenied.cs`, `CommandAttributePolicy.cs`; `Source/DotNET/Arc.Core/Authorization/CommandCaller.cs` | Both lookup forms, parameterless synchronous providers, exact claims, missing/ambiguous denial and caller consistency |
| Arc `Source/DotNET/Chronicle/ReadModels/CommandReadModelLookup.cs`, `ChronicleParameterDependencyResolver.cs` | Provider lookup precedes read protection; guarded and plain keyed loading |
| Arc `Source/DotNET/Chronicle.Specs/ReadModels/for_per_stream_models/when_a_key_provider_redirects_the_stream.cs` | A caller provider cannot redirect a per-stream read away from the command stream |
| Arc `Source/DotNET/Chronicle/Identities/ArcIdentityProvider.cs` | Selected Chronicle identity preservation, non-HTTP principal attribution, actor-first `OnBehalfOf` chain |
| Chronicle `Source/Clients/DotNET/Identities/IIdentityProvider.cs`, `Identity.cs`, `IdentityConverters.cs`; `Source/Clients/AspNetCore/Identities/IdentityProvider.cs` | Current/explicit identity, exact ASP.NET claim mapping, System identity and actor chain |
| Chronicle `Source/Clients/DotNET/EventSequences/EventSequence.cs`, `Events/EventContext.cs` | Append preparation obtains `CausedBy` from `GetCurrent()`; event context exposes attribution |

`CurrentActor`, `ActorSubject`, `CallerPreferences`, `CallerAccount` and
`FeatureEnabled` in the example are application-owned, not
product types. `ConceptAs<string>` follows Chronicle's
`Source/Clients/DotNET/EventStoreName.cs` and `GlobalUsings.cs`.
