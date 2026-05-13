# syntax=docker/dockerfile:1

####################################
# Cratis Chronicle Server - Preparation stage
####################################
FROM mcr.microsoft.com/dotnet/aspnet:10.0-noble AS prep

WORKDIR /app

COPY Docker/copy-server-files.sh ./copy-server-files.sh
RUN chmod +x ./copy-server-files.sh

COPY ./Source/Kernel/Server/out ./out
COPY ./Source/Workbench/wwwroot wwwroot

RUN ./copy-server-files.sh && rm -f appsettings.Development.json

####################################
# Cratis Chronicle Server - Runtime stage
####################################
FROM mcr.microsoft.com/dotnet/aspnet:10.0-noble
ARG VERSION
ARG MONGO_VERSION=8.0.8
ARG MONGO_PACKAGE=mongodb-org
ARG ENTRYPOINT=entrypoint.sh

# Install tools and tini
RUN apt-get update \
    && apt-get install -y --no-install-recommends \
        nano \
        iputils-ping \
        netcat-traditional \
        curl \
        gnupg \
        ca-certificates \
    && curl -fsSL https://github.com/krallin/tini/releases/download/v0.19.0/tini-$(dpkg --print-architecture) \
        -o /usr/bin/tini \
    && chmod +x /usr/bin/tini \
    && rm -rf /var/lib/apt/lists/*

# Install MongoDB using the noble repository with the modern signed-by key method.
# The libssl1.1 workaround and focal repo are no longer needed — MongoDB 8.0 has
# native noble (24.04) packages that link against the OpenSSL 3 already in the base image.
RUN curl -fsSL https://www.mongodb.org/static/pgp/server-8.0.asc \
        | gpg --dearmor -o /usr/share/keyrings/mongodb-server-8.0.gpg \
    && echo "deb [ arch=$(dpkg --print-architecture) signed-by=/usr/share/keyrings/mongodb-server-8.0.gpg ] https://repo.mongodb.org/apt/ubuntu noble/mongodb-org/8.0 multiverse" \
        > /etc/apt/sources.list.d/mongodb-org-8.0.list \
    && apt-get update \
    && apt-get install -y --no-install-recommends \
        ${MONGO_PACKAGE}=$MONGO_VERSION \
        ${MONGO_PACKAGE}-server=$MONGO_VERSION \
        ${MONGO_PACKAGE}-shell=$MONGO_VERSION \
        ${MONGO_PACKAGE}-mongos=$MONGO_VERSION \
        ${MONGO_PACKAGE}-tools=$MONGO_VERSION \
        ${MONGO_PACKAGE}-database=$MONGO_VERSION \
        ${MONGO_PACKAGE}-database-tools-extra=$MONGO_VERSION \
    && rm -rf /var/lib/apt/lists/*

# Create MongoDB data directories. Replica set initialisation is deferred to
# entrypoint.sh at runtime — no need to bake 200+ MB of journal files into the image.
RUN mkdir -p /data/db /data/configdb \
    && chown -R mongodb:mongodb /data/db /data/configdb

VOLUME /data/db /data/configdb

WORKDIR /app

COPY Docker/${ENTRYPOINT} ./entrypoint.sh
RUN chmod +x ./entrypoint.sh

# Copy prepared application files — the large out/ directory stays in the prep
# stage and is never baked into this image.
COPY --from=prep /app/. ./
COPY ./Docker/Development/appsettings.json ./appsettings.json

EXPOSE 8080 11111 30000 35000
ENTRYPOINT ["/usr/bin/tini", "--", "/bin/bash", "/app/entrypoint.sh"]
