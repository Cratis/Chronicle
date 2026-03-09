---
name: write-specs
description: Use this skill when asked to write integration specs for a vertical slice in a Cratis-based project. Produces BDD-style Chronicle integration specs using ChronicleOutOfProcessFixture, Given<context>, and xUnit. Covers State Change commands (happy path, validation, business rules, constraints) and State View queries. For unit specs, standalone class specs, or the general BDD spec pattern (Establish/Because/should_), see cratis-specs-csharp.
---

Write comprehensive BDD integration specs for a vertical slice command or query.

## Spec placement

Specs live **in the same slice folder** as the `.cs` file:

```
Features/<Feature>/<Slice>/
├── <Slice>.cs
└── when_<verb_phrase>/
    ├── and_<happy_scenario>.cs
    └── and_<failure_scenario>.cs
```

## What to cover for every State Change command

Write one spec class for **each** of:

1. **Happy path** — command succeeds, expected event is appended, sequence number advanced
2. **Each validation failure** — one `and_` class per `[Required]`, `[MaxLength]`, etc. rule
3. **Each business rule violation** — one `and_` class per DCB condition in `Handle()` that inspects a read model
4. **Each constraint violation** — one `and_` class per `IConstraint`

## What to cover for State View queries

State View queries project events into read models — they have no command to execute, so specs validate the projection logic by appending events first:

```csharp
// Listing/when_listing_authors/and_authors_have_been_registered.cs
using context = MyApp.Authors.Listing.when_listing_authors.and_authors_have_been_registered.context;

namespace MyApp.Authors.Listing.when_listing_authors;

[Collection(ChronicleCollection.Name)]
public class and_authors_have_been_registered(context context) : Given<context>(context)
{
    public class context(ChronicleOutOfProcessFixture fixture) : given.an_http_client(fixture)
    {
        public IEnumerable<AuthorSummary>? Result;
        static readonly AuthorId _authorId = AuthorId.New();

        async Task Establish() =>
            await EventStore.EventLog.Append(_authorId, new AuthorRegistered(new AuthorName("John Doe")));

        async Task Because() =>
            Result = await Client.GetFromJsonAsync<IEnumerable<AuthorSummary>>("/api/authors");
    }

    [Fact] void should_include_the_registered_author() =>
        Context.Result!.ShouldContain(a => a.Name == "John Doe");
    [Fact] void should_return_one_author() =>
        Context.Result!.Count().ShouldEqual(1);
}
```

## C# spec structure

```csharp
// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using context = <NamespaceRoot>.<Feature>.<Slice>.when_<behavior>.and_<scenario>.context;

namespace <NamespaceRoot>.<Feature>.<Slice>.when_<behavior>;

[Collection(ChronicleCollection.Name)]
public class and_<scenario>(context context) : Given<context>(context)
{
    public class context(ChronicleOutOfProcessFixture fixture) : given.an_http_client(fixture)
    {
        public CommandResult<object>? Result;

        async Task Because()
        {
            Result = await Client.ExecuteCommand<<Command>>(
                "/api/<feature>/<action>",
                new <Command>(...));
        }
    }

    [Fact] void should_<expected_result>() => Context.Result.IsSuccess.ShouldBeTrue();
}
```

For scenarios that require pre-existing state, add an `async Task Establish()` before `Because()`:

```csharp
async Task Establish() =>
    await EventStore.EventLog.Append(SomeId.New(), new SomeEvent(...));
```

## Naming conventions

- Folder: `when_<verb_phrase>` — e.g. `when_registering`, `when_removing_a_project`
- File: `and_<condition>.cs` — e.g. `and_name_is_unique.cs`, `and_name_already_exists.cs`
- Test method: `should_<expected_result>` — e.g. `should_append_the_registered_event`

## Assertion helpers

Use Cratis.Specifications helpers — never raw `Assert.*`:
- `Context.Result.IsSuccess.ShouldBeTrue()`
- `Context.Result.IsSuccess.ShouldBeFalse()`
- `Context.ShouldHaveTailSequenceNumber(EventSequenceNumber.First)` — checks the event log tail

## After writing

Run `dotnet test`. Fix all failures before completing.

---

For full worked examples of each spec type (happy path, validation, business rule, constraint), see [references/EXAMPLES.md](references/EXAMPLES.md).
