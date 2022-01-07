# syntax=docker/dockerfile:1
FROM scratch as shared
ARG CONFIGURATION=Release
ARG VERSION
WORKDIR /build

COPY .editorconfig .
COPY .eslintrc.js .
COPY .prettierrc .
COPY .mocharc.js .
COPY package.json .
COPY global.json .
COPY tsconfig.json .
COPY Directory.Build.props .
COPY Versions.props .
COPY ./Source ./Source

# Server Build
FROM mcr.microsoft.com/dotnet/sdk:6.0 AS server-build
ARG CONFIGURATION=Release
ARG VERSION
WORKDIR /build

COPY --from=shared /build/ .

WORKDIR /build/Source/Kernel/Server

RUN dotnet publish -c ${CONFIGURATION} -p:Version=${VERSION} -o out

# Web Build
FROM node:16 AS web-build
WORKDIR /build

COPY --from=shared /build/ .

WORKDIR /build/Source/Workbench/Events/Web
RUN yarn
RUN yarn build

# Build runtime image
FROM mcr.microsoft.com/dotnet/aspnet:6.0
ARG CONFIGURATION=Release
ARG VERSION

# Cratis Server
WORKDIR /app

RUN echo Configuration = ${CONFIGURATION}
RUN echo Version = ${VERSION}

# Install MongoDB dependencies
RUN apt-get update \
    && apt-get install -y --no-install-recommends \
        wget \
        gnupg \
    && rm -rf /var/lib/apt/lists/*

# Hack to get MongoDB to install even without systemctl
# See https://www.julienrouse.com/blog/systemctl_not_found_while_installing_mongodb_server/#spoiler-a-solution
RUN ln -s /bin/true /usr/local/sbin/systemctl
RUN ln -s /bin/true /usr/local/bin/systemctl
RUN ln -s /bin/true /usr/sbin/systemctl
RUN ln -s /bin/true /usr/bin/systemctl
RUN ln -s /bin/true /sbin/systemctl
RUN ln -s /bin/true /bin/systemctl

# Install MongoDB
RUN wget -qO - https://www.mongodb.org/static/pgp/server-4.2.asc | apt-key add - \
    && echo "deb [ arch=arm64 ] https://repo.mongodb.org/apt/ubuntu bionic/mongodb-org/4.2 multiverse" > /etc/apt/sources.list.d/mongodb-org-4.2.list \
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
ADD https://github.com/krallin/tini/releases/download/v0.19.0/tini-arm64 /usr/bin/tini
RUN chmod +x /usr/bin/tini

# Create entrypoint that runs both MongoDB and Runtime
COPY Docker/entrypoint.sh /app/entrypoint.sh
RUN chmod +x /app/entrypoint.sh

EXPOSE 80 11111 30000

COPY --from=server-build /build/Source/Kernel/Server/out .
COPY --from=web-build /build/Source/Workbench/Events/Web/wwwroot wwwroot

ENTRYPOINT ["/usr/bin/tini", "--", "/bin/bash", "/app/entrypoint.sh"]
