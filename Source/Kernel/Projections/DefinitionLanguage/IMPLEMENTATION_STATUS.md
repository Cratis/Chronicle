# Projection DSL Reimplementation - Implementation Status

## Overview

The Projection DSL has been reimagined from a pipe-based syntax to an elegant, indentation-based rules syntax as specified in `chronicle-projection-rules-dsl.md`.

## Completed Work

### 1. AST Model (`AST/Nodes.cs`) ✅
Created a comprehensive Abstract Syntax Tree model that represents all DSL constructs:
- Document and ProjectionNode
- Projection directives (AutoMap, Key, CompositeKey, Every, OnEvent, Join, Children)
- Child blocks (ChildOnEvent, ChildJoin, NestedChildren, Remove, RemoveViaJoin)
- Mapping operations (Assignment, Increment, Decrement, Count, Add, Subtract)
- Expressions (EventData, EventContext, EventSourceId, Literals, Templates)
- Type references and template parts

### 2. TokenType Update (`TokenType.cs`) ✅
Completely rewrote token types to match the new DSL:
- Added: projection, every, on, key, parent, join, events, children, id, remove, via, automap, exclude
- Added: increment, decrement, count, add, subtract, by
- Added: true, false, null, e (EventRef), ctx (ContextRef)
- Added: Arrow (=>), Dollar ($), LeftBrace ({), RightBrace (})
- Added: Indent, Dedent, TemplateLiteral
- Removed: Pipe, Colon, Plus, Minus, LeftBracket, RightBracket, and other old operators

### 3. Tokenizer (`Tokenizer.cs`) ✅
Completely rewrote the tokenizer to be indentation-aware:
- **Indentation tracking**: Maintains an indent stack and emits INDENT/DEDENT tokens
- **Comments**: Supports `#` line comments
- **String literals**: Supports both single and double quotes with escape sequences
- **Template strings**: Supports backtick templates with `${expr}` interpolation
- **Keywords**: All new DSL keywords properly recognized
- **Error handling**: Throws ProjectionDslSyntaxError with line/column information

### 4. Parser (`RulesProjectionDslParser.cs`) ✅
Created a complete recursive descent parser:
- Parses the full document into AST
- Handles all projection-level directives
- Supports event rules with inline modifiers (automap, key)
- Parses joins with multiple event types
- Handles nested children with proper recursion
- Supports remove blocks (both regular and via join)
- Parses all mapping operations
- Parses all expression types including templates
- Validates syntax and provides clear error messages

### 5. README Update (`README.md`) ✅
Updated the README with:
- Complete DSL syntax examples
- Component descriptions
- Architecture overview
- Usage examples
- Expression reference

## Remaining Work

### 6. Compiler (AST → Chronicle Definitions)
**Status**: NOT STARTED
**File**: `RulesProjectionDslCompiler.cs` (to be created)

This component needs to transform the AST into Chronicle `ProjectionDefinition` objects. Key tasks:
- Map AST nodes to `FromDefinition`, `JoinDefinition`, `ChildrenDefinition`, etc.
- Convert DSL expressions to Chronicle property expressions
- Handle key resolution (simple and composite)
- Map operations (increment, decrement, count, add, subtract)
- Handle automap directives
- Create proper parent/child relationships
- Build RemovedWith and RemovedWithJoin definitions

**Reference Types**:
- `Cratis.Chronicle.Concepts.Projections.Definitions.ProjectionDefinition`
- `Cratis.Chronicle.Concepts.Projections.Definitions.FromDefinition`
- `Cratis.Chronicle.Concepts.Projections.Definitions.FromEveryDefinition`
- `Cratis.Chronicle.Concepts.Projections.Definitions.JoinDefinition`
- `Cratis.Chronicle.Concepts.Projections.Definitions.ChildrenDefinition`
- `Cratis.Chronicle.Concepts.Projections.Definitions.RemovedWithDefinition`
- `Cratis.Chronicle.Concepts.Projections.Definitions.RemovedWithJoinDefinition`

### 7. Generator Update (`ProjectionDslGenerator.cs`)
**Status**: NOT STARTED

The generator needs to be completely rewritten to output the new rules-based DSL:
- Generate `projection Name => ReadModel` declarations
- Generate `every` blocks with mappings
- Generate `on EventType` blocks
- Generate joins, children, and removal rules
- Format with proper 2-space indentation
- Generate string templates for complex expressions

### 8. Facade Update (`ProjectionDsl.cs`, `ProjectionDslParserFacade.cs`)
**Status**: NOT STARTED

Update the facade to use the new pipeline:
- Tokenizer → RulesProjectionDslParser → Compiler
- Maintain backward compatibility if needed
- Update interfaces (`IProjectionDslParser`, `IProjectionDslGenerator`)

### 9. Delete Old Specs
**Status**: NOT STARTED
**Location**: `/Source/Kernel/Projections.Specs/DSL/for_ProjectionDslParser/`

All existing specs are based on the old pipe syntax and need to be removed:
- when_parsing_event_context_mapping.cs
- when_parsing_simple_read_model_name.cs
- when_parsing_increment_operation.cs
- when_parsing_composite_key.cs
- when_parsing_add_operation.cs
- when_parsing_subtract_operation.cs
- when_parsing_removed_with.cs
- when_parsing_complete_example.cs
- when_parsing_decrement_operation.cs
- when_parsing_count_operation.cs
- when_parsing_constant_value.cs
- when_parsing_property_mapping.cs
- when_parsing_simple_key.cs

### 10. Create New Specs
**Status**: NOT STARTED

Create specs for the new implementation following the project's spec conventions:
- **Tokenizer specs**: Test indentation tracking, keywords, literals, templates
- **Parser specs**: Test each DSL construct (projection, on, every, join, children, etc.)
- **Compiler specs**: Test AST → ProjectionDefinition transformation
- **Generator specs**: Test ProjectionDefinition → DSL text generation
- **Integration specs**: End-to-end tests with complete projections

Example structure:
```
for_Tokenizer/
  when_tokenizing_indented_blocks/
  when_tokenizing_keywords/
  when_tokenizing_template_strings/

for_RulesProjectionDslParser/
  when_parsing_projection_with_automap/
  when_parsing_on_event_block/
  when_parsing_every_block/
  when_parsing_join_block/
  when_parsing_children_block/
  when_parsing_composite_key/
  when_parsing_mapping_operations/
  when_parsing_expressions/
```

## Architecture

The new implementation follows a clean compiler pipeline:

```
DSL Text
  ↓
Tokenizer (indentation-aware)
  ↓
Tokens (including INDENT/DEDENT)
  ↓
RulesProjectionDslParser
  ↓
AST (strongly-typed nodes)
  ↓
RulesProjectionDslCompiler
  ↓
ProjectionDefinition (Chronicle)
```

Reverse (generation):
```
ProjectionDefinition
  ↓
ProjectionDslGenerator
  ↓
DSL Text (formatted)
```

## Benefits of the New DSL

1. **Readability**: Indentation-based syntax is more natural and easier to read
2. **Expressiveness**: Rules-based approach clearly shows "when X, do Y"
3. **Toolability**: Clean AST enables formatters, linters, and language servers
4. **Type Safety**: AST provides strong typing throughout the pipeline
5. **Extensibility**: Easy to add new constructs without breaking existing code

## Next Steps

To complete the implementation:

1. **Create the compiler** (`RulesProjectionDslCompiler.cs`)
2. **Update the generator** (rewrite `ProjectionDslGenerator.cs`)
3. **Update facades** (integrate new components)
4. **Delete old specs** (remove pipe-based tests)
5. **Create new specs** (comprehensive test coverage)
6. **Update documentation** (user-facing docs in `/Documentation/projections/dsl.md`)

## Monaco Editor Integration

The file `chronicle-rules-dsl.monaco.ts` already contains language service definitions for Monaco Editor. After completing the compiler and generator, this should be tested and potentially updated to match the final implementation.

## Migration Path

For existing projections using the old pipe syntax:
1. Keep the old `ProjectionDslParser.cs` as legacy support
2. Provide a migration tool to convert old DSL to new DSL
3. Deprecate the old syntax with warnings
4. Eventually remove old parser in a future major version

Alternatively, support both syntaxes simultaneously by detecting the format (presence of `projection` keyword vs. pipe-based).
