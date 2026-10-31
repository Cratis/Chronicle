---
name: cratis-arc-command-validation
description: Add a rejection rule to an existing Arc command — choosing between ConceptValidator, CommandValidator, a short-circuiting Provide, and a read-model-injected Handle returning a Result of either the event or a ValidationResult. Use when placing a C# business rejection in ConceptValidator, CommandValidator, Provide() or a Handle() result containing ValidationResult. Do not use for JVM validators (use cratis-arc-validation-kotlin), Node.js ruleFor (use cratis-arc-validation-typescript), new commands (use cratis-arc-command), or append-time uniqueness (use cratis-chronicle-event-constraints).
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

| Package | Purpose |
| --- | --- |
| `Cratis.Arc.Core` | `CommandValidator<T>`, `ConceptValidator<T>`, `ValidationResult`, `CommandResult`, the command pipeline, `ARC0004`–`ARC0013` |
| `Cratis.Fundamentals` | `Cratis.Monads.Result<TResult, TError>` |
| `Cratis.Chronicle` | Read-model resolution for command-side injection |
| `FluentValidation` | `RuleFor(...)`, `.WithMessage(...)` |

Fundamentals'
[`Result<TResult, TError>`](https://github.com/Cratis/Fundamentals/blob/main/Source/DotNET/Fundamentals/Monads/Result%7BTResult,TError%7D.cs):
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
  repository is opted in (per `general.md` "Model first"; where `cratis-screenplay-modeling-lifecycle` is installed, run its `scripts/model-first-status.mjs`): change the model first with
  `cratis-screenplay-event-modeling`. If the Screenplay skills are not installed,
  say so and do not author `.play` from memory.
  Edit code here only for infrastructure, clients, adapters, Screenplay code
  attachments, or gap-fill scope (`cratis-screenplay-render-and-gap-fill`);
  never edit Stage-managed output.

## Step 1 — Choose the mechanism by what the decision *is*

Which mechanism makes a rule hold under concurrent writers (constraint, protected
decision, stream decision, aggregate, scope, reactor repair) is routed in
[choosing how a rule holds](../cratis-chronicle-decision-reads/references/choosing-how-a-rule-holds.md).
A validator holds a state rule only when the project is on read protection `On`;
under `Off` (the default) it is a hint, never the mechanism.

| The rule is… | Put it in |
| --- | --- |
| A reusable value invariant — length, format, range | `ConceptValidator<T>` on the concept type |
| Command input, cross-field, or a pre-handler check | `CommandValidator<TCommand>` with `RuleFor(...)` |
| A dependency on data the handler must fetch first | `Provide()` — fetch, and short-circuit if unusable |
| State-dependent and must hold **under concurrency** | Under `On`, a `CommandValidator<T>` with the read model as a constructor parameter (Step 5); or `IStreamReads`/`StreamDecision`; return events through Arc. `[ProtectedDecision]` + `DecisionRead<T>` is the older per-command opt-in |
| Uniqueness | A Chronicle constraint, never a command-side read |
| A genuine defect or missing infrastructure | `throw` a domain exception |

The pipeline runs **authorization → validation → `Provide()` → `Handle()`**.
Authorization filters are explicitly ordered ahead of validation filters, so a
caller who may not act never learns whether their input was valid.

Requires Arc 22.87.2 for the frontend `notEmpty` text rule to match the backend's
Unicode whitespace set (including U+0085 and excluding U+FEFF). Check the
`@cratis/arc` package version with `yarn why @cratis/arc`; older client validation
can disagree. Server validation remains authoritative.

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
  Step 5. **State rules ("may this proceed") belong here**, with the read model as a
  constructor parameter. Under read protection `On` that read is fresh from the log
  and guards the append: if an event of the model's types for that key arrives
  after the read, nothing from the command is written. Under `Off` (the default
  when nothing is set) and `Measure` the same validator reads a plain model: a hint
  with a friendly message that guards nothing. Under `On` the validator must be
  convention-registered (transient); hand-registered scoped, singleton, factory or
  instance validators are refused. Under `[ProtectedDecision]` without `On`, a
  discoverable validator needs exactly one public parameterless constructor
  (otherwise HTTP 500).
- **The caller can lower the severity threshold.** Under `Off`/`Measure`, pin
  `[BlockOnValidationSeverity(ValidationResultSeverity.Error)]` when Error rules
  must block. Requires Arc 22.108.0 for commands declared `On` to keep an Error
  floor. Read references/validation-severity.md when choosing that threshold;
  severity is not authorization.
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

Set read protection `On` for new code: `<CratisReadProtection>On</CratisReadProtection>`
in the project (or `[ReadProtection(ReadProtectionMode.On)]` on one command). Under
`On`, the read-model parameters of `Handle()` and of the validator constructor are
the guard, so the state rule is a validator and `Handle()` returns the events.
Requires Arc 22.106.0. Under `Off` (the default) a plain read guards nothing, even
inside `Handle()` returning `Result<,>`. Choose the first boundary that fits:

1. A unique value or create-once: a Chronicle constraint, not a read.
2. One entity's state: a read-model parameter in the validator (this step's example).
   Several entities: one parameter each, with `[LookUpBy(nameof(Command.Prop))]` on
   a parameter whose key is not the command's event source id, and `Handle()`
   returning `IEnumerable<EventForEventSourceId>` with a distinct event type per
   entity.
3. A limit inside one stream: a per-stream read model (`[EventStream<TSource>("…")]`)
   on a command routed `[EventSource<TSource>("…")]` + `[EventStreamId("{Prop}")]`,
   or `Provide(IStreamReads reads) => reads.ForCommand()` returning `StreamDecision`.
4. An editing conflict: exact returned scopes from the revision the client sent.
   `concurrency: true` on routing attributes is **not** a guard: like the default
   check it reads the tail after `Handle`.
5. Rich lifecycle: an aggregate; do not also read the same source.

A read under `On` is refused, with HTTP 500, when it cannot be guarded exactly:
`IReadModels`, `IMongoCollection`, detached reads, an injected `IEventLog.Append`,
and models that are joins, event-property keyed, filtered or dependency-bearing
reducers, Pinned or on another sequence (`ReadCannotBeProtected`; mark a deliberate
advisory read `[Unprotected("why")]`). Several command properties with the model's
`Id` type are `AmbiguousReadModelLookup`: add `[LookUpBy]`. A lost race is HTTP 400
`concurrencyViolation` (`ReadChanged`): the client resubmits; Arc does not re-run
the command. `[ProtectedDecision]` with `DecisionRead<T>` is the older per-command
opt-in; it still works and is not obsolete. A guard protects only guarded
decisions: every writer that decides on the rule needs one, or the rule must be a
constraint.

`ARCCHR0011` (Info, `Off` only) and `ARCCHR0012` flag plain reads and immediate
append bypasses. Return events, never append them directly. Read
[protected and stream decisions](../cratis-arc-command/references/decisions-and-routing.md)
and
[choosing how a rule holds](../cratis-chronicle-decision-reads/references/choosing-how-a-rule-holds.md)
when choosing the guard. The example assumes `CartSummary` is a model-bound
projection with a `CartId Id`; domain types are the slice's existing types.

```csharp
using Cratis.Arc.Commands;
using Cratis.Arc.Commands.ModelBound;
using FluentValidation;

[Command]
public record AddItemToCart(CartId CartId, ItemId ItemId)
{
    /// <summary>Adds the item.</summary>
    /// <returns>The event.</returns>
    public ItemAddedToCart Handle() => new(ItemId);
}

public class AddItemToCartValidator : CommandValidator<AddItemToCart>
{
    public AddItemToCartValidator(CartSummary? cart) =>
        RuleFor(c => c.CartId)
            .Must(_ => cart is { ItemCount: < 3 })
            .WithMessage("The cart must exist and hold fewer than 3 items.");
}
```

Use `Result<TEvent, ValidationResult>` from `Handle()` only when the rule needs a value
computed there:

```csharp
public Result<ItemAddedToCart, ValidationResult> Handle(CartSummary? cart) =>
    cart is not { ItemCount: < 3 }
        ? ValidationResult.Error("A cart can hold at most 3 items.")
        : new ItemAddedToCart(ItemId);
```

- `Result<TSuccess, TError>` is `Cratis.Monads.Result<,>` from Cratis
  Fundamentals — **success first, error second** — and needs
  `using Cratis.Monads;`. Its implicit conversions are what let both `return`
  statements above compile without wrapping.
- **Return the error; never throw it.**
- One parameter per logical read model; several reads mean several parameters.
- Keep the fetch for a *computed* key in `Provide()` with `IDecisionReads.Get`;
  under `On`, `IReadModels` there is refused.

Read references/read-model-resolution.md when choosing lookup keys, handling a
missing injected model, or distinguishing registration from slice conventions.

Read references/input-binding.md when accepting date-time input or returning
member-bound validation results: offset requirements, malformed-body
authorization and camelCase member paths are separate contracts.

## Step 6 — Specify the rejection

Every rule gets a specification of its failure case, using
`CommandScenario<TCommand>`. For guarded reads, `new CommandScenario<T>()`
selects the in-process log from the command's mode and dependencies (do not call
`UseDecisionReads()`). Seed real events with `Given.ForEventSource(id).Events(...)`
(pinned read models cannot supply a guard), queue a competing
`AppendConcurrently(id, events...)` and assert
`ShouldHaveValidationErrorBecauseOf(ValidationResultReason.ConcurrencyViolation)`.
For the ordinary rejection, assert **both** that the command did not succeed
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
- **Two requests both win.** The project is on `Off` (the default) or `Measure`, so
  the validator read is a hint; moving it to `Handle()` or returning `Result<,>`
  does not guard it. Set `On`, or use a stream decision or a constraint.
- **`ARC0005` on build.** `Provide()` returns a value no `Handle()` parameter
  consumes.

## How it is proven

`dotnet build` in Debug and in Release, zero warnings and zero errors — the
analyzers above are how `ARC0005`, `ARC0006` and `ARC0013` surface. Then
`dotnet test` with a specification per rule, each asserting both
`ShouldNotBeSuccessful()` and `ShouldHaveValidationErrors()`. A rule with no
red-first specification has not been shown to reject anything.
