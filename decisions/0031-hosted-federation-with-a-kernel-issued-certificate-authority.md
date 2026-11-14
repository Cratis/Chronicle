---
id: 0031-hosted-federation-with-a-kernel-issued-certificate-authority
title: Let a kernel host federation for many applications by acting as the certificate authority for their links, enrolling them with a one-time code, and letting the dialer carry both directions
status: proposed
stage: none
class: contract
reversibility: costly
proposed: 2026-11-14
applies-to:
  - Source/Kernel/Core/Federation/**
  - Source/Kernel/Core/Configuration/Federation.cs
  - Source/Kernel/Server/FederationListener.cs
  - Documentation/hosting/configuration/federation.md
---

<!-- markdownlint-disable-next-line MD025 -->
# Let a kernel host federation for many applications by acting as the certificate authority for their links, enrolling them with a one-time code, and letting the dialer carry both directions

Proposed on 2026-11-14 for [#5901](https://github.com/Cratis/Chronicle/issues/5901), [#5902](https://github.com/Cratis/Chronicle/issues/5902) and [#5903](https://github.com/Cratis/Chronicle/issues/5903). It is accepted when the pull request that introduces it is approved. It extends [decision 0027](./0027-secure-event-store-federation.md) and does not remove anything it decided.

## Context

Decision 0027 makes a link one direction, pulled by the consumer, with a pinned grant on the producer and a pinned binding on the consumer. That is right for two parties who configure each other. It does not scale to an operator that runs applications for thousands of customers in environments the operator does not control:

- Every customer adds a grant (and, for the reverse flow, a binding) to the operator's kernel, so operator configuration grows with the customer count.
- Data flowing from the customer to the operator would need the operator to dial into the customer's network, or a second link configured by both sides.
- Handing each customer a certificate to mount and restart with is manual, leaks secrets into deployment configuration and makes renewal the customer's problem.

## Decision

1. **The kernel is the certificate authority for hosted federation.** The first time hosted federation is enabled and no CA exists, the kernel creates one (a key pair and a self-signed certificate). The private key is held in the kernel's key custody (decisions 0025 and 0028): encrypted, never in plain configuration, logs, events or audit entries. No third-party CA or tool is required. Operators who prefer a pinned, peer-to-peer link keep decision 0027 unchanged.
2. **Enrollment replaces per-peer registration.** An operator creates an enrollment for a customer (command, Workbench, CLI). The kernel stores a single-use, expiring token, hashed. The **enrollment code** handed to the customer is one string carrying the public endpoint, the fingerprint of the CA root and the token secret. The endpoint is a kernel setting with no built-in default; an operator product supplies its own.
3. **Redeeming a code needs no restart.** `RedeemFederationEnrollment(code)` on the customer's kernel generates a key pair locally, verifies the server against the fingerprint in the code, sends a CSR and the token to the enrollment route, and receives a certificate plus the CA bundle. The identity in the certificate (a SAN URI `spiffe://<trust-domain>/customer/<id>/kernel/<instance>`) comes from the token, never from the CSR. Key and certificate are stored in the customer kernel's secret custody. Redeeming is idempotent when already enrolled, and may also be driven by configuration for automated deployments.
4. **Short-lived certificates, automatic renewal.** The default lifetime is 14 days. The kernel renews itself over mutual TLS with a new key, so a customer that stops renewing is locked out without any revocation step. The renewal response may carry an updated endpoint, so the hostname can move without re-enrolling.
5. **Authenticate by chain, registry and policy.** A client certificate is accepted only if it chains to the federation CA, its identity matches the format, **its serial is in the registry of certificates this kernel issued** (so a leaked CA key cannot mint a working certificate without a registry entry), and a policy for that customer allows the link. Revocation and suspension are events and take effect at once on that link; there are no CRLs.
6. **Policy, not grants, per subject.** The `<id>` in the identity is an opaque subject chosen by the operator, so one customer can hold many links (for example one per product environment). A policy template maps `customer/<id>` to a target (for example a namespace) and the allowed event types in each direction. The ceiling is still the set of public event types. The target is decided by the operator's policy and never by the certificate or the request, so a subject cannot address another subject's target. Per-subject overrides, suspension and revocation are events in the system log, with the identity that acted.
7. **One host, two route groups.** Enrollment and the link sessions are served on the federation listener, on the same host and port. The enrollment route does not require a client certificate; every session route does. The enrollment route is the only pre-authentication surface and is kept minimal: a small request size limit, rate limiting per source, constant-time token comparison, and one generic error for unknown, expired and used tokens. The main port is still never used.
8. **The dialer carries both directions.** Who dials is decoupled from which way data flows. On one connection the dialer consumes with the existing `Pull` and produces through a new `Serve` call (the acceptor sends pull requests as frames and the dialer answers with batches from its own outbox). Pull semantics, backpressure, cursors, rate limits and revocation stay per direction, each governed by its own policy entry. A grain keyed by customer owns the session so any silo can accept the connection.
9. **Dial-only mode.** A kernel that only connects out needs no listener, no listener certificate and no open port. Enabling the consumer side is separate from enabling the listener.
10. **Additive in major 19.** Pin-based grants and bindings, the existing `Pull` rpc and every existing grain interface member are unchanged. New rpcs, commands, events and configuration are additions; the wire-compatibility gate passes without a `major` label. Deprecating pins is a separate decision for a later major.
11. **Enrolling is its own capability.** Redeeming an enrollment code and reading the enrollment status of this kernel are decided by the kernel-scope capability `chronicle.federation.enroll` (decision 0024), bundled into the administrator role. Creating and revoking enrollments, and every authority-side operation, stay with the administrator policies. The kernel has no way yet to grant a kernel-scope capability to a non-administrator principal: store grants are store-scoped and exclude System, and administrators are users. So today the caller redeeming a code is an administrator, or the code is redeemed by configuration at startup; a narrower grant for an application principal is a later, separate decision.

## Out of scope

- Operating Chronicle behind a particular ingress. The deployment must pass TLS through to the listener so client certificates reach it; hostnames, DNS and ingress choice are the operator's.
- Offline roots and multi-level CAs. A later hardening step; one CA is the first version.
- Application-side user interfaces for redeeming a code. Those belong to the products that host a kernel (Showrunner), calling the command.

## Implementation

[#5901](https://github.com/Cratis/Chronicle/issues/5901) delivers the CA, enrollment, redemption and renewal. [#5902](https://github.com/Cratis/Chronicle/issues/5902) delivers chain, registry and policy authentication. [#5903](https://github.com/Cratis/Chronicle/issues/5903) delivers the dialer-initiated bidirectional session and dial-only mode. Documentation is updated in `Documentation/hosting/configuration/federation.md`.

## Consequences

- The kernel becomes security-critical code that issues certificates. It needs a security review, and the CA key custody must follow decisions 0025 and 0028.
- The operator's per-customer state is events and a registry, not configuration, and scales to thousands of customers.
- A customer needs one outbound connection and one enrollment code, and nothing inbound.
- A leaked CA key is contained by the registry check, but still requires rotation of the CA, which is a heavier operation than rotating a pinned peer.
- Pinned federation and hosted federation coexist, so the kernel has two peer authentication paths to specify and test.
