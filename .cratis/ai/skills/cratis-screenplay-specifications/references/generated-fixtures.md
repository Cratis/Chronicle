<!-- cratis-ai-managed: skills/cratis-screenplay-specifications/references/generated-fixtures.md -->
# Generated fixtures and return expectations

## Contents

- Choosing a strategy
- When not to
- Fixtures and outcomes

## Choosing a strategy

Use the [shop example](../../cratis-screenplay-command-surface/references/shop-generation.md):
pin CustomerId (`cus_` + ULID), OrderId (TypeID) and InviteCode (NanoID) as
whole values. ApiKey stays secret with no executable generation; do not write a
passing issuance spec that invents its .NET Bytes(32) factory in Screenplay.
Pin InvoiceNumber's whole date/checksum reference and TicketNumber's integral
pickup-ticket value too; do not use numeric text for Int fixtures.

Choose the concept's strategy for the job, then pin a whole value matching it:
`for` for its generated identifier, named `generated` fixtures for other values.
Use the shared [strategy reference](../../cratis-screenplay-command-surface/references/generated-values-and-responses.md)
for UUID, time-ordered, typed/opaque text, byte budgets, finite patterns and Int
range choices and exact shapes.
`given clock` supplies `$context.occurred` and pattern UTC date segments, not a generated time-ordered ID;
a seed is not a portable fixture.

## When not to

A fixture still states every command and event property with a concrete value, optional ones included (PLAY0524); a generated value does not excuse an omitted field.

Pins do not prove randomness, authorization, idempotency, storage freshness or
secret safety. Client-minted IDs stay inputs; gapless invoice numbering needs an
allocator, not a random fixture or pattern. Check digits catch typos, not forgery;
short payload codes need uniqueness enforcement separate from source freshness.
Pattern/random/rule and checksum validation bind as v21. Stage renders
pattern/random/checksum under the version gates below; `generate rule` renders in a new
enough bundle (the Host and legacy syntax engine refuse it with STAGE-ESM-028). Generated secrets remain refused. Source: Screenplay
`Documentation/screenplay/generated-values/choosing.md`.

## Fixtures and outcomes

A compiler that admits ESM v7 binds and runs these; Stage renders supported UUID/String and Int random generation strategies,
scalar/named responses, fixtures and `then returns`. Unsupported types/result combinations
remain `STAGE-ESM-028`/`-038`; preserve refused specifications as gap-fill contracts. The model and rules are in `cratis-screenplay-command-surface`; the
complete threaded model is the linked shop example. The excerpt below belongs
to its RegisterCustomer command and uses the same concept declarations.

```screenplay excerpt
specification RegistersWithPinnedValues
  when RegisterCustomer
    for "cus_01ARZ3NDEKTSV4RRFFQ69G5FAV"
    generated inviteCode = "abcdefghijklmnopqrstu"
  then CustomerRegistered
    for "cus_01ARZ3NDEKTSV4RRFFQ69G5FAV"
    inviteCode = "abcdefghijklmnopqrstu"
  then returns
    customerId = "cus_01ARZ3NDEKTSV4RRFFQ69G5FAV"
    inviteCode = "abcdefghijklmnopqrstu"
```

- `for` beneath `when` supplies the generated identifier and an indented `generated <name> = <value>` supplies any other
  generated value; neither goes on the `when` header, and neither is a request mapping. Ordinary `generated = <value>`
  is an input mapping for a property named `generated`.
- A scalar response is `then returns <value>`; a record uses `then returns` with a non-empty subset of its named
  fields (unknown, duplicate, nongenerated or identifier fixture targets are rejected). Values are concrete literals or
  structured values. A return expectation needs a command action, occurs at most once and cannot accompany
  `then error` or `then denied`; event and state assertions may coexist.
- The reference runner compares scalar values by semantic equality and records by the asserted subset, reporting
  differences in response field order. An absent optional source yields `Null`; arrays compare in order.
- `then returns` counts as a success outcome but does not relax event comparison: a command specification with
  no expected events still asserts that no facts were produced.
- A command that a **reaction** invokes takes its generated values from `given generated <Command>.<property> = <value>` lines
  before the `when` (ESM v30, decision 0098). Each invocation takes the next value pinned for that
  command and property in authored order; two invocations need two lines. The property must be `generated`; the value is a
  concrete literal.
- If execution reaches generation without every generated value supplied, the run is `Unsupported(IdentityAllocation)` and
  the specification never passes; nothing is invented. A denial or validation failure happens before generation and
  keeps its own outcome.
- For a generated identifier `for` is the generation fixture, not a destination assertion: expected events state their
  own destinations. For commands without a generated identifier `when ... for` keeps the destination assertion.
  UUID fixtures require canonical lowercase hyphenated form. From v15, pins must also match
  exact strategy size, case, UUID version/variant and zero trailing encoding bits; invalid pins are
  Contract rejections. A generated concept with rules selects v20 and checks pins
  against both shape and rules, including implicit UUID. Rule failures are
  `Rejected(Contract)` with `Generated fixture '<property>' fails concept '<Concept>' rules`,
  never client validation failures or redraws. Earlier rule-free implicit-UUID
  models check canonical UUID type, not its version.
- A serialized concept generation strategy selects v15 and Guarded whole-source freshness for all
  generated destinations, including implicit UUIDs. Reusing a generated source with existing facts
  rejects the entire command with Concurrency, without append, response or silent retry.
  Models without a serialized strategy retain legacy reuse even when rules select v20. See the complete [strategy example](../../cratis-screenplay-toolchain/references/generation-strategies-example.md)
  and command-surface `references/generated-values-and-responses.md` for tagged sources.
- `given clock` supplies `$context.occurred` and requires all pattern date segments
  to match the same UTC instant, including offset rollover and leap days. Without
  a clock the reference runner checks digit widths, never today's wall clock.
  Checksum pins check arithmetic only with an authored `validate checksum` rule;
  a generation modifier alone checks suffix shape. It does not synthesize UUIDv7,
  ULID or TypeID: pin the whole identifier, never a seed. Stage time-ordered
  factories use receipt time; admitted command clock specs render. Sources: Screenplay
  `Documentation/screenplay/generated-values/choosing.md`; Stage
  `Documentation/reference/generated-values-rendering.md`.
- Attached generation bodies are not run: pin the resulting String;
  an opaque validator still yields Unsupported. Stage allows unpinned rendered
  generation only when every concept rule is Proven and the expectations support
  relational captures. Unproven/Refuted rules need pins (-038); invalid pins fail
  STAGE-ESM-011. Opaque/unsupported rules refuse -005 before pin evaluation.
  Screenplay's reference runner and Stage's independent executor always need pins.
- A reaction-invoked command that reaches generation is `Unsupported(IdentityAllocation)` (no fixture channel); a
  response-only command runs and its response is discarded.

Requires Screenplay 4.164.0 (`screenplay --version`) for v20 rule-bearing generated pins, 4.172.0 for v21 finite/rule strategies; older compilers refuse PLAY0268.
Requires Stage 4.95.0 for command clock specs and 4.109.0 for v20 rule-bearing pins; older renderers refuse those shapes.
Requires Stage 4.115.0 for v21 admission/all-Proven unpinned rendering, 4.117.0
for warning/info generated-concept rules, 4.118.0 for checksum validation and
4.119.0 for pattern/random. Older renderers require rule-bearing pins or refuse
the respective rules/strategies with -038/-042/-028. `generate rule` renders in a new enough bundle.
