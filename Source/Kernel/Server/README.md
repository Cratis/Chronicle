# Kernel

The Chronicle Kernel is the event-sourcing engine that powers the entire stack. This folder contains the server implementation along with infrastructure setup for local development.

## 🗄️ Database Backends

The Kernel supports multiple storage backends. Choose one using the `run.sh` script. **MongoDB is the default** when no database is specified.

| Database | Connection String |
|----------|-------------------|
| MongoDB | `mongodb://localhost:27017` |
| PostgreSQL | `Host=localhost;Database=chronicle;Username=chronicle;Password=chronicle` |
| Microsoft SQL Server | `Server=localhost;Database=chronicle;User Id=sa;Password=Chronicle_Str0ng!;TrustServerCertificate=true` |
| SQLite | `Data Source=./data/chronicle.db` |

## 🚀 Quick Start

Use the `run.sh` script with a database parameter. It automatically:
1. Starts the infrastructure (docker-compose)
2. Sets the correct storage type and connection string
3. Starts the Kernel server

```bash
# MongoDB (default)
./run.sh

# Or specify a database explicitly
./run.sh mongodb
./run.sh postgresql
./run.sh mssql
./run.sh sqlite
```

The Kernel starts on gRPC port 5000.

### Start Infrastructure Only (with `--docker`)

If you want to start only the infrastructure and run the Kernel manually, pass `--docker`:

```bash
./run.sh postgresql --docker
```

Then in another terminal, start the Kernel manually:

```bash
dotnet run
```

Or with hot reload:

```bash
dotnet watch run
```

> **Note:** When running manually, set the environment variables `Cratis__Chronicle__Storage__Type` and `Cratis__Chronicle__Storage__ConnectionDetails`, or the Kernel will use the defaults from `chronicle.json` (MongoDB on localhost:27017).
>
> Example:
>
> ```bash
> export Cratis__Chronicle__Storage__Type=PostgreSql
> export Cratis__Chronicle__Storage__ConnectionDetails="Host=localhost;Database=chronicle;Username=chronicle;Password=chronicle"
> dotnet run
> ```

---

## Metrics

### CLI

You can monitor the Kernel from the CLI.
Install the `dotnet counters` tool:

```shell
dotnet tool install --global dotnet-counters
```

After you have started the Kernel you need to find the process id.
This can be done by running the following:

```shell
dotnet counters ps
```

Find the process for the Kernel:

```shell
 2483  Cratis.Ke  /Source/Kernel/Server/bin/Debug/net6.0/Cratis.Chronicle.Server
```

Then use the process id as parameter for the monitor:

```shell
dotnet counters monitor --process-id 2483 --counters Cratis.Chronicle
```

You should then be seeing something similar:

```shell
Press p to pause, r to resume, q to quit.
    Status: Running

[Cratis.Chronicle]
    cratis_event_sequences-appended-events (Number of events app
        event_sequence_id=00000000-0000-0000-0000-000000000000,m           0
        event_sequence_id=ae99de1e-b19f-4a33-a5c4-3908508ce59f,m           0
    cratis-connected-clients-00000000-0000-0000-0000-00000000000           1
    cratis-connected-clients-12c737d2-e816-46f4-96fd-67fc1bf7108           1
```

### Prometheus

[Prometheus](https://prometheus.io) has been configured for collecting metrics from the Kernel in the `docker-compose.yml` file.
It leverages an Open Telemetry exporter that the Kernel connects to with the configuration in `chronicle.json` file as below:

```json
{
    "telemetry": {
        "type": "open-telemetry",
        "options": {
            "endpoint": "http://localhost:4317"
        }
    }
}
```

Once the Kernel is running you can navigate to the following location:
<http://localhost:9090/graph?g0.expr=appended_events&g0.tab=0&g0.stacked=0&g0.show_exemplars=0&g0.range_input=15m>

### Zipkin

[Zipkin](https://zipkin.io) has been configured for collecting metrics from the Kernel in the `docker-compose.yml` file.
It leverages an Open Telemetry exporter that the Kernel connects to with the configuration in `chronicle.json` file as below:

```json
{
    "telemetry": {
        "type": "open-telemetry",
        "options": {
            "endpoint": "http://localhost:4317"
        }
    }
}
```

Once the Kernel is running you can navigate to the following location:
<http://localhost:9411/zipkin/?lookback=15m&endTs=1681920441136&limit=10>

## Resources

For more details on how Open Telemetry, metrics & tracing works, the following resources are great:

- <https://www.meziantou.net/monitoring-a-dotnet-application-using-opentelemetry.htm>
- <https://code-maze.com/tracking-dotnet-opentelemetry-metrics/>
- <https://dev.to/jmourtada/how-to-setup-opentelemetry-instrumentation-in-aspnet-core-23p5>
- <https://learn.microsoft.com/en-us/azure/azure-monitor/app/tutorial-asp-net-custom-metrics>
- <https://github.com/open-telemetry/opentelemetry-dotnet/blob/1.0.0-rc9.7/src/OpenTelemetry/README.md>
- <https://learn.microsoft.com/en-us/azure/azure-monitor/app/opentelemetry-enable?tabs=net>
