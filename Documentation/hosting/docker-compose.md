# Running with Docker Compose

Using the development image of Chronicle is convenient for getting started quickly. For more control over the runtime environment, you can run Chronicle with MongoDB and other services such as telemetry collectors.

The following configures a `docker-compose.yml` with Chronicle and [Microsoft Aspire](https://learn.microsoft.com/en-us/dotnet/aspire/get-started/aspire-overview).

```yaml
services:
  chronicle:
    image: cratis/chronicle:latest-development
    environment:
      - OTEL_EXPORTER_OTLP_ENDPOINT=http://aspire-dashboard:18889
    ports:
      - 27017:27017
      - 8080:8080
      - 11111:11111
      - 30000:30000
      - 35000:35000

  aspire-dashboard:
    image: mcr.microsoft.com/dotnet/aspire-dashboard:latest
    environment:
      - DOTNET_DASHBOARD_UNSECURED_ALLOW_ANONYMOUS=true
      - DOTNET_DASHBOARD_OTLP_ENDPOINT_URL=http://chronicle:18889
      - ALLOW_UNSECURED_TRANSPORT=true
      - DOTNET_ENVIRONMENT=Development
    ports:
      - 18888:18888
      - 4317:18889
```

[Snippet source](https://github.com/cratis/samples/blob/main/Chronicle/Quickstart/docker-compose.yml#L2-L23)

With this setup, Chronicle runs alongside Microsoft Aspire, which provides a dashboard for OpenTelemetry.

