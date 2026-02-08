# Creating an event

Events are basic types that represent something that happened in the system
We typically give these a name that has meaning in the domain one is working in.
This name represents something that happened to the system and is typically named
in past tense.

In addition to the name, it needs to be uniquely identified. The same event is
not necessarily ubiquitous in name and therefor needs an identifier. The event
can also change name over time as the domain knowledge and language evolves.
Technically for Cratis, the Kernel also does not know anything about the connected
client and in fact one can be defining events outside of code, leveraging the REST
API and in the future, other clients for other programming languages.

With C# you define an event simply by creating a `record` and adorning it with the
attribute `[EventType]` located in the `Cratis.Chronicle.Events` namespace.

```csharp
using Cratis.Chronicle.Events;

[EventType]
public record DebitAccountOpened(AccountName Name, CustomerId Owner);
```

You can also create event types using classes and properties. However, since events are
to considered immutable, `record` gives you that out of the box without having to resort
to either `init` properties or private properties with a constructor that sets them.

> Note: Notice the use of concepts for the value types
> on the events.

All events are automatically discovered at runtime and registered in the Cratis Schema Store.
