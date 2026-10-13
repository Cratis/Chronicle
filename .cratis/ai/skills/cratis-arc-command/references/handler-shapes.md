<!-- cratis-ai-managed: skills/cratis-arc-command/references/handler-shapes.md -->
# Handler shapes and how Arc dispatches them

## Contents

- The pipeline around `Handle`
- Dispatch
- What claims an event
- The tuple rule
- Cross-stream appends
- Failure shapes

Verified against `Cratis.Arc.Core` and `Cratis.Arc.Chronicle` **v22.75.0**
(Chronicle dependency 19.39.1). Sources: `Source/DotNET/Arc.Core/Commands/CommandPipeline.cs`,
`CommandOperationExecution.cs` and `Source/DotNET/Chronicle/Commands/CommandContextExtensions.cs`.
Read [decisions and routing](decisions-and-routing.md) for guarded reads,
stream templates, tags and post-commit completion.

## The pipeline around `Handle`

One command execution runs in this order:

1. resolve the handler — no handler produces a `MissingHandler` result;
2. build the command context, including the resolved event source id;
3. begin every `ICommandExecutionScope`;
4. run the command filters — **authorization filters first**, then the rest,
   stopping at the first blocking verdict;
5. resolve `Handle`'s arguments, which is where `Provide()` runs;
6. invoke `Handle`;
7. classify returned values and preflight operation metadata/dependencies;
8. dispatch control/other server values (including Chronicle event enrollment),
   then execute declared operations sequentially while the command is successful;
9. complete the execution scopes in reverse order, select eligible operation
   recovery from commitment facts, and finalize backend observations.

If the final result is not successful, the response is **cleared** before it is
serialized. A caller never receives both a failure and a payload.

Authorization runs before validation deliberately: sorting authorization filters
first is what stops a validation failure from short-circuiting the chain and
leaving the authorization verdict at its permissive default.

## Dispatch

The returned value is dispatched on its **runtime** type, not on the method's
declared return type. The one exception is operation participation: Arc decides
from the **declared** return type whether a command may carry operations, and
only then dispatches concrete operations by runtime type. An operation hidden
behind `object` is rejected, and one inside `IEnumerable<object>` is not executed.

- `Task<T>` and `ValueTask<T>` are awaited first, then the inner value is
  dispatched. (`ARC0010` warns when a synchronous result is wrapped in a `Task`
  for no reason.)
- A `Result<TValue, TError>` — `Cratis.Monads.Result<,>` from
  `Cratis.Fundamentals` — is a `OneOf`, so its inner value is unwrapped and
  dispatched. `Result<TEvent, ValidationResult>` therefore appends on success and
  becomes a validation failure on error. Both directions have implicit
  conversions, so `return new SomethingHappened(...)` and
  `return ValidationResult.Error("...")` both compile in the same method.
- A tuple has every non-null element dispatched independently.
- `ICommandOperation` and `CommandOperations` are reserved for operation
  processing, not ordinary response handlers or the client response. Null is
  absent; raw operation collections are rejected (`ARC0017`).
- Other unclaimed values become the response payload. When operations
  participate, control values are reserved for pipeline handling instead.

When operation values participate, Arc flattens tuples and active union branches,
preflights both operation methods' dependencies, and selects at most one ordinary
response. Control values are processed before other server values regardless of
tuple order. Returned events enroll before operations execute, and scopes complete
afterward. Custom response handlers never receive operation values a second time.
See `cratis-arc-command-operation` for compensation and unsupported nesting.

## What claims an event

The Chronicle response-value handlers claim a value only when the event type is
**registered** — that is, the record carries `[EventType]` and Chronicle's event
type registry knows it.

| Runtime value | Claimed by | Appended to |
| --- | --- | --- |
| A registered event record | single-event handler | the command's event source id |
| `IEnumerable<object>` where every element is a registered event | events handler | the command's event source id |
| `EventForEventSourceId` | single wrapper handler | the wrapper's own id |
| A collection containing at least one `EventForEventSourceId`, every other element being a registered event | wrapper-collection handler | each wrapper to its own id, each plain event to the command's |

See "Choose the return shape" in SKILL.md.

An empty collection statically typed as `IEnumerable<EventForEventSourceId>` is
still recognised and appends nothing, rather than being serialized as the
response.

`[EventType]` takes an optional id and generation. Pass neither for a new event:
the identifier defaults to the type name, and `ARCCHR0004` reports an id that
merely repeats the type name. An id that *differs* from the type name is the
supported way to rename the record while stored events keep resolving, and is
left alone.

## The tuple rule

Within a tuple:

1. operations are reserved for their own phase; other elements are classified
   for response/control handling;
2. ordinary elements nothing can handle are candidates for the response;
3. **more than one unhandleable element throws `MultipleUnhandledTupleValues`** —
   Arc cannot decide which one is the response;
4. the single unhandleable element is set as the response *before* the
   handleable elements are handled, which is what lets the events see it.

That last point is the mechanism behind the create-command idiom: an
`EventSourceId`/`EventSourceId<T>`-derived value is not appendable, so it becomes
the response, and `commandContext.GetEventSourceId()` prefers a response that is
an event-source-id value over the id resolved from the command's properties. The
events in the same tuple are therefore appended to the id the handler just
generated, and the client receives it as the response.

```csharp
public (<ThingId>, <ThingRegistered>) Handle([GeneratedValue("thing")] <ThingId> id) =>
    (id, new(<Name>));
```

Returning two ids, or an id and another plain value, is the failure case.

## Cross-stream appends

An event never carries its own event source. To write to another stream, wrap:

```csharp
public IEnumerable<object> Handle() =>
[
    new EventForEventSourceId(From, new <Withdrawn>(Amount)),
    new EventForEventSourceId(To, new <Deposited>(Amount)),
];
```

`EventForEventSourceId` also carries optional `Subject`, `EventStreamType` and
`EventStreamId` init members when the target stream is not the default.
Explicit wrapper routing wins over command-context routing in every return shape.

A command that always appends through a Chronicle event source definition (Arc 22.49.0
and later, definitions from Chronicle 19.30.0) declares it with
`[EventSource<TSource>(stream)]` from `Cratis.Arc.Chronicle.Commands`; the definition
supplies the event source type and validates the optional stream name:

```csharp
[EventSource]
[EventStream("transactions")]
public class Account : IEventSource;

[Command]
[EventSource<Account>("transactions")]
public record RecordTransaction(AccountId AccountId, decimal Amount)
{
    public <TransactionRecorded> Handle() => new(Amount);
}
```

An event that sets legacy `EventSourceType`/`EventStreamType` is appended by those
strings, not through the definition. A command may also implement
`ICanProvideEventStreamId` to supply the stream id. Do not combine a definition with
contradicting `[EventSourceType]`/`[EventStreamType]` attributes.

Concurrency scope is resolved **per target stream** — one expected tail per
event source id, not one shared across the streams a cross-stream command writes
to.

Do not reach for `IEventLog` to do this instead. `ARCCHR0007` warns when a
command's `Handle` injects `IEventLog`, because that bypasses Arc's append
pipeline along with its correlation and ordering guarantees.

## Failure shapes

| Situation | What the caller sees |
| --- | --- |
| No handler for the command type | `ExceptionMessages` names the command type |
| Authorization filter denies | `IsAuthorized` false, `AuthorizationFailureReason` set |
| Validator or `Provide()` returns `ValidationResult.Error` | `IsValid` false, `ValidationResults` populated |
| Append rejected by a Chronicle constraint | a validation failure whose `Reason` says a constraint rejected it and whose `ReasonDetail` names it |
| `Provide()`/`Handle()` throws | `HasExceptions` true — HTTP 500, not a validation failure |
| Two unhandleable tuple elements | `MultipleUnhandledTupleValues` becomes an exception outcome |

Branch on `ValidationResult.ReasonDetail` rather than on `Message`. The message
is prose written for a human and is free to change; the detail is the identity of
the specific thing that rejected the command.
