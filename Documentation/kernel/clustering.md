# Clustering

THe Cratis Kernel is built to support clustering for reliability and scale out scenarios.
It is built on top of [Microsoft Orleans](https://docs.microsoft.com/en-us/dotnet/orleans/) and
leverages it for clustering.

## cluster.json

```json
{
    "name": "Cratis",                   // Name of cluster
    "type": "",                         // The type of clustering (local, static, azure-storage, ado-net)
    "advertisedIP": "127.0.0.1",        // For the Kernel, what IP the specific instance is advertising
    "siloPort": 11111,                  // The Orleans silo port. For kernel this is what it exposes
    "gatewayPort": 30000,               // The Orleans gateway port. For kernel this is what it exposes, for client this is that it connects to
    "options": {}                       // Options specific for the type of cluster configuration configured
}
```

## Development

In local development Cratis leverages the [local development configuration](https://docs.microsoft.com/en-us/dotnet/orleans/host/configuration-guide/local-development-configuration).
This is the default configuration and behavior for both the Kernel and the connecting client.

Both the Kernel and the client can be configured with the following:

```json
{
    "name": "Cratis",                   // Name of cluster
    "type": "local"                     // The type of clustering (local, static, azure-storage, ado-net)
}
```

## Production

When running in production, the local development configuration will not work. The Kernel needs to be put
into cluster mode. This involves having a central

### Unreliable static cluster of dedicated servers

For testing on a cluster of dedicated server when reliability isn't a concern, one can run multiple instances
of the Kernel. You need to designate one of the instances as the **Primary** instance.

Kernel configuration for static cluster:

```json
{
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
```

For the client you need to configure the options slightly different with gateways it should connect to.

```json
{
    "name": "Cratis",
    "type": "static",
    "options": {
        "gateways": [
            {
                "address": "127.0.0.1",
                "port": 30000
            },
            {
                "address": "SECOND_KERNEL_IP",
                "port": 30000
            },
            {
                "address": "THIRD_KERNEL_IP",
                "port": 30000
            }
        ]
    }
}
```

### Azure Storage

```json
{
    "name": "Cratis",
    "type": "azure-storage",
    "advertisedIP": "127.0.0.1",
    "siloPort": 11111,
    "gatewayPort": 30000,
    "options": {
        "connectionString": "",
        "tableName": ""
    }
}
```

## ADO .NET

https://www.devart.com/dotconnect/connection-strings.html
https://dotnet.github.io/orleans/docs/host/configuration_guide/configuring_ADO.NET_providers.html

SQL Scripts:
https://github.com/dotnet/orleans/tree/main/src/AdoNet
https://github.com/dotnet/orleans/tree/main/src/AdoNet/Shared
https://github.com/dotnet/orleans/tree/main/src/AdoNet/Orleans.Clustering.AdoNet


```json
{
    "name": "Cratis",
    "type": "ado-net",
    "advertisedIP": "127.0.0.1",
    "siloPort": 11111,
    "gatewayPort": 30000,
    "options": {
        "connectionString": "Data Source=localhost;Initial Catalog=Orleans;User ID=sa;Password=1234Abcd;Pooling=False;Max Pool Size=200;MultipleActiveResultSets=True",
        "invariant": "System.Data.SqlClient"
    }
}
```


