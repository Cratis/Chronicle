---
title: Cratis Chronicle capture authoring in .NET
description: Choose C# builders or attributes to describe Cratis Chronicle captures, and understand where CDL text is required to manage a capture.
---

Cratis Chronicle [captures](/chronicle/captures/) turn external data changes into events. In .NET, you can describe a capture with fluent C# builders or model-bound attributes.

| Approach | Use it to | Where it's documented |
| --- | --- | --- |
| Capture Declaration Language (CDL) | Write language-neutral text that the kernel parses and validates | [Capture Declaration Language](/chronicle/captures/capture-declaration-language/) |
| Declarative API | Construct a `CaptureDefinition` with a C# fluent builder | [Declarative Captures](declarative.md) |
| Model-bound API | Describe capture intent with C# attributes beside your types | [Model-Bound Captures](model-bound.md) |

`CaptureBuilder.Build()` produces a `CaptureDefinition`. The model-bound attributes expose metadata, but the .NET client does not provide a builder that converts them into a definition. Neither approach registers or starts a capture, and `IEventStore` does not expose capture management. The kernel's management contract takes CDL text.

If you need a capture the engine can run, start with CDL. Its execution limits apply regardless of authoring style: API polling and events captures can run root-level append rules, while webhook and message-topic sources, map operations, and nested or children scopes are rejected during validation.
