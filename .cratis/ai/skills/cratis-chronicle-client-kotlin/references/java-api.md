<!-- cratis-ai-managed: skills/cratis-chronicle-client-kotlin/references/java-api.md -->
## Java

Java is a first-class target here, not an afterthought: there is a compile-only
Java conformance suite under `Source/src/test/java` whose whole point is stated
in `conformance/JavaConformance.java:71-74` — *"It is never run — compiling it is
the assertion"*.

**Start Java code at `BlockingChronicleClient`**, not at the raw `ChronicleClient`
plus static bridges. Verbatim from the compile-checked fixture
(`Source/src/test/java/io/cratis/chronicle/java/JavaClientFlowUsage.java:29-34`):

```java
var client = BlockingChronicleClient.connect(ChronicleOptions.development());
var eventStore = client.getEventStore("<EventStoreName>");

eventStore.getEventLog().append("<event-source-id>", new <EventName>("<value>"));
```

`BlockingChronicleClient` is `AutoCloseable`, so `try (var client = ...)` works
(`java/BlockingChronicleClient.kt:35`, `connect` at `:74-76`). The blocking
surface continues through `BlockingEventStore`, `BlockingEventSequence`,
`BlockingReadModels`, `BlockingReactors`, `BlockingReducers`,
`BlockingUnitOfWork`, and `AppendOptionsBuilder`.

> The repository's own README shows the **older** low-level route —
> `new ChronicleClient(...)` plus `EventStoreJavaBridge` / `EventLogJavaBridge`
> (`README.md:225-244`). Both APIs are real, but the reference documentation and
> the compile-checked fixture both start at `BlockingChronicleClient`. Write new
> Java against that; reach for the `*JavaBridge` statics only for a corner the
> blocking client does not wrap.

In Spring Boot, Java injects the `Chronicle` bean instead — it already takes
`Class<T>` and returns plain values.
