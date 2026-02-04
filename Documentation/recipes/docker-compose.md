# Running with Docker Compose

Using the development image of Chronicle is very convenient for getting started quickly.
Sometimes you want more control over the running environment and have MongoDB as its own
thing and possible other services, for collecting logging and other telemetry
both from Chronicle and your own app running on top.

The following configures a `docker-compose-yml` with Chronicle and [Microsoft Aspire](https://learn.microsoft.com/en-us/dotnet/aspire/get-started/aspire-overview).

```csharp
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

With this setup you will have Chronicle running alongside [Microsoft Aspire](https://learn.microsoft.com/en-us/dotnet/aspire/get-started/aspire-overview),
which will provide you with a dashboard for Open Telemetry.
