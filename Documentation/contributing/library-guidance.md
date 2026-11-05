---
title: Library guidance
description: Review Chronicle contributions against Microsoft's .NET library, options, and logging guidance, with scoped evidence and domain exception naming conventions.
---

Use this checklist when reviewing a Chronicle library contribution. It connects Microsoft's guidance to specific .NET client and kernel surfaces; it is not a claim that every Chronicle package, API, or platform conforms. Source paths below are relative to the repository root.

## Scope and status

Microsoft's library guidance describes inclusive, stable, evolving, debuggable, and trusted libraries. Preserve the strength of each recommendation: **DO** and **DO NOT** express requirements; **CONSIDER** calls for evaluating suitability; **AVOID** advises against a practice rather than prohibiting it outright. Do not turn every recommendation into a mandatory rule.

| Status | Meaning |
| --- | --- |
| Conforming | The named inspected surface matches the cited recommendation. This does not assess other surfaces. |
| Partial | Supporting evidence exists, with a named limit or gap. |
| Not assessed | No assessment has been made. This is neither failure nor approval. |
| Explicit repository waiver | An intentional deviation with named authority, rationale, and scope. A gap alone is not a waiver. |

## General library checklist

Use the general library guidance in the official references for the broader goals. These observations do not establish package compatibility or security compliance.

| Guidance | Status and applicability | Chronicle evidence or gap |
| --- | --- | --- |
| Inclusive platform reach | Partial: configured .NET targets | `Directory.Build.props` selects `net10.0` for ordinary builds and `net8.0;net9.0;net10.0` for packaging. These settings do not prove every package works on every target or platform. |
| Stability and evolution | Partial: kernel wire contracts | `.cratis/ai/rules/project/grain-interfaces-only-grow-within-a-major.md` defines additive grain-interface and payload evolution within a major. `Documentation/contributing/kernel/grain-interfaces.mdx` explains this policy. Other public .NET APIs and released packages are not assessed here; the existence of compatibility gates is not a passing gate result. |
| Debuggability | Partial: build properties | `Directory.Build.props` configures `EmbedUntrackedSources`, `IncludeSource`, `IncludeSymbols`, and `SymbolPackageFormat` (`snupkg`). Published symbols, Source Link, and stepping through a consumed package are not assessed. |
| Trust | Not assessed | Signing, deterministic or reproducible packages, supply-chain assurance, and dependency and security recommendations require separate evidence. |

## Options checklist

Use the options guidance for library authors in the official references. The source files in this table are under `Source/Clients/DotNET/`.

| Guidance | Status and applicability | Chronicle evidence or gap |
| --- | --- | --- |
| `Add{Service}` registration shape | Conforming: inspected host entry point | `ChronicleHostApplicationBuilderExtensions.AddCratisChronicle` follows this shape. Other integrations are not assessed. |
| Package-specific extension namespaces | Partial: known namespace gap, not a waiver | The public `Occurrences/OccurrenceLifecycleServiceCollectionExtensions` is in `Microsoft.Extensions.DependencyInjection`, contrary to the guidance's **DO NOT** for non-Microsoft packages in that namespace. The internal `ChronicleClientServiceCollectionExtensions` also uses it. This observation does not extend that prohibition to every Microsoft namespace. |
| Named-section binding and validation | Conforming: inspected registration mechanisms | `ChronicleHostApplicationBuilderExtensions` defaults to `Cratis:Chronicle`, accepts a section override, and registers `ValidateDataAnnotations` and `ValidateOnStart`. `ChronicleOptionsConfiguration.WithChronicleConfiguration` uses `BindConfiguration` and post-configures a named connection-string fallback. Validation completeness is not assessed. |
| Configuration changes | Partial: binding versus consumption | `BindConfiguration` supports change-token registration, but `ChronicleClientServiceCollectionExtensions` consumes `IOptions<ChronicleClientOptions>.Value`, and `ChronicleClient` stores its constructor options. This does not establish that a running Chronicle connection reloads settings, nor does it establish a runtime defect. |

## Logging checklist

Use the logging guidance for library authors in the official references. The source files in this table are under `Source/Clients/DotNET/`.

| Guidance | Status and applicability | Chronicle evidence or gap |
| --- | --- | --- |
| Generated message templates and guarded expensive arguments | Conforming: inspected discovery example | `Discovery/DiscoveryLogMessages.cs` uses `[LoggerMessage]` templates without interpolation. `LogDiscovery` guards its projection and `string.Join` with `IsEnabled(LogLevel.Debug)`. Templates, interpolation, and argument guards elsewhere are not assessed. |
| No-op defaults | Partial: different construction paths | `Reactors/SideEffects/EventStoreReactorSideEffectHandlerInstances` supplies `NullLogger<T>.Instance`. `ChronicleClient` instead defaults to `new LoggerFactory()`, while `ChronicleClientServiceCollectionExtensions` requires a host-provided `ILoggerFactory`. Do not assume all APIs use `NullLoggerFactory`. |

## Exception-naming convention

**Explicit repository waiver — naming only.** The authority is the Exceptions section of `.cratis/ai/rules/csharp.md`: custom exception types derive from `Exception` and have descriptive names **without an `Exception` suffix**. The rationale is to communicate what went wrong in domain terms. The same rule requires meaningful messages and XML documentation; the naming convention does not waive those requirements or exception semantics, security, validation, or stability guidance.

`Source/Clients/DotNET/EventSequences/UnknownEventType.cs` illustrates the no-suffix name and inheritance. Policy is not proof of universal adoption: `ChronicleClientServiceCollectionExtensions` still throws `InvalidOperationException` for an invalid namespace-resolver type. Existing code does not authorize another waiver. The CA1032 and CA1710 settings in `.globalconfig` are supporting configuration, not the authority for the convention.

## Official references

- [Microsoft .NET library guidance](https://learn.microsoft.com/en-us/dotnet/standard/library-guidance/)
- [Options guidance for .NET library authors](https://learn.microsoft.com/en-us/dotnet/core/extensions/options-library-authors)
- [Logging guidance for .NET library authors](https://learn.microsoft.com/en-us/dotnet/core/extensions/logging/library-guidance)
