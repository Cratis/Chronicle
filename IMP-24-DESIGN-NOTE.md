# IMP-24 — what this branch decided, and what it deliberately left open

This branch implements one half of what Ada's IMP-24 asks for and leaves the other half as a question,
because answering it means choosing a compliance policy Chronicle owns rather than filling in a gap the
repository already settles. Everything below is written for whoever picks the question up.

## What shipped

`[ReleaseUnder(nameof(SomeProperty))]` on a read model property. The release pass groups the read model's
properties by declared subject, releases each group under its own subject, and leaves every undeclared
property on the path it was already on — the read model's own subject, or nothing when none resolves.

Purely client-side. `Compliance.Release` already takes `(Subject, Schema, Payload)` per call, so a
multi-subject row is *n* calls over the existing contract; no proto, contract or kernel change, and no Arc
change (`ReadModelInterceptor<T>` calls `IReadModels.Release` and sees only its result). Wire-compatible in
both directions; a client that declares nothing issues exactly the request it issued before.

## Left open — the release-exempt marker

IMP-24 offers a second form: *"this slot is computed at the query edge and was never stored encrypted; leave
it alone."* It is not implemented, and the reason is not scope.

An exemption has to answer a question `[ReleaseUnder]` never raises: **does it also exempt the value from
being encrypted at rest?**

- **Release-only.** The registered schema still marks the property, so a *stored* read model encrypts it on
  the way in and then refuses to release it on the way out — the exact silent-ciphertext outcome IMP-24 is
  about, now spelled out in an attribute. A footgun.
- **Both sides.** The generated schema drops the marker, and Chronicle stores a `[PII]`-typed value in the
  clear on a consumer's say-so. `JsonComplianceManager` states the opposing rule directly: applying fails
  loudly because *"storing PII that was never protected is never acceptable."* Whether an attribute may
  override that is a policy call, not a design detail.
- **Scoped to unstored read models.** Coherent, but Chronicle cannot tell the two apart where it matters: a
  composed, shape-only read model is never registered (`ReadModels.Register()` walks projection and reducer
  handlers only), so there is no moment at which the distinction could be enforced.

There is also less left for it to do than when CHR-23 was filed. Its no-key half **is fixed in this tree** —
`PIICompliancePropertyValueHandler.Release` now asks `TryDecodeEncryptedValue` *above* the key lookup
(`:55-58`), so a never-encrypted value passes through whether or not the resolved subject owns a key. This
branch measures that: `and_the_declared_value_was_never_encrypted` returns a computed value untouched on a
row keyed by an identity that never minted a key. What an exemption would add on top is the ability to say
so rather than have it inferred from the bytes — worth having, but a smaller and different ask than the one
the report pairs it with.

**The question:** should a release-exempt marker exist, and if so does it suppress encryption at rest?

## Left open — refusing the undeclared shape at registration

IMP-24 observes that *"a `[PII]` value on a record with no resolvable subject and no declaration is, today,
always one of the two failures"* and suggests the client could refuse it. **As stated the claim over-reaches,
and refusing on it would break working code.** With no subject resolvable there is only one outcome — the
release pass does not run — and that is *correct* for a value that was never encrypted, which is the shape
`slice.md` prescribes and which 16.16.0 made safe. Whether it is a failure depends on whether the value is
ciphertext, which is a runtime property of the data, not a static property of the type. So the shape cannot
be refused; at most it can be reported.

Two narrower refusals are defensible, and neither is in this branch:

1. **`[ReleaseUnder]` on a registered (projection- or reducer-backed) read model.** The write path encrypts
   under the document's single `_subject`; a declaration would release under a different one, so the value
   cannot round-trip. This is the one place a registration-time refusal has real information. It is not
   implemented because a throw from `RegisterAll()` reaches the ASP.NET Core boot path through
   `ApplicationStarted.Register(...)` in `ChronicleClientWebApplicationBuilderExtensions`, where a throwing
   callback is logged rather than fatal — a "refusal" nobody sees is the sin this report is about.
   *(Source-traced, not run.)*
2. **An analyzer.** `CHR0038` already reproduces the subject-resolution order to catch a `[Join]` crossing
   subjects, so the machinery for reasoning about this at build time exists. A sibling rule could report a
   `[PII]`-typed property on a read model that resolves no subject and declares nothing. It is deferred
   because the right diagnostic depends on how many declarations exist: with only `[ReleaseUnder]`, the only
   advice it can give a genuinely computed value is to declare a subject it does not have.

**The question:** should `[ReleaseUnder]` on a stored read model be refused, and where — registration, or a
`CHR00xx` analyzer that can also speak to the undeclared shape?

## Scope held

Ada's own policy — person-scoped read models, identity resolved at the render edge — is not encoded here,
and nothing in this branch blesses denormalizing personal data onto a differently-subjected document. The
documentation says so explicitly, and `CHR0038` still reports the join that would do it.
