# The Value Contract

Every Chronicle client — C#, TypeScript, Kotlin, Java, Elixir — writes and reads the same values over the
same wire. The kernel does not adapt to the client that happens to be calling it, so the client is what has
to agree.

This page is that agreement. It is deliberately short: the contract is small, and the reason it is written
down is that until now it was not, which is how a client comes to diverge from it without anyone noticing.

## What has to agree

Only what crosses the wire. That is a much smaller set than what a client library contains — dependency
injection, task scheduling, reactive plumbing and type discovery are host concerns that never leave the
process, and no two clients should be expected to solve them the same way.

### Values

| Value | Form on the wire | Example |
| ----- | ---------------- | ------- |
| Guid / UUID | canonical string | `4db8c897-b236-43a7-9d16-ea95a0742e03` |
| Date, no time | ISO-8601 `yyyy-MM-dd` | `2026-08-06` |
| Time, no date | `HH:mm`, `HH:mm:ss` or `HH:mm:ss.fffffff` | `14:30:00` |
| Duration | `[-][d.]hh:mm:ss[.fffffff]` | `1.02:30:00` |
| Point, LineString, Polygon | GeoJSON | `{ "type": "Point", "coordinates": [10.7, 59.9] }` |

The duration form is .NET's, not ISO-8601. That is a deliberate choice the other clients inherit rather than
a leak — every client has to parse and emit it, whatever its own language would prefer.

### Concepts

A concept — a strongly-typed wrapper over a primitive — serializes as **the primitive it wraps**, with no
envelope. `CustomerId("4db8c897-…")` is written as the string, not as `{ "value": "4db8c897-…" }`.

This is what makes a concept free at the boundary: adding one to a domain model does not change a single
byte of what the kernel stores or what another client reads.

Whether a language expresses concepts through a generic base class (C#), an inline value class (Kotlin), or
a plain wrapper is a matter for that language. The serialized form is not.

### Derived types

When a property is declared as a base type and holds a derived one, the concrete type is named by a
`_derivedTypeId` property carrying that type's declared identifier.

Both stacks that implement it today agree on the name — `DerivedTypeJsonConverter.DerivedTypeIdProperty` in
.NET and `JsonSerializer.DerivedTypeIdProperty` in TypeScript are both `"_derivedTypeId"`. A client adding
derived-type support uses that name.

> `_t` is **not** this. It is MongoDB's own discriminator, written by the kernel's storage layer, and no
> client ever sees it. The two are easy to confuse and mean different things.

### Event type schemas

An event type is registered with a JSON Schema describing its properties. Two things about it are contract
rather than convenience:

- **Property names are verbatim.** The kernel matches a schema property to a payload key by exact name
  first, then case-insensitively. A client that renames properties on the way out — camel-casing them, say —
  must apply the same naming to both the schema and the payload, or the two stop agreeing.
- **A schema with no properties is not the same as no schema.** The converter falls back to raw conversion
  when a schema declares nothing, so an empty schema does not corrupt reads — but the event type registers
  with no properties, and every tool that reads the registry shows it as empty. Registering `{}` is a bug,
  not a degraded mode.

## What deliberately does not have to agree

- **How a client discovers artifacts.** Attribute scanning, annotation processing, explicit registration —
  all fine. The kernel sees the result, not the mechanism.
- **How a client models identity, options, logging, or lifetime.** Idiomatic beats uniform.
- **Whether a client implements a capability at all.** A client may lag; see
  [Contributing to Clients](./index.mdx) for how an unsupported capability is stated so the claim cannot
  rot.

## Why there is no per-language Fundamentals package

Chronicle's `Cratis.Fundamentals` exists for .NET, and `@cratis/fundamentals` for TypeScript. There is no
Kotlin or Elixir equivalent, and that is a decision rather than an omission.

The two that exist are not the same library in two languages. The .NET one carries nineteen areas, most of
them host concerns; the TypeScript one carries about six, and they are exactly the ones on this page. The
TypeScript package is not a port — it is this contract, expressed once for a stack that also needed it.

A third would be justified by a second consumer in that language. Today `Chronicle.Kotlin` is the only
Kotlin consumer and there is no Arc for the JVM, so a shared package would have one caller, one release
cadence to keep in step, and no second implementation to keep honest. Kotlin's inline value classes already
give it concepts more cheaply than a library could.

The rule: **extract when a second consumer appears in that language, not before.** Until then a client keeps
its own implementation, internal to itself, and conforms to this page.

That is only safe if conformance is checked rather than assumed, which is what this page exists to make
possible — and what a conformance suite, run by each client against a real kernel, would make automatic.
