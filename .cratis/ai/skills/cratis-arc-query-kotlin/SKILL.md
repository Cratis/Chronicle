---
name: cratis-arc-query-kotlin
description: Add a query to a Cratis Arc @ReadModel in Kotlin or Java — companion-object or static query methods, GET vs RFC QUERY, and observable queries returning Kotlin Flow, JDK Flow.Publisher, or RxJava 3 Observable over SSE and WebSocket. Use when exposing read-model data from a Kotlin or Java Arc application. Do not use for the .NET Arc query shape, for command definition, or for how events populate the read model (Chronicle projections/reducers).
license: MIT
---
<!-- cratis-ai-managed: skills/cratis-arc-query-kotlin/SKILL.md -->

# Add a query to an Arc read model in Kotlin or Java

A `@ReadModel` is a plain Kotlin or Java type whose companion-object (Kotlin) or
static (Java) methods are queries. KSP generates a reflection-free
`QueryPerformer` per method; there is no controller to write.

## Verified product sources

Verified against `Arc.Kotlin` at tag **`v7.3.0`**, re-verified at commit
`d0aa6a4` (14 commits past the tag), documented consumer setup: JDK 17,
Kotlin 2.4.20, KSP 2.3.12, Spring Boot 4.1.x. `io.cratis:arc` supplies
`io.cratis.arc.artifacts.ReadModel`, `io.cratis.arc.queries.Path`,
`@QueryHttpMethod`, `@QueryTransport`, `ObservableState<T>`. The optional
`arc-rxjava3` artifact adds RxJava 3 `Observable` support at runtime. The
re-verification added `ObservableState<T>` (new since the tag) and the
`observable-queries.allowed-origins` WebSocket setting — both covered below.

## Declare a one-shot query

```kotlin
@ReadModel
@AllowAnonymous
data class TaskView(val id: String, val title: String) {
    companion object {
        @JvmStatic
        @Path("/api/tasks/by-id")
        suspend fun byId(id: String, @FromServices repository: TaskRepository): TaskView? =
            repository.byId(id)

        @JvmStatic
        @Path("/api/tasks")
        fun all(@FromServices repository: TaskRepository): List<TaskView> = repository.all()
    }
}
```

```java
@ReadModel
@AllowAnonymous
public record TaskView(String id, String title) {
    @Path("/api/tasks")
    public static List<TaskView> all(@FromServices TaskRepository repository) {
        return repository.all();
    }
}
```

Rules the framework enforces:

- Query methods live on the read model's Kotlin **companion object** or as
  Java **static** methods. A custom instance method on a `@ReadModel` fails
  compilation.
- Mark dependency parameters `@FromServices`; caller-supplied arguments stay
  unannotated. A query may also declare exact non-null `QueryRequest` and
  `QueryContext` parameters in any position — KSP resolves service, request,
  and context parameters from the execution context while preserving
  declaration order.
- Java query methods return a value or `CompletionStage<T>`; generated
  performers await it inside Arc's request coroutine context.
- Kotlin caller parameters may declare defaults. GET/QUERY/observable
  subscriptions preserve *omission* (leaving the argument out of
  `QueryRequest.arguments`) so the generated performer runs the Kotlin default
  expression; an explicit JSON/subscription `null` is a *supplied* value, not
  omission, and is accepted only by a nullable parameter. Java query
  parameters have no default feature, and overloaded query methods are
  unsupported in either language.
- Only public, non-generic, non-overloaded methods whose return type contains
  the enclosing read-model shape are treated as queries; adding `@Path`,
  `@QueryHttpMethod`, or `@QueryTransport` explicitly marks a method as a
  query, so an invalid annotated return still fails compilation.

## Choose a route and method

Without `@Path`, Arc derives a route from `cratis.arc.endpoints` settings
(prefix, skipped package segments, kebab case, artifact name). `@Path`
preserves what you write verbatim.

| Transport | When | Shape |
| --- | --- | --- |
| `GET` | scalar arguments | `?id=123` |
| `QUERY` (RFC 9110) | structured arguments, paging, sorting; enabled by `enable-query-http-method` | JSON body `{arguments, paging, sorting}` |

```bash
curl -sS 'http://localhost:8080/api/tasks/by-id?id=123'

curl -sS -X QUERY http://localhost:8080/api/tasks \
  -H 'Content-Type: application/json' \
  -d '{"arguments":{},"paging":{"page":0,"pageSize":25},"sorting":{"field":"title","direction":"ascending"}}'
```

GET reserves `page`, `pageSize`, `sortBy`, `sortDirection`. Client argument
names match case-insensitively for GET and QUERY. QUERY responses always carry
`Cache-Control: no-store`. Prefer GET for a query with `DateOnly`/`TimeOnly`
parameters — the pinned `@cratis/arc` client's QUERY body serializes those
component objects with native `JSON.stringify` rather than their typed
serializer, which GET does not go through.

`@QueryHttpMethod(GET|QUERY|AUTO)` sets the generated proxy's preference. Put
it on the `@ReadModel` class for a default, and override per method.

## Declare an observable query

Return one of three streaming families from a companion/static method; KSP
generates an `OBSERVABLE` performer automatically — no `@QueryTransport`
needed for these:

- Kotlin `kotlinx.coroutines.flow.Flow<T>` / `Flow<List<T>>`
- JDK `java.util.concurrent.Flow.Publisher<T>` / `Publisher<List<T>>`
- RxJava 3 `Observable<T>` / `ObservableSource<T>` / `Subject<T>` — requires
  the optional `arc-rxjava3` artifact at runtime

A one-shot return (`T`, `List<T>`, `Page<T>`) stays request-response.

```kotlin
@ReadModel
@AllowAnonymous
data class TaskView(val id: String, val title: String) {
    companion object {
        @JvmStatic
        @Path("/api/tasks/watch")
        fun watch(@FromServices repository: TaskRepository): Flow<List<TaskView>> =
            repository.watchAll()
    }
}
```

### Give a Java query a current value to answer GET with

A plain `SubmissionPublisher` emits but holds nothing — nobody subscribing
learns the current value until the next change, and a snapshot `GET` against
it comes back `202 Not Ready` forever if nothing has happened yet. Kotlin has
`MutableStateFlow` for this; the JDK has no equivalent, so Arc supplies
`ObservableState<T>`:

```java
@Component
public final class TaskSource {
    private final ObservableState<List<TaskView>> tasks = new ObservableState<>(List.of());

    public Flow.Publisher<List<TaskView>> observe() {
        return tasks;
    }

    public void publish(List<TaskView> updated) {
        tasks.set(updated);
    }
}
```

`ObservableState<T>` is a `Flow.Publisher<T>`, so a query method returns it
directly. Every subscriber sees the current value first and then each change;
a subscriber that falls behind sees only the newest value, not a backlog —
the right behavior for state rather than a stream of discrete events. Reach
for it whenever a **Java** observable query should also answer a plain `GET`.
Kotlin code has no reason to — return `MutableStateFlow` as a `Flow` instead.

### Consuming it

The same generated route serves three direct transports:

| Transport | Selected by | Behavior |
| --- | --- | --- |
| HTTP GET/QUERY | ordinary request | `200` with the current value when the source is a `StateFlow` (already holds one); a cold `Flow`/`Publisher` returns `202` (not ready) unless `waitForFirstResult=true`, optionally bounded by `waitForFirstResultTimeout=<seconds>` |
| SSE | `Accept: text/event-stream` | each result as `data: {QueryResult}\n\n` |
| WebSocket | upgrading the query route | `{"type":"Data","data":{QueryResult}}` frames, plus `Ping`/`Pong` |

Multiple subscriptions can multiplex over one physical connection through
`/.cratis/queries/ws`, or `/.cratis/queries/sse` with POST
`/.cratis/queries/sse/subscribe` / `/unsubscribe`. Every subscription still
passes through its own query's authorization — a protected query terminates
`Unauthorized` on that subscription without disturbing others sharing the
connection.

Set `transferMode=delta` to get the full first snapshot followed by change
sets only; `transferMode=full` sends every result as a full snapshot with no
change set. Omitting it keeps the legacy behavior of always sending both. With
`delta`, Arc prefers a stable item identity; without one it falls back to
serialized-JSON identity and reports only additions/removals (a changed field
is a removal plus an addition, never a replacement) — change sets never encode
reordering.

## Configure the WebSocket origin for a separate dev server

Spring answers a cross-origin WebSocket handshake with `403`, and a browser
reports that as a socket that silently never opens rather than as a visible
error. Serving the frontend from a dev server on another port (Vite, webpack
dev server) makes every observable-query WebSocket handshake cross-origin.
Set the allowed origin explicitly:

```properties
cratis.arc.observable-queries.allowed-origins=http://localhost:5173
```

Leave it empty in a real deployment — the WebSocket handshake is not subject
to the same-origin policy `fetch` obeys, so this setting is what stops another
site from opening a socket carrying your visitor's cookies.

## Route near misses

- Defining or changing a `@Command`: `cratis-arc-command-kotlin`.
- Adding a query-argument rejection rule: `cratis-arc-validation-kotlin`.
- How events populate the read model in the first place (Chronicle projections
  and reducers): `cratis-chronicle-client-kotlin`, `cratis-chronicle-projection`,
  `cratis-chronicle-reducer`.
- Server-side paging semantics shared with Arc .NET: `cratis-arc-query-paging`.
- The C# Arc query/read-model shape: the vertical-slices and Arc React skills.

## Verify

- Query methods live on the companion object (Kotlin) or as static methods
  (Java), are public, non-generic, and not overloaded.
- Every dependency parameter is `@FromServices`; caller arguments are not.
- `@Path`, if declared, matches the route the frontend expects; otherwise the
  conventional route (prefix, skipped segments, kebab case) is understood.
- An observable query's return type is one of the three recognized streaming
  families, and the RxJava artifact is present when `Observable` is used.
- A `delta`-mode observable query has a stable item identity, not incidental
  reliance on serialized-JSON fallback identity.
- `./gradlew build` is clean with zero warnings and zero errors.
