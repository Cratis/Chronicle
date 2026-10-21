---
name: cratis-chronicle-client-elixir
description: Talk to a Chronicle server from an Elixir application with the cratis_chronicle Hex package - Chronicle.Client supervision and connection lifecycle, EventType structs, append errors, event sources and streams, Chronicle.Concept, PII and encrypted secrets, reactors, read models/projections with composite keys, children, variants and GlobalHandler, decision reads, in-memory testing, captures, constraints, discovery drift and observer inspection. Use when an OTP application appends to or observes a Chronicle event store. Do not use for the .NET, TypeScript, or Kotlin clients.
license: MIT
---
<!-- cratis-ai-managed: skills/cratis-chronicle-client-elixir/SKILL.md -->

<!-- Copyright (c) Cratis. All rights reserved. -->
<!-- Licensed under the MIT license. See LICENSE file in the project root for full license information. -->

# The Chronicle client for Elixir

`cratis_chronicle` is a **standalone client SDK**. It is a library, not a
framework: it starts no supervision tree of its own, you add `Chronicle.Client`
to yours, and everything after that is ordinary OTP.

## Verified product sources

Source: `Cratis/Chronicle.Elixir` (Hex package `cratis_chronicle`). Source paths below are relative to `Source/chronicle/`.

Read [references/upgrading.md](references/upgrading.md) when moving an application from an older client. Requires Elixir `~> 1.18`; read [references/product-sources.md](references/product-sources.md) for the package and contracts dependency facts.

> **A git tag is not a Hex release.** `mix deps.get` fetches only published versions, and the repository's `VERSION` file is a build-time placeholder: never quote it. Use namespaced modules (`Chronicle.Events.EventType`, not `Chronicle.EventType`); older READMEs got this wrong. Maintained snippets live in `Documentation/client-snippets/`.

`Chronicle.Concept` (`use Chronicle.Concept, type: :string`) is the preferred way to classify a personal value once, mirroring `ConceptAs<T>`; it requires `cratis_chronicle` 3.0.0, and an older client annotates fields with `pii/1,2`.

## Adding it

```elixir
# mix.exs
defp deps do
  [
    {:cratis_chronicle, "~> 3.13"}
  ]
end
```

The library declares `extra_applications: [:logger, :inets, :ssl]` and **no
`mod:`** (`Source/chronicle/mix.exs`), so nothing starts on its own. `:inets` and
`:ssl` back the least-connections load balancer's HTTP probes.

## Starting the client

`Chronicle.Client` is a `Supervisor` (`lib/chronicle/client.ex`) with
`start_link(opts \\ [])`. Put it in your application's tree — this is
the CI-validated snippet from
`Documentation/client-snippets/get-started/console/connect.md`:

```elixir
defmodule MyApp.Application do
  use Application

  @impl true
  def start(_type, _args) do
    children = [
      {Chronicle.Client,
       connection_string: Chronicle.Connections.ConnectionString.development(),
       event_store: "quickstart",
       otp_app: :my_app}
    ]

    Supervisor.start_link(children, strategy: :one_for_one, name: MyApp.Supervisor)
  end
end
```

### Options, supervision and multiple clients

Read [references/client-options.md](references/client-options.md) when setting `Chronicle.Client` options (set `:otp_app`), its supervision tree, or multiple clients.

## Connecting is asynchronous — wait for the lifecycle

The client connects in the background. `Chronicle.Connections.Lifecycle`
broadcasts `{:chronicle_lifecycle, phase, connection_id}` with three phases
(`lib/chronicle/connections/lifecycle.ex`):

| Phase | Meaning |
| --- | --- |
| `:disconnected` | no live session with the kernel |
| `:connected` | the kernel acknowledged the connection id with its first keepalive |
| `:registered` | the registration coordinator registered the base artifacts |

**Wait for `:registered`, never merely `:connected`** — the module's own docs say
so, because reducers and reactors are not attached until registration
finishes.

```elixir
alias Chronicle.Connections.Lifecycle

case Lifecycle.wait_until(Lifecycle.name_for(Chronicle.Client), :registered, 30_000) do
  :ok -> :ready
  {:error, :timeout} -> :not_ready
end
```

`wait_until(lifecycle, target_phase, timeout \\ 30_000)` is the blocking wait;
`subscribe/1` returns `{:ok, phase, connection_id}` **and** sends it as a message
(closing the subscribe-after-transition race); `phase/1` is the plain read.

Reconnect never gives up (backoff 1000 to 10000 ms; `:retry_attempts` is
ignored). Registration failures leave the client in `:connected`, log
`Chronicle registration failed`, and retry every five seconds. Appends before a
channel exists return `{:error, :not_connected}`.

## Authentication and TLS

Use `ConnectionString.development()` locally: `default()` supplies **no
credentials**, so an authentication-required development kernel never lets it
reach `:registered`. Production uses URL userinfo `client-id:secret@host` or
`?apiKey=...` (the API key wins when both are given). Load real credentials from
the application's approved secret mechanism; never log the connection string.

**`skip_tls_validation` defaults to `true`.** Production must set
`skipTlsValidation=false`; a client certificate does not establish server trust.

Read [references/connections.md](references/connections.md) when configuring connection strings, mTLS, grpc/Mint options or credential-safe logging, or diagnosing keepalive and quiet observers.

## Defining event types

```elixir
# Copyright (c) Cratis. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.

defmodule MyApp.Events.FundsDeposited do
  @moduledoc "Funds were deposited into an account."

  use Chronicle.Events.EventType, id: "funds-deposited"

  defstruct amount: 0, active: false, title: ""
end
```

`use Chronicle.Events.EventType` takes `:id` through `Keyword.fetch!`
(`lib/chronicle/events/event_type.ex`) — **the id is required**, unlike the
.NET, Kotlin, and TypeScript clients where it defaults to the type name. Give it
a stable, kebab-case string and never change it. `:generation` defaults to `1`.

**Use typed defaults on events and read models.** Schemas come from struct
defaults: `nil` registers as string, numbers as number, booleans as boolean;
list element schemas use the first default element. Nested structs and concepts
are descended into (`schemas/json_schema_generator.ex`).

The macro imports `unique/1,2`, `unique_event_type/0,1`, `remove_constraint/1,2`,
`closes_stream/1`, `Chronicle.Compliance.pii/1,2` and
`Chronicle.Confidentiality.encrypted/1,2,3`; the accumulating attributes
`@unique`, `@unique_event_type`, `@remove_constraint` also work. Read
[references/constraints.md](references/constraints.md) for their options.

**Only top-level event keys are camelCased.** `encode_event/1` transforms them
before `Jason.encode!` (`event_sequences/event_log.ex`); nested keys are
not transformed. Event structs get no `Jason.Encoder` implementation;
`Chronicle.Concept` does, encoding its wrapped value.

Read [references/compliance.md](references/compliance.md) when defining personal data, data subjects, concepts or encrypted operational secrets. `pii` and `encrypted` are not interchangeable and cannot mark the same leaf.

Schema evolution is
`use Chronicle.Events.Migration, from: {Mod, generation: n}, to: {Mod, generation: n + 1}`
with `upcast/1` and `downcast/1`; the generations must be exactly one apart. Mark
the older module with `generation_for:`, not a second `:id`.

## Appending

```elixir
case Chronicle.append("account-1", %MyApp.Events.FundsDeposited{amount: 50}) do
  :ok -> :ok
  {:error, {:constraint_violations, violations}} -> {:rejected, violations}
  {:error, reason} -> {:error, reason}
end
```

`Chronicle.append/3` delegates to `Chronicle.EventSequences.EventLog`.
**The event source id comes first, the event second.** The return is a bare
`:ok`, carrying no sequence number; use `append_with_result/3` or
`append_and_wait_for_completion/3` when you need the outcome.

Read [references/append-api.md](references/append-api.md) when you need an append variant, a read-back function, or an append option such as `:event_stream_type`, `:concurrency_scope` or `:tags`.

Requires `cratis_chronicle` 3.2.0 for rejected appends to return `{:error, reason}`; an older client could report a rejection as `:ok`.

Failed appends return `{:error, reason}`: `{:constraint_violations, violations}`,
`{:concurrency_violations, violations}`, `{:append_errors, errors}`,
`{:unknown_event_type, module}`, `:not_connected`, and `{:incompatible_server, response}`
from the first append's compatibility preflight per channel. Pair the kernel and
installed contracts; do not blindly retry a timed-out write. **Match on them**: a
bare `:ok = Chronicle.append(...)` raises a `MatchError` on a violation, fine in a
script and wrong in a GenServer.

Read [references/event-sources.md](references/event-sources.md) when declaring named event sources/streams, deriving concurrency scopes or routing mixed-source batches; definitions have client and kernel requirements stated there.

A unit of work is a process: `Chronicle.begin_unit_of_work/1` starts an `Agent`;
while one is current for the calling process, `append`/`append_many` buffer until
`UnitOfWork.commit/1` (`:ok | {:error, reason}`) or `rollback/1`.
`Chronicle.with_unit_of_work(fun, opts)` commits on return and raises on commit
failure.

## Observing

### Reactors

Read [references/reactor-example.md](references/reactor-example.md) when you need a worked reactor module.

- **`@handles` is an accumulating module attribute** declaring the subscription;
  dispatch is then ordinary Elixir pattern matching in `handle/2`.
- The callback is
  `handle(event :: struct(), context :: map()) :: :ok | {:error, term()} | {:ok, struct() | [struct()]}`.
- **Returning `{:ok, event_or_events}` appends those events as a side effect** —
  to the same event source unless you return an `EventForEventSourceId`. A failed
  side-effect append makes the whole `handle/2` result an error.
- The reactor id defaults to `to_string(__MODULE__)`, i.e. `"Elixir.My.Mod"`.
  Pass `id:` for a stable, readable one.
- Optional replay callbacks: `on_replay_begin/0`, `on_replay_end/0`,
  `on_partition_replay_begin/1`, `on_partition_replay_end/1`.

**The context is a `Chronicle.Events.EventContext` struct** (requires 3.11.0; a
bare map before). Pattern match on keys or use `context.caused_by`. It carries
`event_source_id`, `sequence_number`, `occurred`, `event_source` (the registered
source name or `nil`), `event_store`, `namespace`, `correlation_id`, `caused_by`,
`causation`, `tags`, `subject`, `appended_generation`, `replaying?` and, for
reactors only, `delivery_id` (stable across replay and failed-partition
recovery; key a receipt on it). Reducers get the same struct with
`observation_state` set (`lib/chronicle/events/event_context.ex`).

### Read models and projections

Read [references/reactor-example.md](references/reactor-example.md#read-model-example) for a worked model-bound read model with typed defaults.

The DSL macros are `from/1,2`, `join/2`, `removed_with/2`, `from_every/1`, `children/2,3`.
`from/2` options: `:key` (defaults to `"$eventSourceId"`), `:parent_key`, `:set`,
`:add`, `:subtract`, `:count`. `join/2` requires `:on`.

Read [references/projections.md](references/projections.md) when choosing `passive`, `not_rewindable`, `event_sequence`, `no_auto_map`, composite/event-context keys, dictionary counters or `children/2,3` collections. Snake_case field atoms now map correctly; no camelCase-string workaround is needed.

> **The read model id sent to the kernel is the last module segment** —
> `Module.split() |> List.last()` at `read_models/read_model.ex`.
> `MyApp.ReadModels.Account` registers as `"Account"`, so two read models with the
> same final segment collide. Pass `id:` to disambiguate.

A standalone projection is `use Chronicle.Projections.Projection, model: Mod`
(`:model` is required); a reducer is
`use Chronicle.Reducers.Reducer, model: Mod` with
`reduce(event, model_or_nil, context) :: struct()`. **Reducers run in your
process**, so the reduction is Elixir code you own.

#### Variants — mutually exclusive read models for one entity's lifecycle

Requires `cratis_chronicle` `3.4.0` or later. `variant_of/2` requires `:key`; a variant with no `enters_on` raises `Chronicle.Projections.VariantMustDeclareEntersOnEvent` at registration.

Read [references/variants.md](references/variants.md) when an entity has mutually exclusive lifecycle shapes (for example a backlog entry that becomes a pull request) and you need variant read models or a `GlobalHandler`.

Querying (`lib/chronicle/read_models.ex`):

```elixir
{:ok, accounts} = Chronicle.all(MyApp.ReadModels.AccountBalance)
{:ok, account} = Chronicle.read_model(MyApp.ReadModels.AccountBalance, "account-1")
```

`Chronicle.read_model/3` returns `{:ok, struct() | nil} | {:error, term()}`; `nil`
means absent **or not projected yet**.

Use `Chronicle.ReadModels.query(MyApp.ReadModels.AccountBalance, page: 0)` for paging:
`:page` is zero-based (default `0`), `:page_size` defaults to `50`, and
`:occurrence` selects a replay container. **Page 1 is the second page**. `ReadModels.snapshots/3`,
`occurrences/2`, and `definitions/1` expose history, replay containers, and
registered definitions.

**Live updates arrive as messages, not as a stream.** `ReadModels.watch/2` and `subscribe/2` send `{:chronicle_read_model_changed, module, %Changeset{}}` and error messages to the calling process, which needs a `handle_info` for each. Read [references/read-models.md](references/read-models.md) when subscribing, filtering, observing collections, previewing or waiting for a projection.

## Guarding decisions, testing and more

Read the reference that matches the task:

- [references/decision-reads.md](references/decision-reads.md) when an append depends on state a concurrent append could change: `Chronicle.decide/2`, `DecisionReads`, `ConcurrencyScope` builders, the optimistic default.
- [references/testing.md](references/testing.md) when writing specs: `Chronicle.Testing.Case`, assertions, the in-memory kernel's limits.
- [references/constraints.md](references/constraints.md) when declaring uniqueness, standalone constraint modules, `remove_constraint/2`, violation details or closing events.
- [references/discovery-and-generations.md](references/discovery-and-generations.md) when pinning ids, setting `:drift_policy`, running `mix chronicle.discover`, or handling older event generations.
- [references/captures.md](references/captures.md) when turning external API, webhook or other-store data into events.
- [references/observability.md](references/observability.md) and [references/patterns.md](references/patterns.md) for OpenTelemetry/`:telemetry` and `Chronicle.Patterns`.

## Idioms and operational inspection

Read [references/idioms-and-inspection.md](references/idioms-and-inspection.md) when carrying correlation or identity across a `Task`, formatting the DSL, supervising watches, or removing observers (obtain explicit authority first).

## Common pitfalls

Read [references/pitfalls.md](references/pitfalls.md) when troubleshooting discovery, defaults, process context, variants or older-version examples.

## Verify

- Elixir 1.18+ and `mix deps.get` resolve the intended published client; contracts
  and kernel are compatible (event source definitions have their own client and kernel requirements; see the event sources reference).
- Local development uses `ConnectionString.development()` or explicit credentials.
- `Chronicle.Client` is in the supervision tree with an explicit `:otp_app`, and
  the tree starts clean.
- The lifecycle reaches `:registered` before the application claims readiness.
- A production connection string sets `skipTlsValidation=false`.
- Every event type module passes an explicit, stable `id:`.
- Every `Chronicle.append/3` call site handles `{:error, _}` as well as `:ok`.
- Every reactor's `@handles` list matches the clauses of its `handle/2`.
- Reactors, reducers and projections that must survive a rename pin their `:id`.
- Read model module names have distinct final segments, or explicit ids.
- Every variant group has at least one `enters_on` per variant, and every
  `GlobalHandler` member exists on every variant it targets.
- Production logging filters credential-bearing gRPC/Mint crash reports; see
  the connections reference for limits.
- Examples match the APIs and schema defaults at the installed tag.
- `mix compile --warnings-as-errors` and `mix test` are clean; `mix chronicle.discover --otp-app my_app` lists every expected artifact.
