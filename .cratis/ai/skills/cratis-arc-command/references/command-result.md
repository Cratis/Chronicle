<!-- cratis-ai-managed: skills/cratis-arc-command/references/command-result.md -->
# CommandResult and ValidationResult

## Contents

- Server side
- Map integration exceptions
- Severity filtering
- Client side
- Executing with a severity

Source: `Cratis.Arc.Core` and `@cratis/arc`.
Sources: `Source/DotNET/Arc.Core/Commands/CommandResult.cs`,
`CommandValidationResults.cs` and `Source/JavaScript/Arc/commands/Command.ts`.

## Server side

`Cratis.Arc.Commands.CommandResult`:

| Member | Type | Notes |
| --- | --- | --- |
| `CorrelationId` | `CorrelationId` | Defaults to `CorrelationId.NotSet` |
| `IsSuccess` | `bool` | Computed: `IsAuthorized && IsValid && !HasExceptions` |
| `IsAuthorized` | `bool` | Settable; defaults to `true` |
| `IsValid` | `bool` | Computed: `!ValidationResults.Any()` |
| `HasExceptions` | `bool` | Computed: `ExceptionMessages.Any()` |
| `ValidationResults` | `IEnumerable<ValidationResult>` | |
| `ExceptionMessages` | `IEnumerable<string>` | |
| `ExceptionStackTrace` | `string` | |
| `AuthorizationFailureReason` | `string` | |
| `IsRetryable` | `bool` | Temporary refusal; defaults to `false` |
| `IsMissingHandler` | `bool` | No handler: execution/validation did not run; defaults to `false` |

`CommandResult<TResult>` adds a `Response` of `TResult`.

`IsValid` and `HasExceptions` are **derived**, not stored. There is no way to
set them directly — a result is invalid because it carries validation results.

Factories: `Success`, `Unauthorized`, `MissingHandler`, `Error`, `InvalidBody`,
`FromException`.

⚠️ **A failed result carries no response.** The pipeline clears the response once
every execution scope has completed and the result is not successful, so a
transaction that rolls back after `Handle` returned a value does not leak that
value into the error body.

## Map integration exceptions

Requires Arc 22.100.0 or later for `Cratis.Arc.Commands.ICommandExceptionMapper`.
Check `dotnet list package --include-transitive`; older Arc has no mapper hook
and uses `CommandResult.FromException` for unhandled execution exceptions.

Implement the interface for an integration's typed failures, not ordinary
user-correctable rejections (return validation results for those):

```csharp
bool TryMap(CommandContext context, Exception exception,
    [NotNullWhen(true)] out CommandResult? result);
```

`NotNullWhen` is from `System.Diagnostics.CodeAnalysis`. Return `true` with a
non-null **failure** preserving `context.CorrelationId`, or `false` with `null`
to decline. Match exception types/properties rather than parsing messages.
Implementations are convention-discovered through `IInstancesOf<ICommandExceptionMapper>`
(`Cratis.Types`) and resolved from the command's service scope. The first accepting
mapper wins. Mapping runs for exceptions escaping command execution or response
processing, before execution scopes complete, so a mapped failure still rolls
back pending work. If no mapper accepts, the pipeline falls back to
`CommandResult.FromException`; a throwing mapper produces `CommandResult.Error`
and scopes still complete with
failure. This hook does not replace validation-only or scope-completion exception
handling. Specify accepting, declining and throwing cases and assert rollback
and correlation preservation, not only the message.

## Severity filtering

`ValidationResultSeverity`: `Unknown = 0`, `Information = 1`, `Warning = 2`,
`Error = 3`.

Only *blocking* results survive onto the result. For an unannotated command,
with no `allowedSeverity`, only `Error` blocks; with a caller threshold, anything
strictly greater blocks. `[BlockOnValidationSeverity(ValidationResultSeverity.Warning)]`
(`Cratis.Arc.Commands.ModelBound`) declares an inclusive blocking floor across
HTTP, Validate, Provide and in-process execution. Caller `X-Allowed-Severity` or
`allowedSeverity` can tighten, never loosen, that floor; attributed commands
also block `Unknown`. The generated proxy carries `blockOnValidationSeverity`
and applies it client-side. This attribute does not change an unannotated
command's legacy filtering.

## Client side

`@cratis/arc` `ICommandResult<TResponse = object>`:

```ts
readonly correlationId: Guid;
readonly isSuccess: boolean;
readonly isAuthorized: boolean;
readonly isValid: boolean;
readonly hasExceptions: boolean;
readonly isRetryable?: boolean;
readonly isMissingHandler?: boolean;
readonly validationResults: ValidationResult[];
readonly exceptionMessages: string[];
readonly authorizationFailureReason: string;
readonly exceptionStackTrace: string;
readonly response?: TResponse;
```

`ValidationResult` is a class with these readonly members:

```ts
severity: ValidationResultSeverity   // numeric enum, 0..3
message: string
members: string[]
state: any
reason: ValidationResultReason       // defaults to Rule
reasonDetail?: string
```

⚠️ **There is no `propertyName` and no string severity.** A rule attributes its
failure to one or more `members`, and `severity` is the numeric
`ValidationResultSeverity` enum. Code that reads `v.propertyName` or compares
`v.severity === 'Error'` is reading fields that do not exist.

Validator refusals, explicit `ValidationResult` outcomes from `Provide()`/`Handle()`,
and `IEnumerable<ValidationResult>` outcomes from `Provide()` share
`validationResults`, subject to blocking severity. Validators own refusals when
usable facts exist; `Provide()` short-circuits only when acquisition yields
nothing usable, and `Handle()` must not duplicate business validation.

### Result decision table

Check `isMissingHandler` and `isRetryable` before the ordinary success path;
do not recompute success from only authorization, validity and exceptions.
The server's computed `IsSuccess` formula above does not include those flags.
Use `response` only after classifying the result as successful. The two interface
flags are optional: absence is not a positive retry or missing-handler signal.

| Outcome | Application response |
| --- | --- |
| `isMissingHandler === true` | safe unavailable-action feedback; log the integration/configuration fault, not field errors |
| `isRetryable === true` | temporary refusal, not a permanent denial or invalid input; preserve the draft and offer deliberate retry |
| `isSuccess` after those checks | use the typed response, then close/refresh |
| `!isAuthorized` | distinguish unavailable from forbidden via `isNotAvailable(result)` from `@cratis/arc/availability`; do not automatically redirect every refusal to login |
| `!isValid` with a protected-read refusal below | retain the draft, re-read, then let the user deliberately retry |
| `!isValid` with authored `rule` results | match approved user-facing rule copy to fields through `members` |
| Other invalid results | choose safe application copy by `reason`/`reasonDetail`, never parse `.message` |
| `hasExceptions` | generic fault feedback; diagnostics belong in logs |

A retryable flag does not imply that a protected-read result carries that flag:
classify the known reason pair independently, before generic field-error display.

### Protected-read refusals

`ValidationResultReason` is an open string set. Recognize only these exact pairs:

| `reason` | `reasonDetail` | Meaning |
| --- | --- | --- |
| `concurrencyViolation` | `ReadChanged` | the guarded read changed before effects could commit |
| `concurrencyViolation` | `ReadIncomplete` | a required read did not complete; no effects committed |

For both, keep the draft and dialog open, re-read the relevant state and allow a
**deliberate retry**. Never mark the user's input invalid or clear fields for
these refusals. They are not authored business-rule failures. Framework-composed
messages are developer English, not localized user-facing copy: never show them
raw. Do not render read state, keys or conflicting-event details in visible UI
or announcements. Choose application copy by the pair, not by parsing the
message. Unknown reasons or details are not automatically protected-read refusals or framework
errors; use a safe fallback unless the application explicitly approves their copy.

`members` is **camelCased on purpose**, so a form can match a server failure to
the field that caused it without transforming anything. A nested member is a
dotted path from the graph root — `address.postalCode`. A failure raised by a
`ConceptValidator<T>` is attributed to the field holding the concept, not to the
concept's inner `Value`, because TypeScript erases the concept to its primitive
and names the field the same way.

`reason` also distinguishes a genuine rejection from a validator that threw
while validating hostile or partial input: the latter arrives as a single result
with a `ValidatorFailed` (`validatorFailed`) reason and a generic message, replacing every message
the validator's author wrote. Do not read the message to tell the two apart.
Other framework reasons include `dependencyUnavailable` (facts unavailable before
rules), `concurrencyViolation`, `constraintViolation` and `frameworkFailure`.
Use safe application feedback for these non-rule diagnostics, not blanket
field-error rendering. `reasonDetail` can identify a constraint; `state` is
application data, not proof that a message is safe. Never show
`exceptionMessages`/`exceptionStackTrace` to users.

### Feedback helpers are not sanitizers

`CommandForm` renders raw validation messages; `toastCommandResult` joins them
into its invalid-result toast. Neither sanitizes non-rule diagnostics. Use
application-owned feedback. `showErrors={false}` suppresses built-in form error
display, not every dialog announcement. `CommandDialog`'s default validation
announcement also includes a raw message; use application-owned safe
`validationAnnouncement` text when needed. `onValidationFailure` alone does not
suppress either display. See the React page skill's
[dialogs.md](../../cratis-arc-react-page/references/dialogs.md) for form composition,
and [queries-and-commands.md](../../cratis-arc-react-page/references/queries-and-commands.md)
for `/validate` feedback under read protection.

## Executing with a severity

`command.execute(allowedSeverity?, ignoreWarnings?)` mirrors the server-side
filtering. `command.validate()` runs authorization and validation on the server
without the handler; `command.validateClientSide()` runs only the extracted
rules locally and never touches the network.
