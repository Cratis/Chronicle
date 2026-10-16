<!-- cratis-ai-managed: skills/cratis-chronicle-client-kotlin/references/discovery-and-registration.md -->
## Discovery and registration

Source: `Chronicle.Kotlin`: `ClientArtifacts`,
`ArtifactRegistrations.registerAll` and `EventStore`.

`ClientArtifacts` scans the classpath with ClassGraph
(`artifacts/ClientArtifacts.kt`), and `ClientArtifacts.default` is a
process-wide lazy singleton. What it looks for:

| Kind | Rule |
| --- | --- |
| event types | `@EventType` |
| event-source definitions | `@EventSource` |
| event type migrations | implements `IEventTypeMigration` |
| read models | `@ReadModel` |
| declarative projections | implements `IProjectionFor` |
| model-bound projections | `@FromEvent` **and** the synthetic `FromEvent$Container` |
| reactors | `@Reactor` |
| reducers | `@Reducer` |
| constraints | implements `IConstraint` |
| seeders | implements `ICanSeedEvents` |
| webhooks | implements `IWebhookDefiner` |
| captures | implements `ICapture` |
| reactor middlewares | implements `IReactorMiddleware` or `BlockingReactorMiddleware` |
| reactor argument resolvers | implements `IReactorMethodArgumentResolver` or `BlockingReactorMethodArgumentResolver` |

> The `FromEvent$Container` entry is not incidental: Kotlin's `@Repeatable`
> replaces repeated annotations with a synthetic container, so a class carrying
> more than one `@FromEvent` is **not** annotated with `@FromEvent` at runtime.
> A scan that looks only for the annotation silently misses every multi-event
> projection.

Registration order is fixed and matters
(`ArtifactRegistrations.register`): event types and migrations →
event-source definitions → unowned read models → constraints → model-bound constraints → projections →
webhooks → reactors → reducers → captures → seeders. Reactors and reducers are
started once successfully; failed observers are retried on the next pass.
An automatic pass logs each failure to stderr and continues reconnecting.
Explicit `registerAll()` starts the remaining observers then throws
`ArtifactRegistrationFailed` listing failures. A kernel-rejected event-type
registration throws `ChronicleCommandRejected`.

**Registration re-runs on every reconnect.** `EventStore.kt` launches a
coroutine on `Dispatchers.IO` collecting the connection lifecycle and
re-registers each time. The connection id rotates on every disconnect because
the kernel keys observer subscriptions by it
(`connection/ConnectionLifecycle.kt`), so observers must re-register — and
they do.

`store.awaitRegistration()` waits for the first attempted pass; its gate
completes in `finally` even on failure. It throws `ChronicleConnectionFailed`
when the kernel rejects the connection, as do gated appends.
`store.registerAll()` runs it by hand when
`autoDiscoverAndRegister = false`.
