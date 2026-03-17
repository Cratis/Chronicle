---
agent: agent
description: Write comprehensive BDD specs for an existing vertical slice command or query.
---

# Write Specs

I need you to write **comprehensive specs** for an existing vertical slice.

## What to provide

Paste or reference the slice file (`.cs`) you want covered.

## Instructions

Follow `.github/instructions/specs.csharp.instructions.md` and `.github/instructions/specs.instructions.md`.

### For a State Change slice (command)

Write a spec class for **each meaningful outcome**:

1. **Happy path** — command succeeds, expected event is appended, sequence number advanced.
2. **Each validation failure** — one spec per validation rule.
3. **Each business rule violation** — one spec per condition in `Handle()` that inspects a ReadModel argument (DCB pattern).
4. **Each constraint violation** — one spec per `IConstraint` (e.g. uniqueness).

### Structure

```
Features/<Feature>/<Slice>/
└── when_<verb_phrase>/
    ├── and_<happy_scenario>.cs
    ├── and_<failure_scenario>.cs
    └── and_<other_scenario>.cs
```

### C# spec shape

```csharp
using context = <NamespaceRoot>.<Feature>.<Slice>.when_<behavior>.and_<scenario>.context;

namespace <NamespaceRoot>.<Feature>.<Slice>.when_<behavior>;

[Collection(ChronicleCollection.Name)]
public class and_<scenario>(context context) : Given<context>(context)
{
    public class context(ChronicleOutOfProcessFixture fixture) : given.an_http_client(fixture)
    {
        public CommandResult<object>? Result;

        async Task Establish() { /* optional: seed events */ }

        async Task Because()
        {
            Result = await Client.ExecuteCommand<<Command>>(
                "/api/<feature>/<action>",
                new <Command>(...));
        }
    }

    [Fact] void should_<expected_result>() => ...;
}
```

### Assertions

Use `ShouldBeFalse()`, `ShouldBeTrue()`, `ShouldEqual()`, `ShouldHaveTailSequenceNumber()` — never raw `Assert.*`.

## Validation

Run `dotnet test` after writing specs. Fix any failures before completing.
