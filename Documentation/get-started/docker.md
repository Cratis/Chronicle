## Docker

Chronicle is available as a [Docker image](https://hub.docker.com/r/cratis/chronicle).

## Image Tags for Development

Use one of these development tags:

| Tag | Includes MongoDB | Typical Use |
| --- | --- | --- |
| `cratis/chronicle:latest-development` | Yes | Fast local setup with no external database container |
| `cratis/chronicle:latest-development-slim` | No | Local setup with your own database container (MongoDB, PostgreSQL, SQL Server, or SQLite) |

## Quick Start: Embedded MongoDB (Development Image)

The development image bundles MongoDB, so no separate database setup is required.

```shell
docker run -d -p 27017:27017 -p 8080:8080 -p 35000:35000 cratis/chronicle:latest-development
```

## Quick Start: External Database (Development Slim Image)

Use `latest-development-slim` and configure Chronicle storage through Chronicle options environment variables:

- `Cratis__Chronicle__Storage__Type`
- `Cratis__Chronicle__Storage__ConnectionDetails`

### MongoDB with Docker Compose

```yaml
services:
  chronicle:
    image: cratis/chronicle:latest-development-slim
    depends_on:
      - mongodb
    environment:
      - Cratis__Chronicle__Storage__Type=MongoDB
      - Cratis__Chronicle__Storage__ConnectionDetails=mongodb://mongodb:27017
    ports:
      - 8080:8080
      - 35000:35000

  mongodb:
    image: mongo:8
    ports:
      - 27017:27017
```

### PostgreSQL with Docker Compose

```yaml
services:
  chronicle:
    image: cratis/chronicle:latest-development-slim
    depends_on:
      - postgres
    environment:
      - Cratis__Chronicle__Storage__Type=PostgreSql
      - Cratis__Chronicle__Storage__ConnectionDetails=Host=postgres;Port=5432;Database=chronicle;Username=postgres;Password=postgres
    ports:
      - 8080:8080
      - 35000:35000

  postgres:
    image: postgres:16
    environment:
      - POSTGRES_DB=chronicle
      - POSTGRES_USER=postgres
      - POSTGRES_PASSWORD=postgres
    ports:
      - 5432:5432
```

### SQL Server with Docker Compose

> [!WARNING]
> The SQL Server credentials in this example are for local development only. For production, use secure credentials and manage secrets through Docker secrets, environment files, or an external secret manager.

```yaml
services:
  chronicle:
    image: cratis/chronicle:latest-development-slim
    depends_on:
      - sqlserver
    environment:
      - Cratis__Chronicle__Storage__Type=MsSql
      - Cratis__Chronicle__Storage__ConnectionDetails=Server=sqlserver,1433;Database=Chronicle;User Id=sa;Password=Your_strong_password123!;TrustServerCertificate=True;Encrypt=False
    ports:
      - 8080:8080
      - 35000:35000

  sqlserver:
    image: mcr.microsoft.com/mssql/server:2022-latest
    environment:
      - ACCEPT_EULA=Y
      - MSSQL_SA_PASSWORD=Your_strong_password123!
    ports:
      - 1433:1433
```

### SQLite with Docker Compose

```yaml
services:
  chronicle:
    image: cratis/chronicle:latest-development-slim
    environment:
      - Cratis__Chronicle__Storage__Type=Sqlite
      - Cratis__Chronicle__Storage__ConnectionDetails=Data Source=/data/chronicle.db
    volumes:
      - chronicle-data:/data
    ports:
      - 8080:8080
      - 35000:35000

volumes:
  chronicle-data:
```

## Optional: Add Aspire Dashboard for Logs, Traces, and Metrics

If you want local observability while developing, run Chronicle with the Aspire Dashboard and set `OTEL_EXPORTER_OTLP_ENDPOINT` on the Chronicle container:

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
