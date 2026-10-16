<!-- cratis-ai-managed: skills/cratis-screenplay-projections/references/pdl-grammar.md -->
# PDL — syntax grammar, meaning and diagnostics

## Contents

- Grammar
- Parser regexes
- Diagnostics
- Syntax that parses but means something else
- Worked examples

Checked against `Documentation/screenplay/projections/{grammar,semantic-model,keys,variants}.md`
and `Source/DotNET/Screenplay/Parsing/ProjectionParser.cs` at Screenplay tag
`v4.31.0` (commit `355dffb`); the header `description` and the dynamic dictionary-key target
(`Target = ["@"] Path [DynamicKey]`, `DynamicKey = "." "$eventContext" "." EventContextPath`) were added in
later releases and re-read.

The grammar says what **parses**. It does not say what an event does at runtime:
Chronicle's lowering decides that (Screenplay decision 0001), and the
[meaning table](#syntax-that-parses-but-means-something-else) lists the places
where the two differ. The braces on a composite key are optional, and
`EveryBlock` accepts a bare `automap` as well as `no automap`.

## Grammar

```ebnf
Projection      = "projection", Ident, [ "=>", TypeRef ], NL,
                  [ INDENT, { ProjDirective | Block }, DEDENT ] ;

ProjDirective   = "no", "automap", NL
                | "sequence", Ident, NL
                | "file", FilePath, NL
                | KeyDecl
                | CompositeKeyDecl ;

Block           = EveryBlock | FromAllBlock | FromEventBlock | JoinBlock
                | ChildrenBlock | NestedBlock | RemoveWithBlock | RemoveWithJoinBlock
                | VariantBlock ;

VariantBlock    = "variant", Ident, NL,
                  INDENT, EntersOnDecl, { EntersOnDecl }, { ProjDirective | Block }, DEDENT ;
EntersOnDecl    = "enters", "on", TypeRef, [ "key", Expr ], NL ;

(* A projection-level KeyDecl parses but routes no event (PLAY0381). *)

EveryBlock      = "every", NL, INDENT,
                    [ "automap" | "no", "automap", NL ], { MappingLine },
                    [ "exclude", "children", NL ], DEDENT ;

FromAllBlock    = "all", NL,
                  [ INDENT, [ "automap" | "no", "automap", NL ], { MappingLine }, DEDENT ] ;

FromEventBlock  = "from", EventSpec, { ",", EventSpec }, NL,
                  [ INDENT, [ ParentDecl ], { MappingLine | KeyDecl | CompositeKeyDecl }, DEDENT ] ;
EventSpec       = TypeRef, [ "key", Expr ] ;

JoinBlock       = "join", Ident, "on", Ident, NL, INDENT, { WithEventBlock }, DEDENT ;
WithEventBlock  = "with", TypeRef, NL,
                  [ INDENT, [ "automap" | "no", "automap", NL ], { MappingLine }, DEDENT ] ;

ChildrenBlock   = "children", Ident, "identified", "by", Expr, NL,
                  INDENT, [ "no", "automap", NL ], { ChildBlock }, DEDENT ;
ChildBlock      = ChildEveryBlock | FromEventBlock | JoinBlock | RemoveWithBlock
                | RemoveWithJoinBlock | ChildrenBlock | NestedBlock | ClearWithBlock ;
ChildEveryBlock = "every", NL, INDENT, [ "no", "automap", NL ], { MappingLine }, DEDENT ;

NestedBlock     = "nested", Ident, NL,
                  INDENT, [ "automap" | "no", "automap", NL ],
                  { ProjDirective | Block | NestedBlock | ClearWithBlock }, DEDENT ;
ClearWithBlock  = "clear", "with", TypeRef, NL ;

RemoveWithBlock = "remove", "with", TypeRef, [ "key", Expr ], NL,
                  [ INDENT, [ ParentDecl ], DEDENT ] ;
RemoveWithJoinBlock = "remove", "via", "join", "on", TypeRef, [ "key", Expr ], NL ;

KeyDecl         = "key", Expr, NL ;
CompositeKeyDecl= "key", TypeRef, [ "{" ], NL,
                  INDENT, KeyPart, { NL, KeyPart }, DEDENT, [ "}", NL ] ;
KeyPart         = Ident, "=", Expr ;

MappingLine     = Assignment | ClearLine | IncLine | DecLine | CountLine | AddLine | SubLine ;
Assignment      = Ident, "=", Expr, NL ;
ClearLine       = "clear",     Path, NL ;
IncLine         = "increment", Ident, NL ;
DecLine         = "decrement", Ident, NL ;
CountLine       = "count",     Ident, NL ;
AddLine         = "add",       Ident, "by", Expr, NL ;
SubLine         = "subtract",  Ident, "by", Expr, NL ;

Expr            = Template | Literal | DollarExpr | Path ;
DollarExpr      = "$eventSourceId" | "$eventContext", ".", Ident | "$causedBy", ".", Ident ;
Template        = "`", { TemplateChar | "${", Expr, "}" }, "`" ;
Literal         = BoolLiteral | StringLiteral | NumberLiteral | NullLiteral ;
LiteralKeyword  = "literal", " ", Literal ;   (* parsed at the Expr level *)
```

## Parser regexes

| Construct | Regex |
| --- | --- |
| Projection header | `^projection\s+(@?[\w.]+)\s*(?:=>\s*([\w.]+))?$` |
| Event spec | `^(@?[\w.]+)(?:\s+key\s+(.+))?$` |
| Join | `^join\s+(@?[\w.]+)\s+on\s+(@?[\w.]+)$` |
| With | `^with\s+(@?[\w.]+)$` |
| Children | `^children\s+(@?[\w.]+)\s+identified\s+by\s+(.+)$` |
| Nested | `^nested\s+(@?[\w.]+)$` |
| Remove with | `^remove\s+with\s+(@?[\w.]+)(?:\s+key\s+(.+))?$` |
| Remove via join | `^remove\s+via\s+join\s+on\s+(@?[\w.]+)(?:\s+key\s+(.+))?$` |
| Clear with | `^clear\s+with\s+(@?[\w.]+)$` |
| Variant | `^variant\s+(@?[\w.]+)\s*$` |
| Enters on | `^enters\s+on\s+(@?[\w.]+)(?:\s+key\s+(.+))?$` |
| Counters | `^(increment\|decrement\|count\|clear)\s+(@?[$\w.]+)$` |
| Arithmetic | `^(add\|subtract)\s+(@?[$\w.]+)\s+by\s+(.+)$` |
| Assignment | `^(@?[$\w.@]+)\s*=(?!=\|>)\s*(.+)$` |

## Diagnostics

| Code | Meaning |
| --- | --- |
| `PLAY0054` | A projection document's top-level line does not open a `projection` |
| `PLAY0055` | A projection document declares no projection |
| `PLAY0056` | A `projection` line is not `projection <Name> [=> <ReadModel>]` |
| `PLAY0057` | A projection declares no directives, so it builds nothing |
| `PLAY0058` | A projection body line opens with an unknown word |
| `PLAY0059` | A projection declares more than one key |
| `PLAY0060` | A `from` block declares more than one key |
| `PLAY0061` | A `from` line names no event |
| `PLAY0062` | An event reference is not a readable name |
| `PLAY0063` | A `join` line is not `join <property> on <key>` |
| `PLAY0064` | A join block holds a line that is not `with <EventType>` |
| `PLAY0065` | A `children` line is malformed |
| `PLAY0066` | A `nested` line is not `nested <property>` |
| `PLAY0067` | A nested block reads from no event, so nothing ever fills it |
| `PLAY0068` | A `remove` line is neither `remove with` nor `remove via join on` |
| `PLAY0069` | A remove block holds a line other than `parent` |
| `PLAY0070` | A `clear` line is not `clear with <EventType>` |
| `PLAY0071` | `clear with` is written where there is nothing to clear |
| `PLAY0072` | A composite key part is not `<property> = <expression>` |
| `PLAY0073` | A composite key part is a template expression, which a key cannot be |
| `PLAY0074` | A composite key declares no parts |
| `PLAY0075` | A projection mapping line is unreadable |
| `PLAY0186` | A `readmodel` line is not `readmodel <Name>` |
| `PLAY0187` | A `reducer` line is not `reducer <Name> => <ReadModel>` |
| `PLAY0188` | A reducer body line is not `on <EventType>` |
| `PLAY0189` | A reducer declares no rules |
| `PLAY0190` | A reducer rule body opens with an unknown word |
| `PLAY0191` | A read model is built by more than one projection or reducer |
| `PLAY0192` | A read model is declared more than once |
| `PLAY0295`–`PLAY0299` | An `$eventContext` path the event-context catalog does not have; `PLAY0299` also warns when a dynamic dictionary key uses a `$` source other than `$eventContext` |
| `PLAY0380` | Chronicle drops part of a construct: `all` below the top level, or `automap` on a joined event |
| `PLAY0381` | A projection-level `key` routes no event |
| `PLAY0382`–`PLAY0385` | Variant errors: no `enters on`, shared mapping missing on a variant, duplicate variant name, entering event claimed twice |
| `PLAY0397` | Inline code uses a language line instead of a tagged fence |
| `PLAY0398` / `PLAY0399` | A reducer mixes bodied and body-less rules / observes one event twice |

## Syntax that parses but means something else

| Syntax | What Chronicle and the executable model do |
| --- | --- |
| `key` on the projection | Nothing: events route by the `from` key, then the event source id (`PLAY0381`) |
| `from X` with no key | Routes by the event source id, never by another `from`'s key |
| `join … on …` | Updates existing instances only; never creates one; the name after `join` is discarded |
| `automap` under a joined event | Replaced by the auto-map of the join's level (`PLAY0380`) |
| `all` inside `children`/`nested` | Behaves as `every` (`PLAY0380`) |
| `$causedBy.<x>` | Does not bind; use `$eventContext.causedBy.<x>` |
| Templates in mappings or keys | Do not bind |
| A number or Boolean literal key | Read as a property path; write `key literal "…"` |
| `parent` outside `children` | Ignored by Chronicle; does not bind |
| `sequence` | Realization concern; does not bind |
| Reducer rules | Opaque ESM v3 code keyed by the event source id; not computed by the reference runner |
| `$eventContext.<path>` other than `eventSourceId` in a mapping or key | Binds; the reference execution plan refuses it, so no specification in the model runs there |
| `remove via join` on a projection's own level | Chronicle's engine wires it as a child removal; binds, but the reference execution plan refuses it |
| `all` beside removals, `children` or `nested` | Binds, but the reference execution plan refuses it |
| `join`, `children` or `remove via join` inside `nested` | Not wired by Chronicle's engine; binds, but the reference execution plan refuses it |

## Worked examples

Each example is an excerpt: the events and read models it names are declared
elsewhere in the slice or model. Read-model properties in a `.play` model are
camelCase (a PascalCase property line is `PLAY0016`), so mapping targets are too.
Several examples map `$eventContext.occurred`, `sequenceNumber` or
`causedBy.subject`. Those bind, but the reference runner cannot execute a model
that contains them; a target must run their specifications.

### Composite key with event context

```screenplay excerpt
projection LineItems => LineItemReadModel
  from LineItemAdded
    key LineItemKey
      orderId        = orderId
      lineNumber     = lineNumber
      sequenceNumber = $eventContext.sequenceNumber
      createdBy      = $eventContext.causedBy.subject
    product = productName
```

### Global counter with a literal key

```screenplay excerpt
projection SiteStats => SiteStatsReadModel
  from UserLoggedIn key literal "site-stats"
    count totalLogins
    lastLogin = $eventContext.occurred
```

Every `UserLoggedIn` updates the same instance, whatever its event source.

### System-wide audit with `all` alongside `from`

```screenplay excerpt
projection ActivityFeed => ActivityFeedModel
  all
    count totalSystemEvents
    lastActivity = $eventContext.occurred
  from UserRegistered
    recentUsers = name
```

`all` subscribes to **every** event type in the system; `recentUsers` only fills
on `UserRegistered`. Chronicle keys the event types that only `all`
reaches by their **event source id**, so each event source gets its own instance
and its own count; `all` takes no key of its own.

### Children with a join and scoped removal

```screenplay excerpt
projection Group => GroupReadModel
  from GroupCreated
    name = name
  children members identified by userId
    from UserAddedToGroup key userId
      parent groupId
      role = role
    join userName on userId
      with UserCreated
        userName = name
    remove with UserRemovedFromGroup key userId
      parent groupId
```

### `every` that ignores child activity

```screenplay excerpt
projection Group => GroupReadModel
  every
    lastActivity = $eventContext.occurred
    exclude children
  from GroupCreated
    name = name
  children members identified by userId
    from UserAdded key userId
      parent groupId
      name = userName
```

Group-level events bump `lastActivity`; member events do not.

### Nested nullable object

```screenplay excerpt
projection Slice => SliceReadModel
  from SliceCreated
    name = name
  nested command
    from CommandSetForSlice
      name   = commandName
      schema = schema
    from CommandRenamed
      name = newName
    clear with CommandClearedForSlice
```

`command` is null until `CommandSetForSlice`, updated in place by
`CommandRenamed`, and set back to null by `CommandClearedForSlice`.

### Several projections in one slice

```screenplay excerpt
slice StateView CustomerPortalReport
  readmodel PortalReportReadModel
    invitedAt DateTime
  readmodel RevokedPortalTokenReadModel
    revokedAt DateTime
  query GetPortalReport => PortalReportReadModel
    by customerId CustomerId
  projection PortalReport => PortalReportReadModel
    from PortalInvitationSent
      invitedAt = $eventContext.occurred
  projection RevokedPortalToken => RevokedPortalTokenReadModel
    from PortalTokenRevoked
      revokedAt = $eventContext.occurred
```

Two read models, one behavior. Splitting them across two slices would say the
system has two behaviors where it has one.
