# Chronicle Repository Issue Analysis

> **Sections A–C** (Excluded, Copilot-assigned, Potentially Obsolete) are **auto-generated** on 2026-03-13
> by the [issue-analysis workflow](.github/workflows/issue-analysis.yml).
> **Sections 1–3** below the `<!-- HAND-WRITTEN SECTIONS START -->` marker are maintained by AI
> analysis and manual review.
>
> **How the workflow works:**
> 1. Fetches all **open** issues and all open/recently merged PRs via the GitHub CLI.
> 2. Scans every PR body for closing-keyword patterns (`closes #N`, `fixes #N`, `resolves #N`,
>    `owner/repo#N`, etc.) to build an issue → PR map.
> 3. Classifies each open issue into one of the buckets below.
>    Issues with only merged (not open) PRs are moved to the backlog sections, not section B.
> 4. Regenerates sections A–C while leaving everything below the marker intact.
> 5. Closed issues and closed pull requests are **never** included.


---

## A. Excluded — Has an Open Pull Request

Issues with at least one open PR are excluded from the backlog triage; work is actively underway.

| # | Issue | Pull Request(s) |
|---|-------|-----------------|
| [#2831](https://github.com/Cratis/Chronicle/issues/2831) | Outbox - inbox pattern for events | PR [#2832](https://github.com/Cratis/Chronicle/pull/2832) *(Copilot)* |
| [#2798](https://github.com/Cratis/Chronicle/issues/2798) | Add support for Aspire | PR [#2799](https://github.com/Cratis/Chronicle/pull/2799) *(Copilot)* |
| [#2793](https://github.com/Cratis/Chronicle/issues/2793) | Specify Admin password in development image as config option | PR [#2794](https://github.com/Cratis/Chronicle/pull/2794) *(Copilot)* |
| [#2764](https://github.com/Cratis/Chronicle/issues/2764) | Support projecting without specifying a read model - infer it | PR [#2765](https://github.com/Cratis/Chronicle/pull/2765) *(Copilot)* |
| [#2600](https://github.com/Cratis/Chronicle/issues/2600) | Agentic Event Sourcing | PR [#2601](https://github.com/Cratis/Chronicle/pull/2601) *(Copilot)* |
| [#2268](https://github.com/Cratis/Chronicle/issues/2268) | Enable running integration specs for different setups | PR [#2269](https://github.com/Cratis/Chronicle/pull/2269) *(Copilot)* |
| [#1440](https://github.com/Cratis/Chronicle/issues/1440) | Disable replay button for disconnected observers | PR [#2829](https://github.com/Cratis/Chronicle/pull/2829) *(Copilot)* |
| [#1439](https://github.com/Cratis/Chronicle/issues/1439) | Show "Are you sure" dialog when ignoring recommendation | PR [#2828](https://github.com/Cratis/Chronicle/pull/2828) *(Copilot)* |
| [#253](https://github.com/Cratis/Chronicle/issues/253) | Jazz up the readme file | PR [#2827](https://github.com/Cratis/Chronicle/pull/2827) *(Copilot)* |

---

## B. Assigned to Copilot — No PR Yet

These issues are assigned to Copilot and have **no pull request at all** (neither open nor merged). They may be queued, in progress, or awaiting a trigger.

| # | Issue |
|---|-------|
| [#2681](https://github.com/Cratis/Chronicle/issues/2681) | Switch out components in Workbench |
| [#2644](https://github.com/Cratis/Chronicle/issues/2644) | Add the ability to add events directly in the Event Sequence editor / page in Workbench |
| [#2640](https://github.com/Cratis/Chronicle/issues/2640) | Hook up redaction from Workbench |
| [#2464](https://github.com/Cratis/Chronicle/issues/2464) | All and Dictionaries |
| [#2435](https://github.com/Cratis/Chronicle/issues/2435) | Add events in sequences page |
| [#2433](https://github.com/Cratis/Chronicle/issues/2433) | Improve seed data and add editor |
| [#1869](https://github.com/Cratis/Chronicle/issues/1869) | Support SQL type of servers as Sink for Reducers and Projections |
| [#1863](https://github.com/Cratis/Chronicle/issues/1863) | Support clustering for Kernel |
| [#1859](https://github.com/Cratis/Chronicle/issues/1859) | Support migration of events between generations (up & down casting) |
| [#1749](https://github.com/Cratis/Chronicle/issues/1749) | Sink last handled event sequence number is wrong |
| [#1459](https://github.com/Cratis/Chronicle/issues/1459) | Look into switching to .NET 9 new JsonSchemaExporter instead of NJsonSchema |

---

## C. Potentially Obsolete

No issues are currently flagged as potentially obsolete (none have been inactive for ≥ 2 years without a linked PR).


<!-- HAND-WRITTEN SECTIONS START -->

## 1. Already Implemented in Code

These features exist in the current codebase in a substantially complete form. **All issues in this
section should be closed** — they are also listed in Section C (Potentially Obsolete) above as
candidates for closure.

| # | Issue | Evidence in codebase |
|---|-------|----------------------|
| [#238](https://github.com/Cratis/Chronicle/issues/238) | Basic Projection UI editor | `Source/Workbench/Components/ProjectionEditor/` contains a full Monaco-based projection editor with language support (completion, hover, code actions). `Features/EventStore/General/Projections/Projections.tsx` uses it with `PreviewProjection`, `SaveProjection`, `TimeMachineDialog`, and `ReadModelInstances`. |
| [#549](https://github.com/Cratis/Chronicle/issues/549) | Add a way to add tags/classifiers to event types | `Tag.cs`, `TagAttribute.cs`, `TagsAttribute.cs`, `TagExtensions.cs` all exist in `Clients/DotNET/`. The `Append()` method reads static tags from the event type and merges with runtime tags (`EventSequence.cs:92-93`). |
| [#856](https://github.com/Cratis/Chronicle/issues/856) | Failed partition view in workbench | `Features/EventStore/Namespaces/FailedPartitions/FailedPartitions.tsx` shows partition, attempt count, last attempt time, and has a retry action button. |
| [#949](https://github.com/Cratis/Chronicle/issues/949) | Support custom keys beyond EventSourceId | `Clients/DotNET/Projections/` has `KeyBuilder.cs`, `CompositeKeyBuilder.cs`, `ParentKeyBuilder.cs`, `KeyAndParentKeyBuilder.cs`. The kernel has `KeyResolvers.cs` with full resolution logic including parent hierarchy keys. |
| [#1305](https://github.com/Cratis/Chronicle/issues/1305) | Integration tests for core functionality using in-process Orleans client | `Clients/XUnit.Integration/ChronicleInProcessFixture.cs` and `ChronicleOrleansFixture.cs` / `ChronicleOrleansInProcessWebApplicationFactory.cs` exist. Integration test projects use these fixtures. |
| [#1527](https://github.com/Cratis/Chronicle/issues/1527) | Add tracing | `Kernel/Core/Diagnostics/OpenTelemetry/Tracing/ChronicleActivity.cs` defines the `ActivitySource`. `Kernel/Core/Services/Observation/Tracing.cs` instruments observer registration. `OpenTelemetryConfigurationExtensions.cs` wires it up. |
| [#1564](https://github.com/Cratis/Chronicle/issues/1564) | Extract integration test helpers into the testing library | `Clients/Testing/` project contains `EventStoreForTesting.cs`, `EventLogForTesting.cs`, `EventSequenceForTesting.cs`, `CausationManagerForTesting.cs`. |
| [#1860](https://github.com/Cratis/Chronicle/issues/1860) | Support for compensating existing events | `Kernel/Storage.MongoDB/EventSequences/EventCompensation.cs` defines the storage model. `Kernel/Core/EventSequences/IEventSequence.cs` has two `Compensate()` overloads. `Kernel/Core/EventSequences/EventSequence.cs` implements them. |
| [#1858](https://github.com/Cratis/Chronicle/issues/1858) | Add support for redacting events | `Kernel/Core/EventSequences/EventRedacted.cs`, `EventsRedactedForEventSource.cs`, two `Redact()` overloads on `IEventSequence` and `EventSequence`, client `IEventSequence.Redact()` methods, and `Workbench/Api/EventSequences/Redact.ts` / `RedactMany.ts` all exist. |
| [#1911](https://github.com/Cratis/Chronicle/issues/1911) | Sequence Query editor in Workbench | `Features/EventStore/Namespaces/Sequences/Query.tsx` with `QueryDefinition`, `EventList`, `Histogram`, `SequenceSelector`, and time-range filtering exists. |

---

## 2. Can Do Without More Details

These issues have sufficiently clear requirements to implement directly.

### Workbench / UI

| # | Issue | Notes |
|---|-------|-------|
| [#247](https://github.com/Cratis/Chronicle/issues/247) | Workbench event log — export filtered data | Add an "Export" button in Sequences view that downloads filtered events as JSON. |
| [#253](https://github.com/Cratis/Chronicle/issues/253) | Jazz up the readme file | README improvements with better structure, badges, and examples. |
| [#776](https://github.com/Cratis/Chronicle/issues/776) / [#889](https://github.com/Cratis/Chronicle/issues/889) / [#1010](https://github.com/Cratis/Chronicle/issues/1010) | Support replaying a specific event source id / partition from Workbench | `Workbench/Api/Observation/ReplayPartition.ts` is generated but not wired up in `ObserversViewModel.ts`. Add UI for selecting a partition and calling `ReplayPartition`. |
| [#1436](https://github.com/Cratis/Chronicle/issues/1436) | Show failed partition details when hovering and when clicking | Add a hover tooltip showing error glimpse and a detail panel on the right with all attempts when a failed partition is clicked. `FailedPartitions.tsx` needs to be extended. |
| [#1437](https://github.com/Cratis/Chronicle/issues/1437) | Event and EventType details views should have scrollbars when content overflows vertically | CSS fix: add `overflow-y: auto` / `overflow: auto` to the details containers. |
| [#1439](https://github.com/Cratis/Chronicle/issues/1439) | Show "Are you sure" dialog when ignoring a recommendation | `RecommendationsViewModel.ignore()` needs a confirmation dialog (pattern already exists in `ObserversViewModel.replay()`). |
| [#1440](https://github.com/Cratis/Chronicle/issues/1440) | Disable replay button for disconnected observers | In `Observers.tsx`, add a condition on the Replay `MenuItem` that also disables it when `selectedObserver.runningState === ObserverRunningState.disconnected`. |
| [#1557](https://github.com/Cratis/Chronicle/issues/1557) | Show number of handled events for every observer | Add a column to the Observers table showing count of events from sequence start to `lastHandledEventSequenceNumber`. May need a backend query or grain-based cache. |
| [#1684](https://github.com/Cratis/Chronicle/issues/1684) | Add capability of changing name of an identity | Extend identity API in Kernel all the way to client with a `ChangeName(IdentityId, name)` method. |
| [#1925](https://github.com/Cratis/Chronicle/issues/1925) | Filtering of properties in Event Sequence table | The current `Sequences.tsx` has partial filtering. Add column filters for all columns (EventSourceId, EventType, Occurred range). |
| [#2435](https://github.com/Cratis/Chronicle/issues/2435) | Add events in sequences page | Dialog with event type list on left, editable datatable on right, validated against selected event type schema, appending events on confirm. |
| [#2640](https://github.com/Cratis/Chronicle/issues/2640) | Hook up redaction from Workbench | Add "Redact" button to event list (enabled on selection), dialog asking for reason, calling `Redact` command. Backend already supports it. |
| [#2644](https://github.com/Cratis/Chronicle/issues/2644) | Add the ability to add events directly in the Event Sequence editor | Similar to #2435 but inline in the sequence view. "+ Add Event" button at top, row at top with event type dropdown and content dialog. |

### .NET Client / Backend

| # | Issue | Notes |
|---|-------|-------|
| [#362](https://github.com/Cratis/Chronicle/issues/362) | Provide a codespaces / devcontainer | Add `.devcontainer/devcontainer.json` with Docker Compose configuration referencing the existing `docker-compose.yml`. |
| [#482](https://github.com/Cratis/Chronicle/issues/482) | Enable CA1852 (sealed keyword) and apply it | `.editorconfig` currently has `dotnet_diagnostic.CA1852.severity = none`. Enable it, then add `sealed` to all eligible classes. |
| [#523](https://github.com/Cratis/Chronicle/issues/523) | Optimize iterations of lists with `CollectionMarshal.AsSpan()` | Identify `List<T>` iterations in hot paths (event dispatch, serialization) and use `CollectionMarshal.AsSpan()`. |
| [#547](https://github.com/Cratis/Chronicle/issues/547) | Proper error message when specifying same property twice in projection definitions | Add duplicate-key checks in `FromBuilder`, `JoinBuilder`, `CompositeKeyBuilder` etc. and throw a descriptive exception. |
| [#579](https://github.com/Cratis/Chronicle/issues/579) | Add support for enums as values in projections (e.g. `.ToValue()`) | Handle `enum` types in `EventValueProviders.cs` and expression resolvers so enum values are correctly converted rather than failing. |
| [#690](https://github.com/Cratis/Chronicle/issues/690) | Improve error messages from JSON Compliance Manager when releasing events | Catch and rethrow with detailed context info (event type, property, value) when release fails. |
| [#1175](https://github.com/Cratis/Chronicle/issues/1175) | Add global exception handler | Implement `IExceptionHandler` (.NET 8) and register it in the kernel and client ASP.NET Core pipelines to catch unhandled exceptions and log them. |
| [#1258](https://github.com/Cratis/Chronicle/issues/1258) | Use `EventSerializer` everywhere instead of `JsonSerializer` directly | Find all direct `JsonSerializer` usages in event converters and replace with `EventSerializer`. |
| [#1271](https://github.com/Cratis/Chronicle/issues/1271) | Do not create recommendations if there already is one that is the same | Add a duplicate check before inserting a new recommendation in the recommendation grain/manager. |
| [#1309](https://github.com/Cratis/Chronicle/issues/1309) | Change static, never-mutated collections to `FrozenDictionary` / `FrozenSet` | Identify startup-time collections in `DefaultClientArtifactsProvider`, event type registrations, handler dictionaries etc. and call `.ToFrozenDictionary()`. |
| [#1343](https://github.com/Cratis/Chronicle/issues/1343) | Fix unhandled exceptions so they appear in logs (not just console) | Wire up unhandled task exception and domain-level exception sinks to the ILogger infrastructure. |
| [#1350](https://github.com/Cratis/Chronicle/issues/1350) | Introduce a specific Join builder for Children (split from root Join builder) | Create `IChildrenJoinBuilder` / `ChildrenJoinBuilder` without `.On()` and requiring `.IdentifiedBy()`, using the existing `ChildrenBuilder` as a guide. |
| [#1351](https://github.com/Cratis/Chronicle/issues/1351) | Add integration specs for AppendedEventsQueues system | Write `when_appending_events/` integration specs that verify the queue system routes events to the correct observers. |
| [#1395](https://github.com/Cratis/Chronicle/issues/1395) | Add logging messages for client reactors + reducers | Add `[LoggerMessage]` entries to `ReactorLogMessages.cs` and create a `ReducerLogMessages.cs` for trace and error-level log entries. |
| [#1396](https://github.com/Cratis/Chronicle/issues/1396) | Add tracing to UnitOfWork | Use `ChronicleActivity.Source.StartActivity(...)` around `UnitOfWorkMiddleware` and `ITransactionalEventSequence.Commit()` calls. |
| [#1406](https://github.com/Cratis/Chronicle/issues/1406) | Add integration specs for constraints | Write integration specs exercising `UniqueConstraint` (happy path, duplicate violation, retry after violation resolved). |
| [#1418](https://github.com/Cratis/Chronicle/issues/1418) | `ObserverManager.Notify` should use the async `Func` delegate overload | Replace `Task.Wait()` calls in `Notify` with proper async invocation using the existing `Func<…, Task>` overload. |
| [#1475](https://github.com/Cratis/Chronicle/issues/1475) | Add support for clearing all children in a projection | Add `ClearAllWith(…)` to `IChildrenBuilder` / `ChildrenBuilder` that generates a "remove all" operation, similar to `RemovedWithBuilder`. |
| [#1537](https://github.com/Cratis/Chronicle/issues/1537) | Kernel Concept classes that could be `record` types | Convert eligible `class` types (e.g. `FailedPartitionAttempt`) to `record` for value equality and immutability. |
| [#1562](https://github.com/Cratis/Chronicle/issues/1562) | Integration tests for Reactors | Write integration specs for reactor scenarios (happy path, error, reconnect) using the existing `ChronicleInProcessFixture` pattern. |
| [#1563](https://github.com/Cratis/Chronicle/issues/1563) | Integration tests for Reducers | Write integration specs for reducer scenarios using the existing `ChronicleInProcessFixture` pattern. |
| [#1604](https://github.com/Cratis/Chronicle/issues/1604) | Observer grain should have a failed partition count metric | Add an `ObservableGauge` to `ObserverMetrics.cs` that reflects the number of currently failing partitions. |
| [#1612](https://github.com/Cratis/Chronicle/issues/1612) | Constraints should support composite multiple-property unique constraints | Extend `IConstraintBuilder.Unique()` to accept multiple property expressions (`.On(e => e.Name, e => e.FolderId)`). Code example is given in the issue. |
| [#1685](https://github.com/Cratis/Chronicle/issues/1685) | Job system should clean up jobs that are "dead in the water" | Add a timer-based cleanup in `JobsManager` that deletes jobs stuck in `PreparingSteps` state older than a configurable threshold. Also add a `CreatedAt` timestamp to `JobState`. |
| [#1779](https://github.com/Cratis/Chronicle/issues/1779) | Should not be able to start a job on an observer when there is already a replay job | Add a guard in `IObserver` / `Observer.cs` to reject job start requests (except `RetryFailedPartition`) when a replay job is currently running. |
| [#1923](https://github.com/Cratis/Chronicle/issues/1923) | Add OpenTelemetry tracing context to appending of events | Use `ChronicleActivity.Source.StartActivity("Append", ActivityKind.Producer)` in `EventSequence.Append()` and propagate trace context through gRPC calls to the observer pipeline. |
| [#2290](https://github.com/Cratis/Chronicle/issues/2290) | Fix `ReadModels.Register()` behavior for reactor-only applications | In `ReadModels.Register()`, ensure `ReadModelsManager` grain state is initialized even when there are no projections/reducers (remove the early-return guard or handle the reactor-only case). |
| [#2421](https://github.com/Cratis/Chronicle/issues/2421) | Synchronization of CoPilot instructions from other repositories | Add a GitHub Actions workflow triggered via `workflow_dispatch` with a `repository` input that clones the target repo, copies `.github/copilot-instructions.md` and `.github/instructions/` into this repo, and creates a commit. |
| [#2670](https://github.com/Cratis/Chronicle/issues/2670) | Reduce excessive Information-level logging | Go through `[LoggerMessage]` definitions (especially in startup, grain activation, and event type registration paths) and downgrade appropriate ones from `LogLevel.Information` to `LogLevel.Debug` or `LogLevel.Trace`. |
| [#2725](https://github.com/Cratis/Chronicle/issues/2725) | Adding a new event store does not carry over events marked as `[AllEventStores]` | When a new event store is registered, trigger re-registration of event types annotated with `[AllEventStores]` (e.g. `WebhookAdded`, `WebhookRemoved`) for the new store. The `EventTypes.cs` logic already exists; the gap is in the "new store added" flow. |
| [#2727](https://github.com/Cratis/Chronicle/issues/2727) | When removing WebHooks they don't really get removed from the storage | Investigate the grain state write path in `Webhook.cs` / `Webhooks.cs` when Remove is called. Ensure `ClearStateAsync()` or a proper state update removes the entry from the MongoDB backing store. |
| [#2729](https://github.com/Cratis/Chronicle/issues/2729) | Make it possible to override `Occurred` when appending events | Add a nullable `DateTimeOffset? occurred = null` parameter to `Append()` / `AppendMany()` in client `IEventSequence` and propagate through gRPC contracts. Server uses the client value if provided. Add integration specs. |
| [#2731](https://github.com/Cratis/Chronicle/issues/2731) | Add test and verification validator for adding WebHook | Add a "Test" button to `AddWebhookDialog.tsx` that calls `command.validate()`. Add a `CommandValidator<AddWebHook>` with an async rule that calls the backend to verify the endpoint URL and credentials. |
| [#2737](https://github.com/Cratis/Chronicle/issues/2737) | Support for Constant key in Projections | Add `UsingConstantKey(string value)` to `IFromBuilder` / `FromBuilder`, and a `ConstantKey` property to `[FromEvent]`. Kernel `KeyResolvers.cs` needs a new `FromConstant` resolver. |
| [#2739](https://github.com/Cratis/Chronicle/issues/2739) | Bundle Monaco Editor (standardize on `monaco-editor`, remove `@monaco-editor/react`) | Configure Vite with `vite-plugin-monaco-editor` (or the official worker configuration) to bundle Monaco. Migrate all usages from `@monaco-editor/react` to the official `monaco-editor` package. |

---

## 3. Need More Details

These issues are too vague, too large, require architectural decisions, need investigation, or depend on unknown external factors.

### Workbench / UI

| # | Issue | Why more details are needed |
|---|-------|-----------------------------|
| [#1690](https://github.com/Cratis/Chronicle/issues/1690) | Show catching up / replaying partitions in workbench and be able to delete them | No spec on what "catching up partitions" data looks like or what API to call. |
| [#1724](https://github.com/Cratis/Chronicle/issues/1724) | Constraint editor in workbench | No design for the UI or how constraints are edited/validated in the browser. |
| [#1960](https://github.com/Cratis/Chronicle/issues/1960) | Generate test events using LLM in workbench | Requires LLM integration design, API selection, and security model that haven't been specified. |
| [#2433](https://github.com/Cratis/Chronicle/issues/2433) | Improve seed data and add editor | Seed data needs new namespace-scoping API design before the editor can be built. |
| [#2694](https://github.com/Cratis/Chronicle/issues/2694) | Swap out custom components for standardized ones (from `@cratis/components`) | Requires knowing what's in `@cratis/components` and mapping each custom component. Depends on external package release. |
| [#2681](https://github.com/Cratis/Chronicle/issues/2681) | Switch out components in Workbench for `@cratis/components` equivalents | Same as above — needs the external package to be published and mapped. |

### .NET Client / API

| # | Issue | Why more details are needed |
|---|-------|-----------------------------|
| [#423](https://github.com/Cratis/Chronicle/issues/423) | Versioning check for client → server connections | No protocol spec for how version is communicated and what "compatible" means. Overlaps with #2638. |
| [#947](https://github.com/Cratis/Chronicle/issues/947) | Client validation of reducers | No spec for what rules to validate (event coverage, return types, etc.). |
| [#958](https://github.com/Cratis/Chronicle/issues/958) | Compile-time checks for reducers | Needs Roslyn analyzer design decisions. |
| [#963](https://github.com/Cratis/Chronicle/issues/963) | Add a way for testing client observers | No design for the test API — unclear if it should use mocks, an in-memory event store, etc. |
| [#962](https://github.com/Cratis/Chronicle/issues/962) | Add a way for testing client reducers | Same as #963. |
| [#966](https://github.com/Cratis/Chronicle/issues/966) | Client callback for handling migration strategies for observers | No API design for how migration callbacks are registered or invoked. |
| [#1009](https://github.com/Cratis/Chronicle/issues/1009) | IObserverMiddleware should have which observer it is running for | `IObserverMiddleware` doesn't exist yet in the codebase — it needs to be designed first. |
| [#1312](https://github.com/Cratis/Chronicle/issues/1312) | Add support for testing Projections | The testing infrastructure exists but there's no spec for a projection-specific test API. |
| [#1392](https://github.com/Cratis/Chronicle/issues/1392) | Support specifying key for events for Reactors and Reducers | Complex design change to key resolution — how the key is declared and used needs a clear API proposal. |
| [#1424](https://github.com/Cratis/Chronicle/issues/1424) | Fix Compliance hookup for clients | No description of what is broken or what the correct behavior should be. |
| [#1469](https://github.com/Cratis/Chronicle/issues/1469) | Expose configuration for adding `JsonConverters` | Needs a design for where and how converters are registered in the Chronicle client builder. |
| [#1470](https://github.com/Cratis/Chronicle/issues/1470) | Add support for overriding the type representation in JsonSchema | Depends on resolution of #1459 (JsonSchemaExporter migration). Can't be designed independently. |
| [#1473](https://github.com/Cratis/Chronicle/issues/1473) | Support Outbox → Inbox forwarding and subscriptions (configured from client) | No routing spec, no contract for how subscriptions are expressed, no design for transport. |
| [#1474](https://github.com/Cratis/Chronicle/issues/1474) | Unit of Work transactions do not respect event constraints | Needs investigation to determine root cause and the desired semantics when a constraint fails mid-transaction. |
| [#1566](https://github.com/Cratis/Chronicle/issues/1566) | Reactors will never reconnect if connection is lost | Needs reproduction steps and root cause analysis before a fix can be designed. |
| [#1740](https://github.com/Cratis/Chronicle/issues/1740) | Introduce a .NET InProcess client and reorganize clients | Large architectural reorganization — needs a migration plan that covers how to do it without breaking existing integrations. |
| [#1791](https://github.com/Cratis/Chronicle/issues/1791) | Switch to keyed service registration for Meter | Depends on how DI container keyed services interact with Orleans metrics — needs an architecture spike. |
| [#2290](https://github.com/Cratis/Chronicle/issues/2290) | Investigate ReadModels.Register() behavior for reactor-only applications | Wait — this actually has a clear description and a proposed fix. Moved to "Can Do" above. *(Correction: this is in "Can Do" list.)* |
| [#2638](https://github.com/Cratis/Chronicle/issues/2638) | Compatibility checks for clients when connecting to server | No spec for what version metadata to exchange or how to classify breaking vs non-breaking changes. |
| [#2651](https://github.com/Cratis/Chronicle/issues/2651) | Validate gRPC API surface for breaking changes in CI | Needs tooling selection (e.g., `buf breaking`) and a baseline API snapshot to compare against. |

### Kernel / Observation

| # | Issue | Why more details are needed |
|---|-------|-----------------------------|
| [#725](https://github.com/Cratis/Chronicle/issues/725) | Support ignoring specific errors for when a partition is failing | No spec for how ignore rules are declared or matched. |
| [#726](https://github.com/Cratis/Chronicle/issues/726) | Observer supervisor watchdog timer | No design for what state the watchdog checks or what action it takes. |
| [#733](https://github.com/Cratis/Chronicle/issues/733) | Observers should be able to know the running context they're in | No design for what the "context" object looks like or how it's injected. |
| [#734](https://github.com/Cratis/Chronicle/issues/734) | Support bulk events as cursor in the observer | No API design for how observers would consume a cursor vs individual events. |
| [#777](https://github.com/Cratis/Chronicle/issues/777) | Don't recommend replaying observers when event types haven't happened before current offset | Needs investigation into how recommendation generation works and when this case arises. |
| [#784](https://github.com/Cratis/Chronicle/issues/784) | Be notified when replay of an observer starts and stops | No API design for how the notification callback is registered or what it receives. |
| [#815](https://github.com/Cratis/Chronicle/issues/815) | EventRedacted event should only arrive in observer if observer was observing the event type | Needs investigation — unclear whether this is a filter issue in the observer subscriber or in event distribution. |
| [#845](https://github.com/Cratis/Chronicle/issues/845) | Separate event handler methods for events during replay | No API design for how to mark a method as "replay only" or "normal only". |
| [#863](https://github.com/Cratis/Chronicle/issues/863) | Observer should be paused for affected partition until redaction replay finishes | Complex coordination between event sequence, observer, and replay jobs — needs a design spike. |
| [#881](https://github.com/Cratis/Chronicle/issues/881) | Specialized observer worker for catching up/replaying multiple observers at a time | No design for the worker architecture or partitioning strategy. |
| [#884](https://github.com/Cratis/Chronicle/issues/884) | Support observers being stopped on debugger breakpoint without timing out | Needs an opt-in mechanism design and understanding of how Orleans handles grain activation timeouts during debugging. |
| [#888](https://github.com/Cratis/Chronicle/issues/888) | Support throttling for observers | No spec for how throttle rates are configured or where they're enforced. |
| [#892](https://github.com/Cratis/Chronicle/issues/892) | Mark read model with failure info when projection observer fails | Needs a design for how failed state is represented in the read model (extra field? metadata?). |
| [#978](https://github.com/Cratis/Chronicle/issues/978) | Failed partitions should be context-aware of the observer's running state when failing and resuming | Needs design for how running-state context is captured and communicated to the subscriber on resume. |
| [#1054](https://github.com/Cratis/Chronicle/issues/1054) | Exactly Once Processing for reducers, reactors, and projections | Large and complex — requires distributed transaction semantics or idempotency keys; no design spec. |
| [#1067](https://github.com/Cratis/Chronicle/issues/1067) | Support unmanaged observers / reactors | No definition of "unmanaged" in this context. |
| [#1433](https://github.com/Cratis/Chronicle/issues/1433) | While debugging, increase/disable timeout for observers | No spec for how to configure this (env variable, appsettings, debugger-detect?). |
| [#1521](https://github.com/Cratis/Chronicle/issues/1521) | Some Observer properties in state have wrong values | No reproduction steps or description of which properties are wrong. |
| [#1536](https://github.com/Cratis/Chronicle/issues/1536) | Observer does not handle calling Subscribe multiple times | No spec for the desired idempotency behavior. |
| [#1549](https://github.com/Cratis/Chronicle/issues/1549) | Observer does not handle partitions separately / in parallel | Complex concurrency design — needs a spec for how parallelism is enabled and bounded. |
| [#1585](https://github.com/Cratis/Chronicle/issues/1585) | `ConnectedClients` does not dispose the grain timer | Needs investigation to find the timer reference and confirm the fix pattern in Orleans. |
| [#1601](https://github.com/Cratis/Chronicle/issues/1601) | `IObserver` should not implement `IStateMachine` — it's an internal concern | Architectural refactor. The `IStateMachine` is baked into the observer hierarchy; removing it needs a plan for how state transitions are exposed. |
| [#1670](https://github.com/Cratis/Chronicle/issues/1670) | Make it possible for observers to opt in for parallelizing replay | Needs an API design for the opt-in flag and safe parallel execution guarantees. |
| [#1669](https://github.com/Cratis/Chronicle/issues/1669) | Reactors and Reducers notified when a replay begins and ends | No interface design for the callback (`IOnReplayBegin`? attribute?). |
| [#1683](https://github.com/Cratis/Chronicle/issues/1683) | When catching up all partitions, Observer should register all partitions itself | Unclear how the observer enumerates "all" partitions and what triggers the self-registration. |
| [#1682](https://github.com/Cratis/Chronicle/issues/1682) | Improve state consistency around observers and next / handled event sequence number | Needs investigation into where and why the sequence numbers diverge. |
| [#1705](https://github.com/Cratis/Chronicle/issues/1705) | Unsubscribing Observer should also pause all ongoing jobs | Needs a design for job lifecycle ownership and pause semantics. |
| [#1748](https://github.com/Cratis/Chronicle/issues/1748) | It should be possible to run replays without destroying other jobs | No design for how replay jobs co-exist with other job types; depends on #1779. |
| [#1764](https://github.com/Cratis/Chronicle/issues/1764) | Support observer replay cancellation | No design for how cancellation is triggered and how partial-replay state is cleaned up. |

### Jobs

| # | Issue | Why more details are needed |
|---|-------|-----------------------------|
| [#1304](https://github.com/Cratis/Chronicle/issues/1304) | Jobs should have a priority | No priority scheme design or scheduler changes specified. |
| [#1753](https://github.com/Cratis/Chronicle/issues/1753) | More efficient `JobStorage` queries | Needs profiling data to identify which queries are slow. |
| [#1786](https://github.com/Cratis/Chronicle/issues/1786) | Preparing job steps and starting them should run asynchronously | The design impact on job state transitions and error handling needs clarification. |
| [#1789](https://github.com/Cratis/Chronicle/issues/1789) | Optimize Job system | Too vague — no specific bottleneck identified. |
| [#1308](https://github.com/Cratis/Chronicle/issues/1308) | Add a UniqueConstraint indexer job for reindexing constraints on change | No spec for when reindexing triggers or what "reindexing" involves. |

### Projections

| # | Issue | Why more details are needed |
|---|-------|-----------------------------|
| [#195](https://github.com/Cratis/Chronicle/issues/195) | Support discriminated types for children in projections | No API design for how discriminated types are expressed or matched. |
| [#548](https://github.com/Cratis/Chronicle/issues/548) | Declare projections for all event types | No design for the fallback/catch-all handler semantics. Overlaps with #2464. |
| [#551](https://github.com/Cratis/Chronicle/issues/551) | Composite keys for identifying parents in child relationships | No API design for how composite parent keys are specified. |
| [#568](https://github.com/Cratis/Chronicle/issues/568) | `ArrayIndexers` limited to unique property names | Needs investigation to understand the constraint and how to lift it safely. |
| [#570](https://github.com/Cratis/Chronicle/issues/570) | Improve how we reach the target type for properties and array indexers | Needs investigation into where the current logic fails. |
| [#575](https://github.com/Cratis/Chronicle/issues/575) | ImmediateProjections with relationships need to support non-root EventSourceId | Needs investigation and a design for alternate key routing in immediate projections. |
| [#592](https://github.com/Cratis/Chronicle/issues/592) | Support adding children by mapping event properties without a relationship | No spec for how this is different from existing child mapping. |
| [#599](https://github.com/Cratis/Chronicle/issues/599) | Optimize replay by minimizing events needed | Needs profiling and a checkpointing / snapshot design. |
| [#842](https://github.com/Cratis/Chronicle/issues/842) | Do projection replay in-memory then persist at the end | Large design change with complex failure recovery semantics. |
| [#890](https://github.com/Cratis/Chronicle/issues/890) | Projections could change read models without replaying | Thought experiment — no concrete design to implement. |
| [#917](https://github.com/Cratis/Chronicle/issues/917) | Refactor projections to be a specialized type of reducer | Large architectural refactoring with deep implications. |
| [#1051](https://github.com/Cratis/Chronicle/issues/1051) | Improve `.To()` / `.ToValue()` — consolidate and support complex types | No spec for which complex types should be supported or how. |
| [#1058](https://github.com/Cratis/Chronicle/issues/1058) | Make projection definitions versionable | No spec for versioning semantics, migration, and backward compatibility. |
| [#1353](https://github.com/Cratis/Chronicle/issues/1353) | Support joining other event sequences in projections | No design for cross-sequence join semantics. |
| [#1370](https://github.com/Cratis/Chronicle/issues/1370) | Support creating mementos of a stream | No design for what a memento contains or how it's stored. |
| [#1416](https://github.com/Cratis/Chronicle/issues/1416) | Improve projection comparer for safe-to-ignore scenarios | No spec for which changes are safe vs not. |
| [#1533](https://github.com/Cratis/Chronicle/issues/1533) | Move projection building blocks away from extension methods | Large refactoring with no clear design for the replacement API. |
| [#1896](https://github.com/Cratis/Chronicle/issues/1896) | Watch Projection changeset for Children RemoveWithJoin issue | Needs a reproduction case. |
| [#2464](https://github.com/Cratis/Chronicle/issues/2464) | Add `$all` expression and `FromAll()` / `[FromAll]` in projections | Large feature. Mentions late-bound properties which need a design spike. |
| [#2735](https://github.com/Cratis/Chronicle/issues/2735) | Investigate projection functions (count, increment, decrement) | Investigation required before any implementation. |

### Constraints

| # | Issue | Why more details are needed |
|---|-------|-----------------------------|
| [#1449](https://github.com/Cratis/Chronicle/issues/1449) | Add support for constraints scoped to a stream | No design for how stream scoping is expressed or enforced. |
| [#1615](https://github.com/Cratis/Chronicle/issues/1615) | Constraints should be registered in the event store | Needs a design for constraint persistence, discovery, and lifecycle. |
| [#1630](https://github.com/Cratis/Chronicle/issues/1630) | Support defining the key property on a model | Design needed for how this interacts with projections and reducers. |

### Events / Event Sequences

| # | Issue | Why more details are needed |
|---|-------|-----------------------------|
| [#249](https://github.com/Cratis/Chronicle/issues/249) | Allow configuring observers to replay but not append new events on replay | No API design for this opt-in behavior. |
| [#287](https://github.com/Cratis/Chronicle/issues/287) / [#585](https://github.com/Cratis/Chronicle/issues/585) | Validate events against schema when appending | Needs a design for how validation failures are surfaced and what the performance impact is. |
| [#351](https://github.com/Cratis/Chronicle/issues/351) | Observer filter support | No spec for the filter expression language or how it's declared. |
| [#372](https://github.com/Cratis/Chronicle/issues/372) | Support event sequence branching | Complex distributed systems feature with no design spec. |
| [#455](https://github.com/Cratis/Chronicle/issues/455) | Models need a way to define what is the key property | Overlaps with multiple other key-related issues; no concrete API design. |
| [#494](https://github.com/Cratis/Chronicle/issues/494) | Retry policy for failed partitions should be configurable per observer | No spec for what configuration options are available. |
| [#510](https://github.com/Cratis/Chronicle/issues/510) | Use other properties as identifiers for compliance | No design for how alternate compliance identifiers are declared. |
| [#587](https://github.com/Cratis/Chronicle/issues/587) | Next sequence number should show tail number when at end of observed events | Needs investigation to understand the current behavior and desired change. |
| [#664](https://github.com/Cratis/Chronicle/issues/664) | Failed partitions need to honor the actual partition (key) | Needs investigation — unclear which partition mapping is wrong. |
| [#663](https://github.com/Cratis/Chronicle/issues/663) | Observers should be able to define partitions per event type | No API design for per-event-type partition expressions. |
| [#709](https://github.com/Cratis/Chronicle/issues/709) | EventSequence grain activation should favor actual tail sequence number | Needs investigation into how activation currently initializes tail. |
| [#925](https://github.com/Cratis/Chronicle/issues/925) | Add retention policies on event sequences | No spec for retention policy types, enforcement, and what happens to observing consumers. |
| [#927](https://github.com/Cratis/Chronicle/issues/927) | Support EventSequence as a sink type | No design for how the EventSequence sink differs from the existing MongoDB sink. |
| [#932](https://github.com/Cratis/Chronicle/issues/932) | Protect system collections for custom event sequences | No definition of "protect" or what the access model should be. |
| [#1002](https://github.com/Cratis/Chronicle/issues/1002) | Support scheduling Replay at a specific time | No spec for scheduling semantics or UI. |
| [#1012](https://github.com/Cratis/Chronicle/issues/1012) | Import compensations through workbench | No design for the compensation import format or workflow. |
| [#1011](https://github.com/Cratis/Chronicle/issues/1011) | Import events in a specific format from workbench | No spec for the import format. |
| [#1057](https://github.com/Cratis/Chronicle/issues/1057) | Associate custom metadata with events | Needs API design for metadata declaration, storage, and querying. |
| [#1364](https://github.com/Cratis/Chronicle/issues/1364) | Move tombstoned partitions into "cold" storage | No design for the cold storage backend or access API. |
| [#1363](https://github.com/Cratis/Chronicle/issues/1363) | Optimize replays to skip tombstoned partitions | Needs investigation into where partition tombstoning is tracked during replay. |
| [#1446](https://github.com/Cratis/Chronicle/issues/1446) | Support completing a stream | No spec for what "completing" a stream means semantically. |
| [#1448](https://github.com/Cratis/Chronicle/issues/1448) | Support "closing the books" style book keeping | Domain-specific feature with no concrete API design. |
| [#1820](https://github.com/Cratis/Chronicle/issues/1820) | Attempted sequence number of failed partition seems wrong | Needs reproduction steps. |
| [#1845](https://github.com/Cratis/Chronicle/issues/1845) | Add a way to wait for all observers affected by an append | Complex distributed synchronization — no design for how to identify affected observers and await them. |
| [#1856](https://github.com/Cratis/Chronicle/issues/1856) | Bulk handling for Reducers | No API design for how bulk events are received and processed. |
| [#1857](https://github.com/Cratis/Chronicle/issues/1857) | Bulk handling for Reactors | Same as #1856. |
| [#2420](https://github.com/Cratis/Chronicle/issues/2420) | Add support for tombstoning events | Tombstone semantics as a constraint on the event source need a design before coding. |


### Compliance / Security

| # | Issue | Why more details are needed |
|---|-------|-----------------------------|
| [#227](https://github.com/Cratis/Chronicle/issues/227) | Add support for revisioned encryption keys | No spec for how key versions are associated with events or how decryption falls back. |
| [#228](https://github.com/Cratis/Chronicle/issues/228) | Add key rotation support | Depends on #227 being designed first. |
| [#686](https://github.com/Cratis/Chronicle/issues/686) | PII Encryption keys need to be linked to a consistent key | No design for how a "consistent key" is defined or stored. |
| [#685](https://github.com/Cratis/Chronicle/issues/685) | Support Azure KeyVault for encryption keys | No design for the Azure SDK integration or the abstract `IEncryptionKeyProvider` extension point. |
| [#1965](https://github.com/Cratis/Chronicle/issues/1965) | Add authorization on the Kernel side | Large feature — needs an auth policy model and integration spec. |

### Performance / Architecture

| # | Issue | Why more details are needed |
|---|-------|-----------------------------|
| [#140](https://github.com/Cratis/Chronicle/issues/140) | Retention policy support for old collections when doing replay | Depends on #925 being designed first. |
| [#485](https://github.com/Cratis/Chronicle/issues/485) | Dictionaries with string keys should be case-insensitive | Needs investigation to identify which dictionaries and what the correct case policy is. |
| [#634](https://github.com/Cratis/Chronicle/issues/634) | Failed partitions of an observer should be their own Grain | Large architectural change — needs a migration plan. |
| [#723](https://github.com/Cratis/Chronicle/issues/723) | Improve performance by being consistent about the type used to represent event content | Needs profiling to quantify the impact before committing to a representation. |
| [#736](https://github.com/Cratis/Chronicle/issues/736) | Compile-time checks for events and produce warnings for unobserved events | Needs a Roslyn analyzer design. |
| [#814](https://github.com/Cratis/Chronicle/issues/814) | Move general logic out of MongoDB providers to a higher level | Large refactoring with no concrete design. |
| [#817](https://github.com/Cratis/Chronicle/issues/817) | Bubble up errors from MongoDB Async Cursor | Needs investigation into which errors are swallowed and how they should surface. |
| [#872](https://github.com/Cratis/Chronicle/issues/872) | Turn important operations into events and have reactions handle them | Large architectural change — no spec for which operations and how reactions are structured. |
| [#896](https://github.com/Cratis/Chronicle/issues/896) | Add tracing for operations in Client + Kernel | High-level; already partially started (#1527). Needs a scope spec. |
| [#897](https://github.com/Cratis/Chronicle/issues/897) | Add observability to projections (and potentially observers) | No specific metrics/traces defined. |
| [#898](https://github.com/Cratis/Chronicle/issues/898) | Move away from JSON serialization for grain communication | Large architectural change — no alternative serialization spec. |
| [#900](https://github.com/Cratis/Chronicle/issues/900) | Improve AOT compilation for publishing | Needs investigation of which reflection-based paths block AOT. |
| [#911](https://github.com/Cratis/Chronicle/issues/911) | Clean up and simplify storage provider and MongoDB implementation | Too broad — no specific items identified. |
| [#918](https://github.com/Cratis/Chronicle/issues/918) | Switch to immutable collection types as contract | Large refactoring — no scope on which collections and what the migration looks like. |
| [#976](https://github.com/Cratis/Chronicle/issues/976) | Switch to using JSON internally throughout the Kernel | Large architectural change with significant unknown side effects. |
| [#984](https://github.com/Cratis/Chronicle/issues/984) | Optimize event type registrations on server | Needs profiling to identify the bottleneck. |
| [#1018](https://github.com/Cratis/Chronicle/issues/1018) | MongoDB write and read concerns for improved performance | Needs benchmarks to justify concern level changes. |
| [#1019](https://github.com/Cratis/Chronicle/issues/1019) | Make cluster info part of MongoDB by default | Needs investigation into current configuration and the clustering story. |
| [#1056](https://github.com/Cratis/Chronicle/issues/1056) | Orleans serializer codec for JSON event types | No spec for how the codec interacts with existing serialization paths. |
| [#1096](https://github.com/Cratis/Chronicle/issues/1096) | Optimize Docker image sizes | Needs profiling of current image layers. |
| [#1200](https://github.com/Cratis/Chronicle/issues/1200) | Bring back Benchmark project | No info on what benchmarks to include or how they're run in CI. |
| [#1202](https://github.com/Cratis/Chronicle/issues/1202) | Move away from `ExpandoObject` | Large architectural change — needs a replacement type and migration plan. |
| [#1206](https://github.com/Cratis/Chronicle/issues/1206) | Split the repository | Major organizational change — no concrete split plan. |
| [#1248](https://github.com/Cratis/Chronicle/issues/1248) | Optimize Projections storage provider with a local cache | No spec for cache invalidation or consistency guarantees. |
| [#1250](https://github.com/Cratis/Chronicle/issues/1250) | Move contract/chronicle converters from Shared into Services project | Large refactoring — no concrete move plan. |
| [#1251](https://github.com/Cratis/Chronicle/issues/1251) | Move away from custom `DefaultServiceProvider` | No design for the replacement. |
| [#1253](https://github.com/Cratis/Chronicle/issues/1253) | Move Grain Key types into Grains.Interfaces | Needs the repository to agree on final package structure. |
| [#1255](https://github.com/Cratis/Chronicle/issues/1255) | Improve recommendation reasons and cohesion | No concrete design for the improved model. |
| [#1256](https://github.com/Cratis/Chronicle/issues/1256) | Separate Reducer server parts into engine-specific and Orleans-specific | Large refactoring — needs an interface boundary design. |
| [#1260](https://github.com/Cratis/Chronicle/issues/1260) | Automatically hook up existing reducers when a namespace is added | Needs design for how the namespace-added event triggers reducer reconnection. |
| [#1261](https://github.com/Cratis/Chronicle/issues/1261) | Investigate whether we need notification when reducer definitions change | Investigation required before implementation. |
| [#1265](https://github.com/Cratis/Chronicle/issues/1265) | Formalize `ConnectedClient` as its own grain | Architectural change — no design for how `Reactions` and `Reducers` use it. |
| [#1272](https://github.com/Cratis/Chronicle/issues/1272) | Orleans client `SiloBuilder` should not rely on type discovery | Complex Orleans internal change — no replacement design. |
| [#1298](https://github.com/Cratis/Chronicle/issues/1298) | Move away from `Globals.JsonSerializerOptions` from Fundamentals | No design for how options are passed or scoped. |
| [#1299](https://github.com/Cratis/Chronicle/issues/1299) | Adhere to Microsoft library guidance | Too broad — no specific guidelines referenced. |
| [#1306](https://github.com/Cratis/Chronicle/issues/1306) | Clean up storage abstractions | Too broad — no specific refactoring described. |
| [#1307](https://github.com/Cratis/Chronicle/issues/1307) | Fix Storage configuration | No description of what is broken. |
| [#1322](https://github.com/Cratis/Chronicle/issues/1322) | Implement `ImmutableList<T>` Orleans serializer codec | Needs investigation into whether Orleans 8 already handles this. |
| [#1345](https://github.com/Cratis/Chronicle/issues/1345) | Investigate if the way we set invariant culture is the best | Investigation required. |
| [#1382](https://github.com/Cratis/Chronicle/issues/1382) | Optimize `EventSerializer` | Needs profiling to identify hot paths. |
| [#1387](https://github.com/Cratis/Chronicle/issues/1387) | Look into how we represent a Key — do we need to improve this | Investigation required. |
| [#1417](https://github.com/Cratis/Chronicle/issues/1417) | Magic `TimeSpan.From***` values should be configurable | No spec for which constants to expose or what the configuration model looks like. |
| [#1451](https://github.com/Cratis/Chronicle/issues/1451) | Refactor EventSequence grain | No concrete refactoring plan. |
| [#1459](https://github.com/Cratis/Chronicle/issues/1459) | Look into switching to .NET 9 `JsonSchemaExporter` | Investigation required to understand migration path. |
| [#1520](https://github.com/Cratis/Chronicle/issues/1520) | Configurable auto-solving of recommendations | No spec for how rules are defined or managed. |
| [#1522](https://github.com/Cratis/Chronicle/issues/1522) | Add details on recommendations | No spec for what "details" means or how they're captured. |
| [#1749](https://github.com/Cratis/Chronicle/issues/1749) | Sink last-handled event sequence number is wrong | Needs reproduction steps and investigation. |
| [#1751](https://github.com/Cratis/Chronicle/issues/1751) | "Rehydration" of reactors & reducers to fix wrong state on startup | No design for how state is detected as wrong or what rehydration involves. |
| [#1808](https://github.com/Cratis/Chronicle/issues/1808) | Improve performance of integration specs | Too vague — no specific slow specs identified. |
| [#1810](https://github.com/Cratis/Chronicle/issues/1810) | Improve setup / teardown | Too vague. |
| [#1821](https://github.com/Cratis/Chronicle/issues/1821) | Build namespaces for integration specs dynamically based on folder structure | No design for the folder-to-namespace mapping strategy. |
| [#1863](https://github.com/Cratis/Chronicle/issues/1863) | Support clustering for Kernel (multi-instance Orleans) | Needs verification of MongoDB clustering config and an automated multi-instance test environment. |
| [#1898](https://github.com/Cratis/Chronicle/issues/1898) | Simplify and merge projects | Large restructuring — needs a concrete project merge plan. |
| [#1899](https://github.com/Cratis/Chronicle/issues/1899) | Reactor/Reducer/Projection-Handler should be split apart | Large refactoring — needs a boundary design. |
| [#1906](https://github.com/Cratis/Chronicle/issues/1906) | Improve way integration specs are set up | No concrete alternative design proposed. |
| [#1910](https://github.com/Cratis/Chronicle/issues/1910) | Multiple sinks for projections and reducers | Needs a design for how multiple sinks are declared and dispatched to. |
| [#1967](https://github.com/Cratis/Chronicle/issues/1967) | Improve serialization when appending events in MongoDB storage | Needs profiling data to motivate the change. |
| [#1980](https://github.com/Cratis/Chronicle/issues/1980) | Investigate better testing approach for event sourcing | Research / thought experiment — needs an ADR before implementation. |
| [#2148](https://github.com/Cratis/Chronicle/issues/2148) | Integration spec project matrix (different configs, casing, DB backends) | Depends on #2268 (Copilot PR in progress). |
| [#2437](https://github.com/Cratis/Chronicle/issues/2437) | Generate TypeScript gRPC package | Multi-step toolchain work (proto generation + TypeScript generation + publishing). Needs toolchain decisions. |
| [#2464](https://github.com/Cratis/Chronicle/issues/2464) | `$all` expression / `FromAll()` in projections and dictionaries | See Projections section above. |
| [#2586](https://github.com/Cratis/Chronicle/issues/2586) | Handle Keys properly with metadata and make `ExpandoObjectConverter` honor this | No body text. |

| [#2642](https://github.com/Cratis/Chronicle/issues/2642) | Implement compensation support (full round-trip) | The kernel grain has `Compensate()` but the gRPC service and client don't. Full design for the client-facing API and integration specs is needed. |

### Events / Event Sequences (continued)

| # | Issue | Why more details are needed |
|---|-------|-----------------------------|
| [#369](https://github.com/Cratis/Chronicle/issues/369) | Kernel needs to validate all input given through its API | Broad scope — needs a prioritized list of validation rules and how violations are surfaced. |
| [#1435](https://github.com/Cratis/Chronicle/issues/1435) | Reducer methods without `EventContext` are not discovered | Needs a code fix, but requires understanding of the `IsNullableReferenceType()` logic in Fundamentals and its interaction with the method discovery. Could also be a "can do" — pending further investigation. |
| [#1859](https://github.com/Cratis/Chronicle/issues/1859) | Support migration of events between generations (up and down casting) | Complex feature — introduces `IEventTypeMigratorFor<>` and declarative up/down cast definitions. Needs full API and storage design. |
| [#2741](https://github.com/Cratis/Chronicle/issues/2741) | Race condition with seeded events / events produced while kernel is starting | Needs reproduction through integration specs. The fix depends on the root cause found during investigation. |

### Workbench / UI (continued)

| # | Issue | Why more details are needed |
|---|-------|-----------------------------|
| [#735](https://github.com/Cratis/Chronicle/issues/735) | Workbench: track from a failed partition back to the causative event and perform compensation | Complex UX flow that depends on causation tracking, compensation support (#2642), and navigation design. |
| [#867](https://github.com/Cratis/Chronicle/issues/867) | Add automatic benchmark tests for memory and performance | Needs benchmark suite design, CI integration approach, and threshold decisions. |

### Documentation / DevEx

| # | Issue | Why more details are needed |
|---|-------|-----------------------------|
| [#257](https://github.com/Cratis/Chronicle/issues/257) | Initial Kernel development guidance | No outline of what the guide should cover. |
| [#352](https://github.com/Cratis/Chronicle/issues/352) | Documenting and providing specifications for events | No spec for what the documentation format should be. |
| [#399](https://github.com/Cratis/Chronicle/issues/399) | Test coverage reporting for build pipeline(s) | Needs CI/CD decisions (which coverage tool, thresholds, reporting format). |
| [#439](https://github.com/Cratis/Chronicle/issues/439) | Add branch protection rules | Requires GitHub admin access to configure branch protection. Cannot be done via code change. |
| [#661](https://github.com/Cratis/Chronicle/issues/661) | Automatically publish changelog to discussions | Needs CI/CD pipeline decisions and GitHub API token setup. |
| [#915](https://github.com/Cratis/Chronicle/issues/915) | Create learning paths in documentation | No outline of paths or what audiences they target. |
| [#950](https://github.com/Cratis/Chronicle/issues/950) | Documentation for reducers | No outline of what the docs should cover. |
| [#971](https://github.com/Cratis/Chronicle/issues/971) | Move identity store to a per-silo service with distributed state | Architecture design needed. |
| [#977](https://github.com/Cratis/Chronicle/issues/977) | Support for linking events | No design for what "linking" means or how links are stored/queried. |
| [#1017](https://github.com/Cratis/Chronicle/issues/1017) | Create guidance on setting up Docker Compose | No outline of the guide. |
| [#1277](https://github.com/Cratis/Chronicle/issues/1277) | Add overview on Docker Hub for Chronicle images | Needs content decisions. |



