# Plan: remove the Api project and the Services layer

Handoff document. Everything here is verified against the tree unless marked otherwise.

**Issue:** [#2908](https://github.com/Cratis/Chronicle/issues/2908) · **PR:** [#3768](https://github.com/Cratis/Chronicle/pull/3768) ·
**Branch:** `feat/wire-compatibility-and-api-migration`

## The goal, and the mistake to avoid

Per #2908: get rid of **both** `Source/Clients/Api` **and** the gRPC service implementations, replacing them with
Arc model-bound Commands and Queries in Core, placed next to the grains they use.

> **`Cratis.Chronicle.Services` is not a layer to preserve. It is the thing being deleted.**

An earlier pass through this work assumed `Services/<Area>` was a legitimate bridge — Core artifact → hand-written
service → generated contract — and planned to keep it. That is wrong and it is the single most important thing to
carry into the new context. `API-MIGRATION.md` still describes that wrong shape in its per-area plan; **do not
follow it**, and rewrite it as part of this work. The commit "Move Jobs, EventStores and Namespaces from Api into
Core generation" describes Services as a bridge in its body — the change itself is fine, the wording is not.

What stays hand-written, per the issue:

- **Reducers and Reactors** — they stream server→client and need custom service implementations.
- **`ConnectionService`** — a likely third case, hand-rolled contract included. See open question 2.

Everything else: Command or Query in Core → generated.

## The blocker nothing else can proceed without

`Source/Tools/GrpcCodeGenerator` generates **the interface and the DTOs, but not the implementation**.
`ServiceInterfaceGenerator` is the only generator in the project. That gap is precisely why 7,233 lines of
hand-written `Core/Services` exist.

**Until the generator emits implementations, migrating an area only relocates the problem.** Do this first.

### Step 1 — generate the service implementation

Alongside `I<Service>`, emit a class implementing it that dispatches each method to its Core Command or Query:

- Commands → construct the record from the request, invoke `Handle`, wrap in `CommandResult`.
  `Core/Services/Jobs/Jobs.cs` and `Core/Services/CommandExecutor.cs` show the shape to generate.
- Queries → invoke the static method, wrap in `QueryResult<T>`. See `QueryExecutor.cs`.
- Observables → `IObservable<QueryResult<T>>`, `CompletedBy(callContext.CancellationToken)`.

Exclude Reducers, Reactors and `ConnectionService` from generation; they keep hand-written implementations.

Decide where generated implementations live — see open question 1.

`Source/Kernel/Server/GrpcServiceRegistrations.cs` currently registers the hand-written services by hand; that
should become convention-based over the generated ones.

### Step 2 — ungate contract generation

`GenerateGrpcContracts` in `Core.csproj` is gated by `DisableGrpcContractGeneration` (default `true`) because
`Contracts/Observation/IObservers.cs` claims to be generated but carries five methods Core cannot produce:
`GetObserverInformation`, `GetConnectedClientsForObserver`, `WaitForCompletion`,
`GetReplayableObserversForEventTypes`, `ClearObserverQuarantine`. Convert those to Core artifacts, then ungate.

To regenerate deliberately before then:

```bash
dotnet build Source/Kernel/Core/Core.csproj -c Debug -p:DisableGrpcContractGeneration=false -p:CratisProxiesOutputPath=
git checkout -- Source/Kernel/Contracts/Observation/IObservers.cs
```

### Step 3 — migrate area by area

19 areas remain in `Source/Clients/Api`:

`Auditing` `Captures` `Clients` `DevelopmentTools` `EventSequences` `EventTypes` `Events` `ExternalServices`
`Identities` `Observation` `Projections` `ReadModelTypes` `ReadModels` `Recommendations` `Security` `Seeding`
`SequenceQueries` `TypeFormats` `Webhooks`

19 corresponding folders remain in `Core/Services`. For each area:

1. Move the logic out of `Core/Services/<Area>` into Commands and Queries in `Core/<Area>`, **beside the grains** —
   they call grains and storage directly, never a gRPC contract interface.
2. Add `[BelongsTo(WellKnownServices.<Area>)]`; add the constant to `WellKnownServices.cs` if new.
3. Convert the area's controllers to model-bound artifacts (20 remain across all areas). Controller-based proxies
   also carry a known defect where enough nullable parameters make route parameters optional.
4. Add `CommandValidator<T>` where the area has validation rules (#2908 asks for this explicitly).
5. Delete `Api/<Area>` and `Core/Services/<Area>`.
6. Repoint the Workbench imports (91 files still import from `Api/`; **Observation alone is 31** — plan for it).
7. Build the **whole solution** and run `npx tsc -b`.

Order: leave **Observation** and **Security** for last. Observation is the largest and blocks step 2; Security is
missing `AuthorizationType`, `ChangePasswordForUser` and `InitialAdminPasswordSetupStatus` on the Core side.

### Step 4 — delete the Api project

Remove the project, its `Program.cs`, the `useGrpc: true` path in `ApiServiceCollectionExtensions`, the
`AddCratisChronicleApi` call in `Server/Program.cs`, and the references from `Server.csproj`, `Connections.csproj`,
`DotNET.csproj`, `Workbench.csproj`, `Api.Specs` and `Integration/Api`. Decide where Swagger and the
`ResourceEmbedder` XML-doc embedding go. No shipped image runs Api standalone — `Docker/Workbench/Dockerfile` is
nginx over static files — so no deployment topology is lost.

### Step 5 — Workbench structure

Proxies already generate into `Source/Workbench/Features`, co-located with components; there is no `Api` folder for
migrated areas. Align feature folders with Core areas: `Features/EventStore/General/` is a navigation container
holding twelve things that are separate areas in Core. Move nesting into routing so `Features/Captures` imports
from its own folder.

### Step 6 — verify for real

Not done at all yet, and explicitly asked for: start the kernel, run the Console sample to seed data, and click
through every Workbench screen against a live backend. **Jobs, EventStores and Namespaces changed HTTP routes**
(`/api/event-stores/add` → `/api/event-stores/ensure-event-store`) — start there.

## What is already done on PR #3768

Complete and verified (Debug clean, `npx tsc -b` clean, 3,013 specs passing):

- Wire compatibility gate, tool, kernel-side `CheckCompatibility`, descriptor sets in all four contracts packages.
- `DateTimeOffset` reached the wire as an empty message on ten fields — fixed at the edge via `TransportTypes`,
  which now refuses any unrepresentable type rather than emitting nothing.
- Proto generator was aborting for 22 of 23 packages while exiting 0; fixed, and it now deletes its output before
  writing so hand-patches cannot survive.
- Two spec projects that were not in the solution (29 specs never ran) folded into the projects owning the types.
- **Foundation:** Core generates proxies into `Features/`; the two generators are separately gated;
  `CopyLocalLockFileAssemblies=true` on Core is required or the ProxyGenerator cannot resolve Orleans.
- **Migrated:** Jobs, EventStores, Namespaces — Api folders deleted, Workbench on Core proxies. Note these still
  leave `Core/Services/{Jobs}` behind; that is the step-1 gap, not an oversight to repeat.

## Open questions — decide before step 1

1. **Where do generated service implementations live?** Contracts, Core, or a new generated-only project. They
   need to reach grains and storage, which argues against Contracts.
2. **Is `ConnectionService` modelled as artifacts, or kept hand-rolled with a hand-rolled contract?** It was raised
   as a candidate for staying hand-written but left undecided.
3. **The gate will fail this PR** (68 findings) and demands the `major` label, which is ruled out. It needs a
   baseline floor — a `--since <version>` so it compares from a declared point forward, acknowledging that
   intra-16 compatibility is already lost while still catching every new break.

## Practical notes

- `rm -rf Source/Kernel/Server/out` when the build fails with MSB3231 on stale publish output.
- Deleting an Api area needs a **full solution build** — `Api/Recommendations` had a stale `using Api.EventStores`.
- Integration specs pin old HTTP routes; Arc-generated routes are `/api/<area>/<artifact-kebab>`.
- The ProxyGenerator **merges** `index.ts`, so generated proxies and hand-written feature code coexist safely.
- Both generators can write into the same tree, so migration stays incremental with nothing broken in between.
- `Features/Contracts/` holds shared enums and `SerializableDateTimeOffset`. `ProxyGenerator.Build` 21.19.0 exposes
  no namespace-exclusion property; removing that folder means Core artifacts owning their domain enums instead of
  referencing `Contracts.*`.
- `.agents/PROJECT.md` documents the generation pipeline, `[BelongsTo]`, `WellKnownServices` and transport types —
  it is accurate and worth reading first.
