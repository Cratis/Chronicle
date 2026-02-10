# Grammar (EBNF)

This document provides the complete formal grammar for the Projection Declaration Language using Extended Backus-Naur Form (EBNF) notation.

## EBNF Notation

EBNF is a notation for formally describing syntax. Here are the key elements used:

| Notation | Meaning | Example |
|----------|---------|---------|
| `=` | Definition | `Rule = "value" ;` |
| `;` | End of rule | `Rule = "value" ;` |
| `{ }` | Zero or more | `{ "item" }` matches "", "item", "itemitem", etc. |
| `[ ]` | Optional (zero or one) | `[ "optional" ]` matches "" or "optional" |
| `( )` | Grouping | `( "a" \| "b" )` matches "a" or "b" |
| `\|` | Alternative (or) | `"a" \| "b"` matches either "a" or "b" |
| `" "` | Terminal (literal) | `"keyword"` matches the text "keyword" |
| `,` | Sequence | `"a", "b"` matches "a" followed by "b" |

**Special Symbols:**
- `NL` - Newline
- `INDENT` - Increased indentation level
- `DEDENT` - Decreased indentation level
- `Ident` - Identifier (letter followed by letters, digits, or underscores)
- `TypeRef` - Type reference (identifier with optional dot notation)

## Complete Grammar

```ebnf
Document        = { Projection } ;

Projection      = "projection", Ident, "=>", TypeRef, NL,
                  INDENT,
                    { ProjDirective | Block },
                  DEDENT ;

ProjDirective   = "no", "automap", NL
                | KeyDecl
                | CompositeKeyDecl ;

Block           = EveryBlock
                | FromEventBlock
                | JoinBlock
                | ChildrenBlock
                | RemoveWithBlock
                | RemoveWithJoinBlock ;

EveryBlock      = "every", NL,
                  INDENT,
                    [ "no", "automap", NL ],
                    { MappingLine },
                    [ "exclude", "children", NL ],
                  DEDENT ;

FromEventBlock  = "from", EventSpec, { ",", EventSpec },
                  NL,
                  [ INDENT,
                    [ ParentDecl ],
                    { MappingLine | KeyDecl | CompositeKeyDecl },
                  DEDENT ] ;

EventSpec       = TypeRef, [ "key", Expr ] ;

KeyInline       = "key", Expr ;

JoinBlock       = "join", Ident, "on", Ident, NL,
                  INDENT,
                    { WithEventBlock },
                  DEDENT ;

WithEventBlock  = "with", TypeRef, NL,
                  [ INDENT,
                      { MappingLine },
                    DEDENT ] ;

ChildrenBlock   = "children", Ident, "identified", "by", Expr, NL,
                  INDENT,
                    [ "no", "automap", NL ],
                    { ChildBlock },
                  DEDENT ;

ChildBlock      = ChildEveryBlock
                | FromEventBlock
                | JoinBlock
                | RemoveWithBlock
                | RemoveWithJoinBlock
                | ChildrenBlock ;

ChildEveryBlock = "every", NL,
                  INDENT,
                    [ "no", "automap", NL ],
                    { MappingLine },
                  DEDENT ;

RemoveWithBlock = "remove", "with", TypeRef, [ KeyInline ], NL,
                  [ INDENT,
                      [ ParentDecl ],
                    DEDENT ] ;

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

Template        = "`", { TemplateChar | "${", Expr, "}" }, "`" ;
TemplateChar    = (* any character except ` *) ;

Literal         = BoolLiteral
                | StringLiteral
                | NumberLiteral
                | NullLiteral ;

BoolLiteral     = "true" | "false" ;
StringLiteral   = '"', { StringChar }, '"' ;
NumberLiteral   = [ "-" ], Digit, { Digit }, [ ".", Digit, { Digit } ] ;
NullLiteral     = "null" ;

TypeRef         = Ident, { ".", Ident } ;
Ident           = Letter, { Letter | Digit | "_" } ;

Letter          = "A" | "B" | ... | "Z" | "a" | "b" | ... | "z" ;
Digit           = "0" | "1" | "2" | ... | "9" ;
StringChar      = (* any character except " and newline *) ;
```pdl

## Grammar Breakdown

### Document Structure

A document contains one or more projections:

```ebnf
Document = { Projection } ;
```

### Projection

A projection defines the read model and contains directives and blocks:

```ebnf
Projection = "projection", Ident, "=>", TypeRef, NL,
             INDENT,
               { ProjDirective | Block },
             DEDENT ;
```pdl

**Example:**
```
projection User => UserReadModel
  from UserCreated
    Name = name
```pdl

**Note:** AutoMap is enabled by default. Use `no automap` to disable it.

### Directives

Projection-level directives:

```ebnf
ProjDirective = "no", "automap", NL
              | KeyDecl
              | CompositeKeyDecl ;
```

### Blocks

Main building blocks of a projection:

```ebnf
Block = EveryBlock
      | FromEventBlock
      | JoinBlock
      | ChildrenBlock
      | RemoveWithBlock
      | RemoveWithJoinBlock ;
```pdl

### Every Block

Apply mappings to all events:

```ebnf
EveryBlock = "every", NL,
             INDENT,
               [ "no", "automap", NL ],
               { MappingLine },
               [ "exclude", "children", NL ],
             DEDENT ;
```

### From Event Block

Handle specific events:

```ebnf
FromEventBlock = "from", EventSpec, { ",", EventSpec },
                 NL,
                 [ INDENT,
                   [ ParentDecl ],
                   { MappingLine | KeyDecl | CompositeKeyDecl },
                 DEDENT ] ;

EventSpec = TypeRef, [ "key", Expr ] ;
```pdl

**Note:** AutoMap for from blocks is controlled at the projection or children level, not within individual from blocks.

### Join Block

Enrich with joined events:

```ebnf
JoinBlock = "join", Ident, "on", Ident, NL,
            INDENT,
              { WithEventBlock },
            DEDENT ;

WithEventBlock = "with", TypeRef, NL,
                 [ INDENT,
                     { MappingLine },
                   DEDENT ] ;
```

**Note:** AutoMap for join blocks is controlled at the projection or children level, not within individual with blocks.

### Children Block

Define nested collections:

```ebnf
ChildrenBlock = "children", Ident, "identified", "by", Expr, NL,
                INDENT,
                  [ "no", "automap", NL ],
                  { ChildBlock },
                DEDENT ;

ChildBlock = ChildEveryBlock
           | FromEventBlock
           | JoinBlock
           | RemoveWithBlock
           | RemoveWithJoinBlock
           | ChildrenBlock ;

ChildEveryBlock = "every", NL,
                  INDENT,
                    [ "no", "automap", NL ],
                    { MappingLine },
                  DEDENT ;
```pdl

**Note:** `ChildEveryBlock` applies mappings to all events within the children collection. Unlike the top-level `EveryBlock`, it does not support the `exclude children` directive as it operates within a children context.

### Removal Blocks

Remove instances based on events:

```ebnf
RemoveWithBlock = "remove", "with", TypeRef, [ KeyInline ], NL,
                  [ INDENT,
                      [ ParentDecl ],
                    DEDENT ] ;

RemoveWithJoinBlock = "remove", "via", "join", "on", TypeRef, [ KeyInline ], NL ;
```

### Key Declarations

Define instance keys:

```ebnf
KeyDecl = "key", Expr, NL ;

CompositeKeyDecl = "key", TypeRef, "{", NL,
                   INDENT,
                     KeyPart, { NL, KeyPart }, NL?,
                   DEDENT,
                   "}", NL ;

KeyPart = Ident, "=", Expr ;
```pdl

### Mapping Lines

Operations that modify the read model:

```ebnf
MappingLine = Assignment
            | IncLine
            | DecLine
            | CountLine
            | AddLine
            | SubLine ;

Assignment = Ident, "=", Expr, NL ;
IncLine = "increment", Ident, NL ;
DecLine = "decrement", Ident, NL ;
CountLine = "count", Ident, NL ;
AddLine = "add", Ident, "by", Expr, NL ;
SubLine = "subtract", Ident, "by", Expr, NL ;
```

### Expressions

Values and references:

```ebnf
Expr = Template
     | Literal
     | DollarExpr
     | Path ;

DollarExpr = "$eventSourceId"
           | "$eventContext", ".", Ident ;

Path = Ident, { ".", Ident } ;

Template = "`", { TemplateChar | "${", Expr, "}" }, "`" ;

Literal = BoolLiteral
        | StringLiteral
        | NumberLiteral
        | NullLiteral ;
```pdl

## Indentation Rules

The grammar uses indentation to define structure:

1. **INDENT**: Increase indentation by one level (typically 2 spaces)
2. **DEDENT**: Decrease indentation by one level
3. **Consistent Spacing**: All indentation must use spaces (no tabs)
4. **Block Structure**: Each block's content must be indented from its declaration

**Example:**
```
projection User => UserReadModel    # Level 0
  from UserCreated                  # Level 1 (INDENT)
    Name = name                     # Level 2 (INDENT)
    Email = email                   # Level 2
                                    # (DEDENT, DEDENT)
```pdl

## Validation Rules

Beyond the grammar, these semantic rules apply:

1. Event types must exist or be valid identifiers
2. Properties referenced must exist on events and read models
3. Type compatibility between expressions and target properties
4. Numeric operations only on numeric properties
5. `children` blocks must declare `identified by`
6. `remove via join` requires an available join key
7. Composite keys must contain at least one field
8. Parent keys required in children's from and remove blocks

## Formatting Conventions

While not enforced by the grammar, these conventions improve readability:

1. **Indentation**: 2 spaces per level
2. **Blank Lines**: Between top-level blocks
3. **Alignment**: Align `=` signs when helpful
4. **Ordering**: Logical grouping of related mappings

## Example Using Grammar

This example demonstrates how various grammar rules combine:

```
projection Order => OrderReadModel
  every
    LastUpdated = $eventContext.occurred
    exclude children

  from OrderPlaced key orderId
    OrderNumber = orderNumber
    CustomerId = customerId
    Total = total
    Status = "Pending"

  from OrderShipped
    Status = "Shipped"
    ShippedAt = $eventContext.occurred

  join Customer on CustomerId
    events CustomerCreated, CustomerUpdated
    CustomerName = name

  children items identified by lineNumber
    every
      UpdatedAt = $eventContext.occurred

    from LineItemAdded key lineNumber
      parent orderId
      ProductId = productId
      Quantity = quantity
      UnitPrice = price

    remove with LineItemRemoved key lineNumber
      parent orderId

  remove with OrderCancelled
```pdl

This projection uses:
- Projection declaration
- Every block with exclude children at the projection level
- Multiple from blocks with keys
- Join block with multiple events
- Children block with:
  - Child every block for common mappings across all child events
  - Nested from and remove blocks
- Projection-level removal

## See Also

- [Expressions](expressions.md) - Understanding expression syntax
- All other topic pages for specific features described in the grammar
