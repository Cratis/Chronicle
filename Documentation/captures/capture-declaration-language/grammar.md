---
description: The formal EBNF grammar of the Capture Declaration Language (CDL) for Chronicle captures, with the compiler's semantic validation rules.
---
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

SourceBlock      = EventsSource
                 | "source", SourceType, NL,
                   INDENT,
                     { SourceProperty },
                   DEDENT ;

SourceType       = "api" | "webhook" | "message" ;

SourceProperty   = ("api" | "route" | "poll" | "path" | "topic"), LineValue, NL ;

EventsSource     = "source", "events", NL,
                   INDENT,
                     FromEvent, { FromEvent },
                   DEDENT ;

FromEvent        = "from", LineValue, NL ;

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
- Every `from` event in an events source must be registered as public with the same nonempty origin; Chronicle derives the inbox from that origin

Legacy `sequence inbox-<event store>` lines in an events source are deprecated but still accepted, with a deprecation warning. The named inbox must match every event's origin. They are omitted from the grammar for new declarations above.
