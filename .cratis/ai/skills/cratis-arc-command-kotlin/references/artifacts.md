<!-- cratis-ai-managed: skills/cratis-arc-command-kotlin/references/artifacts.md -->
# Arc.Kotlin artifacts

This reference holds the Arc.Kotlin artifacts and what each provides.

| Artifact | Purpose |
| --- | --- |
| `io.cratis:arc` | `io.cratis.arc.artifacts.Command`, `CommandKey`, `FromServices` |
| `io.cratis:arc-ksp` | the KSP processor that generates the command handler |
| `io.cratis:arc-spring-boot-starter` | Spring Boot auto-configuration, the command pipeline host |
| `io.cratis:cratis` | recommended single dependency for an event-sourced app: Arc, Spring Boot wiring and Chronicle integration |
| `io.cratis:arc-chronicle-spring-boot-starter` | explicit alternative to `cratis`, same Arc version |
| `io.cratis:arc-testing` | in-process command and query scenarios (test scope) |
| `io.cratis:arc-spring-data-jpa`, `io.cratis:arc-spring-data-mongodb` | Spring Data read-model integrations |
| `io.cratis:arc-rxjava3` | RxJava 3 observable queries |
| `io.cratis:arc-observability-spring-boot-starter`, `io.cratis:arc-openapi-spring-boot-starter` | optional observability and OpenAPI |
| `io.cratis.arc` Gradle plugin | applies Kotlin/JVM + KSP, adds the dependencies, configures the module name and proxy output |
