# Plan: get Core off Contracts

Handoff document, second one. `PLAN.md` covers replacing `Source/Clients/Api` and `Cratis.Chronicle.Services` with
Arc artifacts in Core — that work landed. This plan covers a problem that landed **with** it and that `PLAN.md`
does not mention: `Source/Kernel/Core/Core.csproj` carries a `ProjectReference` to `Source/Kernel/Contracts` (line
23), and roughly a third of Core's own files import `Cratis.Chronicle.Contracts` directly.

**Issue:** [#2908](https://github.com/Cratis/Chronicle/issues/2908) · **PR:** [#3768](https://github.com/Cratis/Chronicle/pull/3768) ·
**Branch:** `feat/wire-compatibility-and-api-migration`

## The requirement, stated precisely

> Core should not know there is such a thing as gRPC or Protobuf.

**Directive from the project owner (2026-08-21), settling the scope question Phase 3 raised — record this so it
never gets re-litigated:**

> Contracts reference in Core should be gone. Contracts folder in Workbench should be gone. Types that we need to
> expose — enums, DTOs, read models — should sit in Core, not in Contracts. We should then generate C# proxies
> with reliable incremental contracts into Contracts, wire-wise backwards compatible — that's what the tool needs
> to do properly. Frontend proxies should be generated using Arc's proxy generator, landing in folders matching
> the structure of Core.

This is **100% purity, not a documented set of exceptions.** Every one of the areas Phase 3 flagged as "larger
than a `[BelongsTo]` change" still needs to happen — the directive settles *that it happens*, not that it's
optional. Consequences, stated plainly so they don't get re-derived later:

- **`Features/Contracts/**` in the Workbench is a symptom, not a separate task.** It exists only because Arc's
  proxy generator (which walks Core's own assembly) currently encounters `Contracts.*`-typed properties on Core
  artifacts and has to emit something for them. Once no Core artifact references a `Contracts.*` type, this
  folder stops being generated on its own — nothing needs to specially "remove" it beyond deleting what's left
  stale after the last reference moves. Its continued existence at any point is a direct, checkable signal that
  Core→Contracts references remain.
- **The `Observers`/`ConnectionService` hand-rolled exception still stands** — but only because those
  *implementations* already live in `Grpc`, not Core, since Phase 1. The directive is about Core, not about
  whether every gRPC service is generator-derived. If a *type* those areas need (e.g. `ObserverInformation`) can
  become Core-owned via the Phase 2 mechanism independent of whether the *service* interface itself is ever
  generated, do that — the type-level fix and the service-level `[BelongsTo]` fix are separable, and the
  directive requires the former unconditionally.
- **The `Reactors`/`Reducers` `Clients/` mediators are not exempt by virtue of being protocol code.** If they can
  be moved to `Grpc` (they are not `[Command]`/`[ReadModel]` artifacts, so nothing about them requires living in
  Core), do that, the same move Phase 1 already made for the rest of the boundary layer. Only keep something
  Contracts-shaped in Core if it is genuinely impossible to relocate — and that needs to be demonstrated, not
  assumed.
- **The generator has to become a complete, reliable, incrementally-wire-stable tool** — not a mechanism proved
  once on `JobStatus` and left there. Every area Phase 3 found with missing Core artifacts (`EventSequences`'
  five uncovered `IEventSequences` methods, `Projections`' circular `IProjections` injection, `ReadModels`' gap)
  needs those artifacts actually written, following `PLAN.md`'s existing per-area recipe, so `[BelongsTo]` has
  something real to generate from.

Contracts is generated **from** Core, not referenced **by** it. Two separate things currently violate that, and
both have to be fixed for `Core.csproj` to drop the `ProjectReference` to `Contracts.csproj` at all:

- **A — the generated gRPC service-implementation layer lives inside Core.** `Core/Services/<area>/<Service>.cs`
  implements `Contracts.<Area>.I<Service>` — a protobuf-net.Grpc `[Service]` interface — taking `CallContext` and
  returning `CommandResult<T>`/`QueryResult<T>`. That is Core knowing about gRPC, independent of anything else.
- **B — Core artifacts reference shared Contracts value types directly**, because nothing generates those types
  from anywhere else. `Core/Jobs/JobSummary.cs` puts `Contracts.Jobs.JobStatus` directly on a `[ReadModel]`;
  `Core/Sequences/*.cs` commands construct `Contracts.Events.EventType` inside `Handle()`; and so on across every
  area — 74 non-`Services/` files do this (verified by grep across `Source/Kernel/Core`).

Neither is what `PLAN.md`'s "state of the branch" narrative describes. It says every area is migrated; it doesn't
say Core still depends on Contracts to compile.

## Root cause of B, and why it's fixable for free

`Source/Tools/GrpcCodeGenerator` (`TypeDiscovery.cs`, `ServiceInterfaceGenerator.cs`) only synthesizes **one**
kind of type: a `<Command>Request`/`<Command>Response` or `<Query>Response` DTO, generated fresh per command/query
method, one service at a time. When a member's type is anything else — an enum, a plain shared record used by
several artifacts (`Identity`, `Causation`, `EventContext`, `ConcurrencyScope`, `ObserverFilters`,
`AuthorizationType`, …) — `TypeHelper.GetTypeName()` falls through to `Qualified(type)`
(`TypeHelper.cs:278-279`), which just prints `global::{namespace}.{name}` and **assumes the type already exists**.
Today it "works" only because that type already exists — hand-written in Contracts — and Core reaches back for it.
There is no other place for it to come from.

The fix everyone would reach for — "just duplicate the type in Core and hand-convert," the pattern
`Core/Sequences/EventTypeConverters.cs` already uses for `EventType` — is the wrong direction to generalize. It
adds a converter file per shared type forever and never closes the loop. The generator needs a third synthesis
unit: **a mirror of a Core-owned shared type**, generated once (not per-service) and reused by every service that
references it.

**Wire stability is not a new problem to solve — it's a mechanism the generator already has, reused.**
`ServiceInterfaceGenerator.Generate()` already keeps regenerated `[ProtoMember]` numbers stable across runs:
before writing a DTO class, it calls `ProtoMemberIndexReader.ReadExistingIndexes(interfaceFilePath, typeName)`
(`ServiceInterfaceGenerator.cs:76`), which parses the **file that's about to be overwritten** and carries forward
every property's existing index, assigning new numbers only to genuinely new properties. Point that same read at
the mirrored type's target file, and the first time a shared type gets generated, the file being read is the
**current hand-written Contracts file** — so the initial generation reproduces today's exact field numbers with no
extra bookkeeping. For enums it's simpler still: protobuf-net serializes an enum by its declared int value, so
copying `JobStatus`'s nine members with their existing explicit `= 0` .. `= 9` verbatim into Core is the entire
wire-stability story for that type — nothing in the generator needs to know about it.

Confirmed live in the current tree: `Source/Kernel/Contracts/Jobs/JobStatus.cs` (explicit `= 0..9`),
`Events/EventContext.cs`, `Identities/Identity.cs`, `Auditing/Causation.cs` (`[ProtoMember(1..N)]`, hand-numbered,
sequential, no gaps) — these are exactly the shapes `ReadExistingIndexes` already knows how to read.

## Root cause of A, and why the previous decision needs revisiting

`PLAN.md`'s decision #1 keeps generated implementations in Core because "they need grains and storage, which
Contracts cannot reach, and a separate project would have to duplicate Core's whole reference set." That reasoning
doesn't hold: a new project doesn't duplicate Core's references, it **references Core** (one line) and gets
everything Core exposes, the same way `Cratis.Chronicle.Grains` already does today.

Checked directly against `Core/Services/Jobs/Jobs.cs` (the generated implementation) and its two collaborators:

- `command.Handle(grainFactory)` — `Handle()` is `public` on every `[Command]` by Arc's own contract.
- `JobSummary.AllJobs(...)`, `JobStepSummary.GetJobSteps(...)` — `public static` on every `[ReadModel]`, because
  Arc's own HTTP/SSE surface already needs to call them publicly.
- `CommandExecutor` / `QueryExecutor` / `ServiceLogMessages` (`Core/Services/*.cs`, hand-written, not generated) —
  reference only `Cratis.Chronicle.Contracts.*`, FluentValidation, and `System.Reactive`. Zero dependency on
  anything Core-internal. `CommandExecutor.DiscoverValidators` reflects on `commandType.Assembly`, i.e. the
  **command's** assembly, not its own — so it keeps working regardless of which assembly hosts it.

Nothing here needs Core-internal access. `Cratis.Chronicle.Server` currently gets it via
`InternalsVisibleTo Cratis.Chronicle.Server` on `Core.csproj` (`Core.csproj:14`) purely because
`Server/GeneratedGrpcServices.cs` calls `services.AddSingleton<IContract, InternalImplClass>()` against an
`internal sealed class` that happens to live in Core's assembly today. Move the class, move the need.

**Decision: extract `Core/Services/**` (implementations, `CommandExecutor`, `QueryExecutor`,
`ServiceLogMessages`) into a new project.** Proposed name **`Cratis.Chronicle.Grpc`**
(`Source/Kernel/Grpc/Grpc.csproj`) — deliberately not "Services," which `PLAN.md` already uses for the thing that
got deleted. It references `Core.csproj` and `Contracts.csproj`; nothing else references it except `Server`. The
generator's `--implementations`/`--implementations-namespace` flags in `Core.csproj`'s `GenerateGrpcContracts`
target point there instead of at `./Services`; `--registrations` keeps writing to
`Server/GeneratedGrpcServices.cs` unchanged. `InternalsVisibleTo Cratis.Chronicle.Server` moves from `Core.csproj`
to the new `Grpc.csproj`. `Observers` and `ConnectionService` (decided hand-rolled, per `PLAN.md`) move there too,
as hand-written files instead of generated ones — being hand-rolled and gRPC-shaped is no longer a problem once
they're not inside Core.

This is the lower-risk, more mechanical half of the work, has no wire-format implications at all, and is what
makes the `ProjectReference` removable in principle even before shared-type generation exists. Do it first.

**Residual risk, checked per-area, not assumed away:** some `Core/Services/<area>/*.cs` file — most likely inside
`Observation` (Reactors/Reducers, streaming) or the two hand-rolled ones — may reach for something that actually
is Core-internal (an internal grain interface, a storage type not otherwise exposed). If a move breaks the build
on that basis, the fix is a targeted `InternalsVisibleTo Cratis.Chronicle.Grpc` on `Core.csproj`, not reverting the
extraction. Confirm this per area as it's migrated.

## The other correction to `PLAN.md`'s narrative

`WellKnownServices.cs` defines exactly 13 constants (`Users, Applications, Jobs, Observers, Namespaces,
EventStores, Identities, Recommendations, EventSeeding, ExternalServices, Captures, EventTypes, Webhooks`), and
`Core.csproj`'s Debug build confirms exactly 13 services get discovered (verified by build output after the
rebase below — grep for `Service:` lines). `[BelongsTo]` is what `TypeDiscovery.DiscoverServices()` requires
(`TypeDiscovery.cs:65-87`) — a `[Command]`/`[ReadModel]` without it is skipped with a console warning, not an
error, so this fails silently.

**`Core/Sequences/*.cs` (9 files, e.g. `Append`, `RedactMany`), `Core/ProjectionEditor/*.cs` (7 files),
`Core/ReadModelExplorer` + `Core/ReadModelDefinitions` (6 files), and the rest of `Core/Observation/**` beyond
Webhooks carry `[Command]`/`[ReadModel]` with no `[BelongsTo]` at all.** Their contracts
(`IEventSequences.cs`, `IProjections.cs`, `IReadModels.cs`, `IMaterializedReadModels.cs`, `IConstraints.cs`,
`IReactors.cs`, `IReducers.cs`, `ICompliance.cs`, `IEventStoreSubscriptions.cs`, `IServer.cs`,
`IConnectionService.cs`) are hand-written, untouched by the generator, and not marked `<auto-generated />`. This
matters here specifically because their Core artifacts are exactly the "A" files reaching into Contracts directly
— they can't stop doing that until the generator can actually derive their contracts, which needs `[BelongsTo]`
wiring first. This is required work, not a nice-to-have left over from `PLAN.md`.

`Observers` and `ConnectionService` are excluded from this — they keep their explicit hand-rolled status per
`PLAN.md`'s existing decision; they just live in `Grpc` instead of Core once extracted.

## What "done" looks like

- `Core.csproj` has no `ProjectReference` to `Contracts.csproj`, and no file under `Source/Kernel/Core/` has a
  `using Cratis.Chronicle.Contracts...` or fully-qualified `Contracts.*` reference.
- Every area's Contracts interface + DTOs, including the ones this plan newly wires up, carry the
  `<auto-generated />` header and are produced by `GenerateGrpcContracts`. `Observers` and `ConnectionService`
  remain the two named, deliberate exceptions (hand-written, now in `Grpc`, not Core).
- Every shared value type Core needs (enums, cross-cutting records) is defined once in Core, with zero protobuf
  attributes on it, and mirrored into Contracts by the generator.
- The wire is unchanged: `Source/Tools/WireCompatibility` run against the declared baseline reports no new
  breaks beyond what's already known in `COMPATIBILITY-REPORT.md`.
- Full solution build (Debug + Release), specs, `yarn lint:ci`, `npx tsc -b`, and a live-kernel Workbench
  click-through (the `PLAN.md` Step 6 protocol) all pass.

## Phases

### Phase 0 — rebase (done, this session)

Rebased onto `origin/main` (47 commits ahead, 0 behind). Conflicts were confined to: three generated `I<Service>.cs`
files (resolved by taking whichever side's generation was more current, since they're regenerated from source
regardless), `Source/Tools/GrpcCodeGenerator/TypeHelper.cs` (main independently patched the `DateTimeOffset`
special case that our branch's `TransportTypes.cs` generalizes and removes — kept the removal), and
`Source/Workbench/Features/Security/LoginViewModel.ts` (main added a `basePath`-aware `absolutePath()` helper
while our branch independently renamed/relocated the password-change import twice — merged both). Full solution
Debug build is green (0 errors; 2 pre-existing `CS8601` warnings in generated `Users.cs`/`Applications.cs`
predate this work).

### Phase 1 — extract the gRPC implementation layer out of Core

1. Create `Source/Kernel/Grpc/Grpc.csproj` (`AssemblyName Cratis.Chronicle.Grpc`), referencing `Core.csproj` and
   `Contracts.csproj`.
2. Move `Core/Services/**` (implementations + `CommandExecutor`/`QueryExecutor`/`ServiceLogMessages`) to
   `Grpc/`. Move `Observers.cs` and `ConnectionService.cs` (the two hand-rolled ones) there too.
3. Update `Core.csproj`'s `GenerateGrpcContracts` target: `--implementations` → `../Grpc`,
   `--implementations-namespace` stays `Cratis.Chronicle.Services` (renaming the namespace is optional polish, not
   required — don't conflate it with this move).
4. Move `InternalsVisibleTo Cratis.Chronicle.Server` from `Core.csproj` to `Grpc.csproj`. Leave every other
   `InternalsVisibleTo` on `Core.csproj` alone.
5. `Server.csproj` gets a `ProjectReference` to `Grpc.csproj`.
6. Build. Fix whichever `Core/Services/<area>` files (if any) turn out to need something Core-internal by adding
   a scoped `InternalsVisibleTo Cratis.Chronicle.Grpc` — don't move the extraction back.
7. Full solution build, specs, done as its own commit (or small commit sequence) — this phase has zero wire-format
   risk, so it's safe to land independently of everything below.

### Phase 2 — teach the generator to mirror shared types, prove it on one

1. Add a shared-type discovery pass to the generator: after (or alongside) the existing per-service DTO
   discovery, walk the member/property/parameter type graph reachable from every discovered command and query
   (recursively, with a visited-set — `Identity.OnBehalfOf` is self-referential). For each reachable type that is:
   - not a primitive, BCL type, collection, `Task`/`IObservable`/`ISubject` wrapper, or `ConceptAs<T>`-derived,
   - not itself a `[ReadModel]` (that already becomes a `<Name>Response`),
   - not already under `Cratis.Chronicle.Contracts.*`,
   - defined in the loaded (Core) assembly,

   record it once (dedupe by `Type`) as a shared-type generation target, and recurse into its own members/enum
   values.
2. Generate each shared type into its own file, at the location the existing namespace-skip/base-namespace
   transform already produces for services in that area (so a type Core places under `Cratis.Chronicle.Jobs`
   lands under `Cratis.Chronicle.Contracts.Jobs`, matching where it already lives today). For a class/record, reuse
   `ProtoMemberIndexReader` exactly as DTOs do — read existing indexes from the target file before overwriting.
   For an enum, emit `[ProtoContract]`/members with the same explicit int values the Core enum declares.
3. Prove it end-to-end on the smallest real case: `JobStatus`. Move the enum from
   `Contracts/Jobs/JobStatus.cs` to `Core/Jobs/JobStatus.cs` (no protobuf attributes), point
   `JobSummary`/`JobSummaryConverters`/`JobStepSummary`/`JobStepSummaryConverters` at the Core-owned one, add it to
   the generator's discovery, build. Confirm the regenerated `Contracts/Jobs/JobStatus.cs` is
   byte-for-byte identical to the pre-move hand-written version except for the `<auto-generated />` header, and
   confirm `Source/Tools/WireCompatibility` reports no break. This is the checkpoint that validates the whole
   mechanism before spending it across ~15-20 more types — don't proceed to Phase 3 until this is green.

### Phase 3 — wire up the un-discovered areas

**Revised after investigation (see `PROGRESS2.md` for the full per-area evidence) — this is not the quick
`[BelongsTo]`-wiring sweep it first looked like.** Checked every candidate against its actual source rather than
against the generator's "no `[BelongsTo]`" skip list, which is a broader net than "has this plan's problem":

- `Compliance`, `Host`, `DevelopmentTools`, `EventStoreSubscriptions`, `SequenceQueries` — **not part of this
  plan.** Zero files reference `Cratis.Chronicle.Contracts`; they lack `[BelongsTo]` because they were never meant
  to have a gRPC surface (`Core/Schemas/TypeFormat.cs`'s own doc comment says this explicitly for the same
  reason). Do not wire these — there is nothing to fix.
- `Observation` top-level (`ClearObserverQuarantine` etc.) — real Contracts references, but belongs to the
  `Observers` service, which stays excluded from generation until the larger, already-deferred Observation
  migration (`PLAN.md`'s own "Step 3 of the Observation migration") lands. Not part of this phase.
- ~~`Reactors`/`Reducers` `Clients/` mediators~~ **Resolved, not an exception after all.** Their only Contracts
  reference turned out to be `ReplayState` (a plain enum, not the streaming protocol itself) — moved into Core
  the same way `JobStatus` was; the two hand-written Grpc streaming implementations needed one explicit cast
  each at the boundary. Done.
- `EventSequences`, `Projections`, `ReadModels` — **all three share one root cause, precisely identified**: every
  artifact in all three areas injects the corresponding hand-written `Contracts.<Area>.I<Service>` directly into
  its `Handle()`/query method and calls back through it — `Sequences.Append`, `AppendMany`, `Redact`, `RedactMany`,
  `Revise` all take `IEventSequences`; all 7 `ProjectionEditor` artifacts take `IProjections`; both
  `ReadModelExplorer` and `ReadModelDefinitions` take `IReadModels`. **Checked every other "done" area for the same pattern
  (`Jobs`/`Security`/`EventTypes` inject `IGrainFactory`/`IStorage` directly, cleanly) — it is isolated to these
  three**, not systemic. This is why `PLAN.md`'s "state of the branch" could honestly claim these areas were
  migrated: the Workbench works today, because the shortcut still round-trips through the old hand-written Grpc
  service underneath. Fixing it means porting that service's actual logic (grain calls, storage cursors,
  compliance release, paging) into the Core artifacts directly — `Grpc/EventSequences/EventSequences.cs` (310
  lines), `Grpc/Projections/Projections.cs` (533 lines), `Grpc/ReadModels/{ReadModels,MaterializedReadModels}.cs`
  (876 + 104 lines) are the source of truth for what each rewritten artifact must still do.
  - **`EventSequences` also needs 3 new artifacts**, not just a rewrite of the 9 existing ones:
    `CompleteStream`, `HasEventsForEventSourceId`, `GetForEventSourceIdAndEventTypes` have no Core artifact at
    all today, confirmed still real, published `Cratis.Chronicle.Client` (.NET SDK) surface with its own specs —
    not dead code safe to drop.
  - **`RedactMany` (Core's name) already calls the contract operation `RedactForEventSource`** — the rename has
    to go the other way (Core's command becomes `RedactForEventSource`) to keep the wire operation name stable,
    not the reverse.
  - **Zero existing specs target these artifacts directly** — every existing `Core.Specs` spec under
    `EventSequences/`/`Projections/`/`ReadModels/` tests the grain or storage layer beneath them, never
    `Sequences.Append` et al. The relevant coverage that *does* exist is `Integration/Client/for_EventSequence/*`
    (out-of-process, exercises the real command through the real client) — **which needs Docker and cannot run on
    this machine** (see `project_integration_specs_need_linux` — macOS can't run the Docker-backed suites at
    all). Verification for this phase leans on: in-process `CommandScenario`/`ReadModelScenario` specs written
    fresh as part of the rewrite (closing the coverage gap, not just borrowing existing coverage that doesn't
    exist), full `Core.Specs`, full solution build, `WireCompatibility`, and — before calling any of these three
    areas done — a live-kernel Workbench click-through per `PLAN.md`'s Step 6 protocol, since that is the layer
    that actually exercises the full pipeline this rewrite touches and neither a build nor a unit spec can stand
    in for it. CI (`Integration/Client` included) is the other real check, once this is pushed.

**Decided (2026-08-21, see the directive at the top of this file): 100% purity.** Every one of these areas gets
fully migrated — Core artifacts written for every contract method, hand-written interfaces deleted, no narrow
exception carved out beyond `Observers`/`ConnectionService` (which already live in `Grpc`, not Core — see the
directive's consequences). Each area follows the per-area recipe `PLAN.md` already documents (Step 3 in that
file) — write the missing Core artifacts, reconcile naming, delete the hand-written contract/implementation,
update the three composition roots, verify — the same weight as migrating a new area, not a sweep.

### Phase 4 — migrate every shared type, area by area

Using the mechanism proved in Phase 2, for each shared type still hand-written in Contracts and referenced
directly from Core (the ~74-file inventory: `Identity`, `Causation`, `EventContext`, `ConcurrencyScope`,
`ObserverFilters`, `AuthorizationType` + its three variants, `EventType`/`EventTypeOwner`/`EventTypeSource`,
`ObserverInformation`/`ObserverType`/`ObserverRunningState`, `ConstraintType`/`ConstraintScope`, `CaptureStatus`,
`CaptureValidationMessage`, `WebhookTarget`, `ReadModelType`/`ReadModelObserverType`, and whatever Phase 3 exposes
that isn't on this list yet):

1. Check whether Core already half-owns the type under `Concepts.*` (true for `JobStatus`, `Identity`,
   `Causation` — the storage layer already uses these; a hand-written `*Converters.cs` doing `.ToApi()`/
   `.ToContract()` between the two is the signal). If so, reuse it rather than inventing a third copy. If not,
   move the type into Core, in the namespace that maps to its current Contracts location via the existing
   skip/base-namespace rule. Strip every protobuf-net attribute either way.
2. Repoint every Core file that referenced `Contracts.<Area>.<Type>` at the Core-owned one.
   **A hand-written `*Converters.cs` bridging the two types is not deleted — it moves to `Grpc.csproj`,
   alongside whatever it converts for.** The two CLR types stay genuinely distinct even after the Contracts side
   becomes generated (a generated mirror is still its own type), so something still has to convert between them
   at the boundary, and that something must not live in Core. Only delete a converter if the two sides turn out
   to be identical in shape and the conversion was doing nothing but a type-name change (verify, don't assume —
   `JobStatus`/`JobStepStatus` were this simple; `EventContext`/`Identity`/`Causation` are not, based on their
   existing hand-written converters' shape).
3. **A type reachable only from non-artifact Core code (a `[Singleton]` mediator, a delegate, anything that
   isn't a `[Command]`/`[ReadModel]` or a member of one) is never discovered by the generator at all** —
   `SharedTypeRegistry` only walks types reachable from a *discovered* artifact. `ReplayState` is this case: its
   Contracts-side file stays hand-written (still legitimately needed by whichever hand-written Grpc service
   still uses it), and fixing Core only requires repointing step 2 plus adding an explicit cast at every
   Grpc-side call site that now crosses two distinct CLR types instead of one identical one — check every
   consumer solution-wide (`grep` for the type name across `Source/`), not just inside Core or the area's own
   Grpc folder; a hand-written boundary call can live anywhere that references both projects.
4. For a type that *is* reachable from a discovered artifact: it should get picked up by generator discovery
   automatically once Core references it — verify this happened (the build log names every "Generated shared
   type X"), don't assume.
5. Build, diff the regenerated Contracts file against its prior hand-written content (expect identical field
   numbers/types, only the header and possibly formatting changing), run `WireCompatibility`.
6. Full solution build — a hand-written Grpc-side converter or call site touching the migrated type needs
   fixing wherever it lives, not just in the area's own folder (see step 3).

Batch types that are only ever referenced together (e.g. `AuthorizationType` and its three variant records) into
one commit; keep genuinely independent types in separate commits per `git-commits.md`.

### Phase 5 — remove the reference, verify for real

1. Delete the `ProjectReference` to `Contracts.csproj` from `Core.csproj`. Build. Any remaining compile error
   names exactly what Phase 4 missed.
2. Full solution build (Debug + Release, Release with `-p:CratisProxiesOutputPath=` per the standard recipe).
3. `dotnet test` across affected projects; `Source/Tools/WireCompatibility` against the declared baseline.
4. `npx tsc -b`, `yarn lint:ci`.
5. Live-kernel Workbench verification, same protocol as `PLAN.md`'s Step 6 (MongoDB in Docker, kernel on 35000,
   Vite dev server on 9000) — click through every screen the shared-type moves touch, since a wire-shape mistake
   here is exactly the kind of thing a build and a spec run won't catch.

## Open questions worth a deliberate answer, not a default

- **`Grpc` as the project/namespace name.** Reasonable, not load-bearing — a naming call, change it if a better
  one surfaces during Phase 1.
- **Whether `--implementations-namespace` should also change** away from `Cratis.Chronicle.Services` now that the
  namespace no longer lives in Core. Cosmetic; do it only if it's free once Phase 1 lands, don't block on it.
- ~~Whether every shared type is worth moving, or whether a few are small enough to leave as an accepted
  exception.~~ **Resolved by the directive: move everything.** No new exception gets carved out without it being
  demonstrated impossible to relocate, not just inconvenient.

## Practical notes carried forward from `PLAN.md`

- `rm -rf Source/Kernel/Server/out` before any full-solution build that's failing with MSB3030/MSB3231.
- Generated `[ProtoMember]` numbers are hand-assigned today, sequential, no gaps — `ReadExistingIndexes` depends
  on being pointed at the right file at the right time (before it's overwritten), not on any inherent ordering
  guarantee.
- Two spec projects were previously not wired into the solution and their specs silently never ran (`PLAN.md`
  found this) — when adding specs for `Grpc` or for newly-discovered areas, verify the project is actually in
  `Chronicle.slnx` and the CI matrix, not just present on disk.
