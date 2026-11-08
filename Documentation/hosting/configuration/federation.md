---
title: Configure federation in Cratis Chronicle Server
description: Configure the dedicated federation listener that lets other Chronicle instances pull public events across networks, and understand its security model.
---
Federation lets separate Chronicle instances exchange public events across networks. The consuming kernel dials the producing kernel and pulls from its outbox, so the consumer needs no inbound port.

Federation is off by default. The main port (35000) is never used for it: that port carries the Workbench, the client API and administrative operations. Federation is served on its own port, with its own certificate, and exposes nothing else.

> This release adds the listener and the security primitives. Peer bindings, producer grants as runtime state and the streaming service arrive in later releases, so an enabled listener currently authenticates no peer and serves no events. Design: [decision 0027](https://github.com/Cratis/Chronicle/blob/main/decisions/0027-secure-event-store-federation.md).

## Enable the listener

| Setting | Environment variable | Default | Meaning |
| --- | --- | --- | --- |
| `federation.enabled` | `Cratis__Chronicle__Federation__Enabled` | `false` | Opens the federation listener |
| `federation.port` | `Cratis__Chronicle__Federation__Port` | `35100` | Port of the listener. Must differ from the main and health ports |
| `federation.certificatePath` | `Cratis__Chronicle__Federation__CertificatePath` | none | PKCS#12 certificate the listener presents. There is no fallback to the main TLS certificate |
| `federation.certificatePasswordPath` | `Cratis__Chronicle__Federation__CertificatePasswordPath` | none | Path to a file holding the certificate password |

Chronicle refuses to start when federation is enabled without a certificate or when the port collides with another listener.

Passwords are read from a file, never configured inline, so no secret appears in configuration, logs or the audit trail.

## Security model

- **A separate listener.** HTTP/2 only, on its own port. Requests to any path outside the federation service are answered with 404, so the Workbench, the API and the client services are unreachable on it.
- **TLS 1.3 only, with a client certificate required.**
- **Pinned certificates, not a shared authority.** A peer is identified by the SHA-256 hash of its certificate's public key, written `sha256:<base64>`. No certificate chain is trusted, so a certificate issued by the same authority is not enough. With no pin configured, every connection is rejected.
- **One direction per link.** Two services consuming each other are two links, each with its own pin, grant and revocation. Revoking one link does not affect another.
- **Grants are bounded by visibility.** A grant covers specific event types or all public event types. Private and unspecified event types are never granted.
- **No loops, no duplicates.** Forwarded events carry their origin event store and source sequence number. A store rejects events that originated in itself, and a redelivered event is dropped.

## Rotating a certificate

Pins are of the public key. Add the pin of the new key to the link, switch the peer to the new certificate, then remove the old pin.
