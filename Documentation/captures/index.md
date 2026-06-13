# Captures

Captures let you describe Change Data Capture (CDC) pipelines that turn external data changes into Chronicle events.

## Overview

A capture definition describes:

- **Source**: where data comes from (`api`, `webhook`, `message`)
- **Identity key**: the property used to detect changes per entity
- **Mapping**: optional transformations before append logic
- **Append rules**: which event to append and when to append it
- **Scopes**: root, nested objects, and child collections

Chronicle supports three authoring approaches:

- **Capture Declaration Language (CDL)** for text-based definitions
- **Declarative C# API** for fluent definitions in code
- **Model-bound C# API** for attribute-based definitions

## Topics

| Topic | Description |
| ----- | ----------- |
| [Capture Declaration Language](capture-declaration-language/index.md) | CDL syntax, semantics, and formal language specification |
| [Declarative Captures](declarative/index.md) | Fluent C# API (`ICapturer` + `ICaptureBuilder`) |
| [Model-Bound Captures](model-bound/index.md) | Attribute-based capture declarations |
