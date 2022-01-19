# syntax=docker/dockerfile:1
# Server Build
ARG CONFIGURATION=Release
ARG VERSION
FROM mcr.microsoft.com/dotnet/sdk:6.0 AS server-build
WORKDIR /build

COPY .editorconfig .
COPY global.json .
COPY Directory.Build.props .
COPY Versions.props .
COPY ./Source ./Source

WORKDIR /build/Source/Kernel/Server

ADD Docker/build-server.sh .
RUN chmod +x ./build-server.sh
RUN ./build-server.sh

# Workbench
ARG VERSION
FROM aksioinsurtech/cratis:$VERSION-workbench as workbench

####################################
# Cratis Server
# Build runtime image
####################################
ARG CONFIGURATION=Release
ARG VERSION
FROM mcr.microsoft.com/dotnet/runtime:6.0.1-focal

WORKDIR /app

RUN echo Configuration = ${CONFIGURATION}
RUN echo Version = ${VERSION}

# Install MongoDB dependencies
RUN apt-get update \
    && apt-get install -y --no-install-recommends \
    wget \
    curl \
    gnupg \
    apt-transport-https \
    ca-certificates \
    && rm -rf /var/lib/apt/lists/*

# Install MongoDB
RUN wget --no-check-certificate -qO - https://www.mongodb.org/static/pgp/server-5.0.asc | apt-key add - \
    && echo "deb [ arch=$(dpkg --print-architecture) ] https://repo.mongodb.org/apt/ubuntu bionic/mongodb-org/5.0 multiverse" > /etc/apt/sources.list.d/mongodb-org-5.0.list \
    && apt-get update \
    && apt-get install -y --no-install-recommends \
    mongodb-org-server \
    mongodb-org-shell \
    && rm -rf /var/lib/apt/lists/*

# Setup MongoDB as single-node replicaset
RUN mkdir -p /data/db /data/configdb \
    && chown -R mongodb:mongodb /data/db /data/configdb \
    && mongod --logpath /var/log/mongodb/initdb.log --replSet "rs0" --bind_ip 0.0.0.0 --fork \
    && mongo --eval 'rs.initiate({_id: "rs0", members: [{ _id: 0, host: "localhost:27017"}]})' \
    && mongod --shutdown

VOLUME /data/db /data/configdb

# Add Tini to get a real init process
RUN curl -LJ https://github.com/krallin/tini/releases/download/v0.19.0/tini-$(dpkg --print-architecture) --output /usr/bin/tini
RUN chmod +x /usr/bin/tini

# Create entrypoint that runs both MongoDB and Runtime
COPY Docker/entrypoint.sh /app/entrypoint.sh
RUN chmod +x /app/entrypoint.sh

EXPOSE 80 11111 30000

COPY --from=server-build /build/Source/Kernel/Server/out/*.dll .
COPY --from=server-build /build/Source/Kernel/Server/out/*.json .
COPY --from=server-build /build/Source/Kernel/Server/out/*.so .
COPY --from=server-build /build/Source/Kernel/Server/out/Cratis.Server .
COPY --from=workbench /usr/share/nginx/html wwwroot

ENTRYPOINT ["/usr/bin/tini", "--", "/bin/bash", "/app/entrypoint.sh"]
