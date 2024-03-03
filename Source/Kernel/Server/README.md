# Kernel

There is a `docker-compose.yml` file in the folder for Kernel server.
This sets up what is needed to work with Kernel development.

## Seq logging

[Seq](https://datalust.co/seq) has been configured as logging tool in the `docker-compose.yml` file.
To use it for the kernel the Serilog Sink is configured in the `appsettings.Development.json` file as below.
If you're running without Seq, remove the config.

```json
{
    "Serilog": {
    {
        "WriteTo": [
            {
                "Name": "Seq",
                "Args": {
                    "serverUrl": "http://localhost:5341"
                }
            }
        ]
    },
}
```

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
 2483  Cratis.Ke  /Source/Kernel/Server/bin/Debug/net6.0/Cratis.Kernel.Server
```

Then use the process id as parameter for the monitor:

```shell
dotnet counters monitor --process-id 2483 --counters Cratis.Kernel
```

You should then be seeing something similar:

```shell
Press p to pause, r to resume, q to quit.
    Status: Running

[Cratis.Kernel]
    cratis_event_sequences-appended-events (Number of events app
        event_sequence_id=00000000-0000-0000-0000-000000000000,m           0
        event_sequence_id=ae99de1e-b19f-4a33-a5c4-3908508ce59f,m           0
    cratis-connected-clients-00000000-0000-0000-0000-00000000000           1
    cratis-connected-clients-12c737d2-e816-46f4-96fd-67fc1bf7108           1
```

### Prometheus

[Prometheus](https://prometheus.io) has been configured for collecting metrics from the Kernel in the `docker-compose.yml` file.
It leverages an Open Telemetry exporter that the Kernel connects to with the configuration in `cratis.json` file as below:

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
http://localhost:9090/graph?g0.expr=appended_events&g0.tab=0&g0.stacked=0&g0.show_exemplars=0&g0.range_input=15m

### Zipkin

[Zipkin](https://zipkin.io) has been configured for collecting metrics from the Kernel in the `docker-compose.yml` file.
It leverages an Open Telemetry exporter that the Kernel connects to with the configuration in `cratis.json` file as below:

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
http://localhost:9411/zipkin/?lookback=15m&endTs=1681920441136&limit=10

## Resources

For more details on how Open Telemetry, metrics & tracing works, the following resources are great:

* https://www.meziantou.net/monitoring-a-dotnet-application-using-opentelemetry.htm
* https://code-maze.com/tracking-dotnet-opentelemetry-metrics/
* https://dev.to/jmourtada/how-to-setup-opentelemetry-instrumentation-in-aspnet-core-23p5
* https://learn.microsoft.com/en-us/azure/azure-monitor/app/tutorial-asp-net-custom-metrics
* https://github.com/open-telemetry/opentelemetry-dotnet/blob/1.0.0-rc9.7/src/OpenTelemetry/README.md
* https://learn.microsoft.com/en-us/azure/azure-monitor/app/opentelemetry-enable?tabs=net
