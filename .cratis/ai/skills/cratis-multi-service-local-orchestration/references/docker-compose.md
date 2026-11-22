<!-- cratis-ai-managed: skills/cratis-multi-service-local-orchestration/references/docker-compose.md -->
<!-- Copyright (c) Cratis. All rights reserved. -->
<!-- Licensed under the MIT license. See LICENSE file in the project root for full license information. -->

# Docker Compose for several Cratis services

## Contents

- The file
- What each part is for
- Per-service settings
- Checks

## The file

Two services, `ordering` and `billing`, each built from its own Dockerfile, on one
Chronicle kernel backed by a single-node MongoDB replica set. The Chronicle image is
the development image without bundled MongoDB, so it uses the replica set below and
accepts the well-known development client.

```yaml
services:
  mongodb:
    image: mongo:8.2
    command: ["mongod", "--replSet", "rs0", "--bind_ip_all"]
    volumes:
      - mongodb-data:/data/db
    healthcheck:
      test: ["CMD", "mongosh", "--quiet", "--eval", "db.adminCommand('ping')"]
      interval: 5s
      timeout: 5s
      retries: 30

  mongodb-init:
    image: mongo:8.2
    depends_on:
      mongodb:
        condition: service_healthy
    restart: "no"
    command:
      - mongosh
      - --host
      - mongodb
      - --quiet
      - --eval
      - |
        try {
          const status = db.adminCommand({ replSetGetStatus: 1 });
          if (!status.ok) { throw new Error(JSON.stringify(status)); }
        } catch (error) {
          if (error.code !== 94) { throw error; }
          const result = rs.initiate({ _id: 'rs0', members: [{ _id: 0, host: 'mongodb:27017' }] });
          if (!result.ok) { throw new Error(JSON.stringify(result)); }
        }

  chronicle:
    image: cratis/chronicle:latest-development-slim
    depends_on:
      mongodb-init:
        condition: service_completed_successfully
    environment:
      Cratis__Chronicle__Storage__Type: MongoDB
      Cratis__Chronicle__Storage__ConnectionDetails: mongodb://mongodb:27017/?replicaSet=rs0&directConnection=true
      Cratis__Chronicle__Health__Port: "8080"
      Cratis__Chronicle__Health__Tls: "false"
      Cratis__Chronicle__Health__Exclusive: "true"
    healthcheck:
      test: ["CMD", "curl", "-fsS", "http://localhost:8080/health"]
      interval: 5s
      timeout: 5s
      retries: 60
    ports:
      - "127.0.0.1:35000:35000"

  ordering:
    build: ./Ordering
    depends_on:
      chronicle:
        condition: service_healthy
    environment:
      Cratis__Chronicle__ConnectionString: chronicle://chronicle-dev-client:chronicle-dev-secret@chronicle:35000
      Cratis__Chronicle__EventStore: Ordering
      Cratis__MongoDB__Server: mongodb://mongodb:27017/?replicaSet=rs0&directConnection=true
      Cratis__MongoDB__Database: Ordering
    ports:
      - "127.0.0.1:5101:8080"

  billing:
    build: ./Billing
    depends_on:
      chronicle:
        condition: service_healthy
    environment:
      Cratis__Chronicle__ConnectionString: chronicle://chronicle-dev-client:chronicle-dev-secret@chronicle:35000
      Cratis__Chronicle__EventStore: Billing
      Cratis__MongoDB__Server: mongodb://mongodb:27017/?replicaSet=rs0&directConnection=true
      Cratis__MongoDB__Database: Billing
    ports:
      - "127.0.0.1:5102:8080"

volumes:
  mongodb-data:
```

## What each part is for

- **Replica set first.** Chronicle needs MongoDB transactions and change streams,
  which a standalone `mongod` does not provide. `mongodb-init` initiates the set once
  and tolerates an already initiated one (error code 94), so `docker compose up`
  is repeatable. `directConnection=true` keeps the driver on the address it was
  given.
- **Health-gated kernel.** The dedicated health port serves `/health` over plain
  HTTP/1.1; `Health__Exclusive` makes it answer nothing else, and it is not
  published. The image ships `curl` for the check. Services wait for
  `service_healthy`; the client would also reconnect on its own.
- **Ports.** Only 35000 (clients, Workbench, REST and OAuth over TLS) is published,
  bound to `127.0.0.1`, because the development image accepts well-known
  credentials. The Orleans silo (11111) and gateway (30000) ports are for
  clustering and stay unpublished for one node.
- **Image choice.** The development image bundles MongoDB and suits a kernel alone;
  with external storage use the development-slim image as above. The production
  image starts only with storage, a TLS certificate and an encryption certificate,
  and generates no development client (`cratis-chronicle-hosting`). Pin an exact
  image version in shared environments instead of a floating tag.

## Per-service settings

| Setting | Value |
| --- | --- |
| `Cratis__Chronicle__ConnectionString` | The kernel's service name and port inside the network; the development client and secret locally, a per-service client and `skipTlsValidation=false` with a real certificate elsewhere |
| `Cratis__Chronicle__EventStore` | The service's own store name, equal to its contracts' `[EventStore]` |
| `Cratis__MongoDB__Server`, `Cratis__MongoDB__Database` | The replica set, and the service's event-store name as the database |
| Host port | A distinct port per service, bound to `127.0.0.1` |

Connection-string options (`skipTlsValidation`, credentials, several hosts) are
covered in `cratis-chronicle-client-dotnet`.

## Checks

- `docker compose ps` shows `mongodb-init` exited 0 and every other service healthy
  or running.
- `cratis chronicle event-stores list --server chronicle://localhost:35000` lists
  `Ordering` and `Billing` once each.
- `docker compose restart chronicle`: both services reconnect and forwarding resumes
  without restarting them.
