---
name: cratis-chronicle-client-elixir
description: Talk to a Chronicle server from an Elixir application with the cratis_chronicle Hex package - Chronicle.Client supervision and connection lifecycle, EventType structs, append errors, event sources and streams, Chronicle.Concept, PII and encrypted secrets, reactors, read models/projections with composite keys, variants and GlobalHandler, and observer inspection. Use when an OTP application appends to or observes a Chronicle event store. Do not use for the .NET, TypeScript, or Kotlin clients.
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

Re-verified against `Cratis/Chronicle.Elixir` tag **`v3.10.2`** (Hex `3.10.2`).
Source paths below are relative to `Source/chronicle/` at that tag.

Read [references/upgrading.md](references/upgrading.md) when upgrading from 2.x or earlier 3.x releases; wire compatibility and corrected behavior both matter.

| Fact | Value | Source |
| --- | --- | --- |
| OTP app / Hex package | `:cratis_chronicle` | `Source/chronicle/mix.exs:14` |
| Published versions | `3.10.2`, earlier `3.x`, `3.0.0`, `2.2.0`, `2.1.x`, `2.0.0`, `1.0.x`, `0.x` — **no `2.3.0`** | hex.pm |
| Elixir requirement | `~> 1.18` | `Source/chronicle/mix.exs` |
| Contracts dependency | `cratis_chronicle_contracts` resolved to `19.30.0` (`18.2.0` at `v3.1.0`, `16.13.4` at `v2.2.0`); pair it with a compatible kernel | `Source/chronicle/mix.lock` |

> **The `v2.3.0` git tag was never published to Hex.** `mix deps.get` cannot
> fetch it. What it introduced — notably the `Chronicle.Concept` macro
> (`use Chronicle.Concept, type: :string`, a single-value concept that carries its
> `pii` classification once, mirroring `ConceptAs<T>`) — **is published from
> `3.0.0`**. On `2.2.0` it does not exist; on `3.x` it is the preferred way to
> classify a personal value.

> **The repository's `VERSION` file says `0.0.5` and is a build-time
> placeholder** overwritten by the publish workflow. Never quote it.

> Use namespaced modules (`Chronicle.Events.EventType`, not `Chronicle.EventType`).
> The `v2.2.0` README got this wrong; the `3.x` README is correct. Maintained
> snippets live in `Documentation/client-snippets/`.

The current README and get-started guide use `~> 3.5`; select the intended
published version from hex.pm rather than treating a guide's range as a pin.

## Adding it

```elixir
# mix.exs
defp deps do
  [
    {:cratis_chronicle, "~> 3.10"}
  ]
end
```

The library declares `extra_applications: [:logger, :inets, :ssl]` and **no
`mod:`** (`Source/chronicle/mix.exs`), so nothing starts on its own. `:inets` and
`:ssl` back the least-connections load balancer's HTTP probes.

## Starting the client

`Chronicle.Client` is a `Supervisor` (`lib/chronicle/client.ex:135`) with
`start_link(opts \\ [])` (`:145`). Put it in your application's tree — this is
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

### Options

Pass **child-spec keyword options**. The client reads no application env;
your app must read its own configuration and pass it in (`lib/chronicle/client.ex`).

| Option | Default | Line |
| --- | --- | --- |
| `:name` | `Chronicle.Client` | `:146` |
| `:connection_string` | `ConnectionString.default/0` | — |
| `:event_store` | `"default"` | `:165` |
| `:namespace` | `"Default"` — capital D | `:166` |
| `:discover` | `true` | `:167` |
| `:otp_app` | none; falls back to scanning loaded modules | `:174` |
| `:event_types`, `:event_sources`, `:migrations`, `:reactors`, `:reducers`, `:read_models`, `:projections`, `:seeders`, `:webhooks`, `:event_store_subscriptions`, `:global_handlers` | `[]`, merged with discovered | `:194-214` |
| `:default_sink_type_id` | `:mongodb`; accepts `:mongodb`, `:sql`, `:in_memory`, `:none` or a raw string | `:169-170` |
| `:server_address` | alternative `"host:port"` address | connection option |
| `:skip_tls_validation`, `:load_balancer`, `:grpc_options`, `:reconnect_base_delay`, `:reconnect_max_delay` | forwarded to the connection | — |
| `:retry_attempts` | accepted for compatibility, **ignored** | `connections/connection.ex:48-49` |

> The moduledoc at `client.ex:81` says the namespace defaults to `"default"`.
> The code at `:166` uses `"Default"`. **Trust the code.**

**Set `:otp_app`.** With it, discovery is scoped to your application's modules;
without it the client falls back to scanning every loaded module.

Configuration is stashed in `:persistent_term` and read back with
`Chronicle.Client.config/1` (`client.ex:156`). Its subset includes `event_sources`,
but not `read_models`, `projections`, or `migrations` (`:218-228`).

### The supervision tree

`Supervisor.init(children, strategy: :rest_for_one)` — `client.ex:328`.
The lifecycle hub is the root dependency; restarting it restarts its subscribers.
The connection, session and registration precede observers.

### Multiple clients

Pass `name:` to `Chronicle.Client` and `client:` to every API call. Every public
function takes a `:client` option.

## Connecting is asynchronous — wait for the lifecycle

The client connects in the background. `Chronicle.Connections.Lifecycle`
broadcasts `{:chronicle_lifecycle, phase, connection_id}` with three phases
(`lib/chronicle/connections/lifecycle.ex:54`):

| Phase | Meaning |
| --- | --- |
| `:disconnected` | no live session with the kernel |
| `:connected` | the kernel acknowledged the connection id with its first keepalive |
| `:registered` | the registration coordinator registered the base artifacts |

**Wait for `:registered`, never merely `:connected`** — the module's own docs say
so at `:27-29`, because reducers and reactors are not attached until registration
finishes.

```elixir
alias Chronicle.Connections.Lifecycle

case Lifecycle.wait_until(Lifecycle.name_for(Chronicle.Client), :registered, 30_000) do
  :ok -> :ready
  {:error, :timeout} -> :not_ready
end
```

`wait_until(lifecycle, target_phase, timeout \\ 30_000)` is at `:141`.
`subscribe/1` (`:90`) returns `{:ok, phase, connection_id}` **and** sends it as a message,
which closes the subscribe-after-transition race; `phase/1` (`:98`) is the plain
read.

Reconnect never gives up: exponential backoff starts at 1000 ms and caps at
10000 ms, re-resolving addresses each time. `:retry_attempts` is ignored.
Registration failures leave the client in `:connected`, log
`Chronicle registration failed`, and retry every five seconds. Appends before a
channel exists return `{:error, :not_connected}`; read model queries wait up to
30 seconds for `:registered`.

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
(`lib/chronicle/events/event_type.ex:71`) — **the id is required**, unlike the
.NET, Kotlin, and TypeScript clients where it defaults to the type name. Give it
a stable, kebab-case string and never change it. `:generation` defaults to `1`
(`:72`).

**Use typed defaults on events and read models.** Schemas come from struct
defaults: `nil` registers as string, numbers as number, booleans as boolean;
list element schemas use the first default element. Nested structs and concepts
are descended into (`schemas/json_schema_generator.ex:159-201`).

The macro imports constraint macros into your module (`:83-93`):
`unique/1`, `unique/2`, `unique_event_type/0`, `unique_event_type/1`,
`remove_constraint/1`, plus `Chronicle.Compliance.pii/1,2` and
`Chronicle.Confidentiality.encrypted/1,2,3`. The
equivalent accumulating module attributes `@unique`, `@unique_event_type`,
`@remove_constraint` also work.

**Only top-level event keys are camelCased.** `encode_event/1` transforms them
before `Jason.encode!` (`event_sequences/event_log.ex:927-933`); nested keys are
not transformed. Event structs get no `Jason.Encoder` implementation;
`Chronicle.Concept` does, encoding its wrapped value rather than a wrapper.

Read [references/compliance.md](references/compliance.md) when defining personal data, data subjects, concepts or encrypted operational secrets. `pii` and `encrypted` are not interchangeable and cannot mark the same leaf.

Schema evolution is
`use Chronicle.Events.Migration, from: {Mod, generation: n}, to: {Mod, generation: n + 1}`
with `upcast/1` and `downcast/1`; the generations must be exactly one apart or it
raises at compile time.

## Appending

```elixir
case Chronicle.append("account-1", %MyApp.Events.FundsDeposited{amount: 50}) do
  :ok -> :ok
  {:error, {:constraint_violations, violations}} -> {:rejected, violations}
  {:error, reason} -> {:error, reason}
end
```

`Chronicle.append/3` delegates to `Chronicle.EventSequences.EventLog`
(`lib/chronicle.ex:164-165`):

```elixir
@spec append(String.t(), struct(), keyword()) :: :ok | {:error, term()}
```

**The event source id comes first, the event second.** The return is a bare
`:ok`, not a result struct — this client's `append/3` deliberately carries no
sequence number, and `append_and_wait_for_completion/3` exists for when you need
the outcome.

Read [references/append-api.md](references/append-api.md) when you need an append variant, a read-back function, or an append option such as `:event_stream_type`, `:concurrency_scope` or `:tags`.

Failed appends return `{:error, reason}` (before 3.2.0 rejection could incorrectly
return `:ok`). Reasons include `{:constraint_violations, violations}`,
`{:concurrency_violations, violations}`, `{:append_errors, errors}`,
`:not_connected`, and `{:incompatible_server, response}` from the first append's
compatibility preflight per channel. Pair the kernel and installed contracts;
do not blindly retry a timed-out write.

**Match on them.** A bare `:ok = Chronicle.append(...)` raises a `MatchError` on
a constraint violation, which is a fine choice in a script and the wrong one in a
GenServer.

Read [references/event-sources.md](references/event-sources.md) when declaring named event sources/streams, deriving concurrency scopes or routing mixed-source batches; definitions require Chronicle 19.30.0+.

A unit of work is a process: `Chronicle.begin_unit_of_work/1` starts an `Agent`,
and while one is current for the calling process `append`/`append_many` buffer
instead of sending, until `UnitOfWork.commit/1` or `rollback/1`.
`commit/1` returns `:ok | {:error, reason}`. `Chronicle.with_unit_of_work(fun, opts)`
passes the unit to an arity-one function, commits on return, and raises on commit
failure. Source/stream options apply to buffered appends too.

## Observing

### Reactors

```elixir
defmodule MyApp.Reactors.DepositLogger do
  use Chronicle.Reactors.Reactor
  require Logger

  alias MyApp.Events.FundsDeposited
  @handles FundsDeposited

  @impl true
  def handle(%FundsDeposited{}, %{sequence_number: number}) do
    Logger.info("Deposit observed", sequence_number: number)
    :ok
  end
end
```

This follows `Documentation/client-snippets/get-started/common/reactor.md`.

- **`@handles` is an accumulating module attribute** declaring the subscription;
  dispatch is then ordinary Elixir pattern matching in `handle/2`. This is the
  Elixir answer to the other clients' method-name or parameter-type conventions.
- The callback is
  `handle(event :: struct(), context :: map()) :: :ok | {:error, term()} | {:ok, struct() | [struct()]}`.
- **Returning `{:ok, event_or_events}` appends those events as a side effect** —
  to the same event source unless you return an `EventForEventSourceId`. A failed
  side-effect append makes the whole `handle/2` result an error.
- The reactor id defaults to `to_string(__MODULE__)`, i.e. `"Elixir.My.Mod"`.
  Pass `id:` for a stable, readable one.
- Optional replay callbacks: `on_replay_begin/0`, `on_replay_end/0`,
  `on_partition_replay_begin/1`, `on_partition_replay_end/1`.

**The reactor context has six keys**: `event_source_id`, `sequence_number`,
`occurred`, `event_source`, `event_store`, `namespace`
(`lib/chronicle/reactors/handler.ex:421-428`). `event_source` is the registered
source name or `nil` for legacy appends. The moduledoc mentions `:correlation_id`,
but **it is not in the map**. Reducer context differs: `event_source_id`,
`sequence_number`, `occurred`, `event_source`, `observation_state`
(`reducers/handler.ex:381-386`).

### Read models and projections

```elixir
defmodule MyApp.ReadModels.AccountBalance do
  use Chronicle.ReadModels.ReadModel
  defstruct id: "", balance: 0

  from(MyApp.Events.FundsDeposited,
    set: [id: :event_source_id],
    add: [balance: :amount]
  )
end
```

This is the model-bound shape; use typed defaults for its registered schema.

The DSL macros are `from/1`, `from/2`, `join/2`, `removed_with/2`, `from_every/1`.
`from/2` options: `:key` (defaults to `"$eventSourceId"`), `:parent_key`, `:set`,
`:add`, `:subtract`, `:count`. `join/2` requires `:on`.

Read [references/projections.md](references/projections.md) when choosing `passive`, `not_rewindable`, `event_sequence`, `no_auto_map`, composite/event-context keys or dictionary counters. Snake_case field atoms now map correctly; no camelCase-string workaround is needed.

> **The read model id sent to the kernel is the last module segment** —
> `Module.split() |> List.last()` at `lib/chronicle/read_models/read_model.ex:197`.
> `MyApp.ReadModels.Account` registers as `"Account"`, so two read models with the
> same final segment collide. Pass `id:` to disambiguate.

A standalone projection is `use Chronicle.Projections.Projection, model: Mod`
(`:model` is required); a reducer is
`use Chronicle.Reducers.Reducer, model: Mod` with
`reduce(event, model_or_nil, context) :: struct()`. **Reducers run in your
process**, so the reduction is Elixir code you own.

#### Variants — mutually exclusive read models for one entity's lifecycle

Requires `cratis_chronicle` `3.4.0` or later; verified at `3.10.2`. `variant_of/2` requires `:key`; a variant with no `enters_on` raises `Chronicle.Projections.VariantMustDeclareEntersOnEvent` at registration.

Read [references/variants.md](references/variants.md) when an entity has mutually exclusive lifecycle shapes (for example a backlog entry that becomes a pull request) and you need variant read models or a `GlobalHandler`.

Querying (`lib/chronicle/read_models.ex`):

```elixir
{:ok, accounts} = Chronicle.all(MyApp.ReadModels.AccountBalance)
{:ok, account} = Chronicle.read_model(MyApp.ReadModels.AccountBalance, "account-1")
```

`Chronicle.read_model/3` delegates to `ReadModels.get/3` (`lib/chronicle.ex:211`,
`read_models.ex:251-252`), returning `{:ok, struct() | nil} | {:error, term()}`.
`Chronicle.all/2` delegates to `ReadModels.all/2`
(`lib/chronicle.ex:219`, `read_models.ex:320-330`).

Use `Chronicle.ReadModels.query(MyApp.ReadModels.AccountBalance, page: 0)` for paging:
`:page` is zero-based (default `0`), `:page_size` defaults to `50`, and
`:occurrence` selects a replay container. Before 3.5.1, page 0 was rejected and
the default was 1; **page 1 now means the second page**. `ReadModels.snapshots/3`,
`occurrences/2`, and `definitions/1` expose history, replay containers, and
registered definitions.

**Live updates arrive as messages, not as a stream.** `ReadModels.watch/2`
(`read_models.ex:608`) sends
`{:chronicle_read_model_changed, module, %Changeset{}}` and
`{:chronicle_read_model_watch_error, module, reason}` to the calling process. The receiving process needs a `handle_info` for both.

## Operational inspection

`Chronicle.Observers.get_all/1` lists namespace observers; `remove/2` removes an
orphaned observer across the store, refuses running/subscribed observers in any
namespace, and leaves read model data intact (`observers.ex:84,120`). Obtain
explicit authority before removal and inspect `RemovalResult.outcome` —
`{:ok, result}` alone does not mean removed. `Chronicle.FailedPartitions` and
`Chronicle.Jobs` provide failure/job inspection; `Jobs.get/2` lists then finds
client-side. Observer types include `:reactor`, `:projection`, `:reducer`,
`:external`; states include `:active`, `:suspended`, `:replaying`, `:disconnected`,
`:quarantined` and `:unknown`.

## Idioms worth knowing

- **Process-scoped context.** Correlation id, identity, and causation are held per
  process: `Chronicle.current_correlation_id/0`, `set_correlation_id/1`,
  `clear_correlation_id/0`; `current_identity/0`, `set_identity/1`,
  `clear_identity/0`; `Chronicle.Auditing.CausationManager`. They are picked up
  automatically on append. **A `Task` is a different process** and does not
  inherit them.
- **Discovery is compile-time reflection.** The `use` macros generate
  `__chronicle_*__/1` functions via `__before_compile__`, and discovery is
  `function_exported?/3` over the application's modules — not runtime scanning.
- **No client-owned telemetry events.** `telemetry` is a direct dependency;
  transport failure detection subscribes to grpc events. Logging is `Logger`.
- **No formatter export.** `.formatter.exs` declares no `locals_without_parens`
  for `from`, `join`, `removed_with`, `from_every`, `pii`, `subject`, `unique`, or
  `unique_event_type`, and there is no `import_deps: [:cratis_chronicle]` to
  inherit. `mix format` will add parentheses to the DSL.
- **`ReadModels.watch/2` is unsupervised.** It uses `Task.start/1` and `unwatch/1`
  is a raw `Process.exit(pid, :shutdown)`. Supervise it yourself if it matters.

## Common pitfalls

Read [references/pitfalls.md](references/pitfalls.md) when troubleshooting discovery, defaults, process context, variant registration or older-version examples.

## Verify

- Elixir 1.18+ and `mix deps.get` resolve the intended `3.x` client; contracts
  and kernel are compatible (event source definitions need kernel 19.30.0+).
- Local development uses `ConnectionString.development()` or explicit credentials.
- `Chronicle.Client` is in the supervision tree with an explicit `:otp_app`, and
  the tree starts clean.
- The lifecycle reaches `:registered` before the application claims readiness.
- A production connection string sets `skipTlsValidation=false`.
- Every event type module passes an explicit, stable `id:`.
- Every `Chronicle.append/3` call site handles `{:error, _}` as well as `:ok`.
- Every reactor's `@handles` list matches the clauses of its `handle/2`.
- Read model module names have distinct final segments, or explicit ids.
- Every variant group has at least one `enters_on` per variant, and every
  `GlobalHandler` member exists on every variant it targets.
- Production logging filters credential-bearing gRPC/Mint crash reports; see
  the connections reference for limits.
- Examples match the APIs and schema defaults at the installed tag.
- `mix compile --warnings-as-errors` and `mix test` are clean.
