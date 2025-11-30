# Event Type

Every [event](./event.md) stored in the [event store](./event-store.md) must have
a type associated with it.

Any client connecting to Chronicle needs to communicate the event types before any events
can be appended to an [event sequence](./event-sequence.md).

An event type can contain multiple generations. Every new event starts with generation 1
and any changes to the event should then become a new generation. This will allow versioning
your event types and also allow for migrating between different event type generations with
specific **upcasters** and **downcasters**.

The concept of migration is very important when working with systems that evolve over time.
Being then able to just roll out new versions with changes and automatically have the events
migrated to different generations is critical for evolving systems.

For detailed information on how to define and use migrations, see [Event Type Migrations](./event-type-migrations.md).
