---
title: "Captures"
description: "Turn changes in external data into Chronicle events with captures, and what the capturing engine supports today."
---

Captures let you describe Change Data Capture (CDC) pipelines that turn external data changes into Chronicle events.

:::caution[The capturing engine runs a subset of the language]
Captures parse and compile everything described here, but as of Chronicle 19.6 the engine that runs them supports only part of it:

- Only `api` sources are read. A capture with a `webhook` or `message` source never produces events.
- Only root-level `append` rules run. `map` operations, `nested` scopes, and `children` scopes are accepted but not applied.
- An assignment can take a property of the item (`$.path`) or a quoted literal. `$context`, `$env`, and template expressions are rejected at run time, and so are expression-based `when` conditions.

A cycle that hits an unsupported construct fails as a whole and is logged; no event is appended for it.
:::

## Overview

A capture definition describes:

- **Source**: where data comes from (`api`, `webhook`, `message`)
- **Identity key**: the property used to detect changes per entity
- **Mapping**: optional transformations before append logic
- **Append rules**: which event to append and when to append it
- **Scopes**: root, nested objects, and child collections

Chronicle supports three authoring approaches. Only the first is language-neutral — the other two are .NET client features, documented alongside the rest of the .NET client:

- **Capture Declaration Language (CDL)** for text-based definitions
- **Declarative API** (.NET) for fluent definitions in code
- **Model-bound API** (.NET) for attribute-based definitions

## Topics

| Topic | Description |
| ----- | ----------- |
| [Capture Declaration Language](capture-declaration-language/index.md) | CDL syntax, semantics, and formal language specification |
| [Declarative Captures](/chronicle/clients/dotnet/captures/declarative/) | .NET client fluent API (`ICapturer` + `ICaptureBuilder`) |
| [Model-Bound Captures](/chronicle/clients/dotnet/captures/model-bound/) | .NET client attribute-based capture declarations |
