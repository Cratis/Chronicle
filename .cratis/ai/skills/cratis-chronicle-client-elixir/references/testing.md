<!-- cratis-ai-managed: skills/cratis-chronicle-client-elixir/references/testing.md -->
<!-- Copyright (c) Cratis. All rights reserved. -->
<!-- Licensed under the MIT license. See LICENSE file in the project root for full license information. -->

# Testing against an in-memory kernel

Source: `Cratis/Chronicle.Elixir`: `Source/chronicle/lib/chronicle/testing.ex`, `testing/case.ex`, `testing/assertions.ex`, `testing/in_memory_kernel.ex`, `testing/integration.ex`, `testing/projections.ex`, and `Documentation/testing.md`.

Requires `cratis_chronicle` 3.13.0. Earlier clients ship no `Chronicle.Testing` modules.

## Default: `Chronicle.Testing.Case`

Use the case template for ExUnit specs. It starts a real `Chronicle.Client` against `Chronicle.Testing.InMemoryKernel`, puts the client name in the test context as `:chronicle`, and stops it on exit. Every test gets a unique event store, so it is safe under `async: true`.

```elixir
# Copyright (c) Cratis. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.

defmodule MyApp.AccountTest do
  use Chronicle.Testing.Case,
    event_types: [MyApp.Events.AccountOpened],
    reducers: [MyApp.Reducers.AccountReducer]

  import Chronicle.Testing.Assertions

  test "opening an account appends AccountOpened", %{chronicle: client} do
    :ok =
      Chronicle.EventSequences.EventLog.append(
        "account-1",
        %MyApp.Events.AccountOpened{owner_name: "Alice"},
        client: client
      )

    assert_appended(client, MyApp.Events.AccountOpened, event_source_id: "account-1")
  end
end
```

**Pass `client: client` on every call.** Without it the call addresses the default `Chronicle.Client` name, not the test's client.

`start_supervised_client/1` (outside ExUnit templates; pair with `stop_supervised_client/1`) accepts `:event_types`, `:event_sources`, `:constraints` (standalone constraint modules), `:reactors`, `:reducers`, `:projections`, `:name`, `:event_store` and `:namespace`.

## Waiting and reading

An append returns once the event is committed; reactors and reducers run afterward in `Chronicle.Testing.Dispatcher`, synchronously after each committed append. Call `Chronicle.Testing.wait_for_observers(client)` before asserting on their effects; it is a sleep-free barrier that returns `{:error, failures}` when a reactor or reducer raised.

- `Chronicle.Testing.read_model(client, ReducerModule, key)` reads the state a **reducer** built, keyed by event source id.
- `Chronicle.Testing.appended_events(client, event_source_id)` returns decoded `Chronicle.Events.AppendedEvent` structs.

## Assertions

`assert_appended/3` and `refute_appended/3` take a matcher: an exact event struct (`==`), an event type module, or a one-arity predicate; options include `:event_source_id` and `:count`. `assert_constraint_violation(result, constraint_name \\ nil)` and `assert_concurrency_violation(result)` match the `{:error, {:constraint_violations, _}}` and `{:error, {:concurrency_violations, _}}` append results.

## What the in-memory kernel does not model

Appends, reads, the tail lookups, concurrency scopes (including `expecting_no_matching_event/1`), and `unique/2` and `unique_event_type/1` constraints are modeled. **Everything else returns `{:error, {:not_supported_in_memory, rpc}}`**: `closes_stream` constraints, `ReadModels` queries and `Projections` queries. A spec that passes against it proves client logic, not kernel behavior.

`Chronicle.Testing.Projections.apply_event(ReadModel, instance_or_nil, event)` folds one event through the supported subset of a read model's declarations (`from/2` with `:set`, `:add`, `:subtract`, `:count`, `:increment`, `:decrement`, `:clear`, AutoMap, and `removed_with/2`). `join/2`, `children/2`, `from_every/1` and `removed_with_join/2` raise `Chronicle.Testing.UnsupportedProjectionOperation`; test those against a real kernel.

## Real kernel: `Chronicle.Testing.Integration`

Tag these specs `@moduletag :integration`. `Chronicle.Testing.Integration.setup_isolated_client(opts, client \\ Chronicle.Client)` starts a real client with a unique event store and namespace, reading `CHRONICLE_INTEGRATION_CONNECTION_STRING` unless `:connection_string` is given, and waits for registration. Use `append_with_result/3` then `Integration.wait_for_observers(result.sequence_number, client: client)`; do not sleep. The stores it creates are not deleted afterward because the kernel has no delete call.

`Chronicle` adopts `Chronicle.Behaviour`, so code that only needs to be observed calling the facade can use a Mox mock (`Mox.defmock(Mock, for: Chronicle.Behaviour)`); Mox is not a dependency of the client.
