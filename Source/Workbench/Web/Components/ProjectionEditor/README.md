# Projection DSL Monaco Language Service

This directory contains the Monaco Editor language service for the Projection DSL.

## Overview

The language service provides:
- **Syntax highlighting** for DSL keywords, operators, and identifiers
- **Validation** of DSL syntax with error markers
- **IntelliSense** code completion for keywords, properties, and event context
- **Type checking** to prevent arithmetic operations on non-numeric types
- **Schema-aware** suggestions based on JSON Schema of the read model

## Usage

### 1. Install Monaco Editor

First, add Monaco Editor to your package.json:

```bash
yarn add monaco-editor
```

### 2. Register the Language

```typescript
import * as monaco from 'monaco-editor';
import { registerProjectionDslLanguage, setReadModelSchema } from './Monaco/ProjectionDsl';

// Register the language
registerProjectionDslLanguage(monaco);
```

### 3. Provide Read Model Schema

To enable property-aware validation and completion, provide the read model's JSON Schema:

```typescript
import { setReadModelSchema } from './Monaco/ProjectionDsl';

const schema = {
    properties: {
        name: { type: 'string' },
        age: { type: 'integer', format: 'int32' },
        totalSpent: { type: 'number', format: 'decimal' },
        lastLogin: { type: 'string', format: 'date-time' },
        duration: { type: 'string', format: 'duration' },
    },
};

setReadModelSchema(schema);
```

### 4. Create an Editor

```typescript
import * as monaco from 'monaco-editor';
import { languageId } from './Monaco/ProjectionDsl';

const editor = monaco.editor.create(document.getElementById('container'), {
    value: `Users
| name=UserRegistered.name
| age=UserRegistered.age
| totalSpent+=OrderCompleted.amount`,
    language: languageId,
    theme: 'vs-dark',
    minimap: { enabled: false },
});
```

## Features

### Syntax Highlighting

The language service provides syntax highlighting for:
- Keywords: `key`, `increment`, `decrement`, `count`, `by`, `on`, `join`, `identified`, `removedWith`
- Operators: `=`, `+=`, `-=`, `|`, `.`, `:`, `,`
- Comments: Lines starting with `#`
- String literals (with escape sequences)
- Number literals

### Validation

The validator checks for:
- Missing read model name
- Invalid statement format
- Arithmetic operations on non-numeric types
- Increment/decrement on non-numeric types

Supported numeric types (from TypeFormats):
- `int16`, `int32`, `uint32`, `int64`, `uint64`
- `float`, `double`, `decimal`, `byte`
- `duration` (TimeSpan)

### IntelliSense

-- The completion provider suggests:
- **Keywords** when starting a new statement
- **Read model properties** from the provided schema
- **Operators** (`=`, `+=`, `-=`) based on property type
- **Event context properties** (`$eventContext.occurred`, `$eventContext.eventSourceId`, etc.)

Arithmetic operators (`+=`, `-=`) are only suggested for numeric and TimeSpan properties.

## Type Safety

The language service enforces type safety:

✅ **Valid:**
```
Users
| totalSpent+=OrderCompleted.amount      # OK: numeric type
| loginCount increment by UserLoggedIn  # OK: numeric type
```

❌ **Invalid:**
```
Users
| name+=UserRegistered.firstName         # Error: string type
| status increment by UserActivated     # Error: string type
```

## Schema Format

The read model schema should follow this format:

```typescript
interface ReadModelSchema {
    properties: Record<string, PropertySchema>;
}

interface PropertySchema {
    type: string;              // 'string', 'number', 'integer', 'boolean', 'object', 'array'
    format?: string;           // 'int32', 'decimal', 'date-time', 'duration', etc.
    items?: PropertySchema;    // For array types
}
```

## Cleanup

When disposing of the editor, clean up the language service:

```typescript
import { disposeProjectionDslLanguage } from './Monaco/ProjectionDsl';

disposeProjectionDslLanguage();
editor.dispose();
```

## Example

Complete example:

```typescript
import * as monaco from 'monaco-editor';
import {
    registerProjectionDslLanguage,
    setReadModelSchema,
    languageId,
} from './Monaco/ProjectionDsl';

// Register language
registerProjectionDslLanguage(monaco);

// Set schema
setReadModelSchema({
    properties: {
        userId: { type: 'string', format: 'guid' },
        name: { type: 'string' },
        email: { type: 'string' },
        totalSpent: { type: 'number', format: 'decimal' },
        orderCount: { type: 'integer', format: 'int32' },
        lastLogin: { type: 'string', format: 'date-time' },
    },
});

// Create editor
const editor = monaco.editor.create(document.getElementById('editor'), {
    value: `Users
| key=UserRegistered.userId
| name=UserRegistered.name
| email=UserRegistered.email
| totalSpent+OrderCompleted.amount
| orderCount increment by OrderPlaced
| lastLogin=$eventContext.occurred`,
    language: languageId,
    automaticLayout: true,
});
```
