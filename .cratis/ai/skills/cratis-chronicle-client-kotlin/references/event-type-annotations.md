<!-- cratis-ai-managed: skills/cratis-chronicle-client-kotlin/references/event-type-annotations.md -->
# Event type annotations

This reference holds the house shape of an event class and the property-level annotations that travel with it.

The house shape is a Kotlin `data class` with defaulted properties or a Java
`record` (the Spring Boot samples). Property-level
annotations that travel with the event: `@io.cratis.chronicle.keys.Key`,
`@io.cratis.chronicle.compliance.Pii`, `@io.cratis.chronicle.Subject`,
`@io.cratis.chronicle.schemas.JsonSchemaType`.
