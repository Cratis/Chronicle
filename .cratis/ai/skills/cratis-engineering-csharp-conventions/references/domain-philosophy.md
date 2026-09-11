<!-- cratis-ai-managed: skills/cratis-engineering-csharp-conventions/references/domain-philosophy.md -->
# Domain philosophy

## CUPID characteristics

Cratis favors the CUPID characteristics over a strict SOLID reading:

| Letter | Characteristic | What it means |
| --- | --- | --- |
| **C** | Composable | Parts play nicely together with minimal coupling and can be assembled freely |
| **U** | Unix philosophy | Do one thing well — focused, single-purpose components |
| **P** | Predictable | Deterministic behavior, consistent output, no surprises |
| **I** | Idiomatic | Code feels natural for the language and its ecosystem |
| **D** | Domain-based | Domain vocabulary and structure, not technical vocabulary |

## Cohesion over layers

Do not split code by technical role.

```
# Avoid — layered by technical role
Models/
  <Model>.cs
Controllers/
  <Name>Controller.cs
Services/
  <Name>Service.cs
Events/
  <Name>Event.cs
```

Group by feature instead, so everything that changes together lives together.
Feature folders sit directly under the source root; there is no `Features/`
wrapper.

```
# Preferred — cohesive by feature
<Feature>/
  <Behavior>/
    <Behavior>.cs      ← the backend artifacts for this behavior
    <Behavior>.tsx     ← its component
  <OtherBehavior>/
    <OtherBehavior>.cs
    <OtherBehavior>.tsx
```

Frontend and backend concerns naturally separate into different projects, but
each project keeps the cohesive feature structure inside it.

## Ubiquitous language

Name after the domain concept, not the technical pattern.

| Domain-named | Tech-named |
| --- | --- |
| `Authors` | `AuthorController`, `AuthorManager` |
| `Registration` | `RegisterAuthorHandler`, `RegisterAuthorCommand` |
| `AuthorNotFound` | `AuthorNotFoundException`, `NotFoundException` |
| `AuthorId` | a raw `Guid authorId` |
| `Listing` | `GetAllAuthorsQuery` |

## Pluralization

Features are groupings, so pluralize them consistently across folder, route, and
schema: `Authors/`, `/api/Authors/{authorId}`, an `Authors` schema.

## Twelve-factor operability

Systems follow the twelve-factor guidance for scalability, maintainability, and
operability:

- Configuration comes from the environment, never hardcoded.
- Processes are stateless.
- Logs are treated as event streams.
- Setup is declarative so an environment can be replicated.

## Frictionless dependencies

Healthy dependencies mean fast, independent releases. If two components must
have their releases coordinated, that is unhealthy coupling — address it through
events, an interface, or package versioning rather than accepting the lockstep.

## Immutability and side effects

Favor immutable designs to reduce side effects:

- Records with `init`-only properties.
- Return a new instance rather than mutating an existing one.
- Expose `IEnumerable<T>` and `IReadOnlyDictionary<TKey, TValue>` from public
  APIs, never a mutable collection.
- The owner of state is responsible for its mutations. Do not let a consumer
  mutate internal state.
