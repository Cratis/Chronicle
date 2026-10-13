---
applyTo: "**/*.ex,**/*.exs"
paths:
  - "**/*.ex"
  - "**/*.exs"
---
<!-- cratis-ai-managed: harnesses/copilot/instructions/elixir.instructions.md -->
<!-- cratis-ai: generated Copilot instructions. Do not edit; rules/ is the source. -->

<!-- Copyright (c) Cratis. All rights reserved. -->
<!-- Licensed under the MIT license. See LICENSE file in the project root for full license information. -->

# Elixir Conventions

Use Elixir and OTP idioms, not translated C# or JVM shapes. These conventions
are grounded in `Cratis/Chronicle.Elixir v3.10.2` (`Source/chronicle/mix.exs`,
`.formatter.exs`, and `lib/chronicle/`). Client-specific APIs belong in the
**cratis-chronicle-client-elixir** skill.

## Build and format

- For Chronicle 3.10.x use Elixir 1.18 or later (`~> 1.18`).
- Run `mix compile --warnings-as-errors`, `mix test`, and
  `mix format --check-formatted` with the project's formatter inputs.
- Use two-space indentation and let `mix format` own layout.
- Chronicle exports no `locals_without_parens` formatter configuration. Accept
  parentheses around its DSL calls; do not assume `import_deps` supplies them.

## Code shape

- PascalCase module names; snake_case functions, variables and struct fields.
- Use structs for event/read-model data and pattern-matching clauses for
  dispatch. Schema-bearing structs need typed defaults; `nil` is not a type
  declaration and registers as string in this client.
- Preserve tagged results such as `:ok`, `{:ok, value}`, and `{:error, reason}`.
  Handle expected remote failures explicitly rather than using a success-only
  match in a long-lived process.
- Put long-lived processes under the application's supervision tree. Wait for
  actual client readiness, not merely a successful supervisor start.
- Process-local context is not inherited by `Task` processes; pass required
  context explicitly at that boundary.
- Document public APIs with `@moduledoc`, `@doc`, and `@spec`; use `Logger` for
  operational logging and keep credentials out of messages and crash reports.
- Use American English in identifiers, comments and documentation.

Every `.ex` and `.exs` source file starts with:

```elixir
# Copyright (c) Cratis. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.
```
