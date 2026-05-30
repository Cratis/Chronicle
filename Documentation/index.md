---
title: Chronicle
description: The event sourcing platform for .NET — store every change as an immutable event, and turn those events into read models, reactions, and projections.
---

Chronicle is an event sourcing platform for .NET. Instead of storing only the current state, it stores **every change as an immutable event** — the complete history of what happened — and turns those events into the read models, reactions, and projections your application needs.

[![NuGet](https://img.shields.io/nuget/v/Cratis.Chronicle?logo=nuget)](http://nuget.org/packages/cratis.chronicle)
[![Docker](https://img.shields.io/docker/v/cratis/chronicle?label=Chronicle&logo=docker&sort=semver)](https://hub.docker.com/r/cratis/chronicle)

## Why event sourcing

Keeping the full history of change — not just the latest state — gives you audit trails for free, the ability to answer "how did we get here?", time-travel debugging, and as many specialized read models as you need from one source of truth. It isn't the right fit for everything; [Why Event Sourcing](./why-event-sourcing.md) makes the case, and [When to use event sourcing](./concepts/when-to-use-event-sourcing.md) is the honest counterweight.

## Start here

- **Run something in minutes** — [Get started](./get-started/) scaffolds a project from a template and has events flowing into a read model.
- **Learn it step by step** — the [tutorial](./tutorial/) builds a small library system one concept at a time.
- **Coming from CRUD / EF Core?** The [bridge guide](./coming-from-crud.md) maps tables, rows, and `SaveChanges` onto events.

## Understand the model

- [Architecture](./architecture.md) — how the kernel, client, storage, and read models fit together.
- [Concepts](./concepts/) — events, event sources, projections, and more, with a [Glossary](./concepts/glossary.md).
- [Projections, reducers, and reactors](./concepts/observer-patterns.md) — the three ways to respond to events, and which to use.

## Do specific things

- [Scenarios](./scenarios/) — task-oriented recipes (rebuild a read model, enforce uniqueness, react to an event).
- [Events](./events/), [Projections](./projections/), [Reactors](./reactors/), [Read Models](./read-models/) — the feature guides.
- [Troubleshooting](./troubleshooting/) — answers to the questions that come up most.

Chronicle pairs with [Arc](/arc/) and [Components](/components/) for a full-stack, type-safe application — see [Why Cratis](/why-cratis/).
