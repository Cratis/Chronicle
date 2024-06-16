# Kernel

The Cratis client can connect to the Kernel using the supported clustering modes of the Kernel.
To configure the Cratis client you'll need a `chronicle.json` file that sits next to your application
binaries or within a folder called `config` next to your application binaries.
The `chronicle.json` file is an optional file, if it is not present it will assume default values
which are typically what you're looking for when doing local development.

Within the `chronicle.json` file you should have a `kernel` key and object to holds its configuration:

```json
{
    "kernel": {}
}
```

The properties expected on the `kernel` object is as follows:

```json
{
    "kernel": {
        "type": "",                     // Type of cluster to connect to (single, static, azure-storage)
        "advertisedClientEndpoint": "", // The endpoint for the client for the Kernel to call back to in URI format (<scheme>://<host>:<port> e.g. http://localhost:5000)
        "options": {}                   // Options specific for the type of cluster configuration configured
    }
}
```

> Note: The `advertisedClientEndpoint` is

## Single cluster (local development)

You can configure the Kernel to be a single cluster, which is considered an unsafe non-production cluster mode aimed
for local development only.

You configure it as follows:

```json
{
    "kernel": {
        "type": "single",
        "options": {
            "endpoint": "http://localhost:8080"     // The endpoint for the Cratis kernel, will default to http://localhost:8080
        }
    }
}
```

## Production

When running in production, the local development configuration will not work. The Kernel needs to be put
into cluster mode. This involves the nodes need to be aware of the other nodes. It does this by either
having a centralized well known storage that holds the registered known and active nodes, or by statically
configure it into the configuration file.

### Unreliable static cluster of dedicated servers

For testing on a cluster of dedicated server when reliability isn't a concern, one can run multiple instances
of the Kernel.

You configure it as follows:

```json
{
    "kernel": {
        "type": "static",
        "options": {
            "endpoints": [
                "http://kernel-1:8080",
                "http://kernel-2:8080",
                "http://kernel-3:8080"
            ]
        }
    }
}
```

### Azure Storage

When running in Azure there is a provider that can be used that leverages the Azure storage tables.

You configure it as follows:

```json
{
    "kernel": {
        "type": "azure-storage",
        "options": {
            "connectionString": "",         // The Azure Storage connection string found in the Azure portal
            "tableName": "",                // Optional table name for Orleans instances - defaults to 'OrleansSiloInstances'
            "port": 80,                     // What port to connect to, default 80
            "secure": false                 // Whether or not to use HTTPS to connect to the Kernel, default false
        }
    }
}

> Note: `connectionString` is typically in the following format:
> DefaultEndpointsProtocol=https;AccountName=[account name];AccountKey=[account key];EndpointSuffix=core.windows.net
