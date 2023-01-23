# Event Type

Every [event](./event.md) stored in the [event store](./event-store.md) must have
a type associated with it.

Any client connecting to Cratis needs to communicate the event types before any events
can be appended to an [event sequence](./event-sequence.md).

An event type can contain multiple generations. Every new event starts with generation 1
and any changes to the event should then become a new generation. This will allow versioning
your event types and also allow for migrating between different event type generations with
specific **upcasters** and **downcasters**.

The concept of migration is very important when working with systems that evolve over time.
Being then able to just roll out new versions with changes and automatically have the events
migrated to different generations is critical for evolving systems.

> Note: As of 22nd of January 2023, migrations is not supported. But the internals of Cratis
> has been prepared for this and it is one of the top priorities to get this supported.
