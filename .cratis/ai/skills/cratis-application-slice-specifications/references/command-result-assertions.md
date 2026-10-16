<!-- cratis-ai-managed: skills/cratis-application-slice-specifications/references/command-result-assertions.md -->
# CommandResult assertions

This reference holds the CommandResult assertion table and the authorization-versus-validation distinction.

`CommandResult` assertions, from `Cratis.Arc.Testing.Commands`, throwing
`CommandResultAssertionException`:

| Assertion | Says |
| --- | --- |
| `ShouldBeSuccessful()` / `ShouldNotBeSuccessful()` | Authorized, valid, no exceptions — or not |
| `ShouldBeValid()` | Validation only; it checks neither authorization nor exceptions |
| `ShouldHaveValidationErrors()` | At least one validation error |
| `ShouldHaveValidationErrorFor(message)` | A validation error whose message **contains** that text (a substring match; compare messages for equality when exact wording is specified) |
| `ShouldHaveValidationErrorBecauseOf(reason)` | A validation error with that `ValidationResultReason` |
| `ShouldBeAuthorized()` / `ShouldNotBeAuthorized()` | The authorization outcome |
| `ShouldHaveExceptions()` / `ShouldNotHaveExceptions()` | Whether the handler threw |
| `ShouldHaveConstraintViolationFor(name)` | A constraint violation surfaced on the command result |

`ShouldHaveValidationErrorBecauseOf(reason)` is the message-free way to say
*which kind* of rejection happened — `ValidationResultReason.ConstraintViolation`,
`ConcurrencyViolation`, `ValidatorFailed`, `Rule`. Prefer it over
`ShouldHaveValidationErrorFor(message)`, which pins presentation text.

Authorization failures are different in kind: an unauthorized result carries
**no** validation errors, so assert `ShouldNotBeAuthorized()`. Adding an
authorization attribute to an existing command therefore breaks both its
happy-path and its validation-failure specifications — switch the assertions
rather than patching around them.

The scenario's own surface is `Services`, `Context`, `Execute(command[, ct])`,
`Validate(command)` (runs the filters without the handler), `LastResult`,
`Operations` and the three operation assertions
(`ShouldHaveExecutedOperation<T>()`, `ShouldHaveCompensatedOperation<T>()`,
`ShouldHaveNoOperationInvocations()`); it is `IDisposable`/`IAsyncDisposable`.
Seeding (`Given`), the event log and the event assertions arrive as extension
members from the Chronicle integration.
