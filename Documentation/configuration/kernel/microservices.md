# Microservices

Cratis is a built for multi-tenancy from the ground up. It needs to know about
the different microservices.

Within the `chronicle.json` file you should have a `microservices` key and object to hold its configuration:

```json
{
    "microservices": {}
}
```

The microservices object is expecting a key per microservice with a unique identifier (GUID) for each
microservice and then a configuration object for it:

```json
{
    "microservices": {
        "00000000-0000-0000-0000-000000000000": {
            "name": "My Microservice"           // Friendly name of the microservice
        }
    }
}
```
