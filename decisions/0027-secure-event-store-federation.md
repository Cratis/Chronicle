---
id: 0027-secure-event-store-federation
title: Federate event stores across networks with a pull-based, one-direction link over a dedicated mutual-TLS listener with pinned certificates, where code declares what a service consumes and operator-owned runtime state binds and grants access
status: proposed
stage: none
class: contract
reversibility: costly
proposed: 2026-11-06
applies-to:
  - Source/Kernel/Core/Federation/**
  - Source/Kernel/Core/Configuration/Federation.cs
  - Source/Kernel/Server/FederationListener.cs
  - Documentation/hosting/configuration/federation.md
---

<!-- markdownlint-disable-next-line MD025 -->
# Federate event stores across networks with a pull-based, one-direction link over a dedicated mutual-TLS listener with pinned certificates, where code declares what a service consumes and operator-owned runtime state binds and grants access

Proposed on 2026-11-06 for [#5734](https://github.com/Cratis/Chronicle/issues/5734). It is accepted when the pull request that introduces it is approved.

## Context

Subscriptions forward events from one event store's outbox into another's inbox inside one kernel. Separate Chronicle instances, often on different networks, need to exchange public events too. The main kernel port (35000) carries the Workbench, the client API and administrative operations, so exposing it to peers would make a bug anywhere on it reachable by every peer.

## Decision

1. **Dedicated listener.** Federation is served on its own port with its own certificate. It is off by default and serves only the federation service: no admin, query or Workbench surface. The listener is HTTP/2 only.
2. **The consumer pulls, the producer serves.** The consuming kernel dials the producer and streams from the producer's outbox, resuming from its last sequence number. The consumer needs no inbound port.
3. **A link is one direction.** Two services consuming each other are two independent links, each with its own grant, cursor, rate limit and revocation. A duplex stream is not the model. Sharing one HTTP/2 connection is an optimization only; authorization, cursors and revocation stay per direction.
4. **Mutual TLS 1.3 with pinned certificates.** Each link has its own client certificate, pinned to the peer by SPKI SHA-256 hash. No certificate from a shared certificate authority is trusted, and the chain is not used as proof of identity. A listener without any configured pin rejects every connection (fail closed). Revoking a link removes only that link's pin.
5. **Code versus configuration.**
   - Consumer code declares which event types a service consumes from which logical source, as implicit and explicit subscriptions do today.
   - Consumer configuration binds the logical source name to a physical peer: endpoint, pinned server certificate and a client credential referenced by name.
   - Producer configuration holds a grant: which peer may read which namespaces, for specific event types or for all public event types. The ceiling is always the set of public event types, so no grant can expose a private or unspecified type. A grant is never derived from application code.
   - There is no handshake or approval workflow. Operators configure both sides independently.
6. **State lives in the kernel**, changeable at runtime and audited, managed through generated Core commands and read models. Configuration may bootstrap the same state; runtime state wins when both exist.
7. **Delivery is at-least-once**, deduplicated per origin by origin event store and source sequence number. A consumer rejects events whose origin is its own store, so mutual links cannot loop. The outbox accepts only public events and the event log refuses them, so the inbox cannot feed the outbox.
8. **Secrets are referenced, never stored in plain configuration.** Configuration holds paths to certificate files and password files, not values. No secret value is written to a log or an audit entry.
9. **Compliance.** Public events should not carry personal data. `[Encrypted]` and PII values must not cross a link in the clear or under a key the receiver does not hold. The producer therefore refuses to forward an event type that carries protected properties until the cross-organization key and erasure behavior is specified and accepted in a later record.

## Out of scope

A handshake or approval workflow, many-to-many relay routing (the model must not prevent it), and any change to how consumer code selects event types.

## Delivery

Delivered in increments. The first adds the listener, the pin and grant primitives and the loop and duplicate guard, each specified. Peer bindings and grants as runtime state, the streaming service, the Workbench and CLI surface and the compliance behavior follow.

## Consequences

- Chronicle gains a second externally reachable surface, which is why it is off by default and narrow.
- The pin set is the only trust anchor, so rotation means adding the new pin before removing the old one.
- Treating the chain as untrusted costs operators a pin to manage per link and buys isolation between links.
