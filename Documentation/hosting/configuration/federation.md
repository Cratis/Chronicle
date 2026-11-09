---
title: Configure federation in Cratis Chronicle Server
description: "Exchange public events between Chronicle instances across networks with a producer grant, a consumer binding and a mutual-TLS listener with pinned certificates."
---
Federation lets separate Chronicle instances exchange public events across networks. The consuming kernel dials the producing kernel and pulls from its outbox into a local inbox, so the consumer needs no inbound port. Consumer code does not change: the implicit and explicit subscriptions you already write decide which event types arrive.

Federation is off by default. The main port (35000) is never used for it, because it carries the Workbench, the client API and administrative operations. Federation is served on its own port, with its own certificate, and exposes nothing else. The design is recorded in [decision 0027](https://github.com/Cratis/Chronicle/blob/main/decisions/0027-secure-event-store-federation.md).

## How it fits together

| Side | Owns | Where it is configured |
| --- | --- | --- |
| Consumer | Which event types a service wants, from which logical source | Code: `[EventStore("service-name")]` or an explicit subscription, unchanged |
| Consumer | Which physical peer a logical source name means: endpoint, pinned server certificate, client certificate name | A **binding**, in configuration or at runtime |
| Producer | Which peer may read which event store and namespaces, and which event types | A **grant**, in configuration or at runtime |

A link is one direction. Two services consuming each other are two links, each with its own grant, binding, cursor, rate limit and revocation. There is no handshake: the producer operator configures the grant and the consumer operator configures the binding independently. Neither side can widen the other's boundary.

## Enable the listener

| Setting | Environment variable | Default | Meaning |
| --- | --- | --- | --- |
| `federation.enabled` | `Cratis__Chronicle__Federation__Enabled` | `false` | Opens the federation listener and runs consumer bindings. Setting it to `false` is a kill switch for every link |
| `federation.port` | `Cratis__Chronicle__Federation__Port` | `35100` | Port of the listener. Must differ from the main and health ports |
| `federation.certificatePath` | `Cratis__Chronicle__Federation__CertificatePath` | none | PKCS#12 certificate the listener presents. There is no fallback to the main TLS certificate |
| `federation.certificatePasswordPath` | `Cratis__Chronicle__Federation__CertificatePasswordPath` | none | Path to a file holding the certificate password |
| `federation.limits.maxBatchSize` | `Cratis__Chronicle__Federation__Limits__MaxBatchSize` | `500` | Events returned by one pull |
| `federation.limits.maxEventsPerSecond` | `Cratis__Chronicle__Federation__Limits__MaxEventsPerSecond` | `5000` | Per-link event rate |
| `federation.limits.maxBytesPerSecond` | `Cratis__Chronicle__Federation__Limits__MaxBytesPerSecond` | `16777216` | Per-link content bandwidth |

Chronicle refuses to start when federation is enabled without a certificate or when the port collides with another listener. Passwords are read from a file, never configured inline, so no secret appears in configuration, logs or the audit trail.

## Set up a producer grant

A grant states which peer, identified by the pin of its client certificate, may read which event store and namespaces. Compute a pin from a certificate with:

```shell
openssl x509 -in client.crt -pubkey -noout | openssl pkey -pubin -outform DER | openssl dgst -sha256 -binary | base64
```

Write it as `sha256:<base64>`. In configuration:

```json
{
  "Cratis": {
    "Chronicle": {
      "Federation": {
        "Enabled": true,
        "CertificatePath": "/certs/federation.pfx",
        "CertificatePasswordPath": "/certs/federation.pw",
        "Grants": {
          "billing": {
            "ClientPin": "sha256:Q2XbWbpBEd7s7WeD0d9HzCPpwNZUtueSrvq7U97pFGQ=",
            "EventStore": "orders",
            "Namespaces": ["Default"],
            "AllPublic": true
          }
        }
      }
    }
  }
}
```

`AllPublic` covers every public event type. Set `AllPublic` to `false` and list `EventTypes` to grant specific types. Either way the ceiling is the set of public event types: a private or unspecified type is never delivered, whatever the grant says.

Grants are changed at runtime through the `SetFederationGrant` and `RevokeFederationLink` commands, the Federation page of the Workbench or the CLI. Revoking a link stops its delivery at once and does not affect other links. Setting the grant again reinstates it. A consumer retries a denied link every 90 seconds, so a reinstated link resumes within that time.

## Set up a consumer binding

A binding maps the logical source name your code uses to a physical peer.

```json
{
  "Cratis": {
    "Chronicle": {
      "Federation": {
        "Enabled": true,
        "CertificatePath": "/certs/federation.pfx",
        "CertificatePasswordPath": "/certs/federation.pw",
        "Certificates": {
          "billing": { "Path": "/certs/billing-client.pfx", "PasswordPath": "/certs/billing-client.pw" }
        },
        "Bindings": {
          "orders": {
            "LogicalSource": "orders-service",
            "Endpoint": "https://orders.example.com:35100",
            "ServerPin": "sha256:lVTqbnrxIlHRy0QTcY5R94sFNm3PJv5cNUnz3ZbhJMk=",
            "ClientCertificate": "billing",
            "RemoteEventStore": "orders",
            "RemoteNamespace": "Default",
            "TargetEventStore": "billing",
            "TargetNamespace": "Default"
          }
        }
      }
    }
  }
}
```

`ClientCertificate` is the **name** of an entry in `Certificates`, never a secret value. The peer's server certificate is trusted only if its public key matches `ServerPin`. The chain and any certificate authority are ignored.

In code nothing changes: an event contract marked `[EventStore("orders-service")]`, or an explicit subscription from `orders-service`, selects the event types. The consumer requests exactly those types, the producer intersects them with its grant, and the events are appended to the `inbox-orders-service` sequence of the target event store.

The producer and consumer event store names must differ, because an event whose origin is the receiving store is rejected to prevent loops.

### Configuration and runtime state

Configuration bootstraps the same state the commands manage. Runtime state wins: configuration never overwrites an entry that has runtime history, and never brings back one that was removed or revoked. The Workbench shows where each value came from.

## Operating links

The Federation page in the Workbench lists both sides. Bindings show their state (`Streaming`, `Backoff`, `Denied`, `Idle`, `Disabled`), the last consumed sequence number, the remote tail, the lag and the last error. Grants show the last pull, the events and bytes delivered and how often the link was throttled.

Every change to a binding or grant is an event in the system event log, with the identity that made it. That log is the audit trail. Connections and cursors are visible through the same page.

OpenTelemetry meters `Cratis.Chronicle.Federation` and the kernel meter carry events and bytes delivered, throttled, denied and skipped-protected pulls on the producer, and events pulled, appended, skipped and errors per binding on the consumer.

## Security model

- **A separate listener.** HTTP/2 only, on its own port. Requests to any path outside the federation service are answered with 404, so the Workbench, the API and the client services are unreachable on it. The federation service is in turn refused on every other port.
- **TLS 1.3 only, with a client certificate required.** A client that offers only older versions, or no certificate, never completes the handshake.
- **Pinned certificates, not a shared authority.** A peer is identified by the SHA-256 hash of its certificate's public key. A certificate issued by the same authority is not enough. With no active grant for a pin, every pull is denied.
- **Grants are bounded by visibility.** A pull returns only public event types that the grant covers and the consumer asked for.
- **At-least-once delivery, deduplicated.** The consumer records the origin event store and source sequence number on each inbox event and resumes from the last one after a restart or reconnect, so a redelivered event is not duplicated. Events whose origin is the receiving store are rejected, so two stores consuming each other cannot loop.
- **Independent links.** A slow, throttled, denied or failing link backs off alone and never blocks another. Pulls are rate limited per link, and a link that exceeds its limit is throttled rather than served.
- **No secrets.** Certificates and passwords are referenced by path or name. Errors recorded in status and logs never contain certificate material, passwords or event content.

### Personal data across a link

Public events should not carry personal data. As a safeguard, the producer never forwards an event whose event type schema carries protected properties (personal data or `[Encrypted]` values). Such events are skipped, counted by the `skipped-protected` metric, and the cursor moves past them. This keeps plaintext and key material from crossing an organization boundary. Erasure therefore never needs to propagate over a link, because protected values never travel. Cross-organization key sharing is not supported.

## Rotating a certificate

Pins are of the public key. Add a grant with the pin of the new key, switch the peer to the new certificate, then revoke the old link. For a server certificate, deploy the new certificate on the producer, update the binding's `ServerPin`, then retire the old one.
