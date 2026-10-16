<!-- cratis-ai-managed: skills/cratis-screenplay-projections/references/joins.md -->
# Joins and arithmetic

This reference holds the join excerpt and rules and the arithmetic mapping lines.

## Joins

Excerpt, inside a `projection` whose read model has `customerId` and
`customerName`:

```screenplay excerpt
join customer on customerId
  with CustomerCreated
    customerName = name
  with CustomerUpdated
    customerName = name
```

`join <label> on <property>` (error `PLAY0063` otherwise), then one or more
`with <EventType>` blocks (error `PLAY0064` otherwise). **A join never creates an
instance**: a joined event updates every existing instance whose `on` property
equals the joined event's event source id. Chronicle discards the label, but
inside `children` and `nested` the compiler's completeness check (`PLAY0284`)
counts the label as the field the join fills, so name it after that field there.
Joins work at a projection's own level and inside `children`. Inside `nested`,
Chronicle's engine does not wire a join, a `children` block or a
`remove via join`; they bind, but the reference execution plan refuses them.
A join cannot declare its own key or trigger removal — that is
`remove via join on`.

## Arithmetic

| Line | Effect |
| --- | --- |
| `count <prop>` | +1, expressing *counting occurrences* |
| `increment <prop>` | +1, expressing *modifying state* |
| `decrement <prop>` | −1 |
| `add <prop> by <expr>` | += expression |
| `subtract <prop> by <expr>` | −= expression |

`count` and `increment` behave identically; the difference is intent. The target
must be numeric.
