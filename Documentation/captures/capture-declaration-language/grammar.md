# Grammar (EBNF)

This document defines the formal grammar for the Capture Declaration Language (CDL).

## EBNF notation

| Notation | Meaning |
| -------- | ------- |
| `=` | Definition |
| `;` | End of rule |
| `{ ... }` | Zero or more |
| `[ ... ]` | Optional |
| `(...)` | Grouping |
| `\|` | Alternative |
| `"..."` | Literal token |

## Complete grammar

```ebnf
Document         = { Capture } ;

Capture          = "capture", Path, NL,
                   [ INDENT,
                     { Directive },
                   DEDENT ] ;

Directive        = SourceBlock
                 | KeyDirective
                 | MapBlock
                 | AppendBlock
                 | NestedBlock
                 | ChildrenBlock ;

SourceBlock      = "source", SourceType, NL,
                   INDENT,
                     { SourceProperty },
                   DEDENT ;

SourceType       = "api" | "webhook" | "message" ;

SourceProperty   = ("api" | "route" | "poll" | "auth" | "path" | "topic"), LineValue, NL ;

KeyDirective     = "key", LineValue, NL ;

MapBlock         = "map", NL,
                   INDENT,
                     { MapOperation },
                   DEDENT ;

MapOperation     = SplitOperation
                 | RenameOrTemplateOrTranslate ;

RenameOrTemplateOrTranslate
                 = Identifier, "=", LineValue, [ "translate", NL,
                     INDENT,
                       { TranslateEntry },
                     DEDENT
                   ], NL ;

TranslateEntry   = SimpleValue, "=>", SimpleValue, NL ;

SplitOperation   = "split", Path, "by", StringLiteral, NL,
                   INDENT,
                     { Path, NL },
                   DEDENT ;

AppendBlock      = "append", Path, NL,
                   INDENT,
                     "when", WhenExpression, NL,
                     { Assignment },
                   DEDENT ;

WhenExpression   = "added"
                 | "removed"
                 | TemplateLiteral
                 | Path, [ "from", SimpleValue, "to", SimpleValue
                         | { "or", Path }
                         | { "and", Path } ] ;

Assignment       = Identifier, "=", LineValue, NL ;

NestedBlock      = "nested", Path, NL,
                   INDENT,
                     [ MapBlock ],
                     { AppendBlock },
                   DEDENT ;

ChildrenBlock    = "children", Path, "identified", "by", Path, NL,
                   INDENT,
                     [ MapBlock ],
                     { AppendBlock },
                   DEDENT ;

Path             = Identifier, { ".", Identifier } ;
LineValue        = { TokenExceptNewline } ;
SimpleValue      = StringLiteral | NumberLiteral | "true" | "false" | "null" | Identifier ;
Identifier       = Letter, { Letter | Digit | "_" } ;
```

## Semantic constraints

The parser grammar is complemented by compiler validation rules:

- Exactly one `source` block is required
- Exactly one `key` directive is required
- At most one root `map` block is allowed
- Every `append` block must contain one `when` clause
