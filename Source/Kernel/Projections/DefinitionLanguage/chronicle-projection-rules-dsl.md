
# Chronicle Projection Rules DSL

This document specifies the **Rules DSL** used to define Chronicle projections without writing code.
The DSL compiles into the existing Chronicle declarative projection builder model and supports **all current projection capabilities**.

---

## Goals

- Elegant, compact, modern syntax
- Indentation-based (no pipes, no SQL)
- Reads as *rules applied to events*
- Fully toolable (parser, formatter, language server)
- Zero-loss abstraction over existing Chronicle projection APIs

### Supported Capabilities

- AutoMap (projection / event / join / children)
- FromEvery + ExcludeChildProjections
- Property mapping (event data, literals, templates, context)
- Event context access (`occurred`, `sequenceNumber`, `correlationId`, `eventSourceId`)
- Counters: `increment`, `decrement`, `count`
- Arithmetic operations: `add`, `subtract`
- Explicit keys
- Composite keys (including context values)
- Joins
- Children (nested)
- RemovedWith
- RemovedWithJoin

---

## Mental Model

> “When event **X** happens, apply these effects.”

- `projection` defines the read model
- `from Event` defines a rule
- Assignments and operations are **effects**
- `children` and `join` create scoped mutation contexts
- Defaults remove noise; overrides are explicit

---

## DSL Examples

### Projection

```dsl
projection User => UserReadModel
  automap
```

---

### FromEvery

```dsl
every
  LastUpdated = $eventContext.occurred
  EventSourceId = $eventContext.eventSourceId
  exclude children
```

---

### Event Rules

```dsl
from UserCreated
  Name = name
  Email = email
  IsActive = true
```

```dsl
from UserLoggedIn
  LastLogin = $eventContext.occurred
  count LoginCount
```

---

### Keys

```dsl
from UserAssignedToGroup key userId
  GroupId = $eventContext.eventSourceId
```

---

### Composite Keys

```dsl
from OrderCreated
  key OrderKey {
    CustomerId = customerId
    OrderNumber = orderNumber
  }
  Total = total
```

---

### Counters & Arithmetic

```dsl
increment LoginCount
decrement RetryCount
count EventCount

add Balance by amount
subtract Balance by amount
```

---

### Joins

```dsl
join Group on GroupId
  events GroupCreated, GroupRenamed
  automap
```

---

### Children

```dsl
children Members id userId
  automap

  from UserAddedToGroup key userId
    parent $eventContext.eventSourceId
    Role = role

  from UserRoleChanged key userId
    parent groupId
    Role = role

  remove with UserRemovedFromGroup key userId
    parent groupId
```

---

### Removed With Join

```dsl
children Groups id groupId
  from UserAddedToGroup
    parent userId

  join Group on GroupId
    events GroupCreated, GroupRenamed
    automap

  remove via join on GroupDeleted
```

---

## Prompt: Implement the Rules DSL

You are implementing a compact, indentation-based DSL that defines Chronicle projections
(projecting events into read models).

The DSL **must compile into the existing Chronicle declarative projection builder object model**
and support **all current projection capabilities** without loss of expressiveness.

---

## Parsing & Architecture

### Pipeline

1. Parse text into tokens (indent-sensitive lexer)
2. Build AST
3. Validate semantics
4. Compile (lower) into Chronicle projection builders

### General Rules

- Indentation defines structure (spaces only, no tabs)
- Fail fast with actionable diagnostics (line + column)
- No implicit behavior beyond documented defaults
- Deterministic formatting rules

---

## Expressions

### DSL Syntax

Supported expressions in the DSL:

- `<path>` — event payload property access (e.g., `name`, `contactInfo.email`)
- `$eventContext.<property>` — event context property access (`$eventContext.occurred`, `$eventContext.sequenceNumber`, `$eventContext.correlationId`, `$eventContext.eventSourceId`)
- `$eventSourceId` — shorthand for event source ID
- Literals: numbers, strings, booleans, `null`
- String templates: `` `${firstName} ${lastName}` ``

No arbitrary function calls unless explicitly added later.

### Compiled Format

**Important**: The DSL syntax is transformed when compiled into Chronicle projection definitions:

- `<path>` → `<path>` — Event data paths remain unchanged (e.g., `name` stays `name`)
- `$eventContext.<property>` → `$eventContext(<property>)` — Dot notation becomes function-style (e.g., `$eventContext.occurred` becomes `$eventContext(occurred)`)
- `$eventSourceId` → `$eventSourceId` — Remains unchanged
- Literals and templates retain their structure

This transformation ensures the compiled projection definitions use Chronicle's internal expression format while keeping the DSL clean and consistent.

---

## Mapping Operations

### Assignment

```dsl
Prop = expr
```

Maps a value into the read model.

### Counters

```dsl
increment Prop
decrement Prop
count Prop
```

- `increment` / `decrement` always change by ±1
- `count` increments once per matching event

### Arithmetic

```dsl
add Prop by expr
subtract Prop by expr
```

Adds or subtracts a numeric value (usually from the event).

---

## Validation Rules

- Unknown keywords → error
- Invalid indentation → error
- Unknown event types → error (or warning if string-based mode)
- `children` blocks must declare `id`
- `remove via join` requires an available join key
- Numeric operations only allowed on numeric target properties
- `add` / `subtract` must include `by <expr>`
- Composite keys must contain at least one field

---

## Formatting Rules

- 2 spaces indentation
- One blank line between top-level blocks
- No leading punctuation (`|`, `:` etc.)
- Formatter must produce canonical output

---

## EBNF Grammar

```ebnf
Document        = { Projection } ;

Projection      = "projection", Ident, "=>", TypeRef, NL,
                  INDENT,
                    { ProjDirective | Block },
                  DEDENT ;

ProjDirective   = "automap", NL
                | KeyDecl
                | CompositeKeyDecl ;

Block           = EveryBlock
                | FromEventBlock
                | JoinBlock
                | ChildrenBlock ;

EveryBlock      = "every", NL,
                  INDENT,
                    { MappingLine },
                    [ "exclude", "children", NL ],
                  DEDENT ;

FromEventBlock  = "from", TypeRef,
                  { FromEventOpt },
                  NL,
                  INDENT,
                    [ ParentDecl ],
                    { MappingLine | KeyDecl | CompositeKeyDecl },
                  DEDENT ;

FromEventOpt    = "automap"
                | KeyInline ;

JoinBlock       = "join", Ident, "on", Ident, NL,
                  INDENT,
                    "events", TypeRef, { ",", TypeRef }, NL,
                    [ "automap", NL ],
                    { MappingLine },
                  DEDENT ;

ChildrenBlock   = "children", Ident, "id", Expr, NL,
                  INDENT,
                    [ "automap", NL ],
                    { ChildBlock },
                  DEDENT ;

ChildBlock      = FromEventBlock
                | JoinBlock
                | RemoveWithBlock
                | RemoveWithJoinBlock
                | ChildrenBlock ;

RemoveWithBlock = "remove", "on", TypeRef, [ KeyInline ], NL,
                  INDENT,
                    [ ParentDecl ],
                  DEDENT ;

RemoveWithJoinBlock
               = "remove", "via", "join", "on", TypeRef, [ KeyInline ], NL ;

KeyDecl         = "key", Expr, NL ;

CompositeKeyDecl
               = "key", TypeRef, "{", NL,
                  INDENT,
                    KeyPart, { NL, KeyPart }, NL?,
                  DEDENT,
                 "}", NL ;

KeyPart         = Ident, "=", Expr ;

ParentDecl      = "parent", Expr, NL ;

MappingLine     = Assignment
                | IncLine
                | DecLine
                | CountLine
                | AddLine
                | SubLine ;

Assignment      = Ident, "=", Expr, NL ;

IncLine         = "increment", Ident, NL ;
DecLine         = "decrement", Ident, NL ;
CountLine       = "count",     Ident, NL ;

AddLine         = "add",      Ident, "by", Expr, NL ;
SubLine         = "subtract", Ident, "by", Expr, NL ;

Expr            = Template
                | Literal
                | DollarExpr
                | Path ;

DollarExpr      = "$eventSourceId"
                | "$eventContext", ".", Ident ;

Path            = Ident, { ".", Ident } ;

TypeRef         = Ident, { ".", Ident } ;
Ident           = Letter, { Letter | Digit | "_" } ;
```

---

## Lowering Rules (DSL → Chronicle Builder)

### Projection

- `projection X => ReadModel`
  - Create projection builder for `ReadModel`
- `automap`
  - `builder.AutoMap()`

### FromEvery

- `every { ... }`
  - `builder.FromEvery(...)`
- `exclude children`
  - `.ExcludeChildProjections()`

### Event Rules

- `from Event`
  - `builder.From<Event>(...)`
- `automap`
  - `.AutoMap()` on event builder
- `key expr`
  - `.UsingKey(expr)`
- `key Type { ... }`
  - `.UsingCompositeKey<Type>(...)`

### Mapping

- `Prop = expr`
  - `.Set(m => m.Prop).To(expr)`
- `increment Prop`
  - `.Increment(m => m.Prop)`
- `decrement Prop`
  - `.Decrement(m => m.Prop)`
- `count Prop`
  - `.Count(m => m.Prop)`
- `add Prop by expr`
  - `.Add(m => m.Prop).With(expr)`
- `subtract Prop by expr`
  - `.Subtract(m => m.Prop).With(expr)`

### Joins

- `join X on Prop`
  - `.Join<T>(j => j.On(m => m.Prop))`

### Children

- `children C id expr`
  - `.Children(m => m.C, c => c.IdentifiedBy(expr))`
- `parent expr`
  - `.UsingParentKey(expr)` or context variant

### Removal

- `remove with Event`
  - `.RemovedWith<Event>(...)`
- `remove via join on Event`
  - `.RemovedWithJoin<Event>(...)`

---

## Deliverables

1. Lexer + Parser
2. AST model
3. Semantic validator
4. Compiler to Chronicle projection builder
5. Formatter
6. (Optional) Language server integration

---

End of specification.
