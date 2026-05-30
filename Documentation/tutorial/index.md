---
title: "Tutorial: Build a library"
description: Build a small event-sourced library with Chronicle, one concept at a time — and finish understanding events, read models, and reactions from the inside out.
---

Imagine you're building the system for a small town library. Books arrive, members borrow them, and eventually bring them back — and when a popular title comes back, the next person on the waitlist should hear about it. It's a small domain, but it has everything event sourcing is good at: things *happen*, in order, over time, and you care about the history, not just the latest state.

Over the next three short chapters we'll build it together with Chronicle. We won't rush to the finish — at each step we'll stop to look at what just happened and why, so that by the end you don't just have working code, you understand the model well enough to build your own.

Here's the whole system we're heading toward — don't worry if the pieces aren't familiar yet, we'll meet each one in turn:

```mermaid
flowchart LR
    Add["a book arrives"] --> BA["BookAdded"]
    Borrow["a member borrows it"] --> BB["BookBorrowed"]
    Return["they bring it back"] --> BR["BookReturned"]
    BA & BB & BR --> P["a projection"]
    P --> RM["the Books read model<br/>(title · on loan?)"]
    BR -.->|a reactor| N["notify the next person"]
```

## What you'll need

A Chronicle project running locally — the [Get started](/chronicle/get-started/) guide gets you there in a couple of minutes (install the template, scaffold, `docker compose up`). Come back when `dotnet run` works, and we'll start writing the library.

## The tour

1. **[Your first event](./first-event.md)** — record the fact that a book arrived, and meet the event log.
2. **[Building a read model](./read-model.md)** — turn that stream of facts into something you can actually query.
3. **[Reacting to events](./reacting.md)** — do something useful when a book comes back.

By the end you'll have built the loop at the heart of every Chronicle app — *append a fact, project it into state, react to it* — and you'll know it well enough to leave the library behind and model your own domain. Ready? [Let's record the first thing that happens →](./first-event.md)
