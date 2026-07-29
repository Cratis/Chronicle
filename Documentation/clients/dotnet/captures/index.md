---
title: Authoring captures in .NET
description: The two .NET-only ways to author Chronicle captures in code — model-bound attributes and the declarative fluent builder — alongside the language-neutral Capture Declaration Language.
---

[Captures](/chronicle/captures/) turn external data changes into Chronicle events. Chronicle supports three ways to author a capture, and only one of them is language-neutral:

| Approach | Language | Where it's documented |
| --- | --- | --- |
| Capture Declaration Language (CDL) | Text-based, any client | [Capture Declaration Language](/chronicle/captures/capture-declaration-language/) |
| Declarative API | C# fluent builder | [Declarative Captures](declarative.md) |
| Model-bound API | C# attributes | [Model-Bound Captures](model-bound.md) |

The declarative and model-bound APIs are .NET client features — they compile to the same `CaptureDefinition` a CDL text file would, but the authoring surface itself is C#-specific.
