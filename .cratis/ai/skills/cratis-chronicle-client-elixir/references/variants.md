<!-- cratis-ai-managed: skills/cratis-chronicle-client-elixir/references/variants.md -->
<!-- Copyright (c) Cratis. All rights reserved. -->
<!-- Licensed under the MIT license. See LICENSE file in the project root for full license information. -->

# Variants

## Variants — mutually exclusive read models for one entity's lifecycle

> Requires `cratis_chronicle` `3.4.0` or later. Verified at
> `Cratis/Chronicle.Elixir v3.10.2` (`Source/chronicle/lib/chronicle/read_models/read_model.ex:280-300`,
> `projections/global_handler.ex`, `projections/variant_reclassifier.ex`).
>
> The 3.4.0 release reported a kernel `$eventSourceId` property-mapping limitation
> (Cratis/Chronicle#3924). This client-source verification does not establish
> whether kernel 19.x fixed it; verify the full variant transition against your
> kernel before claiming end-to-end support.

Some entities do not have one shape for their whole lifetime — a work item is a
backlog entry until a pull request exists for it, then it is a pull request
until it merges. `variant_of/2` and `enters_on/1,2` are macros imported by
**both** `use Chronicle.ReadModels.ReadModel` and
`use Chronicle.Projections.Projection`, so the model-bound and declarative
paths declare a variant identically:

```elixir
defmodule MyApp.ReadModels.WorkItem do
end

defmodule MyApp.ReadModels.BacklogItem do
  use Chronicle.ReadModels.ReadModel
  defstruct id: "", title: ""

  variant_of MyApp.ReadModels.WorkItem, key: :id
  enters_on MyApp.Events.IssueCreated

  from MyApp.Events.IssueCreated, set: [id: :event_source_id, title: :title]
end

defmodule MyApp.ReadModels.PullRequestItem do
  use Chronicle.ReadModels.ReadModel
  defstruct id: "", title: "", pull_request_url: "", build_status: ""

  variant_of MyApp.ReadModels.WorkItem, key: :id
  enters_on MyApp.Events.PullRequestCreated

  from MyApp.Events.PullRequestCreated,
    set: [id: :event_source_id, pull_request_url: :pull_request_url]

  # not the entering event -> automatically reclassified into an update-only join
  from MyApp.Events.BuildCompleted, set: [build_status: :build_status]
end
```

`variant_of/2` takes `:key` — **required** — the field on this variant that
carries the shared identity. `enters_on/1,2` is repeatable and its own `:key`
option names an *event* property (defaults to `:event_source_id`); every
`from`/`join` this variant declares for a non-entering event, whether declared
locally or merged from a shared handler, is automatically reclassified into an
update-only join keyed on `variant_of`'s `:key`.

A mapping shared across every variant of an identity is a
`Chronicle.Projections.GlobalHandler`, never registered as a projection on its
own:

```elixir
defmodule MyApp.Projections.WorkItemTitleHandler do
  use Chronicle.Projections.GlobalHandler, identity: MyApp.ReadModels.WorkItem

  from MyApp.Events.TitleChanged, set: [title: :title]
end
```

Register it explicitly with `global_handlers: [...]` on `Chronicle.Client`, or
let `:otp_app` auto-discovery find it (modules exporting
`__chronicle_global_handler__/1`). A mapping that targets a field some variant
lacks raises `Chronicle.Projections.GlobalHandlerPropertyNotOnVariant` at
registration, not a silently skipped mapping.

**A variant with no `enters_on` raises
`Chronicle.Projections.VariantMustDeclareEntersOnEvent`** at registration — a
variant that could never be entered could never be written to at all, since
every other handler on it is update-only.
