# Projection DSL

This folder contains the implementation of the Projection Domain-Specific Language (DSL) for Chronicle.

## Overview

The Projection DSL provides an elegant, indentation-based syntax for defining projections without writing code. The DSL is inspired by modern languages and focuses on readability and maintainability.

## Key Features

- **Indentation-based syntax** - No brackets or semicolons
- **Event-driven rules** - Define what happens when events occur
- **Full projection capabilities** - Supports all Chronicle projection features
- **Type-safe** - Compiles to strongly-typed Chronicle projection definitions
- **Toolable** - Parser, formatter, and language server support

## Components

### AST/Nodes.cs
Abstract Syntax Tree (AST) node definitions representing all DSL constructs. The AST is the intermediate representation between tokens and projection definitions.

### Tokenizer.cs
Indentation-aware lexical analyzer that breaks down DSL text into tokens. Features:
- Indentation tracking (INDENT/DEDENT tokens)
- Keywords and identifiers
- String literals and template strings
- Numbers and operators
- Comments (lines starting with #)

### Token.cs & TokenType.cs
Token structure and all supported token types for the DSL.

### RulesProjectionDslParser.cs
Parser that converts tokens into an AST. Handles:
- Projection declarations
- Event rules (`on EventType`)
- Property mappings and operations
- Joins and children (nested projections)
- Removal rules
- Composite keys

### ProjectionDslParser.cs (Legacy)
Original pipe-based parser (being phased out in favor of the rules-based DSL).

### ProjectionDsl.cs
Facade class that combines tokenization and parsing.

### ProjectionDslGenerator.cs
Generates DSL text from Chronicle projection definitions.

### ProjectionDslSyntaxError.cs
Custom exception for DSL syntax errors with line and column information.

## DSL Syntax

### Basic Projection

```dsl
projection User => UserReadModel
  automap
```

### Event Rules

```dsl
on UserCreated
  Name = e.name
  Email = e.email
  IsActive = true
```

### Event Context

```dsl
on UserLoggedIn
  LastLogin = ctx.occurred
  count LoginCount
```

### Keys

```dsl
# Simple key
on UserAssignedToGroup key e.userId
  GroupId = ctx.eventSourceId

# Composite key
on OrderCreated
  key OrderKey {
    CustomerId = e.customerId
    OrderNumber = e.orderNumber
  }
  Total = e.total
```

### Counters & Arithmetic

```dsl
increment LoginCount
decrement RetryCount
count EventCount

add Balance by e.amount
subtract Balance by e.amount
```

### FromEvery

```dsl
every
  LastUpdated = ctx.occurred
  EventSourceId = ctx.eventSourceId
  exclude children
```

### Joins

```dsl
join Group on GroupId
  events GroupCreated, GroupRenamed
  automap
```

### Children

```dsl
children Members id e.userId
  automap

  on UserAddedToGroup key e.userId
    parent ctx.eventSourceId
    Role = e.role

  on UserRoleChanged key e.userId
    parent e.groupId
    Role = e.role

  remove on UserRemovedFromGroup key e.userId
    parent e.groupId
```

### Removed With Join

```dsl
children Groups id e.groupId
  on UserAddedToGroup
    parent e.userId

  join Group on GroupId
    events GroupCreated, GroupRenamed
    automap

  remove via join on GroupDeleted
```

## Expressions

- `e.property` - Event data
- `ctx.occurred`, `ctx.sequenceNumber`, `ctx.correlationId`, `ctx.eventSourceId` - Event context
- `$eventSourceId` - Shorthand for `ctx.eventSourceId`
- Literals: `true`, `false`, `null`, numbers, strings
- String templates: `` `${e.firstName} ${e.lastName}` ``

## Usage

```csharp
var dsl = @"
projection User => UserReadModel
  automap

  on UserCreated
    Name = e.name
    Email = e.email

  on UserLoggedIn
    count LoginCount
";

var tokenizer = new Tokenizer(dsl);
var tokens = tokenizer.Tokenize();
var parser = new RulesProjectionDslParser(tokens);
var document = parser.Parse();

// Compile AST to Chronicle projection definition
// var definition = compiler.Compile(document.Projections[0], ...);
```

## Documentation

See `/Documentation/projections/dsl.md` for comprehensive DSL documentation and examples.

## Testing

Tests are located in `/Source/Kernel/Projections.Specs/DSL/`.

## Architecture

The DSL implementation follows a classic compiler pipeline:

1. **Tokenization** - Text → Tokens
2. **Parsing** - Tokens → AST
3. **Validation** - AST semantic analysis
4. **Compilation** - AST → Chronicle ProjectionDefinition
5. **Generation** (reverse) - ProjectionDefinition → DSL text

This separation allows for:
- Easy testing of each component
- Language tooling (formatters, linters)
- Multiple backends (if needed)
- Clear error messages with source locations

