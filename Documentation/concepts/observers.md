# Observers

An observer is a part of your system that is "listening" to events.
When events are appended to an [event sequence](./event-sequence.md) that the observer
is observing, it will automatically be notified of the event.

Every observer decides what events they are interested in during the subscription process.
This information is stored in the [event store](./event-store.md).

If the observer gets additional event types it is observing, the event store will look if there
are events prior to its current offset of the new event type it is now observing. If there are,
it will automatically be rewound to the beginning of the sequence and replayed.

During replay, the observer will not be active and when it reaches the end of the sequence it
will become active again and observe any new events.

## State management

One of the things you can use an observer for is to maintain application state, typically update
data in a database that is used for reading. However, it is recommended to use [projections](./projection.md)
for this purpose, as long as projections support your scenario. Projections are not a catch all
solution, and sometimes you need to do it manually. It is also a possibility to combine manual state
management with **immediate projections**.

## Reactors

Reactors are great for the **if this then that** type of scenarios. When an event occurs,
you can react to it and perform an action. This could be any type of action, e.g. out-of-process actions
like sending emails or calling an API. You could also append new events in an observer and make
your observer become a pattern matcher or a state machine that responds to certain conditions and
then generate more specific events for that condition.

## Reducers

While projections is a declarative approach and has some limitations, a reducer can help give you more
capabilities. With a reducer, Chronicle manages the actual storage. Chronicle will call you on every
event you are interested in for your reducer and you will get the existing state of the read model, if any,
and you can then provide a new representation of state as a consequence of the event. The result will then
be stored.
