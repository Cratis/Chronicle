---
uid: Chronicle.Testing
---
# Testing

Chronicle provides lightweight, in-process test utilities that let you verify event appending behavior, read model projections, and reactor side-effects without a running server or database. Tests are fast, isolated, and require no infrastructure.

## Topics

| Guide | Description |
|-------|-------------|
| [Events](events/index.md) | Test event appends and constraint behavior in-process using `EventScenario` |
| [Read Models](read-models/index.md) | Test projections and reducers in-process using `ReadModelScenario<TReadModel>` |
| [Reactors](reactors/index.md) | Test reactor side-effects in-process using `ReactorScenario<TReactor>` |
