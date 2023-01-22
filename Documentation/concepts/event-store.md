# Event Store

Cratis offers what is called an event store which basically means a special purpose database
for storing events. The events are stored in [event sequences](./event-sequence.md).

In addition to this, the Cratis event store also maintains information about things like the
[event types](./event-type.md), [observers](./observer.md), [projections](./projection.md) and more.

The Cratis event store is built on top of [MongoDB](https://mongodb.com). In a production environment
you have to bring your own MongoDB environment and configure Cratis to work with it. For local development,
Cratis provides a development Docker image that comes with MongoDB bundled inside it.

The goal of Cratis is to provide tooling to navigate the artifacts within the event store
directly, while all of this is not in place since everything is MongoDB, you can quite easily navigate all
the data using the MongoDB tools of your choice.

## Microservices

Cratis supports microservice environments. Every microservice is uniquely identified with a GUID.
Every microservice gets their own event store database, completely segregating the data.

## Monoliths

Not every system is microservice oriented, Cratis does not have this as a requirement, you can run monoliths with Cratis as well.
For a monolith setup, you would typically only have one microservice. When configuring the client, all you
need to do is to not emit a `microserviceId`. This will then default to a microservice identifier of `00000000-0000-0000-0000-000000000000`.

## Tenants

Cratis is [multi-tenant](./tenancy.md). Every tenant gets its own event store database per microservice,
completely segregating the data between the different tenants for each microservice.

You can run Cratis as a single tenant by just configuring one tenant.
