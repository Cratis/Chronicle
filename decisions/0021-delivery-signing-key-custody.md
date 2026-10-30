---
id: 0021-delivery-signing-key-custody
title: Sign HTTP deliveries with one Ed25519 key per event store and one HMAC secret per target, keep both encrypted in the kernel, rotate them with an overlap, and use BouncyCastle for Ed25519
status: accepted
stage: none
class: contract
reversibility: costly
proposed: 2026-10-25
decided: 2026-10-30
decider: woksin (Q-146)
applies-to:
  - Source/Kernel/Core/Observation/Webhooks/**
  - Source/Kernel/Concepts/Observation/Webhooks/**
  - Source/Kernel/Contracts/Observation/Webhooks/**
  - Source/Kernel/Core/Configuration/Webhooks.cs
  - Source/Clients/DotNET/Webhooks/**
  - Source/Tools/**
  - Documentation/webhooks/**
---

<!-- markdownlint-disable-next-line MD025 -->
# Sign HTTP deliveries with one Ed25519 key per event store and one HMAC secret per target, keep both encrypted in the kernel, rotate them with an overlap, and use BouncyCastle for Ed25519

Proposed on 2026-10-25, accepted on 2026-10-30 by woksin (Q-146). This record decides what [decision 0020](0020-kernel-http-delivery-contract.md) split off under its Q1 and Q9: who holds the signing keys, where they are stored, how they rotate and which library implements Ed25519. It does not change the wire contract in 0020 (Standard Webhooks headers, `v1a` and `v1`, `Cratis-Key-Id`). It is the design input for [#5117](https://github.com/Cratis/Chronicle/issues/5117).

## Context

Decision 0020 requires every envelope delivery to be signed. The default is Ed25519 (`v1a`), so a receiver holds only a public key. HMAC-SHA256 (`v1`) is an opt-in alternative, and its secret is unique per target (0020, "Authentication and signatures"). Each delivery carries `Cratis-Key-Id`, and several signatures can be sent at once during an overlap.

Three things were left open:

- The scope and storage of the keys, and how a receiver gets the public key.
- How keys rotate.
- The Ed25519 library. .NET has no built-in Ed25519 ([dotnet/runtime#63174](https://github.com/dotnet/runtime/issues/63174)), and no published Standard Webhooks library verifies `v1a`.

### The platform in front of the receiver

In the full deployment, AuthProxy sits in front of Arc, Chronicle and Studio.

What it does on the receiver path:

- It strips spoofed identity headers (`x-ms-client-principal*`) and tenant headers (`x-cratis-tenant-id`, `Tenant-ID`).
- With client credentials configured for a route prefix, it rejects callers that hold no token for that prefix.

What it does not do:

- It does not inspect or authenticate the body.
- It does not bind the request to the sender: route values (event store, namespace, observer id), the Standard Webhooks headers, `Cratis-Key-Id`, `traceparent` and the deadline header pass through.
- It does not stop a replay of a captured request inside the timestamp tolerance.
- It admits every client that holds a token for the prefix, not only the kernel.
- On paths listed as anonymous, it admits everything.

So the signature is the only proof that the kernel produced the body, and it stays mandatory behind AuthProxy. The receiver verifies it, rejects timestamps outside the tolerance, takes the event store and namespace from the signed body and takes the public key from its own configuration. Standard Webhooks signs `id.timestamp.body`, not the URL, which is why route values cannot be trusted.

Without AuthProxy nothing changes: the signature is required everywhere, and `AllowUnauthenticated` stays a receiver-host setting that defaults to `false`, logs a startup warning and is never read from the request.

## Decision

### Key scope

- **Ed25519 (`v1a`): one key pair per event store.** One key signs the envelopes for every HTTP target of that store. Receivers hold the public key only, so sharing it across targets of a store does not let one receiver forge deliveries to another.
- **HMAC-SHA256 (`v1`): one secret per target**, as Standard Webhooks requires for symmetric keys. A shared secret would let one receiver sign for another.
- Every key has a key id. `Cratis-Key-Id` lists the id of every key that signed the request, comma-separated, in the same order as the signatures in `webhook-signature`. A receiver verifies with the listed keys it trusts and accepts the request when one of them verifies. It rejects the request when it trusts none of the listed ids or when no trusted key verifies.

### Storage

- The kernel generates the key material. The private key and the HMAC secrets are stored encrypted with the kernel's `IEncryption`, in the same storage as other kernel state, and are never written to logs, events or API responses after creation.
- The public key is not secret. Registration returns it, and a kernel `GET` for an event store returns the current and the previous public keys with their key ids.
- A new HMAC secret is shown once, when the target is registered or the secret is rotated.

### Rotation

- Rotation is an explicit command through the CLI and the kernel API. The kernel does not rotate on a timer.
- On rotation the kernel creates a new key, signs with it, and keeps the previous key as verifiable for an overlap. During the overlap each delivery carries both signatures in `webhook-signature`, and `Cratis-Key-Id` lists both key ids, new first. A receiver that still holds only the previous key verifies with it until it has fetched the new one.
- The overlap defaults to 24 hours and is configurable. The previous key is then retired: no longer used to sign, and removed from the `GET` after a further retention period.
- Receivers load public keys from configuration. They fetch the new key before the overlap ends; the kernel never pushes a key to a receiver.
- A suspected compromise is a rotation with zero overlap. The receiver then rejects deliveries signed with the old key as soon as it has the new one.

### Responsibilities

| Party | Responsibility |
| --- | --- |
| Kernel | Generates and encrypts keys, signs every envelope delivery, exposes public keys, enforces the overlap and retirement, audits rotations. |
| Operator | Runs rotation, distributes the new public key (or HMAC secret) to receivers within the overlap, protects the kernel's encryption key. |
| Receiver | Holds the public key from configuration, verifies in constant time, enforces the timestamp tolerance, verifies only with the listed key ids it trusts (ignoring the others) and rejects a request no trusted key verifies, takes event store and namespace from the signed body, never follows redirects. |
| Platform (AuthProxy) | Strips identity and tenant headers and gates the route by token. Does not replace signature verification. |

### Library

- Ed25519 is implemented with **BouncyCastle.Cryptography**, which is managed code and has no native binaries. This keeps the kernel and the receivers portable to Azure Functions, AWS Lambda and Cloud Run.
- The kernel and the .NET receiver share one small verification and signing component owned by this repository, on top of that one library. HMAC `v1` uses the .NET `HMACSHA256`.
- The choice is conditional on a check on the #5114 spike hosts (the Azure Functions isolated worker and Lambda with SnapStart): signing and verification work, the assembly trims or loads without extra configuration, and the per-request cost is acceptable at the batch rates in 0020. If the check fails, this record is reopened before #5117 ships.
- Test vectors from RFC 8032 and a cross-check against a second implementation are part of the verification.

## Options considered

- **One key per target for Ed25519.** Isolates receivers further, but multiplies keys and public-key distribution for no gain, since receivers hold only the public key. Rejected.
- **One key for the whole kernel.** Fewest keys, but a rotation for one store affects all stores, and a leaked key compromises every store. Rejected.
- **Library: NSec.** Fast and well maintained, but it needs native libsodium for each platform, which complicates Lambda and Azure Functions packaging. Rejected for portability.
- **Library: project-owned Ed25519.** Rejected: the project would own cryptographic primitives.
- **Rely on AuthProxy and skip signatures behind it.** Rejected: the body, route values and replays inside the tolerance pass through it.
- **Timed automatic rotation.** Needs a way to distribute the new public key to receivers on a schedule. Deferred; the explicit command can be scheduled by the operator.

## Default if unanswered

Without this record, #5117 cannot choose a library or a key scope, and the signing key would be guessed in code. Receivers written in the meantime would bind to that guess.

## Verification

**Done when:**

- A specification signs and verifies `v1a` against RFC 8032 vectors and against an independent implementation, and verifies `v1` against the Standard Webhooks reference vectors.
- A specification shows a delivery signed with two keys during an overlap verifies at a receiver that trusts only the previous key, at one that trusts only the new key and at one that trusts both, and that a delivery whose listed ids are all unknown to the receiver is rejected.
- A specification shows the private key and HMAC secrets are stored encrypted, and absent from logs and API responses.
- The library check runs on the Azure Functions isolated worker and on Lambda with SnapStart, and the result is recorded in this record.

## Consequences

- The kernel owns key material and an `IEncryption` dependency on the delivery path.
- Adding BouncyCastle adds one managed dependency to the kernel and the .NET receiver.
- Operators must distribute public keys within the overlap; a missed distribution rejects deliveries and the kernel retries them.
- Receivers in other languages use HMAC `v1` or their own Ed25519 library, since no standard library verifies `v1a`.
- Reversibility: key scope is changeable if receivers take keys from configuration; dropping an algorithm is not (0020, Q8).

## Unknowns

- Whether BouncyCastle meets the host checks above is not verified.
- The default overlap (24 hours) and the retention period of a retired key are proposals.
- Whether a kernel `GET` for public keys needs its own permission is not decided.
