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
# .NET 11 is in release candidate: the floating preview tag below is unverified (no registry access when edited).
# GA: switch to 11.0-noble (and 11.0-noble-chiseled-extra) before any image ships from this file.
FROM mcr.microsoft.com/dotnet/aspnet:11.0-preview-noble

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

# Restore the executable bit on the apphost — publish artifacts can arrive without it
# (upload-artifact drops Unix permissions) and the entrypoint runs it directly (exit 126 otherwise).
RUN chmod +x ./Cratis.Chronicle.Server

EXPOSE 11111 30000 35000
ENTRYPOINT ["/usr/bin/tini", "--", "/bin/bash", "/app/entrypoint.sh"]
