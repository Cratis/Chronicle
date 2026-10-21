<!-- cratis-ai-managed: skills/cratis-chronicle-client-elixir/references/patterns.md -->
<!-- Copyright (c) Cratis. All rights reserved. -->
<!-- Licensed under the MIT license. See LICENSE file in the project root for full license information. -->

# Behavior patterns

Source: `Cratis/Chronicle.Elixir`: `Source/chronicle/lib/chronicle/patterns.ex` and `Documentation/patterns.md`.

Requires `cratis_chronicle` 3.13.0.

`Chronicle.Patterns` queries the recurring behavior (combinations of facets such as `Day`, `TimeBucket`, `CommandType`, `AggregateType`) that Chronicle mined from an event store's history for a scope, for example one user. **An empty result is an answer, not a failure**: the scope has no established behavior for that context. Treat it as "do not claim to know".

```elixir
{:ok, patterns} = Chronicle.Patterns.get_patterns_at("user-42")
{:ok, patterns} = Chronicle.Patterns.get_patterns_at("user-42", moment, also_constraining: %{Chronicle.Patterns.FacetName.aggregate_type() => "Invoice"})
action = pattern.facets[Chronicle.Patterns.FacetName.command_type()]
```

- Default to `get_patterns_at/3` ("what does this scope usually do now"). It derives `Day` and `TimeBucket` from the moment's **own offset**, not UTC, with the rule the kernel mined with; the moment defaults to now.
- Use `get_patterns/3` and `get_usual_actions/3` only for a hand-built facet map; `get_usual_actions/3` returns at most one answer per action.
- `get_patterns_for_scope/2` and `get_scopes/1` inspect what has been mined.
- `Chronicle.Patterns.time_bucket/1` exposes the kernel's part-of-day rule (`:early_morning`, `:morning`, `:midday`, `:afternoon`, `:evening`, `:night`). **Never write a copy**: a slot the mining never used looks exactly like "nothing established".
- Options on every call: `:client`, `:namespace`, `:minimum_confidence` (0.0 to 1.0) and `:max_results`. The all-patterns query is not exposed.
