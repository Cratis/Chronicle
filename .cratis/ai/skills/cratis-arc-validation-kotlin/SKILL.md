---
name: cratis-arc-validation-kotlin
description: Add a rejection rule to a Cratis Arc command or query in Kotlin or Java — FluentModelValidator shared rules that also emit TypeScript, plus the server-only CommandValidator, QueryValidator, ConceptValidator, and ModelValidator. Use when a Kotlin or Java Arc command or query must refuse work under some condition. Do not use to define a new command or query, and do not use for append-time Chronicle constraints.
license: MIT
---
<!-- cratis-ai-managed: skills/cratis-arc-validation-kotlin/SKILL.md -->

# Validate an Arc command or query in Kotlin or Java

Arc.Kotlin has two validation surfaces, and they compose rather than compete:
**shared fluent rules** that a build step also turns into generated TypeScript
client-side checks, and **ordinary imperative validators** that run only on
the server. Pick shared rules first for anything literal enough to express
that way; keep service calls, async work, and cross-member logic imperative.

## Verified product sources

Verified against `Arc.Kotlin` at tag **`v7.3.0`**, re-verified at commit
`d0aa6a4` (14 commits past the tag; `guides/commands.md`, `guides/validation.md`,
and `guides/chronicle.md` are byte-for-byte unchanged since the tag).
Documented consumer setup: JDK 17, Kotlin 2.4.20, KSP 2.3.12, Spring Boot
4.1.x. `io.cratis:arc` supplies `io.cratis.arc.validation.FluentModelValidator`,
`CommandValidator<T>`, `QueryValidator<T>`, `ConceptValidator<T>`,
`ModelValidator<T>`, `ConceptValidationExclusion`, `IgnoreValidation`,
`CommandFilter`, `QueryFilter`. The re-verification added the new
"Reject at feature scope with a pipeline filter" section below, from the new
`guides/pipeline-filters.md`.

## Shared fluent rules — server and generated TypeScript

Use `FluentModelValidator<T>` for a literal member rule that must run
identically on the server and in the generated client. It is a public, final,
top-level class over one concrete model, with one public no-argument
constructor and a single `init` block containing only direct `ruleFor(...)`
chains with literal arguments — KSP statically compiles this shape, so no
lambda, computed accessor, or helper method is allowed inside it.

```kotlin
package example.tasks

import io.cratis.arc.validation.FluentModelValidator

class CreateTaskRules : FluentModelValidator<CreateTask>(CreateTask::class.java) {
    init {
        ruleFor("title").notEmpty().withMessage("{PropertyName} must have text; {PropertyName}")
        ruleFor("title").maxLength(120)
    }
}
```

```java
package example.tasks;

import io.cratis.arc.validation.FluentModelValidator;

public final class CreateTaskRules extends FluentModelValidator<CreateTask> {
    public CreateTaskRules() {
        super(CreateTask.class);
        ruleFor("title").notEmpty();
    }
}
```

Java uses the ordinary constructor and method calls — no Kotlin lambda or
`Continuation`. Records with default or source-proved identity accessors and
public fields are supported.

### The rule vocabulary

| Rule | Checks |
| --- | --- |
| `notNull()` | value is not `null` |
| `notEmpty()` | non-blank string |
| `minLength(n)` / `maxLength(n)` / `length(min, max)` | string length |
| `emailAddress()` | email shape |
| `phone()` | digits/whitespace/parens/plus/hyphen only |
| `url()` | `http://`/`https://` prefix |
| `matches(regex)` | regex match |
| `greaterThan(n)` / `greaterThanOrEqual(n)` | numeric lower bound |
| `lessThan(n)` / `lessThanOrEqual(n)` | numeric upper bound |
| `withMessage(text)` | overrides the preceding rule's message; replaces only the first `{PropertyName}` token |

There is no shared `creditCard()` fluent rule — use the server-only
`@CreditCard` annotation instead (below); the pinned `@cratis/arc` client has
no matching client-side check for it.

Select **one direct public readable member** per `ruleFor(...)` — not a dotted
path, not an index. Shared rules apply to present nested nodes and collection
siblings automatically: a `null` parent creates no model node, so a child's
`notNull()` does not implicitly require the parent to be present — add a
presence rule on the parent member when the parent itself is required.

### Wire it up

Register nothing by hand. The generated module registers every discovered
`FluentModelValidator` automatically:

```kotlin
cratisArc {
    moduleName.set("<ModuleName>")
    dependencyVersion.set("<version>")
}
```

The Arc Gradle plugin wires the dependency index shared validation needs
automatically. A manual KSP build must extract and pass that index itself —
see the Arc.Kotlin `Documentation/reference/configuration.md` dependency
extraction recipe before reaching for manual wiring.

### Opt a member out

```kotlin
import io.cratis.arc.validation.IgnoreValidation

data class CreateTask(
    @IgnoreValidation val internalNote: String?,
    val title: String
)
```

`@IgnoreValidation` cuts one logical validation edge while keeping
serialization and binding intact — it is not `@JsonIgnore`. Kotlin accepts it
on the property, `@field:`, or `@get:` placement; Java accepts it on a field,
bean getter, or record component. Nonignored siblings and independently
validated child roots still run.

## Server-only validators

### `CommandValidator<T>` / `QueryValidator<T>`

```kotlin
@Component
class CreateTaskValidator : CommandValidator<CreateTask> {
    override val commandType = CreateTask::class.java

    override suspend fun validate(command: CreateTask, context: CommandContext): List<ValidationResult> =
        if (command.title.isBlank()) {
            listOf(ValidationResult.error("A task title is required.", listOf("title")))
        } else {
            emptyList()
        }
}
```

Register a Spring bean; `POST <command-route>/validate` runs the same
pipeline without invoking `handle`. `ValidationResult.information` /
`.warning` / `.error` are static factories naming severity, members, state,
reason, and reason detail — Java reads them the same way. Without an explicit
threshold, only errors block execution; add `@TreatWarningsAsErrors` to make
`Information` the nonblocking ceiling, so warnings and errors both block.
Callers can override per request with the `X-Allowed-Severity` header.

Java implementations use `BlockingCommandValidator`/`AsyncCommandValidator`
with the matching `*Adapter`, published as an ordinary `CommandValidator`
Spring bean — there is no second Java-specific discovery mechanism.

### `ConceptValidator<TConcept>`

A reusable invariant on a `ConceptAs<T>` concept type, walked automatically
wherever that concept type appears — command, nested model, collection,
array, or map — without each owner repeating the rule. Matching uses
`conceptType.isInstance`, including subtypes; failures attach to the owning
member's path.

```kotlin
@Component
class TaskTitleValidator : ConceptValidator<TaskTitle> {
    override val conceptType = TaskTitle::class.java

    override fun validate(concept: TaskTitle): List<ValidationResult> =
        if (concept.value.isBlank()) listOf(ValidationResult.error("A task title is required.")) else emptyList()
}
```

Concept rules run independently of Jakarta `@Valid` and do not require a
Jakarta `Validator` bean. Exclude one direct edge deliberately with
`ConceptValidationExclusion(ownerType, member)` — the owner must match the
**exact runtime class**, and the member must be one direct public property (or
Java record component/field) whose *declared* type implements `ConceptAs`. A
scalar root, a collection element, or a nested path cannot be excluded this
way — register the nested owner type and its direct member instead. Excluding
an edge suppresses only `ConceptValidator` execution there; owner rules, exact
`ModelValidator` rules, Jakarta constraints, and root typed validators still
run.

### `ModelValidator<T>`

A server-only rule reused by command roots, nested models, and supplied query
arguments, matched by **exact runtime class** (not subclasses or interfaces).
Nodes run parent-before-child, model rules before concept rules; records use
declaration order, Kotlin public properties use alphabetical order. Java
implementations use `BlockingModelValidator`/`AsyncModelValidator` with the
matching adapter, published as a `ModelValidator` bean.

### Jakarta constraints

When Spring has a Jakarta `Validator` bean (add `spring-boot-starter-validation`
for one), the starter installs a command/query filter automatically. It
validates `@Valid`-marked nested objects, arrays, iterables, and maps; maps a
path like `items[0].name` into Arc member paths; and terminates safely on
cyclic graphs. `@Phone`, `@Url`, `@CreditCard` are Jakarta constraints that
work from both Kotlin and Java; `@Phone`/`@Url` also emit matching generated
TypeScript rules, while `@CreditCard` stays server-only because the pinned
client runtime has no compatible check. `null` is always valid for these three
— pair with `@NotNull` when absence itself is invalid.

## Reject at feature scope with a pipeline filter

`CommandValidator`/`ConceptValidator`/`ModelValidator` all attach a rule to a
specific artifact or type. When a rule instead belongs to a whole *feature* —
"everything under the billing package needs the billing role" — writing it on
every command in turn means the next command somebody adds is the one that
forgets it. A `CommandFilter`/`QueryFilter` is that declaration, written once:

```kotlin
class BillingCommandFilter : CommandFilter {
    override suspend fun execute(context: CommandContext): CommandResult<*> {
        if (!context.commandType.name.startsWith(BILLING_PACKAGE)) {
            return CommandResult.success(context.correlationId)
        }
        return if (context.principal.isInRole(BILLING_ROLE)) {
            CommandResult.success(context.correlationId)
        } else {
            CommandResult.unauthorized(context.correlationId, "Role '$BILLING_ROLE' is required.")
        }
    }
}

@Bean
fun billingCommandFilter(): CommandFilter = BillingCommandFilter()
```

- A `QueryFilter` takes the same shape against `QueryContext`, matched on the
  query's fully qualified name; it runs for an observable query too, at
  subscription time, so one rule covers both the one-shot `GET` and the live
  subscription.
- A filter that does not apply to the artifact in front of it must return
  success and get out of the way — that first check is what keeps a feature's
  rule scoped to the feature instead of becoming a global one by accident.
- Java implements `BlockingCommandFilter`/`BlockingQueryFilter` (synchronous)
  or `AsyncCommandFilter`/`AsyncQueryFilter` (`CompletionStage`), registered
  through the matching `*FilterAdapter`.
- Filters run in Spring `@Order` sequence — that order is the complete rule,
  with one exception: a filter implementing `AuthorizationCommandFilter` /
  `AuthorizationQueryFilter` always runs before the rest, regardless of its
  own `@Order`, so a denial is never reached after a filter that already
  caused an effect.
- Reach for a filter only when the rule genuinely spans artifacts. A rule
  about one command or query reads closer to the code it governs, and stays
  visible in the generated client and OpenAPI document, as `@Authorize`/
  `@Roles` or one of the validators above — prefer those first.

## Order of execution

Typed `CommandValidator`/`QueryValidator` rules run once, before the shared
model/concept traversal. A separately registered `ModelValidator` for the
command's own class is a distinct rule that also runs once at the root.
Cancellation from any validator — a concept validator, a Kotlin property
getter, a Java record accessor — propagates through execution and marks a
cancelled nested command's root rollback-only. An ordinary thrown exception
from a validator becomes safe `validatorFailed` feedback instead, not an
exception result; it is not silently skipped.

## Route near misses

- Defining the command or query the rule attaches to:
  `cratis-arc-command-kotlin`, `cratis-arc-query-kotlin`.
- Append-time Chronicle uniqueness/concurrency: `cratis-chronicle-event-constraints`.
- The C# Arc validation shape (`ConceptValidator<T>`, `CommandValidator<T>`,
  Provide short-circuiting): `cratis-arc-command-validation`.

## Verify

- Every `FluentModelValidator` is public, final, top-level, has exactly one
  public no-argument constructor, and its `init` block contains only direct
  `ruleFor(...)` chains with literal arguments.
- A rejection the user can act on comes back as a `ValidationResult`, never a
  thrown exception.
- A concept rule that must not run on one specific edge is excluded through
  `ConceptValidationExclusion`, not by weakening the concept's own rule.
- `@IgnoreValidation` is used only to cut validation, never in place of
  `@JsonIgnore` for a value that must not serialize at all.
- The generated TypeScript client rejects the same input the server does for
  every rule expressed with `FluentModelValidator` — verify with a build that
  regenerates the proxy.
- `./gradlew build` is clean with zero warnings and zero errors.
