---
title: Cratis Chronicle .NET client setup
description: Use the Cratis Chronicle .NET client to append events, build read models, and connect console apps, ASP.NET Core services, or workers.
---

The Cratis Chronicle .NET client connects your application to an event store so you can record what happened and build read models from that history. Use it directly in a console application or let ASP.NET Core and Worker Service hosting configure the client through dependency injection.

Start with [the .NET setup guide](./getting-started.md) to connect and append an event. This section covers .NET APIs and hosting; the shared Chronicle guides explain event-sourcing concepts and provide examples for the participating clients.

## When to use it

Use the .NET client when you are building:

- a console tool or script that talks directly to Chronicle
- an ASP.NET Core service that appends events from endpoints or Arc commands
- a Worker Service that registers projections, hosts reducers or reactors, or appends events in the background
- a .NET test project that uses Chronicle's in-process testing scenarios

## Install

Add `Cratis.Chronicle` to an existing .NET application. For ASP.NET Core hosting, add `Cratis.Chronicle.AspNetCore` too. Chronicle templates already include the packages for their host style.

```bash
dotnet add package Cratis.Chronicle
dotnet add package Cratis.Chronicle.AspNetCore
```

## Event content round trips

When you append and read an event, a nullable property reads back as `null` whether it was stored as
`null` or omitted. The client can omit nulls during serialization, but a nullable `decimal?` or
`int?` holding `0` remains a value. Missing non-nullable values become their type defaults where
the schema permits them.

Raw JSON equality can therefore misrepresent an event-content comparison. The kernel's
schema-aware `ContentComparison` / `VerifyContent` path accounts for these rules; these are kernel
verification mechanisms, not .NET client methods. See
[Null, absent, and default values](/chronicle/events/schema-representation/#null-absent-and-default-values).

## Shared Chronicle topics

- [Get started](/chronicle/get-started/)
- [Events and event logs](/chronicle/events/)
- [Appending events](/chronicle/events/appending/)
- [Read models](/chronicle/read-models/)
- [Projections](/chronicle/projections/)
- [Reactors](/chronicle/reactors/)
- [Reducers](/chronicle/reducers/)
- [Constraints](/chronicle/constraints/)

## .NET-specific pages

- [Health checks](./health-checks.md)
- [Short-lived processes](./short-lived-processes.md)
- [Explicit artifact registration](./explicit-artifacts)
- [Discovery reports and identity drift](./discovery-report.md)
- [Registration outcomes](./registration-outcome.md)
- [Console quickstart](/chronicle/get-started/console/)
- [ASP.NET Core hosting](/chronicle/get-started/aspnetcore/)
- [Worker Service hosting](/chronicle/get-started/worker/)
- [.NET connection strings](/chronicle/connection-strings/dotnet-client/)
- [.NET namespace resolution](/chronicle/namespaces/dotnet-client/)
- [.NET event type migrations](/chronicle/migrations/dotnet-client/)
- [Seeding events with C#](/chronicle/event-seeding/seeding-with-csharp/)

## Shared examples

Shared guides show C# examples alongside the other clients available for that task in synchronized language tabs. Their C# source lives in `Chronicle/Documentation/client-snippets/**`; use the .NET-specific pages above for setup and APIs that differ by client.
