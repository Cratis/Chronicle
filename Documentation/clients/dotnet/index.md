---
title: .NET client
description: Use the .NET client when your Chronicle application runs on C# or another .NET language.
---

The .NET client is the C#/.NET SDK for Chronicle. It gives .NET applications typed access to event stores, event logs, read models, projections, reducers, reactors, constraints, migrations, namespaces, and hosting integration.

Use this section for .NET-specific setup and APIs. The main Chronicle docs explain the event-sourcing concepts once and use language tabs for examples that apply across clients.

## When to use it

Use the .NET client when you are building:

- a console tool or script that talks directly to Chronicle
- an ASP.NET Core service that appends events from endpoints or Arc commands
- a Worker Service that runs projections, reducers, reactors, or background append logic
- a .NET test project that uses Chronicle's in-process testing scenarios

## Install

Install the .NET packages that match the host style you are building. The templates do this for you, but explicit package references are useful when adding Chronicle to an existing application.

```bash
dotnet add package Cratis.Chronicle
dotnet add package Cratis.Chronicle.AspNetCore
```

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

- [Console quickstart](/chronicle/get-started/console/)
- [ASP.NET Core hosting](/chronicle/get-started/aspnetcore/)
- [Worker Service hosting](/chronicle/get-started/worker/)
- [.NET connection strings](/chronicle/connection-strings/dotnet-client/)
- [.NET namespace resolution](/chronicle/namespaces/dotnet-client/)
- [.NET event type migrations](/chronicle/migrations/dotnet-client/)
- [Seeding events with C#](/chronicle/event-seeding/seeding-with-csharp/)

## Shared examples

When a Chronicle concept applies to every client, keep the prose in the shared Chronicle page and put the C# code in `Chronicle/Documentation/client-snippets/**`. The site renders it beside Kotlin, Elixir, and TypeScript snippets in synchronized language tabs.
