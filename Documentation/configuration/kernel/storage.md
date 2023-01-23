# Storage

Storage of projected read models / documents and events are configured through the Cratis Kernel.
The reason for this is that projections typically share the storage of what the queries in a client
is built on. To avoid configuring things in multiple places, we have it in the Kernel and the
client will get this configuration upon startup.

Within the `cratis.json` file you should have a `storage` key and object to hold its configuration:

```json
{
    "storage": {}
}
```

The `storage` object is expecting the following shape:

```json
{
    "storage": {
        "cluster": {                                            // Configuration for the shared database for the entire Cratis cluster
            "type": "MongoDB",
            "connectionDetails": "mongodb://localhost:27017/cratis-shared"
        },
        "microservices": {                                      // Configuration per microservice
            "00000000-0000-0000-0000-000000000000": {           // Microservice Id
                "shared": {                                     // Data that is shared between all the tenants
                    "eventStore": {
                        "type": "MongoDB",
                        "connectionDetails": "mongodb://localhost:27017/event-store-shared"
                    }
                },
                "tenants": {                                    // Tenant specific configuration
                    "3352d47d-c154-4457-b3fb-8a2efb725113": {   // Tenant identifier of the tenant
                        "readModels": {                         // Read models
                            "type": "MongoDB",
                            "connectionDetails": "mongodb://localhost:27017/dev-read-models"
                        },
                        "eventStore": {                         // Event Store
                            "type": "MongoDB",
                            "connectionDetails": "mongodb://localhost:27017/dev-event-store"
                        }
                    }
                }
            }
        }
    }
}
```

The kernel recognizes 2 types of storage types; **readModels** and **eventStore**.

## Cluster level

The Cratis cluster has data that is shared across all microservices and across all tenants.
This is stored in the `cluster` database that is first configured.

## Microservices

Every microservice would have a key within the `microservices` key of the `storage` object.
If you only have one microservice, you still need this. The default for a single microservice or monolith
would be `00000000-0000-0000-0000-000000000000`.

### Shared

Cratis has information that is shared across all tenants. This is the `shared` configuration within
a microservice.

### Tenancy

A microservice must have at least one tenant, even if your software is a single tenant environment.
The `tenants` part of the configuration configures all your tenants.

## Type

Within each storage configuration there is a **type** property that is required. This property specifies which
storage technology to use.

> Note: As of January 2022 - MongoDB is the only supported type. The Kernel has been designed to support others,
> and could do so if there is enough interest for it.

For MongoDB the value would be a valid MongoDB connection string, fully qualified with the database
name at the end.
