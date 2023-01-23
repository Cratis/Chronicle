# Clustering

The Cratis Kernel is built to support clustering for reliability and scale out scenarios.
It is built on top of [Microsoft Orleans](https://docs.microsoft.com/en-us/dotnet/orleans/) and
leverages it for clustering.

Within the `cratis.json` file you should have a `cluster` key and object to hold its configuration:

```json
{
    "cluster": {}
}
```

The properties expected on the `cluster` object is as follows:

```json
{
    "cluster": {
        "name": "Cratis",                   // Name of cluster
        "type": "",                         // The type of clustering (single, static, azure-storage, ado-net)
        "advertisedIP": "127.0.0.1",        // For the Kernel, what IP the specific instance is advertising
        "siloPort": 11111,                  // The Orleans silo port. For kernel this is what it exposes
        "gatewayPort": 30000,               // The Orleans gateway port. For kernel this is what it exposes, for client this is that it connects to
        "options": {}                       // Options specific for the type of cluster configuration configured
    }
}
```

## Single cluster (local development)

You can configure the Kernel to be a single cluster, which is considered an unsafe non-production cluster mode aimed
for local development only. It leverages the Orleans [local development configuration](https://docs.microsoft.com/en-us/dotnet/orleans/host/configuration-guide/local-development-configuration).
This is the default configuration and behavior for both the Kernel and the connecting client.

You configure it as follows:

```json
{
    "cluster": {
        "name": "Cratis",                   // Name of cluster
        "type": "single"                    // The type of clustering (single, static, azure-storage, ado-net)
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
of the Kernel. You need to designate one of the instances as the **Primary** instance.

Kernel configuration for static cluster:

```json
{
    "cluster": {
        "name": "Cratis",
        "type": "static",                   // static type
        "advertisedIP": "127.0.0.1",
        "siloPort": 11111,
        "gatewayPort": 30000,
        "options": {
            "primarySiloIP": "127.0.0.1",   // The IP address of the primary instance
            "primarySiloPort": 11111        // The port for the primary instance
        }
    }
}
```

> Note: The address can be either an IP address or a hostname.

### Azure Storage

When running in Azure there is a provider that can be used that leverages the Azure storage tables.

The kernel configuration would be:

```json
{
    "cluster": {

        "name": "Cratis",
        "type": "azure-storage",
        "advertisedIP": "127.0.0.1",
        "siloPort": 11111,
        "gatewayPort": 30000,
        "options": {
            "connectionString": "",         // The Azure Storage connection string found in the Azure portal
            "tableName": ""                 // Optional table name for Orleans instances - defaults to 'OrleansSiloInstances'
        }
    }
}
```

Both the Kernel instances and the client instances require the same connection string.

> Note: `connectionString` is typically in the following format:
> DefaultEndpointsProtocol=https;AccountName=[account name];AccountKey=[account key];EndpointSuffix=core.windows.net

## ADO .NET

Another option is to use ADO.NET and supported databases. Out of the box the Kernel and client libraries reference what
is needed to use Microsoft SQL Server. In order for this to work, there is a need to set up artifacts in the SQL server.
You'll find the necessary SQL scripts [here](/Samples/Clustering/create-db.sql), or you can go to the original scripts
from Orleans [here](https://github.com/dotnet/orleans/tree/main/src/AdoNet).

The Kernel configuration would then be:

```json
{
    "cluster": {
        "name": "Cratis",
        "type": "ado-net",
        "advertisedIP": "127.0.0.1",
        "siloPort": 11111,
        "gatewayPort": 30000,
        "options": {
            "connectionString": "Data Source=[sql server];Initial Catalog=Orleans;User ID=[username];Password=[password];Pooling=False;Max Pool Size=200;MultipleActiveResultSets=True",
            "invariant": "System.Data.SqlClient"
        }
    }
}
```

> Note: With version 8.0.0 of Cratis, we temporarily do not support the ADO .NET option for the client.
