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

## Prometheus Metrics

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

## Zipkin tracing

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
