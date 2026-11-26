<!-- cratis-ai-managed: skills/cratis-screenplay-projections/references/dictionaries-and-ordering.md -->
# Set once, dictionary keys and children ordering

Read this when a projection must keep the first value of a property, write one entry per key into a dictionary, or keep children in a chosen order.
Requires Screenplay 6.9.0 or later (`screenplay --version`); a model that uses `set once` or an ordered `children` block selects ESM v43.

## Set once

`set once <property> = <expression>` (or `to`) assigns only while the property is missing or null; later events leave it. It is replay-stable and works in `from`, `every`, `all` and `with` blocks. A property named `once` is written `set @once = ...`.

```screenplay excerpt
from ModelDefined
  name = name
  set once createdBy = $causedBy.subject
  set once createdAt = $eventContext.occurred
```

Choose it over a reducer when the only state needed is the first value ("created by", "created at").

## Dictionary keys

A target may end in a dynamic key: `<property>.$eventContent.<path>`, `.$eventContext.<path>` or `.$causedBy.subject|name|userName`. The value the key reads becomes the dictionary key. Assignment, `set once`, `increment`, `decrement`, `count`, `add`, `subtract` and `remove` all accept one.

```screenplay excerpt
from SlotAssigned
  slots.$eventContent.slot = series
  count assignmentsByUser.$causedBy.userName
from SlotDropped
  remove slots.$eventContent.slot
```

- `remove <target>` deletes the entry; the target must end in a dynamic key (`PLAY0956`). Removing an absent key does nothing. Escape a property named `with` or `via`: `remove @with.$eventContent.slot`.
- `clear <target>` with a dynamic key is an error (`PLAY0963`); write `remove`.
- Screenplay has no dictionary type, so a dynamic key on a property the read model declares is a warning (`PLAY0962`). Dictionary entries are for projections whose read model lives in application code. The reference evaluator does not run them and reports `PLAY0268` for those lines.
- Prefer `$causedBy.subject` for identity keys; the other members are not checked for characters such as `.`.

## Children ordering

One ordering line directly inside a `children` block: `insert last` (the default), `insert first`, or `sort by <property> [ascending|descending]`.

```screenplay excerpt
children lineItems identified by lineNumber
  sort by quantity descending
  from InvoiceLineItemAdded key lineNumber
    parent invoiceId
    quantity = quantity
```

- The order is stored and applied when a child is added; updating a child never moves it. The sort is stable, a missing value sorts first ascending and last descending, and a dotted path is allowed.
- Two ordering lines in one block are `PLAY0959`; an ordering line outside `children`, with indented lines or malformed is `PLAY0958`; a `sort by` property the child type does not declare is a warning (`PLAY0960`).

## Diagnostics

| Code | Severity | Meaning |
| --- | --- | --- |
| `PLAY0956` | Error | `remove <target>` without a dynamic key |
| `PLAY0957` | Error | `.$eventContent` without a path, or a `.$causedBy` member other than `subject`, `name`, `userName` |
| `PLAY0958` | Error | Malformed ordering line, one with indented lines, or one outside `children` |
| `PLAY0959` | Error | More than one ordering line in a `children` block |
| `PLAY0960` | Warning | `sort by` names a property the child type does not declare |
| `PLAY0961` | Warning | `.$eventContent` path the handled event does not have |
| `PLAY0962` | Warning | Dynamic key on a property the read model declares |
| `PLAY0963` | Error | `clear` with a dynamic key |

The .NET fluent and model-bound forms are in [dictionary keys, set once and ordering](../../cratis-chronicle-projection/references/dictionaries-set-once-ordering.md).
