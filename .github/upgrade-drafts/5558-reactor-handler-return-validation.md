<!--
Draft for the documentation session (Cratis/Chronicle#5558). This is not published.

(a) This is a draft for the next major's upgrade page. Move it into Documentation/upgrading/<N>-to-<M>.mdx, add the
    boundary row to major-versions.md, and delete this file before or at the major merge.
(b) The C# blocks must become <ChronicleClientTabs> snippets, because shared pages allow no client-language fences.
(c) Follow-up: Documentation/code-analysis/CHR0054.mdx:8 says such methods are "skipped during handler discovery",
    which is no longer true for public methods.
-->

## Reactors with an unsupported return type now fail at registration

Chronicle now rejects every public reactor handler method that returns a type it cannot process, however many dependencies the method takes. You find out when the reactor registers, instead of discovering later that it silently ignores an event.

### Who is affected

You are affected if a reactor has a public instance method that:

- takes a registered event type as its first parameter,
- takes further parameters other than a single `EventContext` or `ReactorDelivery` (for example a service or a read model), and
- returns something Chronicle cannot process.

### What changes

| Method | Before | Now |
| ------ | ------ | --- |
| `int On(E e)` | Fails at registration | Fails at registration |
| `int On(E e, EventContext c)` | Fails at registration | Fails at registration |
| `int On(E e, IService s)` | Silently skipped | Fails at registration |

A skipped method meant the reactor could end up subscribed to nothing, or quietly miss an event. That is why the check now covers every dependency shape.

### How you notice

Registration throws `InvalidReactorHandlerReturnType`. The message names the reactor, the method and the return type:

```text
Reactor '<reactor>' handler method '<method>' has unsupported return type '<return type>'. ...
```

If you have not suppressed CHR0054, the build already reports these methods.

### Before you upgrade

Audit the public instance methods on your reactors whose first parameter is an event type. Do not suppress CHR0054 to get past it.

### What to do

- If the method is meant to be a handler, return a supported type: `void`, `Task`, an event type, `EventForEventSourceId`, `EventsWithConcurrencyScopes`, an `IEnumerable` of events, a type claimed by a registered `IReactorSideEffectHandler`, an `IEnumerable` of these, or any of these wrapped in a `Task`. Alternatively, register an `IReactorSideEffectHandler` that claims the return type.
- If the method is a helper, make it non-public or move it to a collaborator.

Renaming the method does not help, because discovery is by type, not by name. `[ReactorSideEffect]` alone does not register runtime processing; a registered `IReactorSideEffectHandler` still has to claim the type.

### What does not change

- Private, internal, protected and static helpers.
- `Task<T>` returns and the other supported returns.
- Custom side-effect handlers.
- Dispatch precedence.
- The exception type and its text.
