# Running with Docker Compose

Using the development image of Cratis is very convenient for getting started quickly.
Sometimes you want more control over the running environment and have MongoDB as its own
thing and possible other services, for collecting logging and other telemetry
both from Cratis and your own app running on top.

The following configures a `docker-compose-yml` with Cratis, a version of [MongoDB](https://mongodb.com),
[ZipKin](http://zipkin.io), [Seq](https://datalust.co/seq) and [Prometheus](https://prometheus.io) to
be working together.

```yml
version: '3.1'
services:
  seq:
    image: datalust/seq:latest
    environment:
      - ACCEPT_EULA=Y
    ports:
      - 5341:80

  mongo:
    image: mongodb
    ports:
      - 27017:27017

  cratis:
    image: cratis/cratis:latest
    ports:
      - 27017:27017
      - 8080:80
      - 8081:8081
      - 11111:11111
      - 30000:30000
    volumes:
      - './appsettings.json:/app/appsettings.json'
    extra_hosts:
      - host.docker.internal:host-gateway

  zipkin-all-in-one:
      image: openzipkin/zipkin:latest
      ports:
        - "9411:9411"
  prometheus:
    container_name: prometheus
    image: prom/prometheus:latest
    volumes:
      - ./Metrics/prometheus.yaml:/etc/prometheus/prometheus.yml
    ports:
      - "9090:9090"

  # OpenTelemetry Collector
  otel-collector:
    image: otel/opentelemetry-collector:latest
    command: ["--config=/etc/otel-collector-config.yaml"]
    volumes:
      - ./Metrics/otel-collector-config.yaml:/etc/otel-collector-config.yaml
      - ./output:/etc/output:rw # Store the logs
    ports:
      - "8888:8888"   # Prometheus metrics exposed by the collector
      - "8889:8889"   # Prometheus exporter metrics
      - "4317:4317"   # OTLP gRPC receiver
    depends_on:
      - zipkin-all-in-one
```

The Cratis service is configured with a specific `appsettings.json` file.
Add a file called `appsettings.json` next to your `docker-compose.yml` and
add the following:

```json
{
    "Serilog": {
        "Using": [
            "Serilog.Sinks.Console"
        ],
        "MinimumLevel": {
            "Default": "Verbose",
            "Override": {
                "Cratis": "Information",
                "Microsoft": "Warning",
                "Microsoft.AspNetCore.HttpLogging": "Warning",
                "Microsoft.Hosting.Lifetime": "Information",
                "System": "Warning",
                "Orleans": "Information"
            }
        },
        "Enrich": [
            "FromLogContext",
            "WithMachineName",
            "WithThreadId"
        ],
        "WriteTo": [
            {
                "Name": "Console",
                "Args": {
                    "theme": "Serilog.Sinks.SystemConsole.Themes.AnsiConsoleTheme::Code, Serilog.Sinks.Console",
                    "outputTemplate": "[{Timestamp:HH:mm:ss} {Level}] {SourceContext}{NewLine}{Message:lj}{NewLine}{Exception}{NewLine}"
                }
            },
            {
                "Name": "Seq",
                "Args": {
                    "serverUrl": "http://localhost:5341"
                }
            }
        ]
    },
    "AllowedHosts": "*"
}
```

You will now have a full stack that will give you all the logging from Cratis inside `Seq`
and the individual services separated out.
