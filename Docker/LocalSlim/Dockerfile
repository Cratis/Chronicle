# syntax=docker/dockerfile:1

####################################
# Cratis Chronicle Server — kernel-only image for local integration tests.
# Mirrors the production runtime layout (no embedded databases). Every backing
# store (MongoDB, PostgreSQL, MSSQL, SQLite) runs in its own container or file;
# the harness configures the kernel through Cratis__Chronicle__Storage__* env
# vars. This is the slim equivalent of Docker/Local for the test fixture, and
# the layout parallels Docker/DevelopmentSlim so the local build mirrors the
# published `cratis/chronicle:*-development-slim` image.
####################################
FROM mcr.microsoft.com/dotnet/aspnet:10.0-noble

# Tini propagates signals to the kernel process cleanly so docker stop works
# without orphaning the .NET host. ca-certificates is needed for HTTPS pulls
# inside the runtime (token issuer, telemetry endpoints, etc.).
RUN apt-get update \
    && apt-get install -y --no-install-recommends \
        ca-certificates \
        curl \
    && curl -fsSL https://github.com/krallin/tini/releases/download/v0.19.0/tini-$(dpkg --print-architecture) \
        -o /usr/bin/tini \
    && chmod +x /usr/bin/tini \
    && rm -rf /var/lib/apt/lists/*

WORKDIR /app

COPY Docker/local-slim-entrypoint.sh ./entrypoint.sh
RUN chmod +x ./entrypoint.sh

COPY /Source/Kernel/Server/out /app

# Microsoft.Data.SqlClient does not support Globalization Invariant Mode and throws
# NotSupportedException on connection open. Server.csproj sets InvariantGlobalization=true
# which bakes both switches into runtimeconfig.json; flip them so the MSSQL integration
# tests can construct arbitrary CultureInfo instances. MongoDB and Postgres are unaffected.
RUN sed -i \
        -e 's/"System.Globalization.Invariant": true/"System.Globalization.Invariant": false/g' \
        -e 's/"System.Globalization.PredefinedCulturesOnly": true/"System.Globalization.PredefinedCulturesOnly": false/g' \
    Cratis.Chronicle.Server.runtimeconfig.json

EXPOSE 8080 11111 30000 35000
ENTRYPOINT ["/usr/bin/tini", "--", "/bin/bash", "/app/entrypoint.sh"]
