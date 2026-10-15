<!-- cratis-ai-managed: skills/cratis-chronicle-client-elixir/references/upgrading.md -->
<!-- Copyright (c) Cratis. All rights reserved. -->
<!-- Licensed under the MIT license. See LICENSE file in the project root for full license information. -->

# Upgrading 2.x to 3.x

Verified at `Cratis/Chronicle.Elixir v3.10.2`: `Source/chronicle/mix.exs`, `mix.lock`, `lib/chronicle/wire_result.ex`, `jobs.ex`, `event_sequences/event_log.ex`, `read_models.ex`, `seeding.ex`, and the release notes for 3.0.0, 3.2.0, 3.5.1 and 3.8.0.

## Dependencies and wire compatibility

3.0.0 migrated the wire contracts to Chronicle 17/18, renaming RPCs and unwrapping response envelopes. It removed no public Elixir API, but unauthorized/excepted command responses now return `{:error, reason}` rather than looking successful. `Jobs.get/2` lists jobs then finds the id client-side; `append_many_for_event_sources/2` uses its dedicated RPC rather than the single-source path.

For **3.10.2**:

- Use `{:cratis_chronicle, "~> 3.10"}` and Elixir **1.18+** (`mix.exs` requires `~> 1.18`).
- grpc **1.x** and Mint **1.11+** are direct dependencies; do not carry a mandatory `GRPC.Client.Supervisor` child forward from an older grpc setup.
- Contracts resolve to **19.30.0** in this tag's lockfile. Pair the kernel with the installed contracts descriptor; named event source definitions require **Chronicle 19.30.0+**.
- The first append on each channel runs a compatibility check, shared until reconnect. `{:error, {:incompatible_server, response}}` is a version/descriptor problem, not a transient append rejection. Update kernel and contracts as a compatible pair; do not bypass the preflight. Failed transport calls return `{:error, %GRPC.RPCError{}}`.

## Corrected behavior requires application review

3.5.1 fixed behavior that older applications may have unintentionally relied on:

| Change | Review before rollout |
| --- | --- |
| Reducers register successfully and process from the log start | Old events now reach reducers that previously failed registration |
| Multi-word projection fields and AutoMap now populate | Remove camelCase-string workarounds; inspect fields that were formerly empty |
| `unique(:owner_email)` and `remove_constraint` registration work | Uniqueness is now enforced and release events can remove constraints |
| `ReadModels.query/2` is zero-based, default page 0 | Change first-page callers to `page: 0`; `page: 1` is now the second page |
| Seeding skips an event source with existing events | 3.5.0 re-appended on every start; already-stored duplicates are not removed |
| Tail/next sequence lookups no longer narrow implicitly to `"Default"` | Audit callers that depended on implicit routing narrowing |

Since 3.2.0, rejected appends (including concurrency/constraint failures) return `{:error, reason}` instead of potentially returning `:ok`; batch append metadata survives the rich append RPC. Handle errors on single, batch and transactional paths.

3.10.2 preserves declared `unique(..., message: ...)` violation messages on single, batch and transactional appends. Retain the constraint name and diagnostics when handling violations rather than assuming every message came from the kernel.
