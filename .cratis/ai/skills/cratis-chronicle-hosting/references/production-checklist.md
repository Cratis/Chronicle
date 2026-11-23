<!-- cratis-ai-managed: skills/cratis-chronicle-hosting/references/production-checklist.md -->
<!-- Copyright (c) Cratis. All rights reserved. -->
<!-- Licensed under the MIT license. See LICENSE file in the project root for full license information. -->

# Chronicle production go-live checklist

Source: `Cratis/Chronicle`: `Documentation/hosting/production.md`,
`Documentation/hosting/rolling-deploy.md`, `Documentation/hosting/encryption-certificate.md`,
`Documentation/hosting/configuration/{clustering,compliance-storage}.md`,
`Source/Clients/DotNET/{ChronicleOptions,Tls}.cs`.

Work through the items in order. Each links to the section that owns the detail; this file
adds only the sequence and the facts those sections do not carry. Mark an item done only
after observing it, and list what was not observed.

## 1. Image and build

- [ ] Deploy the production `cratis/chronicle` image, pinned to a tested tag. It has
  no bundled database, generates no certificates and creates no default administrator password.
- [ ] Do not rely on `ASPNETCORE_ENVIRONMENT`: the `DEVELOPMENT` symbol is compile-time. See
  [Choose the build and storage](../SKILL.md#choose-the-build-and-storage).

## 2. Storage

- [ ] `Cratis__Chronicle__Storage__ConnectionDetails` is set (it is empty by default), with
  `Cratis__Chronicle__Storage__Type` for a backend other than MongoDB.
- [ ] MongoDB is a replica set, even for one node; a standalone `mongod` cannot serve
  transactions or change streams. Erasure confirmation also needs linearizable reads and
  majority writes, so on a standalone server erasures stay pending with a warning.

## 3. Certificates and secrets

- [ ] TLS PFX and encryption certificate paths and passwords come from the secret store, and every
  node has the same ring. A missing encryption certificate file fails startup with
  `EncryptionCertificateFileNotFound`. See
  [Supply certificates](../SKILL.md#supply-certificates-before-starting-production).
- [ ] Dumps are secrets: a crash dump holds connection strings, keys and event data, including
  personal data. Keep dumps off by default; if enabled, use a restricted volume and delete every
  copy after analysis.

## 4. Authentication

- [ ] Complete administrator setup before exposing the service: the production image creates the
  administrator without a password, and setup needs the token from the server logs or
  `Authentication:InitialAdminSetupToken`. Protect that token and remove it from configuration
  afterwards.
- [ ] No development application survives in reused storage (see **Supply certificates** above).

## 5. Read model sinks

- [ ] The client option `DefaultSinkTypeId` defaults to MongoDB and applies to every materialized
  read model; a named sink on the kernel decides where its read models land regardless. Set the
  option or a named sink deliberately, then check where one read model actually lands. See
  [named sinks](sinks.md).

## 6. Clustering

- [ ] One node: the default `Localhost` is fine and a lease guards the storage.
- [ ] Several nodes: `clustering.type=MongoDB` with MongoDB storage, or `Sql` with PostgreSQL or
  SQL Server, on every node, with shared cluster id, service id and certificate ring. See
  [clustering and schema ownership](clustering-and-schema.md).

## 7. Clients

- [ ] Set `skipTlsValidation=false` explicitly; the client default skips validation. Use
  `caCertificatePath` for a private CA or `certificatePath` to pin; a pin does not override a host
  name mismatch. See **cratis-chronicle-configuration** and **cratis-chronicle-client-dotnet**.

## 8. Network and probes

- [ ] Keep the main port behind the intended proxy and the health port internal with
  `health.exclusive=true`. Use orchestrator probes: `/health/live` for liveness and
  `/health/ready` for readiness. See [Probe without opening another API entrance](../SKILL.md#probe-without-opening-another-api-entrance).

## 9. Backup and restore drill

- [ ] The backup set is the certificate ring (all certificates and passwords, ring shape at backup
  time), the storage, and the compliance key store, all from the same point in time.
- [ ] Restore in order: ring, storage, key store. Start **one** node and read
  `GET /diagnostics/encryption-certificates`; keys with the role `Retired` mean a certificate is
  missing. Keep traffic blocked, restore it under `previous`, restart that node and check again;
  a running node does not reload its ring. Then start the others.
- [ ] Keep a retired certificate as long as the oldest backup you would restore. Overwriting a
  certificate file is not a rotation.
- [ ] A key store older than the storage releases type-appropriate empty values for newer subjects,
  which looks like erasure, and may resurrect erased subjects. A newer one keeps the intervening
  erasures. Verify with **cratis-chronicle-compliance**.

## 10. Upgrade

- [ ] Back up first; migrations have no rollback.
- [ ] Check the source release against the compatibility table in `rolling-deploy.md`. Rolling
  replacement is supported only from listed source releases; otherwise, and for any new major,
  stop every node first. Key-store naming changes for Vault or Azure Key Vault also need stop-all.
- [ ] Probe readiness per node. See [Upgrade and restore](operations.md#upgrade-and-restore) and
  **cratis-chronicle-event-type-migration**.

## 11. Report

- [ ] Name every item not run. A configuration review is not a deployment.
