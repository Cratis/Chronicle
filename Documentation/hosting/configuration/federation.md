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
- **TLS 1.3 only, with a client certificate required.** A client that offers only older versions never completes the handshake, and every route requires a client certificate, except the two public enrollment routes described in [Hosted federation](#hosted-federation).
- **Pinned certificates, not a shared authority.** A peer is identified by the SHA-256 hash of its certificate's public key. A certificate issued by the same authority is not enough. With no active grant for a pin, every pull is denied.
- **Grants are bounded by visibility.** A pull returns only public event types that the grant covers and the consumer asked for.
- **At-least-once delivery, deduplicated.** The consumer records the origin event store and source sequence number on each inbox event and resumes from the last one after a restart or reconnect, so a redelivered event is not duplicated. Events whose origin is the receiving store are rejected, so two stores consuming each other cannot loop.
- **Independent links.** A slow, throttled, denied or failing link backs off alone and never blocks another. Pulls are rate limited per link, and a link that exceeds its limit is throttled rather than served.
- **No secrets.** Certificates and passwords are referenced by path or name. Errors recorded in status and logs never contain certificate material, passwords or event content.

### Personal data across a link

Public events should not carry personal data. As a safeguard, the producer never forwards an event whose event type schema carries protected properties (personal data or `[Encrypted]` values). Such events are skipped, counted by the `skipped-protected` metric, and the cursor moves past them. This keeps plaintext and key material from crossing an organization boundary. Erasure therefore never needs to propagate over a link, because protected values never travel. Cross-organization key sharing is not supported.

## Hosted federation

Pinned grants suit two parties who configure each other. An operator that runs applications for many customers cannot hand-pin every one. With hosted federation the kernel is the certificate authority for the links of its customers: an operator creates a one-time enrollment, hands the customer a single code, and the customer's kernel turns it into a certificate. Pinned grants and bindings keep working unchanged, and hosted federation is off unless enabled.

### Enable it on the authority kernel

Hosted federation is served on the federation listener, so enable that first. Then enable the authority and tell it the endpoint customers reach it on:

```json
{
  "Chronicle": {
    "Federation": {
      "Enabled": true,
      "Port": 35100,
      "Hosted": {
        "Enabled": true,
        "PublicEndpoint": "https://federation.example.com:35100",
        "TrustDomain": "example"
      }
    }
  }
}
```

| Setting | Default | Meaning |
| --- | --- | --- |
| `Hosted:Enabled` | `false` | Act as the certificate authority and accept enrollments. |
| `Hosted:PublicEndpoint` | none | The public https endpoint put in every enrollment code. There is no built-in default, so it must be set before an enrollment can be created. |
| `Hosted:TrustDomain` | `chronicle` | The trust domain of the identities issued, lowercase. |
| `Hosted:CertificateLifetime` | 14 days | The lifetime of an issued certificate. |
| `Hosted:EnrollmentLifetime` | 7 days | How long an enrollment code can be redeemed. |
| `Hosted:ListenerCertificateLifetime` | 30 days | The lifetime of the listener certificate the kernel issues itself. |
| `Hosted:RequestsPerMinutePerSource` | 10 | How many requests one source address may make per minute, counted separately for fetching the authority and redeeming, and for renewing. |
| `Hosted:MaxConcurrentEnrollmentRequests` | 16 | How many pre-authentication enrollment requests are served at once. More are refused with `503`. |
| `Hosted:KeyEncryptionKeyPath` | none | A file holding the key-encryption key that wraps the authority key. Required, see [Provision the key-encryption key](#provision-the-key-encryption-key). |
| `Hosted:PreviousKeyEncryptionKeyPath` | none | The previous key-encryption key, kept readable while keys wrapped under it exist. |

The first time hosted federation is enabled, the kernel creates the authority: a P-384 key pair and a self-signed certificate that can only sign leaf certificates. The private key is stored only wrapped by the key-encryption key you provision (next section), as a kernel-internal secret kept apart from subject keys. It is never in configuration, logs, events or audit entries, and never in the clear anywhere. Several silos converge on one authority.

### Provision the key-encryption key

The authority private key, and the private key a kernel holds after it enrolled (`FederationIdentityInstalled` in the event log), are wrapped with AES-256-GCM. The wrapping key is a key-encryption key (KEK) that you supply **outside the database**, so that someone who can read the databases and the event log cannot sign with either key. The identity and certificate serial number are bound into every wrapped key as associated data, so a wrapped key cannot be moved to another identity or certificate.

Generate 32 random bytes, base64 encoded, mount the file as a secret and give the kernel its path. The value is never configured inline:

```bash
head -c 32 /dev/urandom | base64 > /run/secrets/chronicle-federation-kek
chmod 400 /run/secrets/chronicle-federation-kek
```

```json
{
  "Chronicle": {
    "Federation": {
      "Hosted": { "KeyEncryptionKeyPath": "/run/secrets/chronicle-federation-kek" }
    }
  }
}
```

Every silo of the cluster needs the same key. Hosted federation (creating the authority, redeeming, renewing) and enrollment on a peer **refuse to operate without it**. The one exception is a storage backend that keeps internal secrets outside the event store databases, which today means only Azure Key Vault; MongoDB, SQL and Vault do not, so with those the file is required. A `Development` environment generates the file at the configured path when it is missing and logs a warning. A production kernel never does, because a key generated by the kernel is no better than one stored next to the data.

#### Rotating the key-encryption key

Put the new key at `KeyEncryptionKeyPath` and keep the old one at `PreviousKeyEncryptionKeyPath`. Keys wrapped under the old one stay readable, and everything wrapped from then on uses the new one.

- **An enrolled peer** re-wraps its key at its next renewal, which happens at half of the certificate lifetime. Once every certificate that was installed under the old key has been renewed, the previous key can go.
- **The authority key** is stored once and cannot be re-wrapped in place, because internal secrets are write-once. The previous key therefore has to stay configured for as long as the authority exists. Replacing the KEK without replacing the authority is not possible. If the key itself is compromised, treat the authority key as compromised too and replace the authority, see [Authority lifetime](#authority-lifetime).

### Authority lifetime

The authority certificate is valid for 10 years. A certificate the authority issues never outlives it, and once it has expired the kernel issues nothing and logs an error. Rotating the authority is not automated: it means removing the stored authority and enrolling every customer again, because every peer pins the fingerprint in its enrollment code. Plan the replacement well before the expiry, and keep the old authority running until the last peer has moved.

When `Federation:CertificatePath` is not set, the kernel also issues the listener certificate itself from its own authority and renews it at half of its lifetime. Customers trust it because the enrollment code carries the fingerprint of the authority root. Set `CertificatePath` to present a certificate from a public authority instead.

### Enroll a customer

Create an enrollment for an opaque subject, which is whatever id you use for the customer. It becomes part of the identity `spiffe://<trust-domain>/customer/<subject>/kernel/<instance>`:

In the .NET client that is `IChronicleClient.Federation.Create("acme", instance: "prod")`. Its result carries the enrollment code, which is shown only now. Hand it to the customer over a channel you trust.

The code is one string carrying the public endpoint, the SHA-256 fingerprint of the authority root and a one-time token secret. Treat it as a password. Only a hash of the token is stored, and the code is single-use and expires.

The customer redeems it on their own kernel with `IChronicleClient.Federation.Redeem(code)`, or with the `RedeemFederationEnrollment` command.

The kernel generates a key pair locally, so the private key never leaves it. It fetches the authority certificate and checks it against the fingerprint in the code before it sends anything, then sends a certificate request and the token. It verifies the server certificate chains to that root, so the connection is trusted by the code and not by the system trust store. The identity in the issued certificate comes from the enrollment, never from the request: the request is only used for its public key. The kernel stores the key and certificate in its key custody and takes them into use without a restart. Redeeming again while already enrolled with the same authority changes nothing.

For automated deployments, put the code in a file and set `Federation:Enrollment:CodePath`. A kernel that is not yet enrolled redeems it at startup. Do not put the code inline in configuration. The code is spent once redeemed, so after a successful redemption the kernel deletes the file when its process can. If it cannot, it logs a warning without the file path content and you remove the file yourself.

A redemption that fails after the authority recorded it, for example because the response was lost, has used the code up: a code can be redeemed once and never again. Create a new enrollment and hand out its code.

Redeeming a code makes the kernel connect to the endpoint in the code, so the kernel treats the endpoint as untrusted input. It must be `https`. It may not resolve to a loopback, link-local (including the `169.254.169.254` cloud metadata address), unspecified, multicast or unique-local address, and it is connected to at the address that was checked, so a name cannot resolve somewhere else a moment later. There are no redirects and no proxy. Every way the authority can be unreachable, wrong or refusing produces the same answer after the same time, so the kernel does not tell what an address can reach. Settings under `Federation:Enrollment`:

| Setting | Default | Meaning |
| --- | --- | --- |
| `AllowedHosts` | empty | When set, an enrollment code may only name these hosts. |
| `AllowRestrictedAddresses` | `false` | Allow the endpoint to resolve to the internal-only addresses above. A `Development` environment allows it regardless. |
| `FailureDuration` | 10 seconds | How long every failed attempt to reach the authority takes. |

To see where things stand, use `client.Federation.GetEnrollments()` and `GetIssuedCertificates()` on the authority, and `GetStatus()` on an enrolled kernel. To cut a customer off, call `client.Federation.Revoke(enrollmentId)`. It revokes the enrollment and every certificate issued from it, and takes effect at once. Creating, revoking and listing enrollments and certificates needs administrator rights.

Redeeming a code and reading the enrollment status of a kernel are decided by their own capability, `chronicle.federation.enroll` (a kernel-scope capability, see [decision 0024](https://github.com/Cratis/Chronicle/blob/main/decisions/0024-kernel-authorization-decision-point.md)), which the administrator role includes. Store grants are scoped to a store and exclude the System store, and administrators are users, not applications, so by default only an administrator can redeem a code, or you drive it from configuration with `Federation:Enrollment:CodePath`.

To let a product redeem a code with its own client credential, list its client id in `Federation:Enrollment:Applications`:

```json
{
  "Chronicle": {
    "Federation": {
      "Enrollment": {
        "Applications": [ "billing-app" ]
      }
    }
  }
}
```

An application on the list is granted this one capability and nothing else. It can redeem a code and read the enrollment status, but it cannot create or revoke enrollments, list issued certificates, or do anything else an administrator can. The list is the only way to grant it: nothing else can make an application hold the capability, and the default (an empty list) changes nothing. The check reads the validated token and this configuration only, so it is the same over gRPC, HTTP and in-process calls, and a change to the list takes effect when the kernel is restarted.

### Renewal

A certificate lasts 14 days by default. After half of its lifetime the kernel renews it over mutual TLS with a new key, and the response can carry an updated endpoint, so the authority can move without re-enrolling customers. The kernel only follows an `https` endpoint, and the new endpoint still has to present a certificate that chains to the pinned authority and names that host, so a moved endpoint cannot be used to reach anything else. A renewed certificate must carry the same identity as the one it replaces, and is refused before it is stored if it does not. A kernel that stopped renewing and let its certificate expire is refused and has to be enrolled again. `Federation:Enrollment:RenewalCheckInterval` (default 1 hour) controls how often the kernel checks.

### The public enrollment routes

The enrollment routes are on the same host and port as the federation service. Two of them do not require a client certificate, because that is how a peer without one gets one: `GET /federation/enrollment/authority`, which returns the public authority certificate, and `POST /federation/enrollment/redeem`. `POST /federation/enrollment/renew` requires the certificate to renew. The listener accepts a connection without a certificate and enforces the requirement per route, so the federation service itself still requires one.

The unauthenticated surface is kept small: a request body of at most 8 KiB, a rate limit per source address (IPv6 sources are grouped by their /64, fetching the authority and redeeming share one budget, renewing has another, and a full table of sources drops the oldest instead of refusing everyone), a cap on how many of these requests are served at once, a constant-time token comparison, and one generic refusal for an unknown, expired, used or revoked enrollment. A renewal is accepted only for a certificate this kernel issued and has not revoked, so a leaked authority key alone cannot mint a working identity.

### Policy per subject

A certificate says who a peer is. A policy says what that peer may do. The policy is keyed by the opaque subject in the identity, so a subject with many links, for example one per product environment, has one policy. Set it on the authority kernel with these commands (administrator only):

| Command | Effect |
| --- | --- |
| `SetFederationPolicy(Subject, Direction, EventStore, Namespace, AllPublic, EventTypes)` | Creates the policy of a subject, or changes what it may do in one direction. |
| `SuspendFederationPolicy(Subject)` | Refuses the subject's links until it is resumed. |
| `ResumeFederationPolicy(Subject)` | Admits a suspended subject again. |
| `RevokeFederationPolicyDirection(Subject, Direction)` | Refuses one direction of the subject's links at once and leaves the other running. Set the direction again to allow it again. |
| `RevokeFederationPolicy(Subject)` | Refuses the subject's links for good. A revoked subject cannot be set again; enroll a new subject instead. |

`AllPolicies` lists them. Every change is an event with the identity that acted, and takes effect on the subject's links at once without affecting any other subject.

Each direction has its own entry, seen from the kernel that accepts the connection: `AcceptorOutbound` means the peer pulls events from this kernel, `AcceptorInbound` means the peer sends events to it. An entry names the target (an event store and a namespace) and the allowed event types, either every public event type or a list. The ceiling is always the public event types, and event types with protected personal data are never forwarded. Only `AcceptorOutbound` is served today; `AcceptorInbound` can be recorded now so a policy does not have to change when the peer-to-acceptor direction arrives.

A peer is admitted only when all of these hold, and every check reads the current state:

1. The client certificate chains to this kernel's authority and is within its validity.
2. The identity in its subject alternative name is exactly `spiffe://<trust-domain>/customer/<subject>/kernel/<instance>` in this kernel's trust domain.
3. Its serial number is in the registry of certificates this kernel issued, was not revoked, and was issued for that same identity. A certificate that chains but was never issued here, for example one signed with a leaked authority key, is refused.
4. The subject has a policy and it is active, not suspended or revoked.

The target comes from the policy alone, never from the certificate or the request. A pull request may leave the event store and namespace empty or name the policy's target, and anything else is refused, so one subject can never address another subject's target. Every refusal gives the peer the same empty, denied answer, so nothing about what exists can be learned; the reason is only logged on this kernel.

Pinned grants and bindings are unaffected. A client certificate that matches a pinned grant is served by that grant only and is never widened by a policy. A certificate with no pinned grant is the only one considered for hosted admission.

### One connection for both directions

A kernel behind a firewall dials out, and one connection carries events both ways. The existing `Pull` call is unchanged. The dialer opens a second call, `Serve`, on the same HTTP/2 connection: the accepting kernel sends pull requests as frames, and the dialer answers each with a batch read from its own outbox. The accepting kernel appends what arrives to the inbox its policy names, with the same guarantees as a pull link:

- Delivery is at least once, duplicates are dropped per origin, and an event whose origin is the accepting kernel's own store is rejected so two stores that serve each other cannot loop.
- The cursor is the tail of the inbox that was appended to, so a reconnect through any silo resumes from the last event that really arrived. The session is owned by a grain keyed by the subject; a newer connection supersedes an older one.
- Backpressure is the request/answer rhythm itself: the dialer is only asked for the next page when the previous one was appended. The per-link rate limits apply to each direction on its own.
- Each direction is decided by its own policy entry, read again before every request. From the accepting kernel's point of view, what the dialer produces is the `AcceptorInbound` entry and what the dialer consumes is the `AcceptorOutbound` entry. Revoking one with `RevokeFederationPolicyDirection` ends that direction while the other keeps running on the same connection; revoking the certificate or the subject ends both.

The dialer reconnects with backoff when the connection is lost and resumes both directions from their cursors. When it renews its certificate it reconnects once so the new certificate is presented.

#### Dial-only mode

`Federation:Enabled` starts both the listener and the consumer, as it always did. To run only the dialer, leave it off and configure `Federation:Dial` instead. The kernel then opens no listener, needs no listener certificate and no open port. It presents the certificate it obtained by [enrolling](#enroll-a-customer), trusts the authority root that came with the enrollment rather than the system trust store, and keeps its connection up through renewals.

```json
{
  "Chronicle": {
    "Federation": {
      "Enrollment": { "CodePath": "/run/secrets/chronicle-enrollment-code" },
      "Dial": {
        "Enabled": true,
        "EventStore": "customer",
        "Namespace": "Default",
        "LogicalSource": "hosted",
        "Serve": true,
        "ServeEventTypes": []
      }
    }
  }
}
```

| Setting | Meaning |
| --- | --- |
| `Enabled` | Dials the authority this kernel enrolled with. Off by default. |
| `EventStore`, `Namespace` | The local event store whose inbox receives the authority's events and whose outbox is served to it. Dialing needs an event store. |
| `LogicalSource` | The source name your event store subscriptions use for the authority. Defaults to `hosted`. |
| `Serve` | Also produces this kernel's public outbox to the authority over the same connection. Off by default; the authority still needs an `AcceptorInbound` entry for this kernel. |
| `ServeEventTypes` | Limits what is served. Empty serves every public event type. |

Because the authority decides the target from the policy, the dialer names no event store or namespace on the authority.

### Audit trail

Every step is an event in the system event log: `FederationAuthorityCreated`, `FederationEnrollmentCreated`, `FederationEnrollmentRevoked`, `FederationCertificateIssued` (subject, serial number, expiry, why, and who acted) and `FederationCertificateRevoked`, `FederationPolicySet`, `FederationPolicySuspended`, `FederationPolicyResumed`, `FederationPolicyDirectionRevoked` and `FederationPolicyRevoked` for the policies, and on an enrolled kernel `FederationIdentityInstalled`. The `FederationCertificateIssued` events are the registry of every certificate the authority issued. Events never contain the token secret, the enrollment code or a private key. The private key of an enrolled kernel is recorded only encrypted with a secret held by the key custody.

## Rotating a certificate

Pins are of the public key. Add a grant with the pin of the new key, switch the peer to the new certificate, then revoke the old link. For a server certificate, deploy the new certificate on the producer, update the binding's `ServerPin`, then retire the old one.
