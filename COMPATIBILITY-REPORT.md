# Wire compatibility of 16.x

Chronicle's promise within a major version is that a client built against any earlier release keeps talking to a
later server. This report measures whether 16.x kept it, says exactly where it did not, and proposes how to get back
to keeping it without cutting 17.

Everything below is produced by `Source/Tools/WireCompatibility`, which compares the wire contract at HEAD against
the earliest published release of every 16.x minor. It reads the contracts from NuGet and diffs descriptor sets — it
does not run anything.

```
dotnet run --project Source/Tools/WireCompatibility/WireCompatibility.csproj -- \
  --major 16 --current Source/Kernel/Protobuf/chronicle.desc
```

## Status per released minor

34 stable minors of 16.x were checked. **32 of them are no longer served.**

> "55 breaking changes" is what the wire says, not what was lost. **See the TL;DR at the end of this document**
> before acting on the count.

| Baseline | Status | Baseline | Status |
| --- | --- | --- | --- |
| 16.0.0 | 55 breaking changes | 16.18.0 | 53 breaking changes |
| 16.1.0 | 55 breaking changes | 16.19.0 | 53 breaking changes |
| 16.2.0 | 55 breaking changes | 16.20.0 | 53 breaking changes |
| 16.3.0 | 55 breaking changes | 16.21.0 | 53 breaking changes |
| 16.4.0 | 55 breaking changes | 16.22.0 | 53 breaking changes |
| 16.6.1 | 55 breaking changes | 16.23.0 | 53 breaking changes |
| 16.7.0 | 55 breaking changes | 16.24.0 | 53 breaking changes |
| 16.8.0 | 54 breaking changes | 16.25.0 | 53 breaking changes |
| 16.9.0 | 54 breaking changes | 16.26.0 | 53 breaking changes |
| 16.10.0 | 54 breaking changes | 16.27.0 | 53 breaking changes |
| 16.11.0 | 54 breaking changes | 16.28.0 | 53 breaking changes |
| 16.12.0 | 53 breaking changes | 16.29.0 | 53 breaking changes |
| 16.13.0 | 53 breaking changes | 16.30.0 | 53 breaking changes |
| 16.15.0 | 53 breaking changes | 16.31.0 | 53 breaking changes |
| 16.16.0 | 53 breaking changes | 16.32.0 | 53 breaking changes |
| 16.17.0 | 53 breaking changes | 16.33.0 | 53 breaking changes |
| | | **16.34.0** | **served** |
| | | **16.35.0** | **served** |

The step pattern locates every break precisely. A finding disappears from later baselines exactly when the element
it is about stopped existing, so the release that removed it is the first baseline that no longer reports it:

| Release | Broke | What it did |
| --- | --- | --- |
| **16.8.0** | 1 | Removed `Server/ReloadState` |
| **16.12.0** | 1 | Removed `UniqueEventTypeConstraintDefinition.EventTypeId` (field 1) |
| **16.34.0** | 53 | Replaced the Security and Jobs surfaces, moved two services to new proto packages, widened one field, renamed one enum value |

**One release did 96% of the damage.** 16.34.0 is where the contract broke, and it broke against every release
before it. That is also the good news: this is one change to reason about, not thirty-four.

## The findings

### 1. Security — 11 methods, 2 signatures, 14 messages (16.34.0)

The `Users` and `Applications` services were regenerated from Arc `[Command]`/`[ReadModel]` artifacts. Every method
was renamed or re-shaped, and the hand-written request types were replaced by `*Request` types wrapped in Arc's
`CommandResult` / `QueryResult`.

Gone from `Security.Users`: `Add`, `ChangePassword`, `GetAll`, `GetInitialAdminPasswordSetupStatus`, `ObserveAll`,
`Remove`. Changed shape: `RequirePasswordChange`, `SetInitialAdminPassword` — both went from
`(X) returns (google.protobuf.Empty)` to `(XRequest) returns (CommandResult)`.

Gone from `Security.Applications`: `Add`, `ChangeSecret`, `GetAll`, `ObserveAll`, `Remove`.

Gone messages: `AddUser`, `AddApplication`, `User`, `Application`, `ChangeUserPassword`, `ChangeApplicationSecret`,
`RemoveUser`, `RemoveApplication`, `RequirePasswordChange`, `SetInitialAdminPassword`,
`InitialAdminPasswordSetupStatus`, `IList_User`, `IList_Application`, `SerializableDateTimeOffset`.

### 2. Jobs — 5 methods, 2 signatures, 10 messages, 1 enum, 1 enum value (16.34.0)

Same cause. `Job`/`JobStep` became `JobSummaryResponse`/`JobStepSummaryResponse`, returns became `QueryResult_*`.

Gone: `Jobs/Delete`, `Jobs/GetJob`, `Jobs/GetJobs`, `Jobs/Resume`, `Jobs/Stop`. Changed shape: `Jobs/GetJobSteps`,
`Jobs/ObserveJobs` (the latter also changed request type, `GetJobsRequest` → `ObserveJobsRequest`).

Gone messages: `Job`, `JobStep`, `IEnumerable_Job`, `IEnumerable_JobStep`, `DeleteJob`, `GetJobRequest`,
`GetJobsRequest`, `ResumeJob`, `StopJob`, `OneOf_Job_JobError`. Gone enum: `JobError`.

`JobStepStatus.Unknown` was renamed to `JOB_STEP_STATUS_Unknown`. **Nobody chose this.** It is the proto generator's
`FixEnumValueConflicts` pass kicking in because the new Jobs enums introduced a second `Unknown` in the same proto
package, and the generator prefixed the wrong one. This is a generator artifact, not a contract decision.

### 3. Two services moved proto package (16.34.0)

`EventStores` and `Namespaces` moved out of the root `Cratis.Chronicle.Contracts` package into
`Cratis.Chronicle.Contracts.EventStores` and `Cratis.Chronicle.Contracts.Namespaces`. The gRPC path is
`/<package>.<Service>/<Method>`, so this changed every method path on both services:

```
/Cratis.Chronicle.Contracts.EventStores/AllEventStores          →
/Cratis.Chronicle.Contracts.EventStores.EventStores/AllEventStores
```

Their messages moved with them: `EnsureEventStore`, `EnsureNamespace`, `GetNamespacesRequest`, `IEnumerable_String`.

### 4. `Constraint.RemovedWith` widened (16.34.0) — source-breaking only

Field 3 went from `string` to `repeated string` (commit `d3dd7220c`, "Widen the removal-event field in place
instead of retiring it"). That reasoning was half right: one occurrence of a length-delimited field encodes
identically either way, so **binary decoding survives**. What does not survive is generated code — the property
becomes `string[]`, which is one of the errors the TypeScript client hits when upgraded from 16.13.4 to 16.35.3.

### 5. `Server/ReloadState` removed (16.8.0)

A single method removed with no replacement.

### 6. `UniqueEventTypeConstraintDefinition.EventTypeId` removed (16.12.0)

Field 1 was retired in favor of a plural `EventTypes` and correctly reserved. Reserving protects the number from
reuse; it does not make an older client's data arrive.

### 7. The published non-.NET clients disagree with the server — invisible to this report

This one is not in the 55, and it is arguably worse.

The comparison above reads the contract from C# on both sides, so it can only see changes that reached C#. The
Kotlin, TypeScript and Elixir clients are not generated from C# — they are generated from the committed `.proto`
files, and those files were stale. Regenerating them changed three method shapes that had **never** changed in the
contract itself:

| Method | Committed `.proto` said | The kernel actually serves |
| --- | --- | --- |
| `EventStores/AllEventStores` | `returns (stream QueryResult_IEnumerable_String)` | `returns (QueryResult_IEnumerable_String)` |
| `Jobs/AllJobs` | `returns (stream QueryResult_IEnumerable_JobSummaryResponse)` | `returns (QueryResult_IEnumerable_JobSummaryResponse)` |
| `Namespaces/AllNamespaces` | `returns (stream QueryResult_IEnumerable_String)` | `returns (QueryResult_IEnumerable_String)` |

Every published Kotlin, TypeScript and Elixir client therefore calls these three as server-streaming against a
kernel that answers unary. The separate `Observe*` methods that do stream were missing from those files entirely, as
was the whole `SequenceQueries` service.

Nothing needs restoring here — the contract was always right. The clients need regenerating from correct `.proto`
files, which is now what a build produces. **Do this before regenerating any client, or the regeneration bakes the
same disagreement back in.**

## How this went unnoticed

The generated `.proto` files were being hand-patched, and the generator that produces them was silently failing for
22 of 23 packages while exiting 0 (#3712). The comparison that would have caught all of this never had a trustworthy
input. Both are fixed: generation runs on every `Contracts` build and deletes its previous output first, and the gate
fails on any drift before it compares. See `.agents/PROJECT.md`.

## Plan

### What changed about this plan

The contracts are **generated**, not written. You write Arc `[Command]`/`[ReadModel]` artifacts in
`Source/Kernel/Core`; `GrpcCodeGenerator` turns them into the C# gRPC contracts, and `ProtoGenerator` turns those
into `.proto`. See `.agents/PROJECT.md`. That rules out the obvious remedy — hand-writing deprecated contract
interfaces alongside the generated ones — and it changes the answer.

It changes it because the generated shape is a **consequence** of the artifact, not a choice:

| The wire has | Comes from | Can it be pinned? |
| --- | --- | --- |
| rpc method name | the record's type name | no |
| proto package | the record's namespace | no |
| request message | `<TypeName>Request`, from the record's properties | no |
| command response | always `CommandResult` | no |
| query response | always `QueryResult<T>` | no |

Compare that with what 16.0.0 actually served:

```
16.0.0    rpc Add (AddUser) returns (google.protobuf.Empty)
now       rpc AddUser (AddUserRequest) returns (CommandResult)
```

Every part of that line differs, and **not one part is expressible today**. Naming the method `Add` would mean a
Core record called `Add`; returning `Empty` is unreachable because a generated command always returns
`CommandResult`. So restoring the pre-16.34.0 Security and Jobs surfaces means one of:

1. **Teach the generator a frozen legacy mode** — per-artifact overrides for method name, message name and response
   shape, plus a way to declare a deprecated method that delegates to a live one. This is a real generator feature,
   and it permanently entangles the generator with one historical surface.
2. **Hand-write the legacy contracts anyway**, in the layer we just declared generated-only, and exempt them from
   the regeneration that deletes its own previous output.
3. **Don't restore those shapes.**

Option 2 is out. Option 1 buys back a surface whose only in-repo consumer ships inside the kernel. So the plan below
splits on a single line: **what generation can express, fix; what it cannot, take to 17.0.0.**

### Fix now — these are expressible, and two are outright bugs

#### Step 1 — `JobStepStatus.Unknown` ★ start here

**This is a generator bug, not a contract decision.** `ProtoSchemaHelper.FixEnumValueConflicts` renamed
`JobStepStatus.Unknown` to `JOB_STEP_STATUS_Unknown` because the new Jobs enums introduced a second `Unknown` in the
same proto package — and it prefixed the pre-existing enum rather than the arriving one. Make the pass prefer the
incumbent, or give the new enum a non-colliding value name in C#.

**1 finding. No deprecated surface, no generator feature, no risk.**

#### Step 2 — `UniqueEventTypeConstraintDefinition.EventTypeId`

Field 1, retired at 16.12.0 in favor of a plural `EventTypes` and reserved. Un-reserve it and add the singular
property back to the Core artifact, populated from the first entry of the plural, marked obsolete. Expressible
because it is a property on a record, and property names and numbers are exactly what the generator preserves
(`ProtoMemberIndexReader` keeps the index across regeneration).

**1 finding.**

#### Step 3 — `Server/ReloadState`

A method removed at 16.8.0 with nothing in its place. Add a `ReloadState` artifact back to Core that either does
what the name now means or fails with a message naming its replacement. Either beats an unimplemented method.

**1 finding.** Note the generated name will be `ReloadState` only if the record is called `ReloadState` — which is
the point of the rule above.

#### Step 4 — `Constraint.RemovedWith`: accept and record

Already binary-compatible; only generated client code breaks. Reverting costs a field number to fix a compile error
in code that has to be regenerated anyway. **Accept it, note it in the release notes, move on.**

#### Step 5 — the two moved proto packages: decide, don't drift

`EventStores` and `Namespaces` moved package because their Core artifacts live in `Cratis.Chronicle.EventStores` and
`Cratis.Chronicle.Namespaces`, and the generator derives the package from the namespace (`--skip-namespaces 2
--base-namespace Cratis.Chronicle.Contracts`). Moving them back means moving the Core artifacts back to the root
namespace, which fights the structure everything else follows.

**Recommendation: leave them, and let 17.0.0 carry it.** Special-casing two services' packages to preserve a path
would put a permanent exception into the one rule that makes the pipeline predictable. **6 findings deferred.**

### Take to 17.0.0 — the Security and Jobs surfaces

46 of the 55 findings. Every one is a name-or-shape change that generation cannot express, and their only consumer
inside this repository is `Source/Clients/Api` — the Workbench's own backend, which ships **with** the kernel and
cannot be version-skewed against it. The .NET SDK exposes Jobs (`Source/Clients/DotNET/Jobs`) but not Security.

Before committing to this, answer one question: **did any client outside this repository ever call
`Security.Users`, `Security.Applications` or `Jobs`?** If the answer is no — and the shape of these surfaces
suggests it is — then 16.34.0 broke a contract nobody was holding, and the correct repair is to say so in a major
rather than to build a generator feature to un-say it.

If the answer turns out to be yes for Jobs specifically, Jobs alone is 19 findings and the smallest possible
version of generator option 1 above: a `[GrpcMethod("GetJobs")]`-style name pin plus a raw-response escape hatch,
scoped to that one service.

### Then — regenerate the Kotlin, TypeScript and Elixir clients

Finding 7, independent of everything above: those clients were generated from `.proto` files that misdescribed three
methods and omitted an entire service. A build now produces correct ones. **Do this before upgrading any client**,
or the regeneration bakes the same disagreement back in.

### Then — close the generation loop

`GenerateGrpcContracts` in `Core.csproj` is still gated off (`DisableProxyGenerator` defaults to `true`) because the
Observation artifacts in Core do not yet replace the hand-written `Contracts/Observation/IObservers.cs`. Until that
is converted and the target ungated, the contracts for converted services are checked in and can drift from Core —
and the gate compares generated output to generated output, so it cannot see drift that never reached Contracts.

**This is the single highest-value follow-up.** It is what makes the wire contract genuinely derived, and it is the
same work as the Api migration (see `API-MIGRATION.md`), because both are blocked on getting Core's artifacts to
cover the whole surface.

### Sequencing

1. Steps 1–4 land in any patch. They cost nothing and remove 3 of the 55 findings plus a real generator bug.
2. Regenerate and republish the three non-.NET clients.
3. Decide the Security/Jobs question. If 17.0.0, cut it — the gate then holds 17.x from its first release, which is
   the state this whole exercise exists to reach.
4. Convert Observation, ungate `GenerateGrpcContracts`, delete the hand-written contracts.

Until step 3, the gate fails every pull request. That is correct — but it is also blocking, so land Steps 1–4 early
to shrink what it reports while the larger decision is being made.

## Reproducing this

```
# every minor of the current major
dotnet run --project Source/Tools/WireCompatibility/WireCompatibility.csproj -- \
  --major 16 --current Source/Kernel/Protobuf/chronicle.desc

# one release, when working through a single fix
dotnet run --project Source/Tools/WireCompatibility/WireCompatibility.csproj -- \
  --baseline 16.33.0 --current Source/Kernel/Protobuf/chronicle.desc
```

Exit 0 means nothing broke, 1 means something did, 2 means the comparison could not be made.

---

# Appendix: what actually drifted, and why

The obvious reading of "55 breaking changes" is that 16.34.0 deleted a large amount of API. It did not. This
appendix answers the question directly, by comparing the whole generated surface of 16.33.0 against HEAD.

```
# how this was produced
curl -sL https://api.nuget.org/v3-flatcontainer/cratis.chronicle.contracts/16.33.0/\
cratis.chronicle.contracts.16.33.0.nupkg -o pkg.nupkg && unzip pkg.nupkg
dotnet run --project Source/Tools/ProtoGenerator/ProtoGenerator.csproj -- \
  lib/net10.0/Cratis.Chronicle.Contracts.dll ./proto
```

## The API surface did not shrink

Counting every rpc in every proto package:

| | 16.33.0 | HEAD |
| --- | --- | --- |
| **Total rpc methods** | **127** | **126** |

And the difference reconciles exactly:

```
127  at 16.33.0
 -3  operations genuinely removed  (Users/GetAll, Applications/GetAll, Jobs/GetJob)
 +1  added since                   (Compliance)
 +1  added by this branch          (ConnectionService/CheckCompatibility)
────
126  at HEAD
```

**Three operations were lost. The other ~50 findings are the same operations under different names.**

## What caused the drift

16.34.0 is where the Security and Jobs surfaces stopped being **hand-written gRPC contracts** and started being
**generated from Arc artifacts in Core**. Nothing about the behavior changed. What changed is that the names and
shapes stopped being chosen and started being derived — and the derivation rules differ from what a human had
written by hand:

| | Hand-written contract | Derived from a Core artifact |
| --- | --- | --- |
| Method name | chosen, short, scoped by the service (`Add` on `Users`) | the record's type name (`AddUser`) |
| Request message | chosen (`AddUser`) | `<TypeName>Request` (`AddUserRequest`) |
| Response | chosen (`Empty`, `IList_User`) | always `CommandResult` / `QueryResult<T>` |
| Proto package | chosen (`Cratis.Chronicle.Contracts`) | the record's namespace (`…Contracts.EventStores`) |

A hand-written `Users` service is free to call its method `Add`, because `Users.Add` reads fine. A generated one
cannot: the record has to be called something unique within its namespace, so it is `AddUser`, and the method is
therefore `AddUser`. Every rename in this report follows from that one rule.

## The mapping, in full

Nothing here is missing functionality — read across.

**Users** — 8 rpcs → 7

| 16.33.0 | HEAD | |
| --- | --- | --- |
| `Add` | `AddUser` | renamed |
| `ChangePassword` | `ChangeUserPassword` | renamed |
| `Remove` | `RemoveUser` | renamed |
| `GetInitialAdminPasswordSetupStatus` | `GetStatus` | renamed |
| `GetAll` + `ObserveAll` | `AllUsers` | **two collapsed into one observable** |
| `RequirePasswordChange` | `RequirePasswordChange` | same name, `Empty` → `CommandResult` |
| `SetInitialAdminPassword` | `SetInitialAdminPassword` | same name, `Empty` → `CommandResult` |

**Applications** — 5 rpcs → 4

| 16.33.0 | HEAD | |
| --- | --- | --- |
| `Add` | `AddApplication` | renamed |
| `ChangeSecret` | `ChangeApplicationSecret` | renamed |
| `Remove` | `RemoveApplication` | renamed |
| `GetAll` + `ObserveAll` | `AllApplications` | **two collapsed into one observable** |

**Jobs** — 7 rpcs → 6

| 16.33.0 | HEAD | |
| --- | --- | --- |
| `Delete` | `DeleteJob` | renamed |
| `Resume` | `ResumeJob` | renamed |
| `Stop` | `StopJob` | renamed |
| `GetJobs` | `AllJobs` | renamed |
| `GetJobSteps` | `GetJobSteps` | same name, response reshaped |
| `ObserveJobs` | `ObserveJobs` | same name, request and response reshaped |
| `GetJob` | — | **removed, no counterpart** |

**EventStores and Namespaces** — 6 rpcs → 6, every path changed

These did not lose a thing. Their Core artifacts live in `Cratis.Chronicle.EventStores` and
`Cratis.Chronicle.Namespaces`, so the generator put them in matching proto packages instead of the root one:

| 16.33.0 | HEAD |
| --- | --- |
| `Cratis.Chronicle.Contracts.EventStores/Ensure` | `…Contracts.EventStores.EventStores/EnsureEventStore` |
| `Cratis.Chronicle.Contracts.EventStores/GetEventStores` | `…Contracts.EventStores.EventStores/AllEventStores` |
| `Cratis.Chronicle.Contracts.EventStores/ObserveEventStores` | `…Contracts.EventStores.EventStores/ObserveEventStores` |
| `Cratis.Chronicle.Contracts.Namespaces/Ensure` | `…Contracts.Namespaces.Namespaces/EnsureNamespace` |
| `Cratis.Chronicle.Contracts.Namespaces/GetNamespaces` | `…Contracts.Namespaces.Namespaces/AllNamespaces` |
| `Cratis.Chronicle.Contracts.Namespaces/ObserveNamespaces` | `…Contracts.Namespaces.Namespaces/ObserveNamespaces` |

Note `ObserveEventStores` and `ObserveNamespaces`: **identical names, still broken**, because a gRPC path is
`/<package>.<Service>/<Method>` and the package moved underneath them.

**Messages** follow the same pattern — every one of the 14 "gone" Security messages has a counterpart:

`AddUser`→`AddUserRequest` · `AddApplication`→`AddApplicationRequest` ·
`ChangeUserPassword`→`ChangeUserPasswordRequest` · `ChangeApplicationSecret`→`ChangeApplicationSecretRequest` ·
`RemoveUser`→`RemoveUserRequest` · `RemoveApplication`→`RemoveApplicationRequest` ·
`RequirePasswordChange`→`RequirePasswordChangeRequest` · `SetInitialAdminPassword`→`SetInitialAdminPasswordRequest` ·
`User`→`UserResponse` · `Application`→`ApplicationResponse` ·
`InitialAdminPasswordSetupStatus`→`AdminPasswordStatusResponse` ·
`IList_User`→`QueryResult_IEnumerable_UserResponse` · `IList_Application`→`QueryResult_IEnumerable_ApplicationResponse`

In Jobs, `Job`→`JobSummaryResponse`, `JobStep`→`JobStepSummaryResponse`, `IEnumerable_Job`→`QueryResult_…`. The
`JobError` enum and `OneOf_Job_JobError` are gone because Arc's `ValidationResult` replaced the OneOf error model —
a genuine change of error contract, though not a lost operation.

## What was genuinely lost

Five things, across all of 16.x:

| | Release | Replacement |
| --- | --- | --- |
| `Jobs/GetJob` — fetch one job by id | 16.34.0 | none |
| `Users/GetAll` — snapshot, non-observable | 16.34.0 | observable `AllUsers` only |
| `Applications/GetAll` — snapshot, non-observable | 16.34.0 | observable `AllApplications` only |
| `Server/ReloadState` | 16.8.0 | none |
| `UniqueEventTypeConstraintDefinition.EventTypeId` | 16.12.0 | plural `EventTypes` |

## One real regression was hiding inside a rename

`Job` → `JobSummaryResponse` looks like a rename. Compare the field:

```
16.33.0   message Job { ... SerializableDateTimeOffset Created = 5; }
HEAD      message JobSummaryResponse { ... DateTimeOffset Created = 5; }

HEAD      message DateTimeOffset {
          }
```

`DateTimeOffset` is an **empty message**. The hand-written contract used the explicit `SerializableDateTimeOffset`
surrogate; the Core record declares a plain `DateTimeOffset`, and the schema generator emits an opaque empty
message for it. protobuf-net has a runtime surrogate, so **.NET-to-.NET still works** — but every protoc-generated
client (Kotlin, TypeScript, Elixir) sees a message with no fields, so a job's creation time does not arrive.

The same empty `DateTimeOffset` types **10 fields across four packages**: `JobSummaryResponse.Created`,
`UserResponse.CreatedAt` / `.LastModifiedAt`, `ApplicationResponse.CreatedAt` / `.LastModifiedAt`,
`EventSequenceQueryCriteria.OccurredFrom` / `.OccurredTo`, `HistogramBucket.Occurred`, and
`SequenceQueryDefinition.OccurredFrom` / `.OccurredTo`.
**This is the most damaging finding in the report and the gate did not flag it**, because the gate compares
C#-derived contracts on both sides and the type is "the same" on both. Fix: teach the generator to map
`System.DateTimeOffset` to the `SerializableDateTimeOffset` surrogate it already emits elsewhere, or declare the
surrogate in the Core artifacts.

## What this changes about the plan

It makes cutting 17.0.0 more attractive and the shim less defensible. The Security and Jobs "breakage" is 46
findings that describe **three lost operations and a large amount of renaming**. Building a generator feature to
re-serve `Users/Add` alongside `Users/AddUser` would preserve names, not capability — and it would permanently
freeze one historical naming scheme into a pipeline whose whole point is that names are derived.

Two things do deserve action regardless of the 17.0.0 decision, and neither is a shim:

1. **The empty `DateTimeOffset`** — a live cross-language data-loss bug, unrelated to versioning. Fix it now.
2. **`Jobs/GetJob`** — the only removed operation with no replacement at all. If anything called it, it should
   come back as a Core artifact (`GetJob`, and the generated name follows).

---

# Appendix: would an `[Alias]` attribute be enough?

The proposal: an `[Alias]` usable on records and on methods, alongside `[BelongsTo]`, so a generated artifact can
declare the wire name it used to have. Checked against every finding, the answer is **yes for the command surface,
no for the query surface** — and the reason is not naming at all.

## First, a design constraint: `[Alias]` has to be additive

If `[Alias("Add")]` *renames* the generated output, 16.33.0 is served and **16.34.0 and 16.35.0 break** — the only
two baselines currently green. That trades one break for another, and the gate would say so immediately.

So the attribute must make the generator emit **both** rpcs, routed to the same handler:

```
rpc Add     (AddUser)        returns (CommandResult);   // alias, for 16.0.0–16.33.0
rpc AddUser (AddUserRequest) returns (CommandResult);   // canonical
```

That also means an aliased record emits **two message types with identical fields** — one under each name. Harmless
on the wire, but it is real duplication in the generated output.

## Commands: `[Alias]` genuinely works

The request messages are already structurally identical. Only the name differs:

```
16.33.0   message AddUser        { .bcl.Guid UserId = 1; string Username = 2; string Email = 3; string Password = 4; }
HEAD      message AddUserRequest { .bcl.Guid UserId = 1; string Username = 2; string Email = 3; string Password = 4; }
```

Same for `ChangeUserPassword`, `RemoveUser` and the rest — every field name, number and type matches. An alias on
the record restoring the message name makes the request **wire-identical**.

The response looks like a problem and is not. 16.33.0's commands returned `google.protobuf.Empty`; they now return
`CommandResult`. `Empty` declares no fields, so a 16.33.0 client decoding a `CommandResult` payload **skips every
field as unknown and succeeds**. The call works.

One caveat worth stating plainly: it works by *discarding the result*. A 16.33.0 client would stop seeing
validation failures and read every command as success. That is a quieter failure than a broken call, and it should
be a documented consequence of the alias rather than a surprise.

## Queries: `[Alias]` does not help, and naming was never the obstacle

Query rpc names come from the **static method name on the read model**, not the record type — `JobSummary.AllJobs`
→ `rpc AllJobs`, `User.AllUsers` → `rpc AllUsers`. That name is already free. Renaming the method to `GetAll` today
would produce `rpc GetAll` with no new attribute at all.

The obstacle is the envelope:

```
16.33.0   message IList_User { repeated User items = 1; }

HEAD      message QueryResult_IEnumerable_UserResponse {
             .bcl.Guid CorrelationId = 1;
             bool IsAuthorized = 2;
             repeated ValidationResult ValidationResults = 3;
             repeated string ExceptionMessages = 4;
             string ExceptionStackTrace = 5;
             repeated UserResponse Data = 6;      ← the payload moved from field 1 to field 6
          }
```

A 16.33.0 client reads field 1 expecting `repeated User` and finds a `bcl.Guid`. This is not a name mismatch that
an alias can paper over — the payload is at a different field number, behind a different type. **No aliasing of any
kind reaches this.** Serving it would need the generator to emit an *unwrapped* response for aliased queries: a
different feature, and a much larger one, because it changes the response contract rather than a label.

## What each mechanism reaches

| | Findings | `[Alias]` on records + methods |
| --- | --- | --- |
| Command rpc names (`Add`→`AddUser`, `Stop`→`StopJob`, …) | 9 | **yes** |
| Command request messages (`AddUser`→`AddUserRequest`, …) | 13 | **yes** |
| Command signature changes (`Empty`→`CommandResult`) | 2 | **yes** — decode-safe, result discarded |
| Entity messages (`User`→`UserResponse`, `Job`→`JobSummaryResponse`) | 5 | **yes**, once the empty `DateTimeOffset` is fixed |
| `SerializableDateTimeOffset` | 1 | comes back with that same fix |
| Query rpc names | 7 | not needed — rename the static method |
| **Query response envelopes** (`IList_User` → `QueryResult_…`) | **5** | **no — needs an unwrapped-response mode** |
| **Query signature changes** (`GetJobSteps`, `ObserveJobs`) | **2** | **no — same reason** |
| Proto package move (`EventStores`, `Namespaces`) | 6 | **no** — needs `[BelongsTo(service, package:)]`, not a type alias |
| `JobError` enum / `OneOf_Job_JobError` | 2 | no — the error model was replaced, not renamed |
| `Jobs/GetJob` | 1 | no — deleted; needs a new Core artifact |
| `GetAll` + `ObserveAll` → `AllUsers` | 2 | no — one artifact cannot be two rpcs with different call shapes |

**Roughly 30 of 55 with `[Alias]` alone — the entire command surface.** The remaining ~20 split into three
independent problems: the query envelope (needs a second generator feature), the proto package (needs `BelongsTo`
to carry one), and three operations that have to be written back as artifacts.

## The recommendation

`[Alias]` is worth adding — but the strongest argument for it is not 16.x.

**Adopt it for what it does going forward.** It decouples the wire name from the C# type name permanently, so
renaming a Core record stops being a breaking change. That is a structural gap in the pipeline as it stands today:
right now the record's name *is* public API, and nobody renaming a domain type is thinking about that. `[Alias]`
closes it, and every future rename becomes free instead of a major.

**For 16.x specifically, it changes the arithmetic but not the conclusion.** It clears the command surface
cleanly. It does not touch the query envelope, which is the deeper half, and reaching that means an
unwrapped-response mode that permanently encodes a pre-Arc response shape into the generator. Weigh that against
what it buys: compatibility for the `Users`, `Applications` and `Jobs` surfaces, whose only in-repo consumer ships
inside the kernel.

So the sequence I would suggest:

1. **Add `[Alias]` now, additively**, for the forward-looking reason. Small, self-contained, and it makes the next
   rename a non-event.
2. **Extend `[BelongsTo]` with an optional package** — same argument, and it makes the `EventStores`/`Namespaces`
   move recoverable without relocating Core artifacts.
3. **Fix the empty `DateTimeOffset`** — unrelated to all of this and the most damaging finding in the report.
4. **Then decide on 17.0.0** with the query envelope as the deciding question, not the names. If nothing outside
   this repository calls those queries, 17.0.0 is still the smaller job.

---

# TL;DR

**What happened.** In 16.34.0 the Security and Jobs APIs stopped being hand-written gRPC contracts and started
being generated from the Arc commands and queries in Core. The behavior did not change. The names and shapes did,
because the generator derives them instead of a person choosing them.

**Nothing was really removed.** 127 rpc methods before, 126 after. Three operations genuinely went away. The other
~50 "breaking changes" are the same operations wearing generated names — `Add` became `AddUser`, `GetJobs` became
`AllJobs`, `Job` became `JobSummaryResponse`.

**Where the breakage is.** Two places:

1. **Names.** Every method and message got renamed, so every old client's calls miss.
2. **Query responses.** Results are now wrapped in an Arc envelope. The actual data moved from field 1 to field 6.
   An old client reads field 1 and finds a correlation id where it expected a list.

**Why it is hard to undo.** Names we could alias back. The envelope we cannot — the payload sits at a different
field number behind a different type, and no renaming reaches that. And we can no longer hand-write the old
contracts, because contracts are generated now. So restoring 16.33 means building new generator features and
freezing a pre-Arc response shape into them permanently.

**Recommendation.** Cut 17.0.0 for Security and Jobs. Their only consumer in this repo ships inside the kernel, so
we would be building machinery to preserve compatibility nobody is using.

**One thing to fix regardless.** The switchover left `DateTimeOffset` as an empty message on 10 fields — job
created-times, user timestamps, event query date ranges. .NET clients are fine; Kotlin, TypeScript and Elixir
receive nothing. That is a live data-loss bug, unrelated to versioning, and it is the most damaging thing in this
report.
