<!-- cratis-ai-managed: skills/cratis-screenplay-projections/references/reducer-identity.md -->
# Reducer instance identity (PLAY0690)

A reducer instance is keyed by the **event-source id** of the event it reduces. A reducer cannot be re-keyed by a payload value, a read-model `key` or a stream partition. A view that must be looked up by anything other than that event-source identity needs a projection.

Requires Screenplay 4.170.0 or later (`screenplay --version`) for PLAY0690; an older compiler does not report it, so a clean run there does not clear the mismatch.

## What PLAY0690 reports

A warning (it fails `--warnaserror`) in two cases:

- A reducer-backed query returns one instance `by` a scalar whose nominal type differs from the identity type of an observed event whose producers are all known, for example `by month Month` while the events live on `ResourceId` streams.
- The producers of one reducer's events use several different nominal identity types, so no single identity can key the instances. One reducer warning replaces the repeated query warnings.

This example reports PLAY0690 on `ByMonth`: the instances follow `ResourceId`, not `Month`.

```screenplay expect PLAY0690
concept ResourceId : String
concept Month : String
module Work
  feature Reporting
    slice StateView Totals
      event Recorded
        month Month
        hours Decimal
      readmodel Totals
        month Month key
        hours Decimal
      reducer Fold => Totals
        on Recorded
          ```csharp
          return context.State is null
              ? new(context.Event.month, context.Event.hours)
              : context.State with { hours = context.State.hours + context.Event.hours };
          ```
      query ByMonth => Totals optional
        by month Month
    slice StateChange Record
      command Record
        resource ResourceId identifier
        month Month
        hours Decimal
        produces Recorded
          for resource
          month = month
          hours = hours
```

## Fix

To aggregate by a payload value, replace the reducer with a projection that declares a property key on each `from`. The read-model `key` and the `from` key then agree, and the warning is gone:

```screenplay
concept ResourceId : String
concept Month : String
module Work
  feature Reporting
    slice StateView Totals
      event Recorded
        month Month
        hours Decimal
      readmodel Totals
        month Month key
        hours Decimal
      projection Fold => Totals
        from Recorded key month
          add hours by hours
      query ByMonth => Totals optional
        by month Month
    slice StateChange Record
      command Record
        resource ResourceId identifier
        month Month
        hours Decimal
        produces Recorded
          for resource
          month = month
          hours = hours
```

When instances already exist under their own identity and another event only enriches them, use a [join](joins.md) instead: it updates the existing instances and never creates one. There is no automatic repair from a reducer to a projection.

## What the check compares

- It compares declared **nominal** identity types (`Month` and `ResourceId` differ even though both wrap text), never property names, primitive backing, stream ids or fixture values. Equal types do not prove equal runtime ids, and distinct concepts can hold equal values, so a finding is a likely modeling mistake rather than proof.
- It compares only where the producer is known: a command production with `for`, an implicit or generated command identifier, and a reaction production with an explicit `for`. All producers across the application count, after source and stream filters and production-route overrides.
- Not compared (unknown provenance): a plain `produces` without `for` (it lands on an identity the request allocates), a reaction without `for`, captures, seeds and code. An unknown producer suppresses the query comparison for that event.
- Collection queries, performer-served or unbuilt views, projection-backed views and public-event reducer targets do not trigger it.
- It is a native C# compiler diagnostic: the standalone tool, `cratis screenplay validate` (when its bundled compiler is new enough) and MCP diagnostics report it. The TypeScript compiler and editors only pass through findings the host supplies, so a clean editor is not evidence.
