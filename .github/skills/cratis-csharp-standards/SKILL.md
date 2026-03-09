---
name: cratis-csharp-standards
description: Reference for Cratis C# coding conventions — formatting, naming, records, nullable handling, exceptions, logging, and DI. Use whenever writing C# in a Cratis project, deciding between record vs class, checking naming rules, applying formatting conventions, handling null safety, creating exception types, or asking "how should this be written?" Also covers CUPID characteristics and domain-based folder structure. Trigger on any C# style or standards question for a Cratis project.
---

## Key rules (quick reference)

- Use C# 13 features always — records, primary constructors, pattern matching
- `var` over explicit types — the right side already tells you the type
- File-scoped namespace declarations — one less indentation level
- `using` directives: alphabetically sorted, single-line, unused ones removed
- No regions — if a file needs them, it needs refactoring
- No postfixes: no `Async`, `Impl`, `Service`, `Manager`, `Handler` on class names
- No `Exception` suffix on exception types — `AuthorNotFound` not `AuthorNotFoundException`
- Never use built-in exceptions — always create custom exception types
- `record` for events, commands, read models, concepts — value equality for free
- Primary constructors for all types — eliminates field boilerplate
- `is null` / `is not null` — never `== null` / `!= null`
- Blank line before opening `{` of every code block
- Final `return` on its own line
- Private fields: `_camelCase` with underscore prefix
- Interfaces: `I` prefix (`IMyService`)
- No `[EventType]` arguments — the type name is the event identifier
- `[Command]` records define `Handle()` directly — no separate handler classes

---

## Formatting

```csharp
// File-scoped namespace (no extra indent)
namespace MyApp.Authors.Registration;

// Alphabetically sorted using directives
using Cratis.Arc.Commands;
using Cratis.Chronicle.Events;
using Microsoft.Extensions.Logging;

// Blank line before { of every block
if (condition)
{
    DoSomething();
}

// Expression-bodied for simple members
public string FullName => $"{FirstName} {LastName}";

// Final return on its own line
public string GetName()
{
    var result = BuildName();

    return result;
}
```

---

## Naming

| Artifact | Convention | Example |
| --- | --- | --- |
| Type / method / public member | PascalCase | `RegisterAuthor`, `AuthorId` |
| Private field | `_camelCase` | `_eventLog` |
| Local variable | camelCase | `authorId` |
| Interface | `I` prefix | `IEventLog` |
| Exception type | No `Exception` suffix | `AuthorNotFound` |
| Feature folder | Pluralized domain noun | `Authors/` |
| Concept file | Concept name | `AuthorId.cs` |

No abbreviations unless widely known (XML, JSON, Id, URL). No prefixes/postfixes that describe technical role (Controller, ViewModel, Handler, Manager, Factory, Base).

---

## Reference files

- `references/code-style.md` — records, primary constructors, nullable, collections, async
- `references/exceptions-logging-di.md` — exception types, logging with [LoggerMessage], DI conventions
- `references/domain-philosophy.md` — CUPID characteristics, cohesion, ubiquitous language
