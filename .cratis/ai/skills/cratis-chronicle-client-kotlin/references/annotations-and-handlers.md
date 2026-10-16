<!-- cratis-ai-managed: skills/cratis-chronicle-client-kotlin/references/annotations-and-handlers.md -->
# Observer annotations and handler shapes

This reference holds the observer annotations, their arguments, filtering annotations and the reducer handler shapes.

### The annotations

| Annotation | Arguments | Source |
| --- | --- | --- |
| `@Reactor` | `id = ""`, `eventSequence = ""` | `observation/Reactor.kt` |
| `@Reducer` | `id = ""`, `eventSequence = ""`, `isActive = true` | `observation/Reducer.kt` |
| `@ReadModel` | `id = ""`, `displayName = ""` | `readModels/ReadModel.kt` |
| `@Projection` | `id = ""`, `eventSequence = ""` — optional | `projections/Projection.kt` |
| `@Constraint` | `id = ""` | `constraints/Constraint.kt` |
| `@Seeder` | none | `seeding/Seeder.kt` |

Handler-level: `@Replay`, `@OnceOnly`. Observer filtering: `@Tag`/`@Tags`,
`@FilterEventsByTag`/`@FilterEventsByTags`, `@EventSequence`,
`@EventSourceType`, `@EventStreamType`.

A reducer's handler may be `(event)`, `(event, state)`, or
`(event, state, context)` — the three shapes accepted by the dispatcher and by
`ReadModelScenario` (`Testing/.../ReadModelScenario.kt`). **Reducers run
client-side**: the kernel streams events and the handler is invoked in your
process (`observation/ReducersService.kt`).

## EventContext

`EventContext` is a data class with `sequenceNumber: Long`, `eventSourceId`,
`eventType`, `occurred`, `correlationId`, `causedBy`, `eventSourceType`,
`eventStreamType`, `eventStreamId`, `eventStore`, `namespace`, `causation`,
`tags`, `hash`, `observationState`, `subject`, `eventSource` —
`events/EventContext.kt`. Recompile when the constructor changes.
