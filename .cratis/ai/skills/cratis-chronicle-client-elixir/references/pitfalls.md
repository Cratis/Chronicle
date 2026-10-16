<!-- cratis-ai-managed: skills/cratis-chronicle-client-elixir/references/pitfalls.md -->
<!-- Copyright (c) Cratis. All rights reserved. -->
<!-- Licensed under the MIT license. See LICENSE file in the project root for full license information. -->

# Common pitfalls

Source: `Cratis/Chronicle.Elixir`.

| Pitfall | Why it bites |
| --- | --- |
| Copying an older root README | Its module names may not exist; use namespaced `Chronicle.*` modules |
| Using `Chronicle.Concept` on a client that lacks it | Annotate fields with `pii/1,2` instead |
| Combining `pii` and `encrypted` | The same leaf raises `PiiAndEncryptedCombinedNotSupported`; neither is allowed on an `event_source_id: true` concept |
| Quoting the `VERSION` file | It is a build-time placeholder |
| Putting options only in `config.exs` | The client reads no application env; your app must pass child-spec keyword options |
| Omitting `:otp_app` | Discovery falls back to scanning every loaded module |
| Assuming the namespace default is `"default"` | It is `"Default"`; the moduledoc is wrong |
| `:ok = Chronicle.append(...)` in a server | A constraint violation returns `{:error, _}` and raises `MatchError` |
| Waiting for `:connected` | Observers attach at `:registered` |
| Reading `:correlation_id` or `:caused_by` from the context of an older client | It was a bare six-key map before the `EventContext` struct; see the upgrading reference |
| Two read models with the same final module segment | The default id is the last segment only |
| Appending from a `Task` and expecting the correlation id | Ambient context is per process |
| Treating quiet observers as "no events" | Keepalive eviction can silence observers while appends still succeed |
| Shipping the default TLS behavior | `skip_tls_validation` defaults to `true` |
| Forgetting `global_handlers:` or `:otp_app` discovery | Shared mappings are not merged into variants unless registered |
| A shared handler mapping a field one variant lacks | `GlobalHandlerPropertyNotOnVariant` at registration, not a silently skipped mapping |
| `DecisionReads.get/2` outside `Chronicle.decide/2` | Returns `{:error, :requires_unit_of_work}`; it guards nothing |
| Unset expected sequence number as "expect no event" | It disables the check; use `ConcurrencyScope.expecting_no_matching_event/1` |
| Passing no `client: client` in a `Chronicle.Testing.Case` spec | The call addresses the default client, not the test's |
| Trusting a spec that passed on the in-memory kernel | Queries, projections and `closes_stream` return `{:error, {:not_supported_in_memory, rpc}}`; use `Testing.Integration` |
| Renaming a reactor, reducer or projection module without a pinned `:id` | Its observation position silently resets |
| Capture event type ids with hyphens | `append/2,3` needs an uppercase-first, letters-digits-underscore id |
| Nested `children/3` ids reused across read model instances | Nested events can land on the wrong instance; use UUIDs |
| Reading a read model right after an append | `{:ok, nil}` means absent or not projected yet; use `Projections.wait_for/3` |
