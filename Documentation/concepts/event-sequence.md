# Event Sequence

Cratis has the concept of event sequences. These can be looked upon as collections of events.
Every event gets assigned an incremental unique sequence number, this if the sequence
number that [observers](./observer.md) uses to maintain their offset.

With every event, Cratis collects additional metadata that is stored together with the event.

Cratis has formalized the following event sequences:

| Type | Description |
| ---- | ----------- |
| Event Log | The main sequence you typically append to |
| Outbox | For events you wish to communicate outside of your microservice to other microservices |
| Inbox | Events coming from other microservices |

## Collections

Looking into the MongoDB and at the different collections for the different types of sequences,
the shape of every event document has the following properties:

| Property | Description |
| -------- | ----------- |
| _id | The sequence number |
| correlationId | A unique identifier of the operation / transaction the event was part of |
| causationId | An identifier for the series of actions that caused the event |
| causedBy | An identifier pointing to information about who or which system caused the event |
| type | The type of the event |
| occurred | When the event occurred |
| validFrom | When the event is valid from, domain specific. Defaults to the minimum date value |
| eventSourceId | The event source identifier the event is for |
| content | The actual content of the event, as a BSON document |
| compensations | An array of compensations performed for the event |

## Event Type generations

Every [event type](./event-type.md) can evolve over time. Evolutions are represented as generations.
The `content` property stores event content per generation, allowing for a perfect audit trail of
the events and its evolution and at the same time also supporting scenarios of delivering different
versions of the events, depending on the observer.

## Compensations

An event sequence is append only, meaning that we do not delete anything within a sequence.
Changing the content of an event is also not supported by Cratis, instead one should perform
compensations.

Compensations are replacements of the original event instance and are in fact the same event type and
are stored together with the original event it replaces. Every compensation is stored with the same
type of metadata as for the root event.

This approach help us guarantee a perfect audit trail of the system.

> Note: As of 22nd of January 2023, compensations are not fully implemented yet, there is no API surface for it.
> Internally Cratis has been prepared for it and that is why you'll see the compensations array on every
> event.
