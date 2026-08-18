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
carry forward.

What stays hand-written, per the issue:

- **Reducers and Reactors** — they stream server→client and need custom service implementations.
- **`ConnectionService`** — decided: **stays hand-rolled**, contract included. Its subject is a connection
  lifetime, not a command or a query; there is no artifact whose shape it is.

Everything else: Command or Query in Core → generated.

## What the blocker was, and that it is gone

`Source/Tools/GrpcCodeGenerator` used to generate **the interface and the DTOs, but not the implementation**. That
gap is precisely why 7,233 lines of hand-written `Core/Services` existed, and why migrating an area only relocated
the problem.

**Done.** `ServiceImplementationGenerator` now writes the class alongside the interface, and
`ServiceRegistrationsGenerator` writes the composition-root entry that registers and maps them. Both are driven
from the same `GenerateGrpcContracts` target in `Core.csproj`.

Three things came out of building it that are worth knowing before touching it again:

- **Nullability is carried.** A `Type` has no annotation, so a member declared `string?` in Core used to become
  `string` on the contract, and every mapping from it was a nullable-assignment warning. `NullableAnnotations`
  reads it from member metadata. It changes the generated C# only — the `.proto` output is byte-identical.
- **Absence is carried.** A query returning `Task<Order?>` says the order may not exist. `QueryNullability` makes
  both generators agree on that, so the generated mapping does not treat an absent read model as present.
- **The two generators share one rule** (`ParameterClassification`) for which parameters travel on the wire and
  which come from the container. If they disagree the class does not implement the interface it claims to; the
  specs compile the two together and assert exactly that.

## Decisions taken on the open questions

1. **Generated implementations live in Core**, at `Source/Kernel/Core/Services/<area>/<Service>.cs`, namespace
   `Cratis.Chronicle.Services.<area>`. They need grains and storage, which Contracts cannot reach and a separate
   project would have to duplicate Core's whole reference set for.

   **The cost of that choice, which you will hit:** Core's own `AddChronicleServicesAsInMemory` references the
   generated classes, so a rename that invalidates a generated implementation makes Core fail to build, and
   regeneration needs Core to build. The bootstrap is three steps — see *Renames and the build cycle* below.

2. **`ConnectionService` stays hand-rolled**, as above.

3. **The wire gate measures from a declared release.** `--since <version>`, declared as `WIRE_BASELINE_FLOOR` in
   `.github/workflows/wire-compatibility.yml`. 16.x already lost intra-major compatibility (see
   `COMPATIBILITY-REPORT.md`), and a gate that fails every run on a break that already shipped is a gate nobody
   reads. A floor that excludes every baseline reports a **warning**, never a clean run.

   **The floor currently says `16.37.0`** — the release this conversion is expected to first ship in. If it ships
   under a different number, correct it; a floor above the actual release measures nothing.

## Step 2 — ungate contract generation

`GenerateGrpcContracts` in `Core.csproj` is still gated by `DisableGrpcContractGeneration` (default `true`).

The reason is now narrower than it was. `NonDerivedGrpcServices` in `Core.csproj` lists the services the generator
leaves entirely alone — contract *and* implementation. It holds one name: **`Observers`**.
`Contracts/Observation/IObservers.cs` carries five methods Core has no artifacts for — `GetObserverInformation`,
`GetConnectedClientsForObserver`, `WaitForCompletion`, `GetReplayableObserversForEventTypes`,
`ClearObserverQuarantine` — and the rpcs it *does* have are named differently from what the artifacts would
produce (`Replay` vs `ReplayObserver`, `GetObservers` vs `AllObservers`). Emptying that list is what Step 3 of the
Observation migration means.

The `git checkout -- IObservers.cs` dance the old workflow needed is gone; the exclusion does that job properly.

To regenerate deliberately:

```bash
dotnet build Source/Kernel/Core/Core.csproj -c Debug -p:DisableGrpcContractGeneration=false -p:CratisProxiesOutputPath=
```

## Step 3 — migrate area by area

**Done: Jobs, EventStores, Namespaces, Security, Identities.** 18 areas remain in `Source/Clients/Api`:

`Auditing` `Captures` `Clients` `DevelopmentTools` `EventSequences` `EventTypes` `Events` `ExternalServices`
`Observation` `Projections` `ReadModelTypes` `ReadModels` `Recommendations` `Seeding` `SequenceQueries`
`TypeFormats` `Webhooks`

(`Auditing` is not a service area at all — it is causation middleware and converters. It has no commands or
queries; what it needs is a home once the Api host goes, not a migration.)

### The recipe, per area

1. Write the Core artifacts under `Source/Kernel/Core/<Area>/`, **beside the grains** — they call grains and
   storage directly, never a gRPC contract interface. Add `[BelongsTo(WellKnownServices.<Area>)]`, adding the
   constant to `WellKnownServices.cs` first.
2. **Conversion helpers go in a separate `*Converters` class, never as a static on the `[ReadModel]`.** The proxy
   generator emits a query proxy for any static method whose return shape is a supported query shape and does not
   look at accessibility, so a private helper becomes a public HTTP endpoint. Nine of them were live before this
   was noticed.
3. Convert the area's controllers to model-bound artifacts. Controller-based proxies also carry a known defect
   where enough nullable parameters makes route parameters optional.
4. Add `CommandValidator<T>` where the area has validation rules (#2908 asks for this explicitly).
5. Delete `Core/Services/<Area>` and the hand-written `Contracts/<Area>/I<Service>.cs` together with the request
   and response types the generator will now produce. **Anything shared stays**: a converter used by another area
   moves beside the artifacts rather than dying with the service, and an `Api/<Area>` payload record that other
   Api areas still reference stays until they move too.
6. Build Core, run the generator, then update `GrpcServiceRegistrations`, `AddChronicleServicesAsInMemory` and
   `TestingServices` — the three hand-maintained composition roots.
7. Delete `Api/<Area>`, repoint the Workbench imports from `Api/<Area>` to `Features/<Area>`, and delete the stale
   proxies under `Source/Workbench/Api/<Area>`.
8. Build the **whole solution**, run `npx tsc -b`, run the specs.

Order: leave **Observation** for last. It is the largest (1,645 lines of service, 31 Workbench files) and it is
what unblocks Step 2.

### Renames and the build cycle

When an artifact is renamed or removed, the previously generated implementation stops compiling, and Core cannot
build, and the generator needs Core to build. Break it like this:

1. Delete the stale `Core/Services/<area>/<Service>.cs`.
2. Replace the reference to it in `AddChronicleServicesAsInMemory` with `null!` (temporarily).
3. Build Core, run the generator, restore the reference, build again.

Generating that composition root would remove the cycle, and is the obvious next improvement to the generator.

## Step 4 — delete the Api project

Remove the project, its `Program.cs`, the `useGrpc: true` path in `ApiServiceCollectionExtensions`, the
`AddCratisChronicleApi` call in `Server/Program.cs`, and the references from `Server.csproj`, `Connections.csproj`,
`DotNET.csproj`, `Workbench.csproj`, `Api.Specs` and `Integration/Api`. Decide where Swagger and the
`ResourceEmbedder` XML-doc embedding go. No shipped image runs Api standalone — `Docker/Workbench/Dockerfile` is
nginx over static files — so no deployment topology is lost.

## Step 5 — Workbench structure, and the lint problem it has to solve

Proxies generate into `Source/Workbench/Features`, co-located with components. Align feature folders with Core
areas: `Features/EventStore/General/` is a navigation container holding twelve things that are separate areas in
Core. Move nesting into routing so `Features/Captures` imports from its own folder.

**This is now also a quality gate, not only tidiness.** `yarn lint:ci` reports 127 errors, all of them in
generated proxies under `Features/**`:

- `**/Api/**` is in the ESLint `ignores`, which is why the old location was clean. `Features/**` is not, and
  cannot be — `Features/Security` already mixes generated proxies with hand-written components
  (`Login.tsx`, `AuthContext.tsx`), so no folder-level ignore is correct.
- The generated header is `// @generated by Cratis …`, which the `@tony.ganchev/header` rule rejects, and the
  `// eslint-disable-next-line header/header` the generator emits names a plugin this config does not register.

⚠️ **Do not run `yarn lint` to "fix" it.** That script is `eslint --fix`, and its fix is to replace the
`@generated` provenance line with the license header — in every proxy, silently. Use **`yarn lint:ci`** to check.

Deciding this is deciding where generated output lives relative to hand-written code, which is what this step is
for.

## Step 6 — verify for real, and what it found

Done once, against a live kernel (MongoDB in Docker, kernel on 35000, Vite dev server on 9000, Workbench in a
browser). **The Workbench does not work on this branch.** It cannot list event stores, so no screen past the
first is reachable. Two independent defects, neither visible from a build, a spec run or a type-check — which is
the whole argument for this step existing.

Both were introduced by the earlier migrations on this branch, not by anything on `main`, where every artifact
still lived in Api and the numbers agreed.

### Defect A — the routes the proxies call are not the routes the kernel serves

`Server/Program.cs` (and `Clients/Workbench/WebServer.cs`) configure Arc with
`options.GeneratedApis.SegmentsToSkipForRoute = 3`. That is right for an Api artifact —
`Cratis.Chronicle.Api.Security` minus three segments is `security`, giving `/api/security/…`. It is wrong for a
Core artifact: `Cratis.Chronicle.Jobs` minus three segments is *nothing*, so the kernel serves `/api/all-jobs`
while the generated proxy calls `/api/jobs/all-jobs`.

Measured, with `/api/nonsense-xyz` as the calibration for "no such route" (it falls through to the SPA and
answers 200 with `index.html`; a route that exists answers 401 unauthenticated):

| Route | Kernel |
| --- | --- |
| `/api/nonsense-xyz` | 200, SPA — the shape of a missing route |
| `/api/all-jobs` | 401 — exists |
| `/api/jobs/all-jobs` | 200, SPA — what the proxy calls, and it is not there |

The same holds for Identities, Recommendations, TypeFormats and Observation.

**There is no setting that serves both.** `CratisProxiesSegmentsToSkip` drives the proxy's route *and* its output
folder from the same number, so `Features/<Area>/` folders and `/api/<area>/…` routes are the same choice; and
Arc 21.19.0 has one global `SegmentsToSkipForRoute`, not one per assembly. So while Api and Core coexist, either
the migrated areas are wrong or the un-migrated ones are.

The options, none of them free:

1. **Set the server to 2 and let the remaining Api areas answer at `/api/api/<area>/…`** until each one moves.
   Correct end state, one line, and it makes every migrated screen work today — at the cost of a silly route and
   91 Workbench imports moving to `Api/Api/<Area>` for the duration.
2. **Leave the server at 3 and generate Core proxies with 3**, so routes match and every Core proxy lands flat in
   `Features/`. Loses the area folders and invites file-name collisions between areas.
3. **Finish the migration first, then set the server to 2.** Nothing else changes, but the Workbench stays broken
   for migrated areas until the last area lands, which makes every intermediate step unverifiable — the state
   this branch is in now.

This wants a decision before more areas move, because option 3 is what "just keep going" chooses by default.

### Defect B — a property-less read model has no HTTP endpoint at all

`EventStoreNames` and `NamespaceNames` are `[ReadModel]` records with **no properties**, whose queries return
`IEnumerable<string>`. Arc registers a query performer for the read-model type and its collections; a query
answering with strings is not a shape it recognizes, so nothing is registered — no route at either spelling, and
the SSE hub answers a subscription with:

```
No performer found for query Cratis.Chronicle.EventStores.EventStoreNames.ObserveEventStores
```

which is what leaves the event store picker empty and the app unusable. The gRPC surface is unaffected: the
generated service calls the static method directly and never goes through Arc's performers, so the .NET client
still works and no spec notices.

The fix is to give the read model the property it describes — `EventStoreNames(string Name)` — which changes what
the query answers with from `["a","b"]` to `[{"name":"a"},{"name":"b"}]`, and so touches the generated contract,
the .NET client's `AllEventStores`, and the Workbench components that read the list.

### Reproducing it

```bash
docker run -d --name chronicle-verify-mongo -p 27017:27017 mongo:latest
dotnet run --project Source/Kernel/Server/Server.csproj -c Debug --no-build     # https://localhost:35000
cd Source/Workbench && yarn dev                                                  # http://localhost:9000
```

The kernel's TLS certificate is self-signed, so drive the Workbench through the Vite dev server on 9000 (its
proxy sets `secure: false`); a browser automation tool refuses 35000 directly. `curl -k` reaches the kernel fine.
Only `storage.connectionDetails` in `chronicle.json` moves the event storage — Orleans clustering and reminders
read their own configuration and stayed on 27017, so pointing storage elsewhere just makes startup time out.

### Still not verified

Seeding data with the Console sample, and clicking through the screens past the event store picker. Neither is
reachable until Defect B is fixed, and neither means anything until Defect A is.

## The state of the branch, honestly

The build is clean, the specs pass, the wire contract is derived, and **the Workbench does not run**. Step 6 above
says why, and it is the next thing to fix — ahead of migrating any further area, because every area migrated
under Defect A is another screen that cannot be verified.

## What is already done on PR #3768

Whole-solution Debug build clean (0 warnings, 0 errors), `npx tsc -b` clean, 7,567 specs passing across every unit
spec project. None of that catches either defect in Step 6.

- Wire compatibility gate, tool, kernel-side `CheckCompatibility`, descriptor sets in all four contracts packages.
- `DateTimeOffset` reached the wire as an empty message on ten fields — fixed at the edge via `TransportTypes`,
  which now refuses any unrepresentable type rather than emitting nothing.
- Proto generator was aborting for 22 of 23 packages while exiting 0; fixed, and it now deletes its output before
  writing so hand-patches cannot survive.
- Two spec projects that were not in the solution (29 specs never ran) folded into the projects owning the types.
- **Foundation:** Core generates proxies into `Features/`; the two generators are separately gated;
  `CopyLocalLockFileAssemblies=true` on Core is required or the ProxyGenerator cannot resolve Orleans.
- **The generator emits implementations and registrations**, so migrating an area now removes the hand-written
  service rather than relocating it.
- **The wire gate measures from a declared floor**, so a break that already shipped stops masking new ones.
- **Nine conversion helpers stopped being HTTP endpoints** (`/api/jobs/to-job`, `/api/security/to-user`,
  `/api/observation/join`, …).
- **Migrated:** Jobs, EventStores, Namespaces, Security, Identities — hand-written services deleted, Workbench on
  Core proxies.

## Practical notes

- `rm -rf Source/Kernel/Server/out` when the build fails with MSB3231/MSB3030 on stale publish output. It happens
  often enough to be worth doing before any full-solution build.
- Deleting an Api area needs a **full solution build** — `Api/Recommendations` had a stale `using Api.EventStores`,
  and `Api/Identities/Identity` turned out to be shared with three other areas.
- Integration specs pin old HTTP routes; Arc-generated routes are `/api/<area>/<artifact-kebab>`.
- The ProxyGenerator **merges** `index.ts`, so generated proxies and hand-written feature code coexist safely.
- Both generators can write into the same tree, so migration stays incremental with nothing broken in between.
- `Features/Contracts/` holds shared enums, `SerializableDateTimeOffset` and the payload types Core artifacts
  reference directly. `ProxyGenerator.Build` 21.19.0 exposes no namespace-exclusion property; removing that folder
  means Core artifacts owning their domain enums instead of referencing `Contracts.*`.
- A Core read model must not be named the same as a `Contracts.*` type it references — the generated TypeScript
  imports the one and declares the other in the same file. `IdentityDetails` exists for that reason.
- `.agents/PROJECT.md` documents the generation pipeline, `[BelongsTo]`, `WellKnownServices` and transport types —
  it is accurate and worth reading first.
