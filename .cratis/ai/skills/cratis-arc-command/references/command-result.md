<!-- cratis-ai-managed: skills/cratis-arc-command/references/command-result.md -->
# CommandResult and ValidationResult

Verified against `Cratis.Arc.Core` `22.10.4` and `@cratis/arc` `22.10.4`.

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

`CommandResult<TResult>` adds a `Response` of `TResult`.

`IsValid` and `HasExceptions` are **derived**, not stored. There is no way to
set them directly — a result is invalid because it carries validation results.

Factories: `Success`, `Unauthorized`, `MissingHandler`, `Error`, `InvalidBody`,
`FromException`.

⚠️ **A failed result carries no response.** The pipeline clears the response once
every execution scope has completed and the result is not successful, so a
transaction that rolls back after `Handle` returned a value does not leak that
value into the error body.

## Severity filtering

`ValidationResultSeverity`: `Unknown = 0`, `Information = 1`, `Warning = 2`,
`Error = 3`.

Only *blocking* results survive onto the result. With no `allowedSeverity`, only
`Error` blocks — information and warnings are filtered out entirely. With an
`allowedSeverity`, anything strictly greater than it blocks. So passing
`Warning` keeps errors blocking and lets warnings through.

## Client side

`@cratis/arc` `ICommandResult<TResponse = object>`:

```ts
readonly correlationId: Guid;
readonly isSuccess: boolean;
readonly isAuthorized: boolean;
readonly isValid: boolean;
readonly hasExceptions: boolean;
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

Match a failure to a field through `members`, and branch on `reasonDetail`
rather than parsing `message` — the message is prose written for a human and is
free to change, while `reasonDetail` is the identity of the specific thing that
rejected the command (a constraint name, for instance).

```tsx
const result = await command.execute();

if (!result.isAuthorized) { /* the caller is not allowed to do this */ }
else if (!result.isValid) {
    const errorFor = (property: string) =>
        result.validationResults.find(v => v.members.includes(property))?.message;
}
else if (result.hasExceptions) { /* server fault */ }
else { /* result.response */ }
```

`members` is **camelCased on purpose**, so a form can match a server failure to
the field that caused it without transforming anything. A nested member is a
dotted path from the graph root — `address.postalCode`. A failure raised by a
`ConceptValidator<T>` is attributed to the field holding the concept, not to the
concept's inner `Value`, because TypeScript erases the concept to its primitive
and names the field the same way.

`reason` also distinguishes a genuine rejection from a validator that threw
while validating hostile or partial input: the latter arrives as a single result
with a `ValidatorFailed` reason and a generic message, replacing every message
the validator's author wrote. Do not read the message to tell the two apart.

## Executing with a severity

`command.execute(allowedSeverity?, ignoreWarnings?)` mirrors the server-side
filtering. `command.validate()` runs authorization and validation on the server
without the handler; `command.validateClientSide()` runs only the extracted
rules locally and never touches the network.
