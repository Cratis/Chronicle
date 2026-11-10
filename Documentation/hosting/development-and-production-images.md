---
title: Development and production images
description: What differs between the Cratis Chronicle development and production images - the DEVELOPMENT build symbol, generated certificates, admin password handling, webhook validation and changed default values.
---

Chronicle ships a development image and a production image. They are not the same binary with a different environment name. The development image is built with the compile-time `DEVELOPMENT` symbol and carries its own `chronicle.json` and `appsettings.json`; the production image is built without the symbol and ships neither file's development values. Read this page before you move from `latest-development` to `latest`, or when behavior verified on the development image differs in production.

## The `DEVELOPMENT` symbol, not the environment name

Which image behavior you get is decided when the kernel is compiled. `ASPNETCORE_ENVIRONMENT` and `DOTNET_ENVIRONMENT` do not turn the development behavior on or off, so setting `ASPNETCORE_ENVIRONMENT=Production` on the development image does not make it behave like the production image, and setting `Development` on the production image does not enable the features below.

| Behavior | Development image (`DEVELOPMENT` defined) | Production image |
| --- | --- | --- |
| TLS certificate for the main port | Generates a self-signed certificate when TLS is enabled and none is configured | Fails at startup with "No TLS certificate is configured". Configure `Tls:CertificatePath` |
| Encryption certificate (Data Protection key protection, webhook secrets) | Loads or generates a development certificate under `certificates/` in the working directory when none is configured | Throws `EncryptionCertificateNotConfigured` on first use |
| OAuth authority signing and encryption keys | Uses ephemeral keys when no encryption certificate is configured, so tokens stop validating after a restart | Fails at startup with "An encryption certificate is required in production" |
| `Authentication:DefaultAdminPassword` | Honored: the administrator is created with that password and the first-password setup flow is skipped | The property does not exist in the build, so the key is ignored without an error. Use `Authentication:AdminUser` instead |
| `Webhooks:Validation` and `ExternalServices:Validation` default | `Warn` | `BlockLocalAddresses` |
| Development-only operations such as resetting kernel state | Available | Not compiled in; the calls throw |

Because the production image ignores `DefaultAdminPassword` silently, a configuration that works in the development image can leave the production kernel without the administrator password you expected. Set `Authentication:AdminUser` (username and password) for every environment, and see [Authentication](configuration/authentication.md) for the first-login flow.

> [!WARNING]
> Do not treat a generated certificate or an ephemeral key as a deployment option. They exist so the development image starts without setup. See [Data Protection Key Encryption](encryption-certificate.md) and [TLS Configuration](configuration/tls.md) for the production setup.

## Webhook and external-service validation

`Webhooks:Validation` and `ExternalServices:Validation` choose how Chronicle treats a destination that resolves to a local or private address. The code default is `BlockLocalAddresses` in production builds and `Warn` in development builds, so a webhook that targets `localhost` delivers on the development image and is blocked on the production image. `AllowInsecureHttp` is honored only in Development and is ignored in the production image. To reach an internal destination in production, add it to `AllowedHosts` (or `EventStoreAllowedHosts` per event store) instead of weakening validation. See [Webhooks](../webhooks/index.mdx) and [External services](../external-services/index.md).

## Values that differ from the code defaults

The development image ships two configuration files that change defaults. The production image removes `chronicle.json` and `appsettings.Development.json`, so none of these values apply there.

| Setting | Code default (production) | Development image | Source |
| --- | --- | --- | --- |
| `observers.subscriberTimeout` | `30` seconds | `5` seconds | `Source/Kernel/Server/chronicle.json` |
| `observers.replayOnDefinitionChange` | `false` | `true` | `Docker/Development/appsettings.json` |
| `observers.definitionEvolution` | `Automatic` | `Automatic` (set explicitly, same as the code default) | `Docker/Development/appsettings.json` |
| `storage` | Empty connection details; there is no built-in fallback | MongoDB at `mongodb://localhost:27017` (the bundled MongoDB) | `Source/Kernel/Server/chronicle.json` |
| `authentication.defaultAdminPassword` | Not available | Set to a well-known development value | `Source/Kernel/Server/chronicle.json` |

Two of these change behavior you can observe in a test:

- A subscriber that takes longer than 5 seconds is reported as timed out on the development image but would have succeeded within the 30-second default. Raise it to the production value when you want the development image to match. See [Observers](configuration/observers.md).
- With `replayOnDefinitionChange` on, changing a handler or an event type definition replays the observer automatically in development. In production it does not, so plan the replay explicitly. See [Observers](configuration/observers.md).

To make the development image behave like production for these two settings, set them in your Compose file:

```yaml
environment:
  Cratis__Chronicle__Observers__SubscriberTimeout: "30"
  Cratis__Chronicle__Observers__ReplayOnDefinitionChange: "false"
```

## Moving to the production image

1. Configure [storage](configuration/storage.mdx); the production image has no bundled database.
2. Configure the [TLS certificate](configuration/tls.md) and the [encryption certificate](encryption-certificate.md).
3. Set `Authentication:AdminUser` instead of `DefaultAdminPassword`.
4. Review `Webhooks:Validation`, `ExternalServices:Validation` and the allowed hosts for destinations that are local in development.
5. Set `observers.subscriberTimeout` and `observers.replayOnDefinitionChange` to the values you want, since the development values no longer apply.

See [Production hosting](production.md) for the full checklist.
