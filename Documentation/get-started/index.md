---
title: Get started
description: Scaffold a Chronicle application from a template, run it, and see events turn into a read model in a few minutes.
---

This is the fastest way from nothing to a running, event-sourced application. You install the Cratis templates, scaffold a project, start Chronicle, and run — then watch events you append become a queryable read model.

:::note[New to event sourcing?]
You can follow these steps without any prior event-sourcing knowledge. If you want the *why* first, read [Why Event Sourcing](/chronicle/why-event-sourcing/) — then come back here.
:::

## Prerequisites

- The [.NET SDK](https://dotnet.microsoft.com/download) (.NET 8 or newer).
- [Docker](https://www.docker.com/) — Chronicle runs as a small kernel alongside your app, and the template wires it up with `docker compose`.

See [Prerequisites](./prereq.md) for details.

## 1. Install the templates

```bash
dotnet new install Cratis.Templates
```

This adds the Cratis `dotnet new` templates, including a console starter and a full-stack web starter.

## 2. Scaffold a project

Start with the console template — it is the smallest thing that exercises the whole event-sourcing loop:

```bash
dotnet new cratis-chronicle-console -o Library
cd Library
```

:::tip[Want a full-stack app instead?]
Use `dotnet new cratis -o Library` to scaffold an [Arc](/arc/) + Chronicle backend with a React frontend and generated proxies. The same concepts apply — there is just a UI on top.
:::

## 3. Start Chronicle

The template includes a `docker-compose.yml` for the Chronicle kernel and its storage. Bring it up:

```bash
docker compose up -d
```

## 4. Run

```bash
dotnet run
```

The sample appends a few events and projects them into a read model. **That loop — append a fact, read a model built from it — is the whole point of Chronicle**, and you now have it running locally.

## What just happened

- Your app connected to the Chronicle kernel through a `ChronicleClient` and resolved an **event store**.
- It appended **events** (immutable facts) to an event sequence.
- A **projection** turned those events into a **read model** you can query — no update code, just a declaration of how events map to data.

These are the core concepts; the [Concepts](/chronicle/concepts/) section explains each one, and the [Glossary](/chronicle/concepts/glossary/) defines every term.

## Where to go next

- **Build it yourself, step by step** — the [tutorial](/chronicle/tutorial/) builds a small library system one concept at a time.
- **Other ways to start** — wire Chronicle into an existing app: [Console](./console.md), [ASP.NET Core](./aspnetcore.md), or [Worker Service](./worker.md).
- **Understand the model** — [Why Event Sourcing](/chronicle/why-event-sourcing/) and the [Concepts](/chronicle/concepts/) section.
