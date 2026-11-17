<!-- cratis-ai-managed: skills/cratis-screenplay-projections/references/reducers.md -->
# Reducer details

Read this when writing a reducer body that needs the read model's key, or when checking what the
executable model and the reference runner do with a reducer.

## Reading the key as a typed value

`context.Key` is text. Read it as the read model's key type with `context.KeyAs<T>()`
instead of parsing it: `T` is `string`, `Guid` (UUID text in N, D, B or P form, without
whitespace) or a key concept with one public constructor taking a `string` or `Guid`.

```csharp
var id = context.KeyAs<AccountId>();
return context.State is null
    ? new(id, context.Event.amount)
    : context.State with { balance = context.State.balance + context.Event.amount };
```

Malformed UUID text and an unsupported `T` throw `ContextKeyNotConvertible` (the raw key is
not in the message); a concept's own validation exception propagates unchanged. The compiler
accepts `KeyAs<T>()` only for a read model with one required, non-collection Uuid or Text key;
a composite key and a reducer that targets an event keep reading `Key`. This is executable-model
support: Stage does not yet generate `KeyAs<T>()` on typed reducer contexts, so a rendered reducer
that needs it is a gap to fill by hand.
Requires Screenplay 5.2.0 (`screenplay --version`); an older tool has no `KeyAs<T>()`.

## What the executable model does (ESM v3)

- Every `on` rule needs a body, inline or `file`. Mixing bodied and body-less
  rules is `PLAY0398`; two rules for one event is `PLAY0399`; a reducer with no
  bodies is rejected with a hint to write a projection.
- The key is **always the event source id** — a reducer cannot route by an event
  property. State starts at null; returning null deletes the instance. A query
  `by` another identity type warns `PLAY0690`; use a projection keyed on each `from`
  or a join ([reducer identity](reducer-identity.md)).
- The body is an opaque attachment identified by a content hash, never compiled
  or run by Screenplay. The reference runner cannot compute reducer state, so a
  specification that reads a reducer-built read model reports unsupported and
  never passes. A target must supply the transition. Stage admits only pure reducer bodies
  (Roslyn allowlist, `STAGE-ESM-019`/`-022`); an impure body is gap-fill code, and the model stays the contract.
