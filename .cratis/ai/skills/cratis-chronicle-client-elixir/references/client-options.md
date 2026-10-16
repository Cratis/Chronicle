<!-- cratis-ai-managed: skills/cratis-chronicle-client-elixir/references/client-options.md -->
# Client options and supervision

This reference holds the `Chronicle.Client` child-spec options, its supervision tree and the multiple-client rule.

### Options

Pass **child-spec keyword options**. The client reads no application env;
your app must read its own configuration and pass it in (`lib/chronicle/client.ex`).

| Option | Default |
| --- | --- |
| `:name` | `Chronicle.Client` |
| `:connection_string` | `ConnectionString.default/0` |
| `:event_store` | `"default"` |
| `:namespace` | `"Default"` — capital D |
| `:discover` | `true` |
| `:otp_app` | none; falls back to scanning loaded modules |
| `:event_types`, `:event_sources`, `:migrations`, `:reactors`, `:reducers`, `:read_models`, `:projections`, `:seeders`, `:webhooks`, `:event_store_subscriptions`, `:global_handlers` | `[]`, merged with discovered |
| `:constraints`, `:captures`, `:read_model_reactors` | `[]`, merged with discovered where discoverable |
| `:drift_policy` | `:warn`; `:ignore` or `:fail`. See [discovery-and-generations.md](discovery-and-generations.md) |
| `:strict_validation` | `false`; `true` raises on event type and migration mistakes instead of logging |
| `:warn_defaulted_ids` | `true` |
| `:event_generation_delivery` | `:compatibility`; `:pinned` delivers events at their appended generation |
| `:concurrency` | none; `[default: :optimistic, check_first_append: bool]`. See [decision-reads.md](decision-reads.md) |
| `:default_sink_type_id` | `:mongodb`; accepts `:mongodb`, `:sql`, `:in_memory`, `:none` or a raw string |
| `:server_address` | alternative `"host:port"` address (connection option) |
| `:skip_tls_validation`, `:load_balancer`, `:grpc_options`, `:reconnect_base_delay`, `:reconnect_max_delay` | forwarded to the connection | — |
| `:retry_attempts` | accepted for compatibility, **ignored** |

> The moduledoc in `client.ex` says the namespace defaults to `"default"`.
> The code uses `"Default"`. **Trust the code.**

**Set `:otp_app`.** With it, discovery is scoped to your application's modules;
without it the client falls back to scanning every loaded module.

Configuration is stashed in `:persistent_term` and read back with
`Chronicle.Client.config/1` (`client.ex`). Its subset includes `event_sources`,
but not `read_models`, `projections`, or `migrations`.

### The supervision tree

`Supervisor.init(children, strategy: :rest_for_one)` — `client.ex`.
The lifecycle hub is the root dependency; restarting it restarts its subscribers.
The connection, session and registration precede observers.

### Multiple clients

Pass `name:` to `Chronicle.Client` and `client:` to every API call. Every public
function takes a `:client` option.
