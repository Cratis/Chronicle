<!-- cratis-ai-managed: skills/cratis-chronicle-client-kotlin/references/spring-boot.md -->
## Spring Boot

Source: `Cratis/Chronicle.Kotlin`: `ChronicleProperties`,
`ChronicleAutoConfiguration`, `ChronicleStartup` and `ChronicleWebAutoConfiguration`.

The starter registers exactly two auto-configurations —
`Integrations/SpringBoot/src/main/resources/META-INF/spring/org.springframework.boot.autoconfigure.AutoConfiguration.imports`:

- `io.cratis.chronicle.spring.ChronicleAutoConfiguration`
- `io.cratis.chronicle.spring.ChronicleWebAutoConfiguration`

Configuration binds under the prefix `cratis.chronicle` —
`ChronicleProperties.kt`. The minimum real configuration is one key, as in
the shipped sample (`Samples/Kotlin/SpringBoot/src/main/resources/application.yml`):

```yaml
cratis:
  chronicle:
    event-store: <EventStoreName>
```

Everything else has a default (`ChronicleProperties.kt`):

| Key under `cratis.chronicle` | Default |
| --- | --- |
| `connection-string` | the development connection string |
| `event-store` | `"Default"` |
| `namespace` | `"Default"` |
| `auto-discover-and-register` | `true` |
| `artifact-packages` | empty — falls back to Spring's auto-configuration packages |
| `default-sink-type-id` | `null` |
| `program-identifier` | `null` — falls back to `spring.application.name` |
| `registration-timeout` | `PT30S` |
| `namespace-resolution.strategy` | `FIXED` |
| `namespace-resolution.http-header` | `"x-cratis-tenant-id"` |
| `namespace-resolution.claim` | `"tenant_id"` |
| `unit-of-work.enabled` | `true` | `ChronicleProperties.UnitOfWork` |
| `causation.enabled` | `true` | `ChronicleProperties.Causation` |
| `identity.enabled` | `true` | `ChronicleProperties.Identity` |

The starter selects a unique `ReadModelNamingPolicy` bean. With none or an
ambiguous set, it retains identifier-based naming. A rejected connection at
startup propagates `ChronicleConnectionFailed` and fails the application;
an unreachable kernel only logs after `registration-timeout` and keeps retrying.
Requires Chronicle.Kotlin 6.5.0 for the starter to run on the kotlinx-coroutines version Spring Boot manages; remove a
`kotlin-coroutines.version` override added for an older client.

`NamespaceResolution.Strategy` is `FIXED`, `HTTP_HEADER`, `SUBDOMAIN`,
`AUTHENTICATION` (`ChronicleProperties.kt`).

The beans you inject are `IChronicleClient`, `IEventStore`, and the convenience
facade `Chronicle` (`ChronicleAutoConfiguration.kt`). `Chronicle` wraps
the suspending API in blocking calls and takes `Class<T>` rather than
`KClass<T>`, so Java can use it unchanged —
`Integrations/SpringBoot/src/main/kotlin/io/cratis/chronicle/spring/Chronicle.kt`,
`append` and `appendMany`.

`ChronicleWebAutoConfiguration` is **servlet-only**
(`@ConditionalOnWebApplication(SERVLET)`, `ChronicleWebAutoConfiguration.kt`).
A WebFlux application gets no per-request namespace, identity, causation, or
unit-of-work filter.

**Artifacts are Spring beans.** `SpringArtifactActivator` resolves a discovered
artifact from the container when it is uniquely defined and otherwise builds it
through `autowireCapableBeanFactory.createBean`
(`SpringArtifactActivator.kt`), so a reactor takes constructor
dependencies exactly like a `@Service` would.
