# Event Store

Chronicle offers what is called an event store which basically means a special purpose database
for storing events. The events are stored in [event sequences](./event-sequence.md).

In addition to this, the Chronicle event store also maintains information about things like the
[event types](./event-type.md), [observers](./observers.md), [projections](./projection.md) and more.

The Chronicle event store is built on top of [MongoDB](https://mongodb.com). In a production environment
you have to bring your own MongoDB environment and configure Chronicle to work with it. For local development,
Chronicle provides a development Docker image that comes with MongoDB bundled inside it.

## Namespaces

Every event store can have [namespaces](./namespaces.md). The namespaces provides a way to segregate data that is specific for
a partition. Typically, a namespace can be used for multi-tenancy.

By default, Chronicle will use the **Default** namespace if a namespace is not provided.
