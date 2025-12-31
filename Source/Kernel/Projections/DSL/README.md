# Projection DSL

This folder contains the implementation of the Projection Domain-Specific Language (DSL) for Chronicle.

## Overview

The Projection DSL provides a simple, text-based syntax for defining projections without writing code. This is particularly useful for:

- Rapid prototyping and experimentation with projections
- Testing projection definitions in the Workbench
- Creating projections without recompiling applications
- Visualizing projection results in real-time

## Components

### Tokenizer.cs
Lexical analyzer that breaks down DSL text into tokens. Handles:
- Keywords (key, increment, decrement, count, etc.)
- Operators (=, +, -, |, etc.)
- Identifiers (event types, property names)
- String and number literals
- Comments (lines starting with #)
- Escape sequences in strings

### Token.cs & TokenType.cs
Define the token structure and all supported token types.

### ProjectionDslParser.cs
Parser that converts tokens into a `ProjectionDefinition` object. Supports:
- Read model declarations
- Property mappings from events
- Arithmetic operations (add, subtract, increment, decrement, count)
- Event context properties ($eventContext)
- Constant values
- Simple and composite keys
- One-to-many relationships (children)
- Removal definitions (removedWith)

### ProjectionDsl.cs
Facade class that combines tokenization and parsing into a single `Parse` method.

### ProjectionDslSyntaxError.cs
Custom exception for DSL syntax errors, including line and column information.

## Usage

```csharp
var dsl = @"Users
| key=UserRegistered.userId
| name=UserRegistered.name
| email=UserRegistered.email
| orderCount increment by OrderPlaced";

var definition = ProjectionDsl.Parse(
    dsl,
    new ProjectionId("my-projection"),
    ProjectionOwner.Client,
    EventSequenceId.Log);
```

## Documentation

See `/Documentation/projections/dsl.md` for comprehensive DSL syntax documentation.

## Testing

Tests are located in `/Source/Kernel/Projections.Specs/DSL/for_ProjectionDslParser/`.
