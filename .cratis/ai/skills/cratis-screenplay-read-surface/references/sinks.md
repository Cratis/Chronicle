<!-- cratis-ai-managed: skills/cratis-screenplay-read-surface/references/sinks.md -->
# Where a read model is stored

## Contents

- Default and named sinks
- Choose a sink
- A property named `sink`
- Passive read models have no sink
- What the deployment and the compiler do with it
- Diagnostics

Requires Screenplay 6.4.0 (`screenplay --version`); older compilers do not know sinks and read
`sink <Name>` in a read model as a property.

## Default and named sinks

A sink is where a read model or a graph is stored. Every application has two implicit sinks, one
for read models and one for graphs, and anything that names no sink uses the default for its kind.
Never declare a default sink: a model with no `sink` lines keeps its meaning.

Declare a **named** sink only when part of the state belongs somewhere else, such as reporting read
models in a separate database. Declare each one once, at the application root (beside `concept` and
`module`, never inside one), with the kind it stores: `sink <Name> for readmodels` or
`sink <Name> for graphs`. Names are unique across the application, compared ignoring case.

The model never names a database, an engine or a connection. The deployment binds each sink name
to storage, so moving reporting read models to another database changes the deployment, not the
model.

## Choose a sink

A bare `sink <Name>` line in the read model body chooses it, the same way `passive` is a bare
directive; it may sit anywhere among the body's directives and properties. The model below is
complete:

```screenplay
sink Reporting for readmodels

concept CustomerId : Uuid
concept Money : Decimal

module Sales
  feature Revenue
    slice StateView CustomerRevenue
      event OrderPlaced
        customerId CustomerId
        amount     Money

      readmodel CustomerRevenue
        sink Reporting
        customerId CustomerId key
        total      Money

      projection CustomerRevenueProjection => CustomerRevenue
        from OrderPlaced key customerId
          add total by amount

      query RevenueForCustomer => CustomerRevenue optional
        by customerId CustomerId
```

Nothing else names a sink. The projection writes to `Reporting` because its target is stored
there, and the query reads from `Reporting` because its read model is. Do not add a sink to a
projection, reducer or query: what builds or reads a read model follows the read model's sink.

## A property named `sink`

A read model has always been able to declare a property called `sink`, so the line is ambiguous on
its own. `sink <Name>` is the **directive** only when `<Name>` is a sink the application declares,
spelled exactly as declared, in any file of the model (a declaration in another file counts once the
files are merged). Otherwise it is a **property** named `sink` of type `<Name>`, as it always was.

- `sink Warehouse` with no sink called `Warehouse` declares a property `sink` of type `Warehouse`.
- `@sink <Type>` is always the property, even when a sink has that name. Write it that way when a
  property's type shares a sink's name; the printer writes it that way too.
- A collection (`sink Reporting[]`), an optional type or a modifier also makes the line a property.

```screenplay
sink Reporting for readmodels

concept OrderId : Uuid
concept Warehouse : String
concept Reporting : String

module Shipping
  feature Dispatch
    slice StateView Shipments
      readmodel Shipment
        sink Reporting
        orderId OrderId key

      readmodel Dispatch
        orderId OrderId key
        sink    Warehouse

      readmodel Audit
        orderId OrderId key
        @sink   Reporting
```

`Shipment` is stored in `Reporting`. `Dispatch` uses the default sink and has a property `sink` of
type `Warehouse`, because no sink is called `Warehouse`. `Audit` uses the default sink and has a
property `sink` of type `Reporting`.

A property type that differs from a declared sink only in case keeps its meaning but warns, because
the author probably meant the directive:

```screenplay expect PLAY0939
sink Reporting for readmodels

concept OrderId : Uuid
concept reporting : String

module Shipping
  feature Dispatch
    slice StateView Shipments
      readmodel Shipment
        sink Reporting
        orderId OrderId key

      readmodel Audit
        orderId OrderId key
        sink reporting
```

Fix `PLAY0939` by spelling the sink exactly (`sink Reporting`) when the directive was meant, or by
writing `@sink reporting` when the property was.

## Passive read models have no sink

A `passive` read model is never materialized: it is replayed for command decisions, so it has
nowhere to be stored. Naming a sink on it is error `PLAY0935`; remove the `sink` line, or remove
`passive` when the read model should be stored.

```screenplay excerpt
readmodel PendingShipment
  passive
  sink Reporting
  orderId OrderId key
```

## What the deployment and the compiler do with it

A sink is realization metadata, not part of the executable semantic model (ESM). Adding, removing
or changing a `sink` line leaves the ESM bytes and its revision unchanged, and the compiler reports
each one as information `PLAY0270`. A renderer that keys on the ESM revision alone misses a sink
change: see `cratis-screenplay-render-and-gap-fill`.

In Chronicle the same choice is `[Sink("name")]` on the read model type and a `sinks` entry in the
Kernel configuration (`cratis-chronicle-read-model`, `cratis-chronicle-hosting`).

## Diagnostics

| Code | Severity | Cause | Fix |
| --- | --- | --- | --- |
| `PLAY0931` | Error | A malformed sink declaration or graph `sink` line, or lines under a declaration | `sink <Name> for readmodels\|graphs` at the root; `sink <Name>` in a body |
| `PLAY0932` | Error | Two sinks share a name, ignoring case | Rename one |
| `PLAY0933` | Error | A graph names an undeclared sink | Declare it `for graphs`, or fix the name |
| `PLAY0934` | Error | A read model names a `for graphs` sink, or a graph a `for readmodels` sink | Name a sink of the right kind |
| `PLAY0935` | Error | A `passive` read model names a sink | Remove the `sink` line or `passive` |
| `PLAY0936` | Error | A read model or graph names two different sinks | Keep one |
| `PLAY0937` | Warning | The same `sink` line repeated | Remove the repeat |
| `PLAY0938` | Warning | A declared sink nothing uses | Use it or remove the declaration |
| `PLAY0939` | Warning | A property `sink` whose type differs only in case from a declared sink | Spell the sink exactly, or write `@sink <Type>` |

A read model's `sink <Name>` naming no declared sink is never `PLAY0933`: it is a property.
