# Running with Docker Compose

Using the development image of Chronicle is very convenient for getting started quickly.
Sometimes you want more control over the running environment and have MongoDB as its own
thing and possible other services, for collecting logging and other telemetry
both from Chronicle and your own app running on top.

The following configures a `docker-compose-yml` with Chronicle, a version of [MongoDB](https://mongodb.com),
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

  chronicle:
    image: cratis/chronicle:latest
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

You will now have a full stack that will give you all the logging from Chronicle inside `Seq`
and the individual services separated out.
