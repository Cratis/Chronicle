---
uid: Chronicle.Testing.Reactors
---
# Testing Reactors

A reactor turns an event into a side effect — the command or event that should follow. When your reactors return those side effects, most of what you want to verify is a pure function you can test with a plain method call, and Chronicle's `ReactorScenario<TReactor>` covers the framework wiring around it — all in-process, with no Chronicle server, gRPC transport, or observer registration required.

## Two levels of testing

- **The logic** — construct the reactor and call the handler directly, asserting the command or event it returns. Fast, no infrastructure, and resilient to framework changes. This is your default.
- **The wiring** — use [`ReactorScenario<TReactor>`](scenario) to prove Chronicle materializes the right read model, resolves dependencies, dispatches the right handler, and routes the produced side effect through the real invoker.

The rule of thumb: logic goes to a pure function, the framework contract goes to `ReactorScenario`.

## Topics

| Guide | Description |
|-------|-------------|
| [ReactorScenario](scenario) | Test reactor side-effects in-process using `ReactorScenario<TReactor>` |
