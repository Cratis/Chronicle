<!-- cratis-ai-managed: skills/cratis-chronicle-client-elixir/references/idioms-and-inspection.md -->
# Idioms and operational inspection

This reference holds process-scoped context, discovery and formatter notes, watch supervision, and observer, failed-partition and job inspection.

## Operational inspection

`Chronicle.Observers.get_all/1` lists namespace observers; `remove/2` removes an
orphaned observer across the store, refuses running/subscribed observers in any
namespace, and leaves read model data intact (`observers.ex`). Obtain
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
- **Telemetry is built in.** The client emits OpenTelemetry spans and `:telemetry`
  events; read [observability.md](observability.md). Logging is `Logger`.
- **No formatter export.** `.formatter.exs` declares no `locals_without_parens`
  for `from`, `join`, `removed_with`, `from_every`, `pii`, `subject`, `unique`, or
  `unique_event_type`, and there is no `import_deps: [:cratis_chronicle]` to
  inherit. `mix format` will add parentheses to the DSL.
- **`ReadModels.watch/2` is unsupervised.** It uses `Task.start/1` and `unwatch/1`
  is a raw `Process.exit(pid, :shutdown)`. Supervise it yourself if it matters.
