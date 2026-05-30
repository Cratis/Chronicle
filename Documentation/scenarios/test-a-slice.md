---
title: Test a slice end to end
description: Prove a whole vertical slice works — command in, the right events out — with a Chronicle integration spec.
---

**Goal:** verify that a feature actually works from the outside: a command goes in, and exactly the right events come out, against a real event store. If this passes, the whole slice — handling, appending, constraints — works.

## Why integration specs are the high-value tests

Unit-testing a `Handle()` method in isolation proves a little. An **integration spec** drives the command the way a client does and asserts on the events that landed in the store — so it exercises the command, its validation, its constraints, and the append in one go. These are the specs worth writing first.

## Write the spec

Specs read as specifications by example: a `context` sets up the world, `Because` performs the one action, and each `should_` asserts a single outcome.

```csharp
using context = Library.Authors.Registration.when_registering.context;

namespace Library.Authors.Registration.when_registering;

[Collection(ChronicleCollection.Name)]
public class and_the_name_is_new(context context) : Given<context>(context)
{
    public class context(ChronicleOutOfProcessFixture fixture) : given.an_http_client(fixture)
    {
        public CommandResult<object>? Result;

        async Task Because() =>
            Result = await Client.ExecuteCommand<RegisterAuthor>(
                "/api/authors/register",
                new RegisterAuthor("Jane Austen"));
    }

    [Fact] void should_succeed() => Context.Result!.IsSuccess.ShouldBeTrue();
    [Fact] void should_append_one_event() =>
        Context.ShouldHaveTailSequenceNumber(EventSequenceNumber.First);
    [Fact] void should_append_author_registered() =>
        Context.ShouldHaveAppendedEvent<AuthorRegistered>(
            EventSequenceNumber.First, e => e.Name.ShouldEqual("Jane Austen"));
}
```

## What to assert

- **Success or failure** — `Result.IsSuccess` (a constraint violation should make it `false`).
- **The events appended** — `ShouldHaveAppendedEvent<TEvent>(...)` checks the right fact, with the right data, landed.
- **The tail** — `ShouldHaveTailSequenceNumber(...)` confirms *how many* events were appended (the first event sits at `EventSequenceNumber.First`, which is `0`).

## Seed preconditions

Use `async Task Establish()` on the `context` to append the events that must already exist — for example, to test that registering a duplicate name is rejected, append the first `AuthorRegistered` in `Establish`, then assert the second command fails.

## See also

- [Testing](../testing/) — the full testing model (events, reactors, read models).
- [Enforce a unique value](./enforce-a-unique-value.md) — the constraint these specs would exercise.
