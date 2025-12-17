# Event

Chronicle is optimized for the scenario of [domain events](https://www.martinfowler.com/eaaDev/DomainEvent.html).
Figuring out the correct name of an [event type](./event-type.md) and what properties it should have can be hard.
This page walks through some general guidance on how to do so. Recommend also reading the [Microsoft guide](https://docs.microsoft.com/en-us/dotnet/architecture/microservices/microservice-ddd-cqrs-patterns/domain-events-design-implementation) on this.

> Note: Read more about the relationship to [event source](./event-source.md)

## Past tense

All events should be named as past tense, something that has happened:

- ItemAddedToCart
- UserRegistered
- AddressChangedForPerson

## Singular purpose

An event should never be ambiguous. It should have a clear meaning and purpose.
It should not be holding information for multiple purposes. For instance holding properties that are empty and only used
for specific purposes. Doing so makes the event unclear to reason about and requires logic in all observers and consumers
of the event.

## Immutable

In an event sourced system, events are considered immutable because they represent facts that have occurred in the past.
Once an event is recorded, it should not be altered, as doing so would compromise the integrity and reliability of the event log.
Immutability ensures that the history of changes is preserved accurately, allowing the system to reconstruct past states and understand the sequence of events that led to the current state.
This immutability is crucial for debugging, auditing, and maintaining a consistent and trustworthy system.

## Avoid nullables / empty values

An event has happened, its not supposed to represent unclear states.
As with singular purpose, nullables means its up for interpretation and logic needs to be in place to reason about it
for every observers and consumers of the event.

> Note: There are conditions where it makes sense to allow null, typical data collection scenario might be the case.
> For instance, a person might not have a middle name. Recommend reading up on concepts and nullability [here](../../Fundamentals/csharp/concepts.md)

## Cohesion

Figuring out how "big" an event should be can be even harder.
The things that needs to go together to represent a meaningful change in the system should constitute the boundaries.
If we take a system that holds personal details, the name and social security numbers would be separate from
the address.

Below are some samples of events.

### PersonRegistered

During registration, it might be necessary to capture all the details about a person as below:

```json
{
    "socialSecurityNumber": "12345678901",
    "firstName": "John",
    "middleName": "David",
    "lastName": "Doe",
    "address": "32nd & Main street",
    "postalCode": "123456",
    "city": "Somewhere",
    "country": "Atlantis"
}
```

### NameChangedForPerson

Once registered, changing the name could then only be:

```json
{
    "firstName": "John",
    "middleName": "David",
    "lastName": "Doe"
}
```

### AddressChangedForPerson

Similar for address:

```json
{
    "address": "32nd & Main street",
    "postalCode": "123456",
    "city": "Somewhere",
    "country": "Atlantis"
}
```

## Integrations

When integrating with systems that are non event sourced you would need to deduct the event types from the data structure.
The same approach applies for defining the events as described in this document.

## Event discovery

There are a couple of well known approaches to discovering events in your domain; [Event Storming](https://www.eventstorming.com)
and [Event Modelling](https://eventmodeling.org). We tend to favor the latter as it has more focused building blocks. It gives a good
overview of a an entire system and the flows in it.
