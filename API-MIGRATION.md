# Deleting the Api project

An assessment of what it takes to remove `Source/Clients/Api` and let the Arc proxy generator produce the
Workbench's Web API surface directly from `Source/Kernel/Core`, plus the structural changes that follow in the
Workbench.

Everything measured here was measured — the commands are given so you can repeat them.

> **This is the assessment, not the plan.** `PLAN.md` is the working document: it carries the per-area recipe, the
> decisions taken, and what is done. Where the two disagree, `PLAN.md` is current. Phases 1 and 2 below are done,
> and Phase 3 is under way — Jobs, EventStores, Namespaces, Security and Identities have moved.
>
> One thing this document got wrong that cost a pass through the work: it treats moving an area into Core as
> finished when the artifact exists. It is not. The hand-written service in `Cratis.Chronicle.Services` is part of
> what #2908 deletes, and until the generator emitted implementations — which it now does — moving an area only
> relocated the work rather than removing it.

## The short version

**This is a consolidation, not a re-architecture.** Most of the destination already exists, and the one blocker
that the codebase documents as fundamental turns out to be a single MSBuild property.

| | |
| --- | --- |
| Blocker the code calls fatal | **Not fatal.** One property. Measured working. |
| Real cost | Moving 65 artifacts from Api into Core, 20 of them controllers that must become model-bound |
| Workbench proxy import paths | Mostly unchanged — the namespace maths already lines up |
| Deployment topology lost | None — the `-workbench` image is nginx serving static files |
| Prerequisite for | Ungating `GenerateGrpcContracts`, which is what makes the wire contract genuinely derived |

## Where things actually stand

Four facts that make this much smaller than it looks:

**1. The kernel already hosts Arc.** `Source/Kernel/Server/Program.cs` calls `.AddCratisArc(...)` and
`app.UseCratisArc()`. There is no new host to build.

**2. The Api project is already in-process.** The same file calls `AddCratisChronicleApi(useGrpc: false)`. In the
shipped topology Api is a *library* in the kernel process, not a network hop. Its gRPC client path exists but is
not what runs.

**3. Core's artifacts are already exposed.** Because the kernel runs Arc and Core is in that host, the
`[Command]`/`[ReadModel]` records in Core already serve HTTP endpoints today. Nothing needs to be turned on for
them to be reachable — only for their proxies to be generated.

**4. Nothing is deployed standalone.** `Source/Clients/Api/Program.cs` can run as its own web app, but
`Docker/Workbench/Dockerfile` is `FROM nginx` copying `Source/Workbench/wwwroot`. No shipped image runs the Api
process, so deleting it costs no deployment topology.

## Blocker 1 — the proxy generator on Core: solved, measured

`Core.csproj` says:

> ProxyGenerator is disabled for Core because it is an Orleans grain project. Orleans base types cannot be resolved
> in the ProxyGenerator's isolated assembly load context, causing a hard crash.

That is the right symptom and the wrong diagnosis. Running it:

```
dotnet build Source/Kernel/Core/Core.csproj -c Debug -p:DisableProxyGenerator=false
```

```
System.IO.FileNotFoundException: Could not find assembly 'Orleans.Serialization, Version=10.0.0.0'.
   at System.Reflection.TypeLoading.Ecma.EcmaResolver.ResolveAssembly(...)
   at System.Reflection.TypeLoading.RoType.get_BaseType()
   at Cratis.Arc.ProxyGenerator.ModelBound.TypeExtensionsModelBound.IsQuery(Type type)
```

It is not an `AssemblyLoadContext` — it is a **`MetadataLoadContext` whose resolver is missing an assembly**. The
generator enumerates every type in the assembly and asks `IsQuery`, which reads `Type.IsClass`, which needs
`BaseType`, which needs the assembly the base type lives in. Core's output directory holds **7 DLLs**, because a
library project does not copy its NuGet runtime assets. `Orleans.Serialization.dll` is simply not there for the
resolver to find.

```
dotnet build Source/Kernel/Core/Core.csproj -c Debug \
  -p:DisableProxyGenerator=false -p:CopyLocalLockFileAssemblies=true
```

```
Service: Observers (namespace: Cratis.Chronicle.Observation)    Queries: 1
Service: Namespaces (namespace: Cratis.Chronicle.Namespaces)    Commands: 1  Queries: 1
Service: Jobs (namespace: Cratis.Chronicle.Jobs)                Commands: 3  Queries: 2
Service: EventStores (namespace: Cratis.Chronicle.EventStores)  Commands: 1  Queries: 1
Generation complete.
Build succeeded.
```

**Both generators ran clean** — the TypeScript proxies into `Source/Workbench/Api` and the gRPC contracts into
`Source/Kernel/Contracts`. The tree was restored afterwards; nothing in this branch depends on the experiment.

`CopyLocalLockFileAssemblies=true` is a blunt instrument — it fattens Core's output with its whole dependency
closure. Two better endings, in order of preference:

1. **Fix it upstream in Arc.** `IsQuery` should tolerate an unresolvable base type instead of aborting the process,
   and the generator should be handed the resolved `ReferencePath` rather than scanning an output directory.
   Small change, fixes it for every Orleans-hosted Arc project, not just this one.
2. **Set the property on Core** and move on. It works today.

Either way: **the blocker is not structural, and it does not gate the rest of this plan.**

## Blocker 2 — coverage

Api carries **65 artifacts**; Core covers 5 of the 23 areas with 24.

| Area | Api commands | Api read models | Api controllers | Core artifacts |
| --- | --- | --- | --- | --- |
| Security | 8 | 3 | — | 11 |
| Observation | — | 3 | 1 | 4 |
| Jobs | — | — | 2 | 5 |
| EventStores | — | — | 2 | 2 |
| Namespaces | — | — | 2 | 2 |
| Projections | 5 | 2 | — | — |
| SequenceQueries | 4 | 2 | — | — |
| Captures | 5 | 1 | — | — |
| EventSequences | — | 2 | 3 | — |
| ReadModels | — | 3 | — | — |
| Seeding | 1 | 1 | — | — |
| DevelopmentTools | 1 | 1 | — | — |
| ReadModelTypes | — | 1 | 1 | — |
| Webhooks | — | — | 2 | — |
| ExternalServices | — | — | 2 | — |
| Recommendations | — | — | 2 | — |
| Clients | — | 1 | — | — |
| Events | — | 1 | — | — |
| EventTypes | — | — | 1 | — |
| Identities | — | — | 1 | — |
| TypeFormats | — | — | 1 | — |
| Auditing | — | — | — | — |
| **Total** | **24** | **21** | **20** | **24** |

> The table below is the state as first measured. Jobs, EventStores, Namespaces, Security and Identities have
> since moved; their rows are historical.

**44 artifacts across 17 areas exist only in Api.** They are not new behavior — they are wrappers that call the
gRPC contract interfaces, which are themselves generated from Core. Moving them into Core generally means
*collapsing* two layers into one: the Api artifact and the Core grain call become one artifact.

The five covered areas need reconciling rather than moving, and their names already differ:

| Api proxy | Core proxy |
| --- | --- |
| `EventStores/AddEventStore` | `EventStores/EnsureEventStore` |
| `EventStores/GetEventStores` | `EventStores/AllEventStores` |
| `EventStores/AllEventStores` (observable) | `EventStores/ObserveEventStores` |
| `Jobs/Job` | *(gone — `JobSummary`)* |

Every one of those is a Workbench import that has to change. They are also, unavoidably, renames — the generated
method name is the record's type name (see `.agents/PROJECT.md`).

## Blocker 3 — 20 controllers

A third of the Api surface is `ControllerBase` with `[Route]`. Those cannot move as-is: model-bound artifacts are
the only shape Core generates from, and the project rules say no controllers anyway.

This is worth doing on its own merits. Controller-based proxies go through a different generator path with a known
defect — enough nullable parameters flips Roslyn's `NullableContext` and route parameters get emitted as optional,
so a query silently stops re-running when its arguments change. Model-bound queries are immune. Converting these
20 removes a class of Workbench bug that is very hard to see.

## Blocker 4 — what Api does that Core does not

Small, but real, and each needs a decision rather than a move:

| Concern | Where it lives | Disposition |
| --- | --- | --- |
| Swagger + `SwaggerDark.css` | `Api.csproj`, `ApiApplicationBuilderExtensions` | Move to the kernel host, or drop |
| XML doc embedding (`ResourceEmbedder`) | `EmbedXmlDocs` target | Move to Core, feeds Swagger descriptions |
| `ChronicleApiOptions` | `ChronicleApiOptions.cs` | Fold into `ChronicleOptions` |
| `CustomControllerActivator` | Api root | Dies with the controllers |
| Standalone `Program.cs` | Api root | Delete — no image runs it |
| `useGrpc: true` path | `ApiServiceCollectionExtensions` | Delete — the remote-kernel topology it serves is not shipped |
| `Integration/Api` specs | `Integration/Api` | Retarget at the kernel host |

The `useGrpc: true` deletion is the one to think about before doing: it is the only thing standing between here and
"the Workbench can only run inside the kernel process." That is already true in practice, but deleting the code
makes it true by construction. If a remote-Workbench topology is wanted later, it comes back as a reverse proxy in
front of the kernel, not as a second Arc host.

## The namespace maths already lines up

The Workbench imports generated proxies from `Source/Workbench/Api/<Area>/<Artifact>`. That path comes from the
artifact's namespace with leading segments skipped:

```
Api    Cratis.Chronicle.Api.Security   --skip 3  →  Security/
Core   Cratis.Chronicle.Security       --skip 2  →  Security/
```

**Same output path.** Area folders stay put; only per-artifact file names move, and only where the artifact is
genuinely renamed. That is a large de-risking: the Workbench's import graph does not need a wholesale rewrite.

One correction needed: with `--skip 2`, Core's `Cratis.Chronicle.Contracts.*` types map to a `Contracts/` folder in
the proxy output — the experiment produced one. The generator's namespace exclusions need to cover it.

## Workbench structural alignment

The Workbench today groups by **navigation**; Core groups by **domain area**:

```
Features/EventStore/General/Captures       →   Features/Captures
Features/EventStore/General/Projections    →   Features/Projections
Features/EventStore/General/Webhooks       →   Features/Webhooks
Features/EventStore/General/Sequences      →   Features/EventSequences
Features/EventStore/Namespaces             →   Features/Namespaces
Features/Security                          →   Features/Security      (already aligned)
```

`Features/EventStore/General/` is a navigation container, not a domain: it holds Captures, EventTypes,
ExternalServices, Namespaces, Projections, Reactors, ReadModelTypes, Reducers, Seeding, Sequences, Sinks and
Webhooks side by side, all of which are separate areas in Core.

Aligning means **the folder mirrors the area, and the nesting moves into routing.** A page's position in the
navigation tree becomes a route definition, not a directory path. The payoff is that a feature folder and its
generated proxy folder have the same name, so `Features/Captures` imports from `Api/Captures` — which is what makes
the whole pipeline navigable end to end, from the Core record to the React component.

Two areas do not map cleanly and need a decision: `Dashboard` (composes widgets across many areas — it is a view,
not a domain, and should stay a view) and `Reactors`/`Reducers`/`Sinks` (facets of `Observation` in Core).

## Plan

**Phase 1 — unblock, without changing anything.** ✅ Done. `CopyLocalLockFileAssemblies` is set on Core. The
namespace exclusion for `Contracts` was not available — `ProxyGenerator.Build` 21.19.0 exposes no such property —
so `Features/Contracts/` exists and holds the enums and payload types Core artifacts reference directly.

**Phase 1b — generate the implementations too.** ✅ Done, and it was the real blocker. The generator emitted the
interface and the DTOs but nothing that served them, which is why 7,233 lines of hand-written `Core/Services`
existed. `ServiceImplementationGenerator` and `ServiceRegistrationsGenerator` close that; see `PLAN.md`.

**Phase 2 — convert the controllers.** In practice this folded into Phase 3: a controller and the artifact that
replaces it are the same edit, and converting in place in Api would mean writing each one twice. Convert as you
move the area.

**Phase 3 — move areas into Core, one at a time.** For each, collapse the Api artifact, the hand-written service
and the grain call into one Core artifact with `[BelongsTo]`; delete `Api/<Area>` *and* `Core/Services/<Area>`;
update the Workbench imports. The full recipe, including what must not be deleted with the service and how to get
out of the build cycle a rename causes, is in `PLAN.md`. **Verifiable per area:** the proxy folder regenerates,
the whole solution builds, and the Workbench type-checks.

**Phase 4 — flip generation to Core.** Set `DisableProxyGenerator=false` on Core, remove it from Api, delete the
Api project and its standalone host. **Verifiable:** `Source/Workbench/Api` regenerates from Core alone and the
Workbench builds.

**Phase 5 — restructure the Workbench** to mirror Core's areas, moving nesting into routing.

**Phase 6 — ungate `GenerateGrpcContracts`** and delete the hand-written contracts under `Source/Kernel/Contracts`.
This is the real prize: Core becomes the single source for both the Web API and the wire protocol, and the
compatibility gate finally compares generated output against generated output with nothing hand-maintained in
between. See `COMPATIBILITY-REPORT.md`.

Phase 3 is the bulk. Phase 6 is blocked on Observation, which is the last area precisely because of that.

## What I did not verify

- That every Api artifact *can* be expressed as a Core artifact. The 44 uncovered ones are wrappers over contract
  interfaces, so it should hold, but I checked the shape of a sample, not all of them.
- Whether Swagger output survives the move with descriptions intact — it depends on the XML doc embedding running
  against Core.
- Whether the kernel's authorization attributes apply the same way to Core artifacts as to Api ones. Core commands
  are already exposed as authenticated Arc endpoints, so the mechanism works; the per-endpoint policies need
  checking case by case.
- Any runtime behavior. Everything above is build-time and structural.
