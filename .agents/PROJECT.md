# Chronicle — Project-Specific Instructions

Project-local context for this repository. See `.claude/CLAUDE.md` for the shared Cratis corpus.

## Running the local stack

The Workbench is a Vite dev server that proxies to the Chronicle Kernel:

| Piece | Address | Notes |
| --- | --- | --- |
| Workbench (dev) | `http://localhost:9000` | `yarn dev` from `Source/Workbench` |
| Chronicle Kernel | `https://localhost:35000` | **HTTPS** with a dev certificate — use `curl -k`; plain `http://` returns an empty reply |
| Orleans gateway / silo | `127.0.0.1:30000` / `127.0.0.1:11111` | |
| MongoDB | `localhost:27017` | Runs in Docker |

The kernel serves everything on the single TLS port; the Vite proxy sets `secure: false` for it.

## Workbench credentials (local development)

The Workbench requires signing in before any view loads — unauthenticated requests to
`/.cratis/me` and `/api/event-stores` return `401` and the app redirects to `/login`.

| Username | Password |
| --- | --- |
| `Admin` | `ChangeMeNow!` |

These are the local development defaults only. They are not valid anywhere else, and nothing
outside a developer machine should ever accept them.

## Verifying Workbench behavior

Drive the Workbench through the browser tooling rather than guessing from source — several of its
views are fed by observable (SSE) queries whose behavior only shows up at runtime:

- Sign in first, or every view renders empty and looks like a data bug.
- Inspect the SSE frames when a live view misbehaves. Observable queries default to `Delta`
  transfer mode, so a frame carries a `changeSet` (`added` / `replaced` / `removed`) and an empty
  `data` array — the client reconstructs the collection from the previous state.
- Delta reconciliation matches items by a property named `id` on both the server
  (`ChangeSetComputor`) and the client (`useObservableQuery`). A read model without an `id`
  falls back to whole-payload JSON equality, which is fragile for models carrying a changing
  timestamp. Check this first when a live list grows, duplicates, or fails to drop rows.

## The wire protocol is generated from Core — don't think about the protocol

Chronicle's gRPC surface is **derived, end to end**. You write Arc commands and queries in `Source/Kernel/Core`;
everything below that is generated:

```
Source/Kernel/Core                 Arc artifacts — [Command] / [ReadModel] records with [BelongsTo(service)]
        │                          ← this is the only layer you write
        │  GrpcCodeGenerator       (Source/Tools/GrpcCodeGenerator, run from Core.csproj)
        ▼
Source/Kernel/Contracts            generated C# gRPC contracts — I<Service>.cs + [ProtoContract] DTOs
        │
        │  ProtoGenerator          (Source/Tools/ProtoGenerator, run from Contracts.csproj)
        ▼
Source/Kernel/Protobuf             generated .proto files + chronicle.desc
        │
        │  protoc                  (in the publish workflows)
        ▼
Kotlin · TypeScript · Elixir       generated client contracts packages
```

**We no longer hand-write gRPC contract interfaces or message types.** A hand-written `IWhatever.cs` under
`Source/Kernel/Contracts` is legacy that has not been converted yet, not a pattern to copy.

### What you actually write

An Arc artifact in Core. `[BelongsTo]` says which gRPC service it joins:

```csharp
[Command]
[BelongsTo(WellKnownServices.Users)]
public record AddUser(Guid UserId, string Username, string Email, string Password)
{
    internal async Task Handle(IGrainFactory grainFactory) { ... }
}
```

```csharp
[ReadModel]
[BelongsTo(WellKnownServices.Jobs)]
public record JobSummary(Guid Id, string Details, JobStatus Status, DateTimeOffset Created)
{
    internal static async Task<IEnumerable<JobSummary>> AllJobs(string eventStore, string @namespace, IGrainFactory grainFactory) { ... }
    internal static ISubject<IEnumerable<JobSummary>> ObserveJobs(string eventStore, string @namespace, IStorage storage) { ... }
}
```

### `[BelongsTo]` and `WellKnownServices`

`[BelongsTo]` groups artifacts into a gRPC service. Artifacts carrying the same service name land on the same
`I<Service>` interface in Contracts and the same `service` block in the `.proto`, regardless of which file they
live in. Service names are constants on `Source/Kernel/Core/WellKnownServices.cs` — never a string literal, because
a typo silently creates a second service:

```csharp
public static class WellKnownServices
{
    public const string Users = "Users";
    public const string Applications = "Applications";
    public const string Jobs = "Jobs";
    public const string Observers = "Observers";
    public const string Namespaces = "Namespaces";
    public const string EventStores = "EventStores";
}
```

Adding a service means adding a constant here first. The proto **package** does not come from `[BelongsTo]` — it
comes from the artifact's namespace, with the leading segments configured in `Core.csproj`
(`--skip-namespaces 2 --base-namespace Cratis.Chronicle.Contracts`). So `Cratis.Chronicle.Jobs` becomes package
`Cratis.Chronicle.Contracts.Jobs`.

### What the generator derives, and from what

| Wire element | Derived from |
| --- | --- |
| gRPC service | the `[BelongsTo]` constant |
| proto package | the artifact's namespace, minus the skipped segments |
| **command** rpc name | the **record's type name** (`AddUser` → `rpc AddUser`) |
| **query** rpc name | the **static method's name** (`JobSummary.AllJobs` → `rpc AllJobs`) |
| request message | `<Name>Request`, from the record's properties or the method's parameters |
| command response | always `CommandResult` |
| query response | always `QueryResult<T>`, with the payload in a `Data` field |
| read-model message | `<ReadModelName>Response` |

The consequences are worth internalizing, because none of them look like API changes while you are making them:

- **Renaming a Core record renames an rpc.** The record's name *is* public API.
- **Moving a Core artifact between namespaces moves its proto package**, which changes every gRPC path on the
  service — even for methods whose own names did not change.
- **A query's rpc name is free** (rename the static method), but its response shape is not.
- **`[ProtoMember]` indexes survive regeneration** via `ProtoMemberIndexReader`, which reads the previous output
  before overwriting. Never renumber by hand.

### Transport types — domain types that cannot travel

Core declares what the **domain** means: `DateTimeOffset`, not a serializable stand-in. How a value travels is a
transport concern and is handled at the edge, by `TransportTypes` in the generator, which substitutes a contract
primitive on the way into Contracts.

Today that is `DateTimeOffset` → `SerializableDateTimeOffset` (an ISO 8601 string, and what the pre-16.34.0
contracts used, so already-published clients understand it).

**This exists because the failure mode is silent.** protobuf-net emits an opaque *empty* message for a type it
cannot represent — a schema that parses, generates and compiles while carrying nothing. Ten fields shipped that
way; protobuf-net's runtime surrogate hid it from .NET, so only Kotlin, TypeScript and Elixir lost the values.

So `TransportTypes` does not merely map — it **classifies**. A type protobuf cannot represent is either given a
stand-in or generation **fails** with `UnrepresentableTransportType`. To add one:

1. Add a `[ProtoContract]` primitive under `Source/Kernel/Contracts/Primitives`, with implicit conversions **both
   ways** so the hand-written service implementations keep compiling without casts.
2. Map it in `TransportTypes`.

Never work around it by declaring a transport type in a Core artifact.

### Current state of the conversion

Generation from Core into Contracts is wired up in `Core.csproj` (`GenerateGrpcContracts`) but **gated off**:
`DisableProxyGenerator` defaults to `true` there. Two reasons, both temporary:

- `Contracts/Observation/IObservers.cs` claims to be generated but has drifted: it carries five methods Core cannot
  produce yet — `GetObserverInformation`, `GetConnectedClientsForObserver`, `WaitForCompletion`,
  `GetReplayableObserversForEventTypes` and `ClearObserverQuarantine`. Regenerating drops them.
- Most of the Web API surface still lives in `Source/Clients/Api` rather than Core. See `API-MIGRATION.md`.

To regenerate deliberately (which is how the `DateTimeOffset` fix was applied):

```bash
dotnet build Source/Kernel/Core/Core.csproj -c Debug \
  -p:DisableProxyGenerator=false -p:CopyLocalLockFileAssemblies=true -p:CratisProxiesOutputPath=
git checkout -- Source/Kernel/Contracts/Observation/IObservers.cs   # until Observation is converted
```

`CopyLocalLockFileAssemblies=true` is required: the generator resolves referenced assemblies from the output
directory, and a library project does not copy its NuGet runtime assets there, so `Orleans.Serialization` is
missing and generation aborts.

Converting Observation and ungating this target is what makes the wire contract genuinely derived. Until then the
contracts for converted services are checked in and can drift from Core.

### Hand-patching generated output is off limits

Everything under `Source/Kernel/Protobuf` is generated from `Source/Kernel/Contracts` — the `.proto` files and the
`chronicle.desc` descriptor set alike. **Editing any of them by hand is off limits.** They are not "generated but
you can touch them up"; they are output. The same goes for the generated files under `Source/Kernel/Contracts`,
which carry an `<auto-generated />` header.

Building `Contracts.csproj` regenerates them, and the generator **deletes** the previous files before writing rather
than overwriting them, so a hand-applied edit is removed by the next build rather than quietly surviving in a file
nobody re-reads. `Source/Kernel/Protobuf/generate-protos.sh` does the same thing on demand.

This is enforced rather than requested because it already went wrong: while the generator was silently failing for
22 of 23 packages (#3712) and still exiting 0, `eventsequences.proto` was hand-patched with three fields, an entire
`SequenceQueries` package went unemitted for months, and the published Kotlin, TypeScript and Elixir clients were
generated from those files. A generated file somebody can edit and keep is a second, unversioned source of truth for
the wire format.

**To change the wire contract, change the C# in `Source/Kernel/Contracts` and build.** If the generator will not
produce what you need, fix the generator (`Source/Tools/ProtoGenerator`) — do not work around it in its output.

Two gates back this up, both in `.github/workflows/wire-compatibility.yml`, which runs before anything else builds:

- The committed generated files must match what the contracts produce, or the build fails as stale.
- The wire contract must still serve every released minor of the current major (`Source/Tools/WireCompatibility`).
  Breaking it deliberately means labeling the pull request `major`.
