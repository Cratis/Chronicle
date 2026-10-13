---
name: cratis-arc-command-validation
description: Add a rejection rule to an existing Arc command — choosing between ConceptValidator, CommandValidator, a short-circuiting Provide, and a read-model-injected Handle returning a Result of either the event or a ValidationResult. Use when a command must refuse work under some condition. Do not use to define a new command, and do not use for append-time Chronicle constraints.
license: MIT
---
<!-- cratis-ai-managed: skills/cratis-arc-command-validation/SKILL.md -->

# Arc command validation

Every rejection a command can produce has exactly one right home. Putting a rule
in the wrong one is not a style problem: a value invariant written per-command
stops travelling with the value, and a state-dependent rule written in a
validator loses the race it exists to win.

**Never throw for a normal business rejection.** An exception out of `Provide()`
or `Handle()` is caught by the pipeline and merged as `HasExceptions`, which the
HTTP layer maps to **500** — not a validation result. Recoverable, user-facing
rejections are validation. (The one exception: an exception type implementing
`IValidationFailure` is converted to a validation failure and returns 400. Arc
uses that for its own dependency-resolution failures; do not build application
rejection on it.)

## Verified product sources

| Package | Version | Purpose |
| --- | --- | --- |
| `Cratis.Arc.Core` | `22.75.0` | `CommandValidator<T>`, `ConceptValidator<T>`, `ValidationResult`, `CommandResult`, the command pipeline, `ARC0004`–`ARC0013` |
| `Cratis.Fundamentals` | `7.23.0` | `Cratis.Monads.Result<TResult, TError>` |
| `Cratis.Chronicle` | `18.3.0` | Read-model resolution for command-side injection |
| `FluentValidation` | `12.1.1` | `RuleFor(...)`, `.WithMessage(...)` |

> Arc validation and decision boundaries rechecked at v22.75.0. Arc depends on
> Chronicle 19.39.1 / Fundamentals 7.23.0; separate product claims retain their
> version-qualified verification.

Fundamentals rechecked at v7.23.0 in
[`Result<TResult, TError>`](https://github.com/Cratis/Fundamentals/blob/v7.23.0/Source/DotNET/Fundamentals/Monads/Result%7BTResult,TError%7D.cs):
success is first, error second, with implicit conversions from both types.

Reverify against the owning product repository before claiming support for
another version.

## Route near misses

- Defining a new command, its `Handle()` return shape, or event-source id
  resolution: use `cratis-arc-command`.
- Uniqueness, or any rule the **event store** must enforce at append time: use
  `cratis-chronicle-event-constraints`. A read-model pre-check for uniqueness is
  not race-safe and is the classic wrong answer here.
- Authorization: that is a filter and an attribute at the boundary, not a
  validator. Use `cratis-arc-authentication-authorization-and-identity`.
- Writing the specification for the rejection: use
  `cratis-application-slice-specifications`.
- An accepted `.play` model under the model root covers the behavior, or the
  repository is opted in (the root holds a committed `.play` file (`git ls-tree -r --name-only HEAD` lists a `.play` file there, narrowed to `-- <root>` when a root is configured), or the project set
  `mcpServers.screenplay.root` in `.cratis/ai.json`; an empty directory, install
  output, an uncommitted `.play` draft or a `.play` file outside the root does not count; master definition:
  `cratis-screenplay-modeling-lifecycle`): change the model first with
  `cratis-screenplay-event-modeling`. If the Screenplay skills are not installed,
  say so and do not author `.play` from memory.
  Edit code here only for infrastructure, clients, adapters, Screenplay code
  attachments, or gap-fill scope (`cratis-screenplay-render-and-gap-fill`);
  never edit Stage-managed output.

## Step 1 — Choose the mechanism by what the decision *is*

| The rule is… | Put it in |
| --- | --- |
| A reusable value invariant — length, format, range | `ConceptValidator<T>` on the concept type |
| Command input, cross-field, or a pre-handler check | `CommandValidator<TCommand>` with `RuleFor(...)` |
| A dependency on data the handler must fetch first | `Provide()` — fetch, and short-circuit if unusable |
| State-dependent and must hold **under concurrency** | Guard the read with `IStreamReads`/`StreamDecision`, or `[ProtectedDecision]` + `DecisionRead<T>`; return events through Arc |
| Uniqueness | A Chronicle constraint, never a command-side read |
| A genuine defect or missing infrastructure | `throw` a domain exception |

The pipeline runs **authorization → validation → `Provide()` → `Handle()`**.
Authorization filters are explicitly ordered ahead of validation filters, so a
caller who may not act never learns whether their input was valid.

## Step 2 — Value invariants: `ConceptValidator<T>`

```csharp
public class EmailAddressValidator : ConceptValidator<EmailAddress>
{
    public EmailAddressValidator() => RuleFor(_ => _.Value).EmailAddress();
}
```

A `ConceptValidator<T>` is discovered automatically and applies **globally**.
Arc walks the whole object graph of every model it validates and runs a
validator resolved by each node's runtime type — so the rule fires on every
command *and every query* carrying that concept, wherever it appears. That is
the point: declare the invariant once and it cannot be forgotten on the next
command.

When one command legitimately needs the concept unchecked, opt that property out
in its own validator with `.IgnoreConceptRules()` rather than weakening the
concept rule for everyone.

`ARC0013` warns when a validator rule dereferences a possibly-null concept
member.

## Step 3 — Command input rules: `CommandValidator<T>`

```csharp
public class TransferFundsValidator : CommandValidator<TransferFunds>
{
    public TransferFundsValidator() =>
        RuleFor(c => c.Amount).GreaterThan(0).WithMessage("Amount must be positive.");
}
```

- `CommandValidator<T>` is a FluentValidation `AbstractValidator<T>` underneath,
  so `RuleFor`, the standard rule set, and `.WithMessage(...)` are
  FluentValidation's. Arc adds one overload that unwraps a `ConceptAs<T>` to its
  primitive so `RuleFor(c => c.SomeConcept)` reads naturally.
- The validator is discovered by type — no registration.
- Constructor dependencies (read models, services) are injected. A read model
  injected here resolves the same way as one injected into `Handle()`; see
  Step 5 for what "the same way" actually means.
- **Omit the validator entirely when there are no rules.** An empty subclass is
  noise the reader has to check.
- Single-property intrinsic rules belong on `ConceptValidator<T>` instead, so
  they travel with the value everywhere.
- A validator that itself throws does not become a 500 — Arc catches it and
  emits a validation error with reason `ValidatorFailed`, returning 400.

## Step 4 — Data the handler needs: `Provide()`

`Provide()` runs after authorization and validation and before `Handle()`. Its
parameters resolve from dependency injection; the command instance is `this`.

```csharp
public async Task<Result<Customer, ValidationResult>> Provide(IReadModels readModels)
{
    var customer = await readModels.GetInstanceById<Customer>((EventSourceId)CustomerId);
    return customer is null ? ValidationResult.Error("Customer must exist.") : customer;
}
```

- Returning a `ValidationResult`, an `IEnumerable<ValidationResult>`, an
  `AuthorizationResult`, or a `CommandResult` short-circuits: `Handle()` is
  never invoked.
- A `Result<TProvided, ValidationResult>` has its inner value unwrapped, so the
  success arm flows to `Handle()` and the error arm short-circuits.
- A returned tuple is flattened and its elements bound to `Handle()` parameters
  by type. **Every provided value must be consumed by a `Handle()` parameter** —
  an unused one is `ARC0005`.
- Keep the fetch in `Provide()` and the decision in `Handle()`. Do not write a
  pass-through `Provide()` that only forwards a read model `Handle()` could take
  directly, and do not duplicate one rejection in both the validator and
  `Provide()` — pick one owner.

## Step 5 — Guard a concurrency-sensitive decision

Plain read-model injection or `IReadModels.GetInstanceById` is **advisory**, even
inside `Handle()` returning `Result<,>`: it enrolls no guard against a competing
append. Choose the first boundary that fits at Arc v22.75.0:

1. One routed stream: `Provide(IStreamReads reads) => reads.ForCommand()` returns
   `Task<StreamDecision>`; `Handle(StreamDecision decision)` returns
   `decision.Append(...)` (`EventsWithConcurrencyScopes`), guarding the read tail.
2. Projected state: mark the command `[ProtectedDecision]` and take
   `DecisionRead<T>` (`.Exists`/`.Instance`) or command-aware `IDecisionReads`.
   Only Chronicle-admitted, direct event-source-keyed projections with known
   event types qualify; reducers, joins, children and passive shapes are refused.
   Only parameterless validators constructed/certified by Arc run. Constructor
   dependencies are refused; static/service-locator decision state is still your
   responsibility. Command-level `[Unprotected]` opts out, not into safety.
3. Stream-level automatic scopes: `concurrency: true` on routing attributes
   (`EventStreamId`, `EventStreamType`, `EventSourceType`). Exact returned scopes
   take precedence; do not combine automatic capture with exact scopes.

`ARCCHR0011/0012` flag unprotected reads and immediate append bypasses. Return
events, never append them directly. Read
[protected and stream decisions](../cratis-arc-command/references/decisions-and-routing.md)
when choosing the guard. This handler excerpt assumes `CartSummary` is an
admitted projection; domain types are the slice's existing types.

```csharp
using Cratis.Monads;
using Cratis.Arc.Chronicle.ReadModels;

[Command]
[ProtectedDecision]
public record AddItemToCart(CartId CartId, ItemId ItemId)
{
    /// <summary>Adds the item; rejects when the cart already holds the maximum.</summary>
    /// <param name="cart">The current cart summary, resolved by the command's event-source id.</param>
    /// <returns>The event on success, or a validation error.</returns>
    public Result<ItemAddedToCart, ValidationResult> Handle(DecisionRead<CartSummary> cart) =>
        !cart.Exists
            ? ValidationResult.Error("Cart must exist.")
            : cart.Instance.ItemCount >= 3
                ? ValidationResult.Error("A cart can hold at most 3 items.")
                : new ItemAddedToCart(ItemId);
}
```

- `Result<TSuccess, TError>` is `Cratis.Monads.Result<,>` from Cratis
  Fundamentals — **success first, error second** — and needs
  `using Cratis.Monads;`. Its implicit conversions are what let both `return`
  statements above compile without wrapping.
- **Return the error; never throw it.**
- One parameter per logical read model; several reads mean several parameters.

⚠️ **Two things the resolution does *not* do.** It does not key off the
`[ReadModel]` attribute — a read model becomes injectable because a Chronicle
projection or reducer, an EF Core `DbSet<T>`, or a MongoDB collection backs it.
And there is **no slice or feature restriction**: registration is global by
type, so "the read model must be in the same slice" is a convention with nothing
enforcing it.

The resolution is **by the command's resolved event-source id**, not by the
read-model type and not by "the property that looks like its key". If the
instance you need is keyed by anything else — a referenced other entity, a
command with more than one candidate identity, or an id generated inside
`Handle()` — direct injection hands you the wrong instance or none. Read by
explicit key in `Provide()` instead:

```csharp
var other = await readModels.GetInstanceById<Other>((EventSourceId)otherId);
```

A missing instance resolves to `null`, never to a default object. A nullable
parameter receives that `null`; a non-nullable one is **rejected as a validation
failure (400)**, not crashed. `ARC0006` warns on a non-nullable command-scoped
read-model parameter for exactly this reason. Handle a nullable parameter by rejecting (a validator,
`Provide()`, or `Result<TEvent, ValidationResult>`), never by returning a
nullable event from `Handle()`. `cratis-arc-command` carries the
full resolution and nullability reference, including the passive-projection case
where absence is *not* `null`.

## Step 6 — Specify the rejection

Every rule gets a specification of its failure case, using
`CommandScenario<TCommand>`. For protected reads, call `UseDecisionReads()`
before seeding actual events; pinned read models cannot supply a guard. Queue a
competing append on the relevant route and assert the conflict. Assert **both** that the command did not succeed
and that it failed *as a validation*:

```csharp
[Fact] void should_not_succeed() => _result.ShouldNotBeSuccessful();
[Fact] void should_have_validation_errors() => _result.ShouldHaveValidationErrors();
```

`ShouldNotBeSuccessful()` alone cannot tell a rejection from an unhandled
exception, which is precisely the mistake this skill exists to prevent. Do not
assert on a message string by default — it is presentation text. Assert exact
wording only when it is the specified behavior, and name that requirement in
the fact. See
`cratis-application-slice-specifications` for the scenario mechanics.

## What breaks

- **The rejection returns HTTP 500.** Something threw instead of returning a
  validation result. Check `Provide()` and `Handle()` for a `throw` on a
  recoverable path.
- **The rule reads the wrong instance.** The injected read model is keyed by the
  command's event-source id and the command carries a different identity. Read
  by explicit key in `Provide()`.
- **The rule never fires.** The read-model type has no backing Chronicle
  projection, reducer, `DbSet<T>`, or MongoDB collection, so nothing registered
  it for command-side resolution.
- **Two requests both win.** A plain read was mistaken for a protected decision;
  moving it to `Handle()` or returning `Result<,>` does not guard it. Enroll a
  stream/projection decision read, or use an append-time uniqueness constraint.
- **`ARC0005` on build.** `Provide()` returns a value no `Handle()` parameter
  consumes.

## How it is proven

`dotnet build` in Debug and in Release, zero warnings and zero errors — the
analyzers above are how `ARC0005`, `ARC0006` and `ARC0013` surface. Then
`dotnet test` with a specification per rule, each asserting both
`ShouldNotBeSuccessful()` and `ShouldHaveValidationErrors()`. A rule with no
red-first specification has not been shown to reject anything.
