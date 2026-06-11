---
applyTo: "**/*.kt"
---

# Kotlin Conventions

## Naming

### No Postfixes

**Do not postfix class names** — this is a hard rule across the entire Kotlin client.

- No `*Service` suffix. A class that registers reactors is `Reactors`, not `ReactorsService`. A class that registers reducers is `Reducers`, not `ReducersService`.
- No `*Annotation` suffix on annotation classes. An annotation that marks a reactor is `@Reactor`, not `@ReactorAnnotation`.
- No `*Manager`, `*Handler`, `*Helper`, `*Utils` — these are all content-free names that say nothing about what the class actually does.

Name classes after what they **represent in the domain** — typically the plural form of the concept:

| Concept | Correct name | Wrong name |
|---|---|---|
| Registers and manages reactors | `Reactors` | `ReactorsService` |
| Registers and manages reducers | `Reducers` | `ReducersService` |
| Registers event types | `EventTypes` | `EventTypesService` |
| Registers projections | `Projections` | `ProjectionsService` |
| Manages constraints | `Constraints` | `ConstraintsService` |
| Marks a class as a reactor | `@Reactor` | `@ReactorAnnotation` |
| Marks a class as a reducer | `@Reducer` | `@ReducerAnnotation` |
| Marks a class as an event type | `@EventType` | `@EventTypeAnnotation` |

The only exception is when the class IS genuinely a service (e.g., a gRPC stub wrapper intended exclusively as an internal infrastructure adapter), but even then, prefer naming it after the protocol or transport concept.

## Annotations

- Annotation classes must not carry an `Annotation` suffix. Use `@EventType`, `@Reactor`, `@Reducer`, `@Projection`, `@Constraint`, `@ReadModel`, `@Seeder`, `@Pii`, etc.
- All identity/id fields on annotation classes must be named `id` for consistency — never `reactorId`, `reducerId`, `projectionId`, `readModelId`, or `name`.
- All id fields on annotations default to `""`. The service layer uses reflection (`simpleName` or `qualifiedName`) as the fallback, so callers never need to specify an id explicitly unless they want to override the default.

## File Organization

- One top-level declaration per file.
- File name matches the top-level class or annotation name exactly.
- Extension functions on a type live in the same file as the type, unless they are so numerous that a dedicated `*Extensions.kt` file is warranted.
