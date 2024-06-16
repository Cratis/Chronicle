# Tenants

Cratis is a built for multi-tenancy from the ground up. It needs to know about
the different tenants.

Within the `chronicle.json` file you should have a `tenants` key and object to hold its configuration:

```json
{
    "tenants": {}
}
```

The tenants object is expecting a key per tenant with a unique identifier (GUID) for each
tenant and then a configuration object for it:

```json
{
    "tenants": {
        "3352d47d-c154-4457-b3fb-8a2efb725113": {
            "name": "development"           // Friendly name of the tenant
        }
    }
}
```

## Tenant specific configuration values

For each tenant, you can associate global configuration values. These are stored as simple key / value pairs for every tenant
within the Kernel. The key / value pairs are global across all microservices.

```json
{
    "tenants": {
        "3352d47d-c154-4457-b3fb-8a2efb725113": {
            "name": "development",
            "configuration": {              // Configuration values.
                "something": "42.42"
            }
        }
    }
}
```

The values can also be set using the Kernels API and tooling, read more about it and how to leverage the values from
code [here](../../clients/dotnet/tenants.md).
