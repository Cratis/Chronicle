# syntax=docker/dockerfile:1
# Development slim image — Chronicle server only (no embedded MongoDB).

####################################
# Cratis Chronicle Server - Preparation stage
####################################
# MCR publishes no .NET 11 noble tags; use the GA images on Ubuntu 26.04 (resolute).
FROM mcr.microsoft.com/dotnet/aspnet:11.0-resolute AS prep

WORKDIR /app

COPY Docker/copy-server-files.sh ./copy-server-files.sh
RUN chmod +x ./copy-server-files.sh

COPY ./Source/Kernel/Server/out ./out
COPY ./Source/Workbench/wwwroot wwwroot

RUN ./copy-server-files.sh && rm -f appsettings.Development.json

####################################
# Cratis Chronicle Server - Runtime stage
####################################
FROM mcr.microsoft.com/dotnet/aspnet:11.0-resolute

# Install tools and tini
RUN apt-get update \
    && apt-get install -y --no-install-recommends \
        nano \
        iputils-ping \
        netcat-traditional \
        curl \
        ca-certificates \
    && curl -fsSL https://github.com/krallin/tini/releases/download/v0.19.0/tini-$(dpkg --print-architecture) \
        -o /usr/bin/tini \
    && chmod +x /usr/bin/tini \
    && rm -rf /var/lib/apt/lists/*

WORKDIR /app

COPY Docker/development-slim-entrypoint.sh ./entrypoint.sh
RUN chmod +x ./entrypoint.sh

# Copy prepared application files — the large out/ directory stays in the prep
# stage and is never baked into this image.
COPY --from=prep /app/. ./
COPY ./Docker/Development/appsettings.json ./appsettings.json

EXPOSE 11111 30000 35000
ENTRYPOINT ["/usr/bin/tini", "--", "/bin/bash", "/app/entrypoint.sh"]
