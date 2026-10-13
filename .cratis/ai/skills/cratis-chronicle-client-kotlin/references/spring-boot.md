<!-- cratis-ai-managed: skills/cratis-chronicle-client-kotlin/references/spring-boot.md -->
## Spring Boot

Verified at `Chronicle.Kotlin v6.13.0`: `ChronicleProperties`,
`ChronicleAutoConfiguration`, `ChronicleStartup` and `ChronicleWebAutoConfiguration`.

The starter registers exactly two auto-configurations —
`Integrations/SpringBoot/src/main/resources/META-INF/spring/org.springframework.boot.autoconfigure.AutoConfiguration.imports`:

- `io.cratis.chronicle.spring.ChronicleAutoConfiguration`
- `io.cratis.chronicle.spring.ChronicleWebAutoConfiguration`

Configuration binds under the prefix `cratis.chronicle` —
`ChronicleProperties.kt:42-43`. The minimum real configuration is one key, as in
the shipped sample (`Samples/Kotlin/SpringBoot/src/main/resources/application.yml`):

```yaml
cratis:
  chronicle:
    event-store: <EventStoreName>
```

Everything else has a default (`ChronicleProperties.kt:44-55`):

| Key under `cratis.chronicle` | Default | Line |
| --- | --- | --- |
| `connection-string` | the development connection string | `:44` |
| `event-store` | `"Default"` | `:45` |
| `namespace` | `"Default"` | `:46` |
| `auto-discover-and-register` | `true` | `:47` |
| `artifact-packages` | empty — falls back to Spring's auto-configuration packages | `:48` |
| `default-sink-type-id` | `null` | `:49` |
| `program-identifier` | `null` — falls back to `spring.application.name` | `:50` |
| `registration-timeout` | `PT30S` | `:51` |
| `namespace-resolution.strategy` | `FIXED` | `:65` |
| `namespace-resolution.http-header` | `"x-cratis-tenant-id"` | `:66` |
| `namespace-resolution.claim` | `"tenant_id"` | `:67` |
| `unit-of-work.enabled` | `true` | `ChronicleProperties.UnitOfWork` |
| `causation.enabled` | `true` | `ChronicleProperties.Causation` |
| `identity.enabled` | `true` | `ChronicleProperties.Identity` |

The starter selects a unique `ReadModelNamingPolicy` bean. With none or an
ambiguous set, it retains identifier-based naming. A rejected connection at
startup propagates `ChronicleConnectionFailed` and fails the application;
an unreachable kernel only logs after `registration-timeout` and keeps retrying.
Chronicle uses kotlinx-coroutines `1.10.2`; remove an older
`kotlin-coroutines.version` override to `1.11.0`.

`NamespaceResolution.Strategy` is `FIXED`, `HTTP_HEADER`, `SUBDOMAIN`,
`AUTHENTICATION` (`ChronicleProperties.kt:70-82`).

The beans you inject are `IChronicleClient`, `IEventStore`, and the convenience
facade `Chronicle` (`ChronicleAutoConfiguration.kt:93-114`). `Chronicle` wraps
the suspending API in blocking calls and takes `Class<T>` rather than
`KClass<T>`, so Java can use it unchanged —
`Integrations/SpringBoot/src/main/kotlin/io/cratis/chronicle/spring/Chronicle.kt:47`,
`append` at `:57`, `appendMany` at `:69`.

`ChronicleWebAutoConfiguration` is **servlet-only**
(`@ConditionalOnWebApplication(SERVLET)`, `ChronicleWebAutoConfiguration.kt:29`).
A WebFlux application gets no per-request namespace, identity, causation, or
unit-of-work filter.

**Artifacts are Spring beans.** `SpringArtifactActivator` resolves a discovered
artifact from the container when it is uniquely defined and otherwise builds it
through `autowireCapableBeanFactory.createBean`
(`SpringArtifactActivator.kt:28-43`), so a reactor takes constructor
dependencies exactly like a `@Service` would.
