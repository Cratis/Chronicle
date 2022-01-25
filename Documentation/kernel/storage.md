# Storage

Storage of projected read models / documents and events are configured through the Cratis Kernel.
The reason for this is that projections typically share the storage of what the queries in a client
is built on. To avoid configuring things in multiple places, we have it in the Kernel and the
client will get this configuration upon startup.

The kernel looks for a file called `storage.json` upon startup. Within the Docker images, we've
configured a default one to get up and running fast.

The `storage.json` should look like below:

```json
{
    "readModels": {
        "type": "MongoDB",
        "tenants": {
            "3352d47d-c154-4457-b3fb-8a2efb725113": "mongodb://localhost:27017/development-read-models"
        }
    },
    "eventStore": {
        "type": "MongoDB",
        "shared": "mongodb://localhost:27017/event-store-shared",
        "tenants": {
            "3352d47d-c154-4457-b3fb-8a2efb725113": "mongodb://localhost:27017/development-event-store"
        }
    }
}
```

The kernel recognizes 2 types of storage types; **readModels** and **eventStore**.

## Type

Within each storage type there is a **type** property that is required. This property specifies which
storage technology to use.

> Note: As of January 2022 - MongoDB is the only supported type. This will however be expanded on
> as there are needs for it.

For MongoDB the value would be a valid MongoDB connection string, fully qualified with the database
name at the end.

## Multi tenancy

The configuration object of each of these requires a property called **tenants**. This property is
a key/value of specific configuration for the target type.

## Event Store

The event store has an added property in its configuration; **shared**.
It requires a shared database to keep non-tenant specific information such as event schemas, projection definitions
and more.
