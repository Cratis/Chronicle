---
uid: Chronicle.Testing.Reactors
title: Test Chronicle reactor decisions and dispatch
description: Test Chronicle reactor decisions with direct handler calls, then use ReactorScenario for dispatch and seeded read-model lookup without a server.
---

<a id="testing-reactors"></a>

Test a Chronicle reactor's decision with a direct handler call. Use the .NET `ReactorScenario<TReactor>` when you also need to check event dispatch, dependency resolution, or read-model lookup without starting a Chronicle server.

A reactor responds to an event by producing a side effect, such as another event or a command. Separating that decision from the work of executing it lets a test answer a specific question: given this event and this read model, what should happen next?

## Two levels of testing

Start by constructing the reactor and calling its handler with the event and read model it needs. Assert on the returned command or event. When the handler depends only on those inputs, this is a pure-function test: Chronicle does not participate in the call.

Move to [`ReactorScenario<TReactor>`](scenario) when the question involves Chronicle's invocation path. The scenario selects the handler by event type, resolves dependencies, looks up explicitly seeded read models, and records returned side effects through the real `ReactorInvoker`. It does not build those read models from the supplied events or execute the recorded side effects.

Keep server-backed integration tests for observer registration and delivery, retry scheduling, persistence, and execution of external effects. A passing scenario shows what the reactor produced in-process, not that a command ran or an event reached another observer.

## Topics

| Guide | Description |
|-------|-------------|
| [ReactorScenario](scenario) | Test reactor side-effects in-process using `ReactorScenario<TReactor>` |
