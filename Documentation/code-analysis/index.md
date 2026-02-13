# Code Analysis Rules

This section documents the code analysis rules provided by the Chronicle Code Analyzer for the .NET client.

All rules follow the identifier format `CHR####` where the numbers are sequential without gaps.

## Rules Overview

| Rule ID | Title | Severity | Description |
|---------|-------|----------|-------------|
| [CHR0001](CHR0001.md) | Event type must have [EventType] attribute | Error | Types appended to event sequences must be marked with [EventType] attribute |
| [CHR0002](CHR0002.md) | Declarative projection event type must have [EventType] attribute | Error | Declarative projection generic arguments must reference types with [EventType] attribute |
| [CHR0003](CHR0003.md) | Model bound projection attribute must reference event type with [EventType] attribute | Error | Model bound projection attributes must reference types with [EventType] attribute |
| [CHR0004](CHR0004.md) | Reactor method signature must match allowed signatures | Warning | Reactor methods must follow allowed signatures |
| [CHR0005](CHR0005.md) | Reactor event parameter must have [EventType] attribute | Error | Event parameters in reactor methods must be marked with [EventType] attribute |
| [CHR0006](CHR0006.md) | Reducer method signature must match allowed signatures | Warning | Reducer methods must follow allowed signatures |
| [CHR0007](CHR0007.md) | Reducer event parameter must have [EventType] attribute | Error | Event parameters in reducer methods must be marked with [EventType] attribute |

## Quick Fixes

All error rules (CHR0001, CHR0002, CHR0003, CHR0005, CHR0007) provide a code fix that automatically adds the `[EventType]` attribute to the referenced type.

## Installation

The analyzer is automatically included when you reference the `Cratis.Chronicle` NuGet package.
