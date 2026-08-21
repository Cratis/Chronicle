# Progress: get Core off Contracts

Tracks execution of `PLAN2.md`. Update as each step lands — this is a status log, not a duplicate of the plan.

## Phase 0 — rebase

- [x] Rebased `feat/wire-compatibility-and-api-migration` onto `origin/main` (b1d037350). 47 commits ahead, 0 behind.
- [x] Resolved conflicts: 3× generated `I<Service>.cs` (Jobs, Applications, Users — cosmetic, regenerated fresh
      after), `TypeHelper.cs` (kept our `TransportTypes.cs` generalization over main's narrower inline fix),
      `LoginViewModel.ts` (merged main's `absolutePath()` addition with our own two successive import reworks).
- [x] Fresh Core Debug build after rebase: 0 errors, regenerated 2 files with a stale nullable annotation from
      conflict resolution back to correct — committed as `2a0072bf3`.
- [x] Full solution Debug build after rebase: 0 errors, 6 pre-existing warnings (not introduced by rebase).

## Phase 1 — extract the gRPC implementation layer out of Core

- [x] Created `Source/Kernel/Grpc/Grpc.csproj`.
- [x] Moved `Core/Services/**` (71 of 76 files: implementations + `CommandExecutor`/`QueryExecutor`/
      `ServiceLogMessages` + `Observers.cs`/`ConnectionService.cs`) to `Grpc/`.
- [x] Relocated the other 5 files (domain exceptions misplaced under `Services/Security/` — `UserNotFound`,
      `InvalidOldPassword`, `NewPasswordMustBeDifferent`, `PasswordConfirmationMismatch`,
      `ApplicationClientIdAlreadyRegistered`) to `Core/Security/`, namespace `Cratis.Chronicle.Security`.
- [x] Repointed `Core.csproj`'s `GenerateGrpcContracts` `--implementations` at `../Grpc`.
- [x] Moved `InternalsVisibleTo Cratis.Chronicle.Server` from `Core.csproj` to `Grpc.csproj`.
- [x] `Server.csproj` references `Grpc.csproj`.
- [x] **Unplanned blocker, resolved:** `Core/Setup/ChronicleServerSiloBuilderExtensions.cs` (Core's Orleans
      silo-wiring entry point) directly constructed all 24 moved implementation classes to build an in-process
      `Contracts.Services` aggregate — a circular dependency once those classes left Core. Resolved by moving the
      whole file into `Grpc.csproj` (its `AddChronicleToSilo` is 96% generic Core/Orleans setup, but the one line
      calling `.AddChronicleServicesAsInMemory()` made it inseparably boundary code) and adding a `Grpc.csproj`
      reference to every caller: `Server`, `Storage.MongoDB.Specs`, `XUnit.Integration` (+ `ChronicleConnection.cs`),
      `Cratis.Chronicle.Testing` (4 files), `Integration/Clustering`, `Benchmarks/Chronicle.Benchmarks.Clustering`.
- [x] Second unplanned fix: `Core/Captures/CapturedEvent.cs` called `EventContext.ToContract()` (a converter that
      moved to Grpc) — fixed with a new `Core/Captures/EventContextConverters.cs`, following the same
      duplicate-rather-than-depend pattern `EventTypeConverters`/`IdentityConverters` already established.
- [x] Release build treats warnings as errors; the 2 pre-existing `CS8601` warnings in generated `Users.cs`/
      `Applications.cs` (noted in Phase 0, predate this work) became Release build failures once Release was
      actually exercised for this code — suppressed via `NoWarn` on `Grpc.csproj` with a comment; the real fix is
      generator-side nullable-annotation work, out of Phase 1 scope.
- [x] Full solution build green: Debug 0 errors, Release 0 errors (1 unrelated pre-existing `CS9057` warning in
      `Samples/SimpleConsole`, nothing to do with this change) — independently re-verified with a fresh
      `rm -rf Server/out` + `dotnet build Chronicle.slnx -c Debug` after the agent's report.
- [x] Specs green: Core.Specs 2854/0 (1 pre-existing skip), Testing.Specs 438/0, Storage.MongoDB.Specs 19/0,
      Server.Specs 66/0, **XUnit.Integration.Specs 66/0 including the embedded-kernel-closure spec (5/5)** — the
      highest-risk check in this phase, since that project ships as a package and the spec verifies its embedded
      dependency closure by computing it from the project files (not a hardcoded list it was easy to game).
- [x] Commits: `b531a0b25` (file moves), `2fc46fdef` (project wiring + consumer repointing).
- [x] Independently re-verified post-agent: `grep` confirms 45 files remain under `Core/` referencing
      `Cratis.Chronicle.Contracts` (the Category-B artifact files — expected, that's Phase 4's job), tree clean,
      commits present.

## Phase 2 — generator: shared-type mirroring, proved on `JobStatus`

- [ ] Add shared-type discovery pass (transitive walk, dedupe by `Type`, visited-set for cycles).
- [ ] Generate shared-type files at the namespace-mapped location; reuse `ProtoMemberIndexReader` for classes,
      explicit enum values for enums.
- [ ] Move `JobStatus` into `Core/Jobs/JobStatus.cs`; repoint `JobSummary`/`JobSummaryConverters`/
      `JobStepSummary`/`JobStepSummaryConverters`.
- [ ] Confirm regenerated `Contracts/Jobs/JobStatus.cs` is byte-identical (modulo header) to the pre-move file.
- [ ] Run `Source/Tools/WireCompatibility` — no new break.
- [ ] **Checkpoint — do not proceed to Phase 3/4 until this is green.**

## Phase 3 — wire up the un-discovered areas

Add `WellKnownServices` + `[BelongsTo]` for each (currently: no `[BelongsTo]` at all → generator silently skips them):

- [ ] `EventSequences` (Core folder `Sequences`)
- [ ] `Projections` (`ProjectionEditor`)
- [ ] `ReadModels` (`ReadModelExplorer` + `ReadModelDefinitions`)
- [ ] `Observation` — Reactors
- [ ] `Observation` — Reducers
- [ ] `Observation` — EventStoreSubscriptions
- [ ] `Compliance`
- [ ] `Host`

(`Observers`, `ConnectionService` intentionally excluded — stay hand-rolled, per `PLAN.md`.)

## Phase 4 — migrate shared types, area by area

Inventory from the Phase-research Explore pass (74 non-`Services/` Core files reference Contracts directly).
Check off as each type is moved + regenerated + wire-verified. Update this list if research turns up more once
Phase 3's areas are wired (their Contracts.* references aren't fully catalogued yet).

- [ ] `JobStatus` (the Phase 2 checkpoint — not started yet)
- [ ] `Identity` (self-referential via `OnBehalfOf` — first real test of cycle handling)
- [ ] `Causation`
- [ ] `EventContext`
- [ ] `ConcurrencyScope`
- [ ] `ObserverFilters`
- [ ] `AuthorizationType` + `BasicAuthorization` + `BearerTokenAuthorization` + `OAuthAuthorization` (one commit)
- [ ] `EventType` / `EventTypeOwner` / `EventTypeSource` (note: Core already has its own `Sequences.EventType` with
      a hand-written converter — this one folds the converter away rather than starting from zero)
- [ ] `ObserverInformation` / `ObserverType` / `ObserverRunningState`
- [ ] `ConstraintType` / `ConstraintScope`
- [ ] `CaptureStatus` / `CaptureValidationMessage`
- [ ] `WebhookTarget` / `WebhookDefinition`
- [ ] `ReadModelType` / `ReadModelObserverType`
- [ ] Whatever Phase 3's newly-wired areas surface beyond the above (fill in as found)

## Phase 5 — remove the reference, verify

- [ ] Delete `ProjectReference` to `Contracts.csproj` from `Core.csproj`.
- [ ] Full solution build (Debug + Release).
- [ ] `dotnet test` (affected projects).
- [ ] `Source/Tools/WireCompatibility` vs. declared baseline.
- [ ] `npx tsc -b`.
- [ ] `yarn lint:ci`.
- [ ] Live-kernel Workbench click-through (MongoDB in Docker, kernel :35000, Vite :9000) — verify every screen
      touched by a shared-type move.

## Log

- **2026-08-21** — Researched the problem (Explore agent inventory of Core→Contracts references; read the
  generator source directly). Rebased onto origin/main. Wrote `PLAN2.md`. Starting Phase 1.
- **2026-08-21** — Phase 1 complete: `Core/Services/**` extracted into `Cratis.Chronicle.Grpc`, including an
  unplanned but necessary extension (`ChronicleServerSiloBuilderExtensions.cs` and its callers) discovered
  mid-execution. Independently re-verified build + reference count. Next: Phase 2 (generator shared-type
  mirroring, proved on `JobStatus`).
