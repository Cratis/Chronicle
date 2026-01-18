# syntax=docker/dockerfile:1

####################################
# Cratis Chronicle Server
# Build runtime image.
####################################
FROM mcr.microsoft.com/dotnet/aspnet:10.0-noble
ARG VERSION
ARG MONGO_VERSION=8.0.8
ARG MONGO_PACKAGE=mongodb-org

ARG ENTRYPOINT=entrypoint.sh

RUN echo Version = ${VERSION}
RUN echo Mongo Version = ${VERSION}
RUN echo ${ENTRYPOINT}

# Install Tools
RUN apt-get update \
    && apt-get install -y --no-install-recommends \
    nano \
    iputils-ping \
    netcat-traditional \
    wget \
    curl \
    gnupg \
    apt-transport-https \
    ca-certificates \
    && rm -rf /var/lib/apt/lists/*

RUN ARCH=$(dpkg --print-architecture) && \
    case "$ARCH" in \
        amd64)  SSL_PKG_URL="http://archive.ubuntu.com/ubuntu/pool/main/o/openssl/libssl1.1_1.1.1f-1ubuntu2_amd64.deb" ;; \
        arm64)  SSL_PKG_URL="http://launchpadlibrarian.net/475575244/libssl1.1_1.1.1f-1ubuntu2_arm64.deb" ;; \
        *)      echo "Unsupported architecture: $ARCH" && exit 1 ;; \
    esac && \
    wget $SSL_PKG_URL -O libssl1.1.deb && \
    dpkg -i libssl1.1.deb && \
    rm libssl1.1.deb

# Install MongoDB
RUN wget --no-check-certificate -qO - https://www.mongodb.org/static/pgp/server-8.0.asc | apt-key add - \
    && echo "deb [ arch=$(dpkg --print-architecture) ] https://repo.mongodb.org/apt/ubuntu focal/mongodb-org/8.0 multiverse" > /etc/apt/sources.list.d/mongodb-org-8.0.list \
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

# Setup MongoDB as single-node replicaset
RUN mkdir -p /data/db /data/configdb \
    && chown -R mongodb:mongodb /data/db /data/configdb \
    && mongod --logpath /var/log/mongodb/initdb.log --replSet "rs0" --bind_ip 0.0.0.0 --fork \
    && mongosh --eval 'rs.initiate({_id: "rs0", members: [{ _id: 0, host: "localhost:27017"}]})' \
    && mongod --shutdown

VOLUME /data/db /data/configdb

# Add Tini to get a real init process
RUN curl -LJ https://github.com/krallin/tini/releases/download/v0.19.0/tini-$(dpkg --print-architecture) --output /usr/bin/tini
RUN chmod +x /usr/bin/tini


WORKDIR /app

# Create entrypoint that runs both MongoDB and Runtime
COPY Docker/${ENTRYPOINT} ./entrypoint.sh
RUN chmod +x ./entrypoint.sh

COPY Docker/copy-server-files.sh ./copy-server-files.sh
RUN chmod +x ./copy-server-files.sh

COPY ./Source/Kernel/Server/out ./out
COPY ./Source/Workbench/Web/wwwroot wwwroot

RUN echo $PWD
RUN ./copy-server-files.sh

COPY ./Docker/Development/appsettings.json ./appsettings.json
RUN rm appsettings.Development.json

EXPOSE 8080 11111 30000 35000
ENTRYPOINT ["/usr/bin/tini", "--", "/bin/bash", "/app/entrypoint.sh"]
