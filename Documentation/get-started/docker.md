## Docker

Chronicle is available as a [Docker Image](https://hub.docker.com/r/cratis/chronicle). For local development, we recommend
using the development images. The `latest-development` tag will get you the most recent version.

The development image includes a MongoDB server, so you don't need any additional setup.

To run the server as a daemon, execute the following command in your terminal:

```shell
docker run -d -p 27017:27017 -p 8080:8080 -p 35000:35000 cratis/chronicle:latest-development
```

If you prefer to have a Docker Compose file, we recommend the following setup with Aspire to give
you open telemetry data:

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
