<!-- cratis-ai-managed: skills/cratis-chronicle-client-elixir/references/upgrading.md -->
<!-- Copyright (c) Cratis. All rights reserved. -->
<!-- Licensed under the MIT license. See LICENSE file in the project root for full license information. -->

# Upgrading from older clients

Source: `Cratis/Chronicle.Elixir` (`Source/chronicle/mix.exs`, `mix.lock`, `lib/chronicle/wire_result.ex`, `jobs.ex`, `event_sequences/event_log.ex`, `read_models.ex`, `seeding.ex`).

## Dependencies and wire compatibility

The wire contracts target the current Chronicle kernel generations, with renamed RPCs and unwrapped response envelopes. No public Elixir API was removed, but unauthorized or excepted command responses return `{:error, reason}` rather than looking successful. `Jobs.get/2` lists jobs then finds the id client-side; `append_many_for_event_sources/2` uses its dedicated RPC rather than the single-source path.

- Use the `~>` requirement for the published release you intend (`{:cratis_chronicle, "~> 3.10"}` style) and the Elixir version `mix.exs` declares.
- grpc 1.x and Mint are direct dependencies; do not carry a mandatory `GRPC.Client.Supervisor` child forward from an older grpc setup.
- Contracts resolve from the lockfile. Pair the kernel with the installed contracts descriptor; named event source definitions need a kernel that supports them (see below).
- The first append on each channel runs a compatibility check, shared until reconnect. `{:error, {:incompatible_server, response}}` is a version/descriptor problem, not a transient append rejection. Update kernel and contracts as a compatible pair; do not bypass the preflight. Failed transport calls return `{:error, %GRPC.RPCError{}}`.

## Behavior to review in an older application

An application written against an older client may have relied on behavior that is now corrected:

| Behavior | Review |
| --- | --- |
| Reducers register successfully and process from the log start | Old events reach reducers that previously failed registration |
| Multi-word projection fields and AutoMap populate | Remove camelCase-string workarounds; inspect fields that were formerly empty |
| `unique(:owner_email)` and `remove_constraint` registration work | Uniqueness is enforced and release events can remove constraints |
| `ReadModels.query/2` is zero-based, default page 0 | Change first-page callers to `page: 0`; `page: 1` is the second page |
| Seeding skips an event source with existing events | Already-stored duplicates are not removed |
| Tail/next sequence lookups do not narrow implicitly to `"Default"` | Audit callers that depended on implicit routing narrowing |

Rejected appends (including concurrency/constraint failures) return `{:error, reason}`, never `:ok`; batch append metadata survives the rich append RPC. Handle errors on single, batch and transactional paths.

Declared `unique(..., message: ...)` violation messages are preserved on single, batch and transactional appends. Retain the constraint name and diagnostics when handling violations rather than assuming every message came from the kernel.

Requires `cratis_chronicle` 3.10.0 and Chronicle kernel 19.30.0 for named event source definitions.

## Behavior changes to review when raising the requirement

Requires `cratis_chronicle` 3.13.0 for the behaviors below; each is a difference an older application can observe.

| Behavior | Review |
| --- | --- |
| Reactor and reducer `context` is a `Chronicle.Events.EventContext` struct | Pattern matches on a bare map keep working; `correlation_id`, `caused_by`, `causation`, `tags`, `subject`, `appended_generation` and `replaying?` are now present |
| `drift_policy` defaults to `:warn` | Start-up makes extra kernel calls and logs findings; set `:ignore` to skip them, `:fail` to block on a drift warning |
| Discovery warns once per module for a reactor, reducer or projection with no pinned `:id` | Pin ids before renaming or moving modules; see [discovery-and-generations.md](discovery-and-generations.md) |
| Appending a struct that is not an event type returns `{:error, {:unknown_event_type, module}}` | Handle it as an ordinary append error |
| Spans and `:telemetry` events are always emitted | No action without an SDK; see [observability.md](observability.md) |
| The client installs a Logger filter for Mint response crash reports | Keep your own production filter for the remaining crash shapes |
| `children/2,3` and projection mistakes are rejected at compile time | Fix declarations that assigned one target twice or named a missing field |

Requires `cratis_chronicle` 3.11.0 for the context struct; it was a bare map before.

Capabilities covered by these references: decision reads ([decision-reads.md](decision-reads.md)), `Chronicle.Testing` ([testing.md](testing.md)), captures ([captures.md](captures.md)), standalone constraints ([constraints.md](constraints.md)), `Chronicle.Patterns` ([patterns.md](patterns.md)) and read model subscriptions ([read-models.md](read-models.md)). A git tag is not a Hex release: confirm the version is on hex.pm before depending on it.
