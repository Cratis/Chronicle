---
name: cratis-chronicle-compliance
description: Model personal data in Chronicle with [PII] and [Subject], and erase it through crypto-shredding with IPIIManager. Use when an event or read model carries data about a natural person, when a subject must be identified for erasure, when a right-to-erasure request must be executed, or when redacting a stored event. Do not use for authorization, authentication, secret handling, or generic data-protection policy.
license: MIT
---
<!-- cratis-ai-managed: skills/cratis-chronicle-compliance/SKILL.md -->

# Chronicle compliance and personal data

Chronicle keeps events forever, so personal data cannot be deleted from the log.
Chronicle protects it by encrypting each marked value under a key owned by the
subject, and erases it by destroying that key. Decide the subject before you
decide the event shape — the subject is what erasure operates on.

## Verified product sources

This skill is verified against this exact public release:

| Package | Version | Purpose |
| --- | --- | --- |
| `Cratis.Chronicle` | `16.45.3` | `Cratis.Chronicle.Compliance.GDPR`, `Cratis.Chronicle.SubjectAttribute`, `IEventSequence.Redact`, `IReadModels.Release` |
| `Cratis.Chronicle.CodeAnalysis` | `16.45.3` | `CHR0026`, `CHR0034`, `CHR0035`, `CHR0043`, `CHR0046` |

Reverify product sources before claiming support for another version.

## Mark personal data with `[PII]`

`Cratis.Chronicle.Compliance.GDPR.PIIAttribute` marks a value as personal data.
It targets a class, a property, or a constructor parameter, and it is not
repeatable.

```csharp
using Cratis.Chronicle.Compliance;
using Cratis.Chronicle.Compliance.GDPR;

[EventType]
public record <PersonEventName>(
    [property: PII] string <PersonalPropertyName>,
    <NonPersonalType> <BusinessPropertyName>);
```

On a positional record the attribute must be written as `[property: PII]` or
placed on the constructor parameter; Chronicle looks for it on the property, the
property's declaring type, the property's own type, and the matching primary
constructor parameter.

Marking the concept type itself makes the value personal everywhere it appears,
which is usually what you want for an email, a phone number, or a personal name:

```csharp
[PII]
[ComplianceDetails("<why this value is collected and under what basis>")]
public record <ConceptName>(string Value) : ConceptAs<string>(Value);
```

`PIIAttribute` accepts an optional `details` string, **but Chronicle never reads
it.** The rationale that actually reaches the event schema comes from
`Cratis.Chronicle.Compliance.ComplianceDetailsAttribute`. Write `[PII]` for the
marker and `[ComplianceDetails("...")]` for the reason; do not write
`[PII("reason")]` and expect the reason to be recorded.

`PII` is the only compliance category Chronicle defines — there is exactly one
well-known `ComplianceMetadataType`. Chronicle has no `[Redact]`, `[Erase]`,
`[Anonymize]`, `[Encrypt]`, or `[NotAudited]` attribute; do not write one into
an example. Redaction is an API call, not an annotation.

Mark only genuinely personal values. Business metadata, identifiers of
non-persons, and amounts stay unmarked — every marked value costs an encryption
round trip and becomes unreadable the moment the subject is erased.

## Decide the subject

The subject is the natural person a value belongs to, and it is the unit of
erasure. `Cratis.Chronicle.SubjectAttribute` marks the value that supplies it.
The attribute is a pure marker with no arguments and targets a property or a
constructor parameter.

For an appended event, Chronicle uses the explicit subject when one is supplied
and otherwise falls back to the event source id. That default is the shape you
want: a person-per-stream model needs no `[Subject]` at all.

```csharp
[EventType]
public record <PersonEventName>(
    [property: Subject] <SubjectValueType> <SubjectPropertyName>,
    [property: PII] string <PersonalPropertyName>);
```

Add `[Subject]` only when the person is **not** the event source — for example
an event on an order stream that carries the customer's personal data. A
reactor can supply the subject for its side-effect events by implementing
`Cratis.Chronicle.Events.ICanProvideSubject`, whose single member is
`Subject GetSubject()`.

`Cratis.Chronicle.Subject` is `record Subject(string Value) : ConceptAs<string>`
with a `NotSet` value, an `IsSet` test, and implicit conversion from `string`,
`Guid`, and `EventSourceId`.

For a read model instance, Chronicle resolves the subject from the `[Subject]`
property that has a value, then the `[Subject]` constructor parameter, then a
property named `Id`. A stored document carries one default subject; keep one
person's personal data in one document rather than mixing several people into
one row.

### Do not put `[Subject]` or `[PII]` on the stream identity

An `EventSourceId<T>`-derived value already is both the key and the compliance
subject.

- `[Key]` or `[Subject]` on it is redundant — analyzer **CHR0026** (warning).
- `[PII]` on it is rejected — analyzer **CHR0034** (error), and at runtime
  `Cratis.Chronicle.Compliance.GDPR.PIINotSupportedOnEventSourceId`.

Chronicle cannot encrypt an event source id. When the natural identifier is
itself sensitive, use a random surrogate stream id and carry the sensitive value
as a `[PII]` property.

### Reserved read-model property names

Chronicle stamps `__subject` and `__subjects` onto stored read-model documents.
Analyzer **CHR0035** (error) rejects a `[ReadModel]` that declares `_subject`,
`__subject`, or `__subjects`. Rename the property.

## Erase a subject

`IEventStore.PII` exposes `Cratis.Chronicle.Compliance.GDPR.IPIIManager`:

```csharp
Task DeleteEncryptionKeyFor(EncryptionKeyIdentifier identifier);
Task AllowNewEncryptionKeyFor(EncryptionKeyIdentifier identifier);
```

`EncryptionKeyIdentifier` is a `ConceptAs<string>` with implicit conversion from
`string`, so the subject value can be passed directly:

```csharp
var eventStore = await chronicleClient.GetEventStore("<EventStoreName>");
await eventStore.PII.DeleteEncryptionKeyFor("<subject-value>");
```

What this does and does not do:

- It destroys the subject's key and records an **erasure fence** so a later
  write cannot resurrect a key for that subject. The fence is monotonic and is
  recorded before any key is deleted.
- The events stay in the log. Their `[PII]` values remain as unreadable
  ciphertext — this is crypto-shredding, not deletion.
- Reading an erased value yields an **empty string** rather than an exception,
  so queries and read models over an erased subject keep working. An erased
  container value comes back as an empty object or array.
- Appending a new `[PII]` value for an erased subject **fails** with
  `EncryptionKeyErased`. It does not quietly mint a new key. Call
  `AllowNewEncryptionKeyFor` deliberately if the subject must be re-enrolled.
- A partially completed erasure throws `EncryptionKeyErasureIncomplete`, which
  carries the individual failures. Treat it as unfinished and retry — the fence
  is already in place, so retrying is safe.

**Erasure is scoped to one namespace.** There is no cross-namespace erasure. In
a multi-tenant deployment, an erasure request that spans tenants is one call per
tenant namespace, and completing only some of them is an incomplete erasure.

## Redact a stored event

Erasure removes a subject's readability. Redaction removes a specific event's
content. `Cratis.Chronicle.EventSequences.IEventSequence` exposes both forms:

```csharp
Task Redact(EventSequenceNumber sequenceNumber, RedactionReason reason);
Task Redact(EventSourceId eventSourceId, RedactionReason reason, params Type[] clrEventTypes);
```

`RedactionReason` is a `ConceptAs<string>` with an `Unknown` value and implicit
conversion from `string`. Chronicle records the redaction itself as an
`EventRedacted` event, so the fact that a redaction happened stays auditable.

Use redaction for content that should never have been appended. Use key erasure
for a subject exercising a right to erasure. They are different mechanisms and
one does not imply the other.

## Read personal data back

The kernel decrypts on the paths it owns, so projections and observers see plain
values. When a read-model instance is fetched outside those paths, decrypt it
explicitly through `Cratis.Chronicle.ReadModels.IReadModels`:

```csharp
Task<TReadModel> Release<TReadModel>(TReadModel instance);
Task<IEnumerable<TReadModel>> Release<TReadModel>(IEnumerable<TReadModel> instances);
```

`Release` degrades quietly by design: it returns the instance unchanged when no
subject can be derived, when the type carries no compliance metadata, or when
decryption fails. It logs, it does not throw. Do not treat a value that came
back looking encrypted as proof the data is intact — check the subject
resolution first.

The asymmetry is deliberate and worth knowing: **applying** compliance on append
fails loudly, **releasing** it on read fails quietly.

## Setup

An ASP.NET Core host that calls `builder.AddCratisChronicle()` needs no
compliance setup — `AddCompliance()` is already called for you, and the raw
client path discovers the metadata providers by convention. The developer-facing
requirement is only to annotate the types.

Key storage is a hosting decision, not an application one. Chronicle ships
MongoDB (the default), SQL, in-memory, HashiCorp Vault, and Azure Key Vault key
storage, configured under `Cratis:Chronicle:Compliance:Encryption`.

> **Operational trap.** Switching to a dedicated compliance key store without
> setting `MigrateFromDefaultStorage` leaves every existing key unreachable.
> Every `[PII]` value then reads back as an empty string, with no exception and
> no log — byte-for-byte indistinguishable from a completed erasure. Verify the
> migration flag before switching stores, and confirm a known subject still
> reads after the switch.

## Modeling rules that follow from erasure

- Prefer **one subject per event stream** for person-level personal data. The
  default subject is then correct everywhere and erasure is one call.
- A key redirected away from the event's own stream can carry a `[PII]` value
  across a compliance subject boundary — analyzers **CHR0043** and **CHR0046**
  (warnings) flag that shape. Treat the warning as a modeling question, not
  noise: which person owns the resulting document?
- Bearer tokens, magic links, and signed URLs are secrets, not durable facts.
  Store a keyed hash or an opaque reference. Chronicle has no attribute that
  withholds a secret from the log.
- If a subject boundary cannot be made person-level without changing product
  behavior, stop and surface the trade-off before implementing.

## Verify

- Every value that identifies or describes a natural person carries `[PII]`;
  business metadata does not.
- Rationale is written with `[ComplianceDetails]`, not with a `[PII]` argument.
- The subject is explicit wherever the person is not the event source, and
  absent where the event source already is the person.
- No `[Key]`, `[Subject]`, or `[PII]` sits on an `EventSourceId<T>` value.
- No read model declares `_subject`, `__subject`, or `__subjects`.
- One stored read-model document holds one person's personal data.
- Erasure is executed per namespace, and an incomplete erasure is retried rather
  than reported as done.
- Reads that must show personal data outside the kernel's own paths call
  `Release`, and an unchanged instance is investigated rather than assumed.
- The build is clean with zero `CHR00xx` diagnostics outstanding, and the
  relevant specifications pass against the verified package version.
