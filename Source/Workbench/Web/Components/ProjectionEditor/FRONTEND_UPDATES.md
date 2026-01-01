# Frontend Updates for Projection DSL

## Overview

The ProjectionEditor Monaco editor component has been updated to support the new indentation-based Chronicle Projection DSL syntax.

## Files Updated

### 1. language.ts
- **Complete rewrite** of the monarch language definition
- Added new keywords: `projection`, `every`, `on`, `key`, `parent`, `join`, `automap`, `children`, `add`, `remove`, `via`, `from`, `default`, `set`, `unset`
- Added built-in identifiers: `$eventSourceId`, `$causedBy`, `$occurred`, `$namespace`, `$eventContext`
- Added template string support with `${...}` interpolation
- Updated language configuration with:
  - Indentation rules (increase indent after keywords like `on`, `every`, `children`, `parent`)
  - Folding support for code blocks
  - Auto-closing pairs for braces, brackets, parentheses, quotes, and backticks

### 2. validation.ts
- **Complete rewrite** of validation and completion logic
- Simplified validation to check for proper projection structure
- Updated completion provider to support new DSL syntax:
  - Line 1: Suggests `projection` keyword and read model names
  - After `every`/`on`: Suggests event type names
  - After `key`: Suggests property names from the read model schema
  - After `children`/`parent`/`join`: Suggests collection/parent property names
  - Property assignments: Suggests event types, `$eventSourceId`, `$causedBy`, etc.
  - After `EventType.`: Suggests event properties from the event schema
- Preserved existing read model and event schema integration

### 3. index.ts
- Updated trigger characters for completion (removed `[`, added `\n` for newline triggers)
- No other changes needed - existing wiring works perfectly

### 4. ProjectionEditor.tsx
- No changes needed - component already properly set up

## New Features

### Template String Support
The editor now supports template literals with syntax highlighting for `${...}` expressions:
```
message = `Order ${OrderCreated.orderId} created`
```

### Indentation-Based Syntax
The editor automatically increases/decreases indentation based on keywords:
```
projection OrderSummary

every OrderCreated
  key orderId = $eventSourceId  # Indented automatically
  totalAmount = OrderCreated.amount
```

### Smart Completion

1. **Keyword Completion**: Type at the start of a line to get suggestions for `every`, `on`, `key`, `automap`, etc.

2. **Read Model Name Completion**: On line 1 after `projection `, get completion for available read model names

3. **Event Type Completion**: After `every` or `on`, get completion for all registered event types

4. **Property Completion**:
   - After `key `, get completion for read model properties
   - After event type and dot (e.g., `OrderCreated.`), get completion for event properties
   - At the start of indented lines, get completion for read model properties

5. **Built-in Completion**: Get suggestions for `$eventSourceId`, `$causedBy`, `$occurred`, `$namespace`

### Syntax Highlighting

- Keywords highlighted in keyword color
- Event types highlighted as classes
- Properties highlighted appropriately
- Built-in identifiers highlighted as predefined variables
- Template strings with special highlighting for `${...}` expressions
- Comments (starting with `#`) highlighted

## Migration Notes

### Old Syntax (Pipe-Based)
```
OrderSummary
| key orderId
| customerId=OrderCreated.customerId
| totalAmount+=OrderLineAdded.amount
```

### New Syntax (Indentation-Based)
```
projection OrderSummary

every OrderCreated
  key orderId = $eventSourceId
  customerId = OrderCreated.customerId

on OrderLineAdded
  key orderId = $eventSourceId
  totalAmount += OrderLineAdded.amount
```

## Backward Compatibility

The old `validation.old.ts` file has been preserved for reference. The backend parser (ProjectionDslParser.cs) is marked as [Obsolete] but still functional for backward compatibility.

## Testing

To test the new editor:
1. Open a Projection editor in the Workbench
2. Start typing `projection ` and verify read model completion works
3. Type `every ` or `on ` and verify event type completion works
4. After an event type dot (e.g., `OrderCreated.`), verify event property completion works
5. Verify indentation automatically increases after keywords
6. Verify template strings with `${...}` are highlighted correctly

## Files for Reference

- New DSL specification: `/Source/Kernel/Projections/DSL/chronicle-projection-rules-dsl.md`
- Example projections: `/Source/Kernel/Projections/DSL/example.projection`
- Monaco reference: `/Source/Kernel/Projections/DSL/chronicle-rules-dsl.monaco.ts`
