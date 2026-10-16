<!-- cratis-ai-managed: skills/cratis-chronicle-client-elixir/references/captures.md -->
<!-- Copyright (c) Cratis. All rights reserved. -->
<!-- Licensed under the MIT license. See LICENSE file in the project root for full license information. -->

# Captures

Source: `Cratis/Chronicle.Elixir`: `Source/chronicle/lib/chronicle/captures.ex`, `captures/capture.ex`, `captures/registrar.ex`, and `Documentation/captures.md`.

Requires `cratis_chronicle` 3.13.0.

A capture turns data that arrives from outside the event log (an external API, an inbound webhook, another event store's public events) into events appended to this event store. The DSL compiles at build time to Capture Declaration Language (CDL), the text the kernel's `SaveCapture` accepts. Use a capture instead of a hand-written poller when the source data should become facts you own.

```elixir
# Copyright (c) Cratis. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.

defmodule MyApp.Captures.InvoiceCapture do
  use Chronicle.Captures.Capture, name: "InvoiceCapture"

  api_source("InvoicingApi", route: "/invoices", poll: "10m")

  key("id")

  append MyApp.Events.InvoiceStatusChanged, when: "status" do
    set(:status, "$.status")
  end
end
```

- Exactly one source: `api_source/1,2` (polls a named external service), `webhook_source/1`, or `events_source/1` (a list of registered event type modules from another event store's public events).
- `key/1` names the property path that identifies and diffs an item; use `"$eventSourceId"` for an events source.
- `:id` defaults to a UUIDv5 of the module name, so redeploying updates the same kernel-side capture. `:name` defaults to the last module segment. `start: true` makes the registrar start it once saved.
- `when:` takes a property name, `{:any, [...]}`, `{:all, [...]}`, `{:transition, property, from, to}`, `:added`, `:removed`, or `{:expression, "template"}`. `set/2` maps a target field from `"$.path"` (payload) or `"$context.<property>"`.
- The DSL also has `nested` and `children` scopes, and a `map do` block with `rename`, `template`, `translate` and `split`.

**An appended event type's id must start with an uppercase letter and contain only letters, digits and underscores.** The kernel's CDL parser rejects hyphens, so `use Chronicle.Events.EventType, id: "InvoiceStatusChanged"` works as an `append` target and `id: "invoice-status-changed"` raises `ArgumentError` at compile time. This restriction does not apply to `events_source/1`.

**The kernel's capture engine runs a subset of the language**: only `api` and `events` sources, and only root-level `append` rules. `map`, `nested` and `children` compile and validate but are not applied at runtime, and webhook sources cannot carry authentication.

## Registering and managing

Captures are discovered with `otp_app:` or listed in the client's `:captures` option. `Chronicle.Captures.Registrar` saves them at `:registered` and again on every reconnect. It skips a capture whose declaration already matches, and warns before saving over a **started** capture whose declaration changed, because saving stops it on the kernel.

Manage directly with `Chronicle.Captures.register/1`, `start/1`, `stop/1`, `delete/1`, `list/0`, `validate/1` (a module or raw CDL text) and `captured_events/1`. `start`, `stop` and `delete` also accept a raw capture id; `delete` is destructive, so obtain authority for the specific capture first.
