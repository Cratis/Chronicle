<!-- cratis-ai-managed: skills/cratis-arc-command/references/handler-shapes.md -->
# Handler shapes and how Arc dispatches them

Verified against `Cratis.Arc.Core` and `Cratis.Arc.Chronicle` `22.10.4` with
`Cratis.Chronicle` `16.39.1`.

## The pipeline around `Handle`

One command execution runs in this order:

1. resolve the handler — no handler produces a `MissingHandler` result;
2. build the command context, including the resolved event source id;
3. begin every `ICommandExecutionScope`;
4. run the command filters — **authorization filters first**, then the rest,
   stopping at the first blocking verdict;
5. resolve `Handle`'s arguments, which is where `Provide()` runs;
6. invoke `Handle`;
7. dispatch the returned value;
8. complete the execution scopes in reverse order.

If the final result is not successful, the response is **cleared** before it is
serialized. A caller never receives both a failure and a payload.

Authorization runs before validation deliberately: sorting authorization filters
first is what stops a validation failure from short-circuiting the chain and
leaving the authorization verdict at its permissive default.

## Dispatch

The returned value is dispatched on its **runtime** type, not on the method's
declared return type.

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
- Anything no handler claims becomes the response payload.

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

⚠️ **An unregistered event type is not an error.** No handler claims it, so it
silently becomes the HTTP response body instead of being appended. A command
that "runs fine" but appends nothing is almost always a missing `[EventType]`.

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

1. every element is offered to the handlers;
2. the elements nothing can handle are candidates for the response;
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
public (<ThingId>, <ThingRegistered>) Handle()
{
    var id = <ThingId>.New();
    return (id, new(<Name>));
}
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
