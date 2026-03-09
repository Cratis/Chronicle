# Domain Philosophy — Reference

## CUPID characteristics

Rather than adhering purely to SOLID principles, Cratis favors the **CUPID** characteristics (Dan North):

| Letter | Characteristic | What it means |
| --- | --- | --- |
| **C** | Composable | Things play nicely together, minimal coupling, components can be assembled freely |
| **U** | Unix philosophy | Do one thing and do it well — focused, single-purpose components |
| **P** | Predictable | Deterministic behavior, consistent output, no surprises |
| **I** | Idiomatic | Code feels natural for the language and ecosystem |
| **D** | Domain-based | Use domain vocabulary and structure, not technical vocabulary |

---

## Cohesion over layers

**Do not** split code by technical role (MVC-style):

```
❌ Layered (avoid)
Models/
  Author.cs
Controllers/
  AuthorsController.cs
Services/
  AuthorService.cs
Events/
  AuthorRegistered.cs
```

**Do** group by feature — everything that changes together lives together:

```
✅ Feature-cohesive (Cratis style)
Features/
  Authors/
    Registration/
      Registration.cs   ← command + event + validator
      AddAuthor.tsx
    Listing/
      Listing.cs        ← read model + projection + query
      Listing.tsx
```

For different technical concerns (frontend vs backend), naturally separate into different projects, but maintain the cohesive feature structure within each project.

---

## Domain language (Ubiquitous Language)

Name things after the domain concept they represent, not after the technical pattern:

| ✅ Domain-named | ❌ Tech-named |
| --- | --- |
| `Authors` | `AuthorController`, `AuthorManager` |
| `Registration` | `RegisterAuthorHandler`, `RegisterAuthorCommand` |
| `AuthorNotFound` | `AuthorNotFoundException`, `NotFoundException` |
| `AuthorId` | `Guid authorId` |
| `Listing` | `GetAllAuthorsQuery` |

---

## Pluralization

Features are groupings — pluralize them:

- Folder: `Authors/`, `Employees/`, `Orders/`
- Route: `/api/Authors/{authorId}`, `/api/Orders`
- Schema: `Authors`, `OrderItems`

---

## 12-Factor

Systems should follow [12factor.net](https://12factor.net) for scalability, maintainability, and operability:

- Config from environment, not hardcoded
- Stateless processes
- Treat logs as event streams
- Declarative setup for easy replication

---

## Frictionless dependencies

Healthy dependencies = fast, independent releases. If you must coordinate releases between two components, there is an unhealthy coupling that should be addressed through events, interfaces, or package versioning.

---

## Immutability & side-effects

Favor immutable designs to reduce side effects:

- Records with `init`-only properties
- Return new instances instead of mutating existing ones
- Expose `IEnumerable<T>` and `IReadOnlyDictionary<K,V>` — never mutable collections from public APIs
- The owner of state is responsible for its mutations — don't let consumers mutate your internal state
