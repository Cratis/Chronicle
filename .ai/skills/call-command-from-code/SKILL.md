---
name: call-command-from-code
description: Execute a Cratis Arc command from backend code via ICommandPipeline — the scopeless and scoped overloads, typed Execute<TResult>, pre-flight Validate, and reading CommandResult. Use when a reactor, background job, scheduled task, integration test, or backend service needs to run a command without going through HTTP.
---

# Call a Command from Code

Backend code can drive a command through `ICommandPipeline` instead of an HTTP request. The pipeline runs the same authorization → validation → `Provide()` → `Handle()` chain as the HTTP boundary — only the entry point changes. Common callers: a reactor spawning a follow-up command, a background/scheduled job, an integration test harness.

> Inside `Handle()` wanting to append to a *different stream*, do **not** inject `IEventLog` — return `IEnumerable<object>` with `EventForEventSourceId(targetId, @event)` wrappers (see `vertical-slices.md`). `ICommandPipeline` is for **command-shaped** invocations that need validation/authorization.

## Steps

### 1. Inject and execute (scopeless overload)

```csharp
public class <Source>Handler(ICommandPipeline pipeline) : IReactor
{
    [OnceOnly]
    public Task <Source>Created(<Source>Created @event, EventContext context) =>
        pipeline.Execute(new <TargetCommand>(@event.SourceId, @event.Name));
}
```

`Execute(command)` is the scopeless overload — the pipeline manages its own DI scope per call. This is the normal case.

### 2. Scoped overload — only when needed

`Execute(command, serviceProvider)` reuses the caller's ambient DI scope. Use it only when the handler genuinely depends on resolving something from that scope (e.g. a scoped DB session). Most callers don't need it.

### 3. Typed result

```csharp
var result = await pipeline.Execute<RegistrationToken>(new Register(...));
if (result.IsSuccess) { var token = result.Response; }
```

### 4. Pre-flight `Validate`

`await pipeline.Validate(command)` runs authorization + validation **without** executing `Provide()`/`Handle()` — useful to confirm validity before committing to side effects. Scoped overload available.

**Validation severity filtering:** both `Execute` and `Validate` take an optional `allowedSeverity` of type `ValidationResultSeverity?` (from `Cratis.Arc.Validation`) — `pipeline.Execute(command, allowedSeverity: ValidationResultSeverity.Warning)`. The levels are `Unknown=0`, `Information=1`, `Warning=2`, `Error=3`; by default only `Error` blocks, and failures at or below `allowedSeverity` are filtered out (so passing `Warning` lets warnings through but still blocks errors).

### 5. Inspect `CommandResult` by the granular flag

`IsSuccess` is the AND of the others — check the specific one your caller cares about:

| Flag | Meaning |
|---|---|
| `IsAuthorized` | identity satisfied the authorization attributes |
| `IsValid` | validators passed (`ValidationResults` carries failures) |
| `HasExceptions` | `Provide()`/`Handle()` threw (`ExceptionMessages`) |
| `CorrelationId` | pipeline-wide id for log/trace correlation |

### 6. Don't throw for expected failures

Validation and authorization failures are normal flow — surface them through `CommandResult`, never as exceptions. Reserve exceptions for genuine `Provide()`/handler crashes.

### 7. Reactors: pair with `[OnceOnly]`

A reactor calling `Execute` is not idempotent on replay — every replay re-fires the command. Always combine with `[OnceOnly]`.

## Common pitfalls

| Pitfall | Why |
|---|---|
| Throwing on `!result.IsValid` instead of surfacing `ValidationResults` | loses structured failure info |
| `Execute` from a reactor without `[OnceOnly]` | replay re-fires the command |
| Defaulting to the scoped overload | most callers don't need it |
| Asserting only `IsSuccess` | can't tell auth vs validation vs exception apart |

## Quality gate

- [ ] Build is clean.
- [ ] A reactor that calls `Execute` is decorated with `[OnceOnly]`.
- [ ] The caller checks the granular `CommandResult` flags before treating the result as success.

## See also

- `vertical-slices.md` — reactors, `Handle()` return shapes, cross-stream events.
- `add-business-rule` / `auth-and-identity` — the validation/authorization the pipeline runs.
