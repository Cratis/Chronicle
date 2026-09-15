---
name: cratis-chronicle-client-elixir
description: Talk to a Chronicle server from an Elixir application with the cratis_chronicle Hex package - putting Chronicle.Client in a supervision tree, connection strings, use Chronicle.Events.EventType structs, Chronicle.append returning ok or error tuples, reactors with the @handles attribute and a handle/2 callback, model-bound read models, and the connection lifecycle phases and keepalive. Use when an OTP application appends to or observes a Chronicle event store. Do not use for the .NET, TypeScript, or Kotlin clients.
license: MIT
---
<!-- cratis-ai-managed: skills/cratis-chronicle-client-elixir/SKILL.md -->

# The Chronicle client for Elixir

`cratis_chronicle` is a **standalone client SDK**. It is a library, not a
framework: it starts no supervision tree of its own, you add `Chronicle.Client`
to yours, and everything after that is ordinary OTP.

## Verified product sources

This skill is verified against `Cratis/Chronicle.Elixir` at tag **`v2.2.0`**,
which is the version actually published on Hex.

| Fact | Value | Source |
| --- | --- | --- |
| OTP app / Hex package | `:cratis_chronicle` | `Source/chronicle/mix.exs:14` |
| Published version | `2.2.0` | hex.pm |
| Elixir requirement | `~> 1.14` | `Source/chronicle/mix.exs:16` |
| Contracts dependency | `cratis_chronicle_contracts` resolved to `16.13.4` | `Source/chronicle/mix.lock` |

> **The `v2.3.0` git tag was never published to Hex.** `mix deps.get` cannot
> fetch it. Anything introduced there — notably the `Chronicle.Concept` macro,
> which does not exist at `v2.2.0` — must not be documented as available.

> **The repository's `VERSION` file says `0.0.5` and is a build-time
> placeholder** overwritten by the publish workflow. Never quote it.

> **Do not copy from the repository's root `README.md`.** At `v2.2.0` it uses
> module names that do not exist — `use Chronicle.EventType`,
> `use Chronicle.ReadModel`, `use Chronicle.Reactor`, `use Chronicle.Reducer`,
> `use Chronicle.Seeder` (`README.md:9-13`, `:49`, `:54`). The real modules are
> namespaced: `Chronicle.Events.EventType`, `Chronicle.ReadModels.ReadModel`,
> `Chronicle.Reactors.Reactor`, `Chronicle.Reducers.Reducer`,
> `Chronicle.Seeding.Seeder`. **Copy from `Documentation/client-snippets/`
> instead** — those are CI-validated and correct at every version.

> `Documentation/get-started.md:12` pins `{:cratis_chronicle, "~> 0.1"}`, which
> does not match the published `2.2.0`. Use a `2.x` requirement.

## Adding it

```elixir
# mix.exs
defp deps do
  [
    {:cratis_chronicle, "~> 2.2"}
  ]
end
```

The library declares `extra_applications: [:logger, :inets, :ssl]` and **no
`mod:`** (`Source/chronicle/mix.exs`), so nothing starts on its own. `:inets` and
`:ssl` back the least-connections load balancer's HTTP probes.

## Starting the client

`Chronicle.Client` is a `Supervisor` (`lib/chronicle/client.ex:129`) with
`start_link(opts \\ [])` (`:139`). Put it in your application's tree — this is
the CI-validated snippet from
`Documentation/client-snippets/get-started/console/connect.md`:

```elixir
defmodule MyApp.Application do
  use Application

  @impl true
  def start(_type, _args) do
    children = [
      {Chronicle.Client,
       connection_string: "chronicle://localhost:35000",
       event_store: "quickstart",
       otp_app: :my_app}
    ]

    Supervisor.start_link(children, strategy: :one_for_one, name: MyApp.Supervisor)
  end
end
```

### Options

All configuration is **child-spec keyword options**. There is no
`Application.get_env` anywhere in the library and no `config/` directory —
`config.exs` does nothing for this client. Read the options at
`lib/chronicle/client.ex`:

| Option | Default | Line |
| --- | --- | --- |
| `:name` | `Chronicle.Client` | `:140` |
| `:connection_string` | `ConnectionString.default/0` | — |
| `:event_store` | `"default"` | `:159` |
| `:namespace` | `"Default"` — capital D | `:160` |
| `:discover` | `true` | `:161` |
| `:otp_app` | none; falls back to scanning loaded modules | `:168` |
| `:event_types`, `:migrations`, `:reactors`, `:reducers`, `:read_models`, `:projections`, `:seeders`, `:webhooks`, `:event_store_subscriptions` | `[]`, merged with discovered | `:186-197` |
| `:skip_tls_validation`, `:load_balancer`, `:grpc_options`, `:retry_attempts`, `:reconnect_base_delay`, `:reconnect_max_delay` | forwarded to the connection | — |

> The moduledoc at `client.ex:81` says the namespace defaults to `"default"`.
> The code at `:160` uses `"Default"`. **Trust the code.**

**Set `:otp_app`.** With it, discovery is scoped to your application's modules;
without it the client falls back to scanning every loaded module.

Configuration is stashed in `:persistent_term` and read back with
`Chronicle.Client.config/1` (`client.ex:150`). Note it stores only a subset —
`read_models`, `projections`, and `migrations` are not retrievable at runtime.

### The supervision tree

`Supervisor.init(children, strategy: :rest_for_one)` — `client.ex:308`. The
strategy is deliberate: the children are ordered connection → session →
registration → observers, so restarting the connection restarts everything that
depends on it, and nothing is left holding a dead channel.

### Multiple clients

Pass `name:` to `Chronicle.Client` and `client:` to every API call. Every public
function takes a `:client` option.

## Connection strings

`Chronicle.Connections.ConnectionString` accepts `chronicle://` and
`chronicle+srv://` (`lib/chronicle/connections/connection_string.ex:8`,
validated at `:342`). Default port is `35_000` (`:83`).

| Function | Line |
| --- | --- |
| `default/0` — `chronicle://localhost:35000` | `:144` |
| `development/0` — adds the dev client credentials | `:154` |
| `parse/1` | `:186` |

Query parameters include `apiKey`, `disableTls`, `skipTlsValidation`,
`certificatePath`, `certificatePassword`, `authPort`, `loadBalancer`,
`srvNameServer`. A `chronicle+srv://` string accepts only one host and raises
otherwise (`:197-198`).

> **`skip_tls_validation` defaults to `true`** — the struct default at `:112` and
> the query parse at `:214`. TLS is on but the certificate chain is not checked,
> because a development kernel serves a self-signed certificate. **A production
> connection string must carry `?skipTlsValidation=false`.**

## Defining event types

```elixir
defmodule MyApp.Events.<EventName> do
  @moduledoc "<What happened, in the past tense.>"

  use Chronicle.Events.EventType, id: "<event-name>"

  defstruct [:<field>, :<other_field>]
end
```

`use Chronicle.Events.EventType` takes `:id` through `Keyword.fetch!`
(`lib/chronicle/events/event_type.ex:68`) — **the id is required**, unlike the
.NET, Kotlin, and TypeScript clients where it defaults to the type name. Give it
a stable, kebab-case string and never change it. `:generation` defaults to `1`
(`:69`).

The macro imports constraint macros into your module (`:79-86`):
`unique/1`, `unique/2`, `unique_event_type/0`, `unique_event_type/1`,
`remove_constraint/1`, plus `Chronicle.Compliance.pii/1,2` (`:88`). The
equivalent accumulating module attributes `@unique`, `@unique_event_type`,
`@remove_constraint` also work.

**Struct fields are sent as camelCase.** `encode_event/1` converts snake_case to
camelCase and `Jason.encode!`s the result
(`lib/chronicle/event_sequences/event_log.ex`).

> The moduledoc claims Chronicle generates a `Jason.Encoder` implementation
> automatically. There is no `defimpl Jason.Encoder` in the macro — encoding
> happens in `encode_event/1`. Do not rely on the claim.

Schema evolution is
`use Chronicle.Events.Migration, from: {Mod, generation: n}, to: {Mod, generation: n + 1}`
with `upcast/1` and `downcast/1`; the generations must be exactly one apart or it
raises at compile time.

## Appending

```elixir
:ok =
  Chronicle.append(book_id, %MyApp.Events.<EventName>{
    <field>: "<value>"
  })
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

| Function | Arity | Line in `event_log.ex` |
| --- | --- | --- |
| `append/3` | `(event_source_id, event, opts)` | `:99` |
| `append_many/3` | `(event_source_id, events, opts)` | `:120` |
| `append_many_for_event_sources/2` | `(events, opts)` | `:155` |
| `append_and_wait_for_completion/3` | returns `{:ok, %{success: _, failed_partitions: _}}` | `:215` |
| `get_for_event_source/2` | | `:326` |
| `get_from_sequence_number/2` | | `:369` |
| `get_tail_sequence_number/2` | | `:408` |

Append options (`event_log.ex:79-94`): `:client`, `:namespace`,
`:event_sequence_id` (default `"event-log"`), `:event_source_type` (default
`"Default"`), `:event_stream_type` (default **`"All"`**), `:event_stream_id`
(default `"Default"`), `:tags`, `:subject`, `:correlation_id`, `:identity`,
`:causation`, `:concurrency_scope`, `:occurred`.

Errors are normalized to one of two shapes:

```elixir
{:error, {:constraint_violations, violations}}
{:error, {:append_errors, errors}}
```

**Match on them.** A bare `:ok = Chronicle.append(...)` raises a `MatchError` on
a constraint violation, which is a fine choice in a script and the wrong one in a
GenServer.

A unit of work is a process: `Chronicle.begin_unit_of_work/1` starts an `Agent`,
and while one is current for the calling process `append`/`append_many` buffer
instead of sending, until `UnitOfWork.commit/1` or `rollback/1`.

## Observing

### Reactors

```elixir
defmodule MyApp.Reactors.<ReactorName> do
  use Chronicle.Reactors.Reactor

  alias MyApp.Events.<EventName>

  @handles <EventName>

  @impl true
  def handle(%<EventName>{}, %{event_source_id: <id>}) do
    # side effect here
    :ok
  end
end
```

That is the CI-validated snippet
(`Documentation/client-snippets/get-started/common/reactor.md`).

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

**The context map has exactly five keys** —
`%{event_source_id, sequence_number, occurred, event_store, namespace}`
(`lib/chronicle/reactors/handler.ex:393-397`). The reactor moduledoc also
mentions `:correlation_id`; **it is not in the map**.

### Read models and projections

```elixir
defmodule MyApp.ReadModels.<ReadModelName> do
  use Chronicle.ReadModels.ReadModel

  defstruct id: nil, <field>: nil, <flag>: false

  from MyApp.Events.<EventName>,
    set: [id: :event_source_id, <field>: :<event_field>]

  from MyApp.Events.<OtherEvent>,
    set: [<flag>: true]
end
```

That shape is the CI-validated snippet
(`Documentation/client-snippets/get-started/common/book-read-model.md`).

The DSL macros are `from/1`, `from/2`, `join/2`, `removed_with/2`, `from_every/1`.
`from/2` options: `:key` (defaults to `"$eventSourceId"`), `:parent_key`, `:set`,
`:add`, `:subtract`, `:count`.

> **The read model id sent to the kernel is the last module segment** —
> `Module.split() |> List.last()` at `lib/chronicle/read_models/read_model.ex:148`.
> `MyApp.ReadModels.Account` registers as `"Account"`, so two read models with the
> same final segment collide. Pass `id:` to disambiguate.

A standalone projection is `use Chronicle.Projections.Projection, model: Mod`
(`:model` is required); a reducer is
`use Chronicle.Reducers.Reducer, model: Mod` with
`reduce(event, model_or_nil, context) :: struct()`. **Reducers run in your
process**, so the reduction is Elixir code you own.

Querying (`lib/chronicle/read_models.ex`):

```elixir
{:ok, books} = Chronicle.all(MyApp.ReadModels.<ReadModelName>)
{:ok, book}  = Chronicle.read_model(MyApp.ReadModels.<ReadModelName>, book_id)
```

`Chronicle.read_model/3` delegates to `ReadModels.get/3` (`lib/chronicle.ex:211`,
`read_models.ex:245-246`), returning `{:ok, struct() | nil} | {:error, term()}`.
`Chronicle.all/2` delegates to `ReadModels.get_instances/2`
(`lib/chronicle.ex:219`, `read_models.ex:314-315`).

**Live updates arrive as messages, not as a stream.** `ReadModels.watch/2`
(`read_models.ex:593`) sends
`{:chronicle_read_model_changed, module, %Changeset{}}` and
`{:chronicle_read_model_watch_error, module, reason}` to the calling process. This
is the Elixir analogue of the other clients' observable APIs, and it means the
receiving process must have a `handle_info` for both.

## Connecting is asynchronous — wait for the lifecycle

The client connects in the background. `Chronicle.Connections.Lifecycle`
broadcasts `{:chronicle_lifecycle, phase, connection_id}` with three phases
(`lib/chronicle/connections/lifecycle.ex:54`):

| Phase | Meaning |
| --- | --- |
| `:disconnected` | no live session with the kernel |
| `:connected` | the session handshake completed |
| `:registered` | the registration coordinator registered the base artifacts |

**Wait for `:registered`, never merely `:connected`** — the module's own docs say
so at `:27-29`, because reducers and reactors are not attached until registration
finishes.

```elixir
config = Chronicle.Client.config()

case Chronicle.Connections.Lifecycle.wait_until(config.lifecycle, :registered, 30_000) do
  :ok -> :ok
  {:error, :timeout} -> # decide what a not-yet-registered client means for you
end
```

`wait_until(lifecycle, target_phase, timeout \\ 30_000)` is at `:141`.
`subscribe/1` (`:90`) returns the current phase **and** sends it as a message,
which closes the subscribe-after-transition race; `phase/1` (`:98`) is the plain
read.

## Keepalive — the failure mode is silence

The contract is spelled out in `lib/chronicle/connections/keep_alive.ex:7-17`:
the kernel pushes a `ConnectionKeepAlive` down the `Connect` server stream once
per second, and for each one **the client must call back the separate unary
`ConnectionKeepAlive` RPC** (`answer/2` at `:56`). A client that only consumes the
stream is evicted once the kernel's `LastSeen` falls more than five seconds
behind, and the kernel then unsubscribes its observers.

The consequence is stated in that same comment and is worth carrying into any
diagnosis: **reactors and reducers go quiet while the `Connect` stream stays open
and every append keeps working.** Nothing raises. If observers stop firing but
appends succeed, look at the connection before you look at the observer.

Reconnect is exponential backoff — `:retry_attempts` 5, `:reconnect_base_delay`
1000 ms, `:reconnect_max_delay` 10000 ms — re-resolving addresses on every
attempt.

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
- **No telemetry.** The client emits no `:telemetry` events; `telemetry` appears
  only as a transitive dependency of `grpc`. Logging is plain `Logger`.
- **No formatter export.** `.formatter.exs` declares no `locals_without_parens`
  for `from`, `join`, `removed_with`, `from_every`, `pii`, `subject`, `unique`, or
  `unique_event_type`, and there is no `import_deps: [:cratis_chronicle]` to
  inherit. `mix format` will add parentheses to the DSL.
- **`ReadModels.watch/2` is unsupervised.** It uses `Task.start/1` and `unwatch/1`
  is a raw `Process.exit(pid, :shutdown)`. Supervise it yourself if it matters.

## Common pitfalls

| Pitfall | Why it bites |
| --- | --- |
| Copying the root `README.md` | Its module names do not exist at `v2.2.0` |
| Using `Chronicle.Concept` | It exists only in the unpublished `v2.3.0` tag |
| `{:cratis_chronicle, "~> 0.1"}` from the docs | Does not match the published `2.2.0` |
| Quoting the `VERSION` file | It is a `0.0.5` build-time placeholder |
| Putting configuration in `config.exs` | The client reads no application env; options are child-spec keywords |
| Omitting `:otp_app` | Discovery falls back to scanning every loaded module |
| Assuming the namespace default is `"default"` | It is `"Default"`; the moduledoc is wrong |
| `:ok = Chronicle.append(...)` in a server | A constraint violation returns `{:error, _}` and raises `MatchError` |
| Waiting for `:connected` | Observers attach at `:registered` |
| Reading `:correlation_id` from a reactor context | The map has five keys and that is not one |
| Two read models with the same final module segment | The id is the last segment only |
| Appending from a `Task` and expecting the correlation id | Ambient context is per process |
| Treating quiet observers as "no events" | Keepalive eviction silences observers while appends still succeed |
| Shipping the default TLS behavior | `skip_tls_validation` defaults to `true` |

## Verify

- `mix deps.get` resolves `cratis_chronicle` to the `2.x` version you intended.
- `Chronicle.Client` is in the supervision tree with an explicit `:otp_app`, and
  the tree starts clean.
- The lifecycle reaches `:registered` before the application claims readiness.
- A production connection string sets `skipTlsValidation=false`.
- Every event type module passes an explicit, stable `id:`.
- Every `Chronicle.append/3` call site handles `{:error, _}` as well as `:ok`.
- Every reactor's `@handles` list matches the clauses of its `handle/2`.
- Read model module names have distinct final segments, or explicit ids.
- Every code example was copied from `Documentation/client-snippets/`, not from
  the README.
- `mix compile --warnings-as-errors` and `mix test` are clean.
