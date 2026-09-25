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
      - 127.0.0.1:35000:35000
      - 127.0.0.1:27017:27017

  aspire-dashboard:
    image: mcr.microsoft.com/dotnet/aspire-dashboard:latest
    environment:
      - DOTNET_DASHBOARD_UNSECURED_ALLOW_ANONYMOUS=true
      - ALLOW_UNSECURED_TRANSPORT=true
      - DOTNET_ENVIRONMENT=Development
    ports:
      - 127.0.0.1:18888:18888
      - 127.0.0.1:4317:18889
```

Every port is published on this machine only: the development image's Workbench accepts well-known credentials, its bundled MongoDB has no authentication, and the dashboard allows anonymous access. The Orleans ports (`11111`, `30000`) are left unpublished because a single local node does not need them.

With this setup, Chronicle runs alongside Microsoft Aspire, which provides a dashboard for OpenTelemetry.

