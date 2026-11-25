---
id: 0033-operator-application-manages-hosted-federation
title: Let a configured operator application manage the hosted federation a kernel is the authority for, through one capability that administrators also hold
status: proposed
stage: none
class: contract
reversibility: moderate
proposed: 2026-11-16
applies-to:
  - Source/Kernel/Core/Security/**
  - Source/Kernel/Core/Federation/**
  - Source/Kernel/Core/Configuration/FederationHostedOptions.cs
  - Documentation/hosting/configuration/federation.md
---

<!-- markdownlint-disable-next-line MD025 -->
# Let a configured operator application manage the hosted federation a kernel is the authority for, through one capability that administrators also hold

Proposed on 2026-11-16. It is accepted when the pull request that introduces it is approved. It extends [decision 0032](./0032-hosted-federation-with-a-kernel-issued-certificate-authority.md), and changes only its point 11 sentence that creating and revoking enrollments, and every authority-side operation, stay administrator-only. It decides through the decision point of [decision 0024](./0024-kernel-authorization-decision-point.md).

## Context

Decision 0032 makes the kernel the certificate authority for many applications' links, and expects an operator to create an enrollment per customer environment and set a policy per subject. An operator that does this at scale is itself an application: a platform console hands out enrollment codes and sets policies with its own client credential, without a person signing in to the kernel.

Decision 0032 kept creating, listing and revoking enrollments, and setting policies, with the administrator policies. Administrators are users, not applications, and nothing lets an application hold administrator rights except an external authority's claim mapping, which grants every administrator capability. So an operator application could either not do its job, or had to be made a full administrator of a kernel that may be shared by many products.

The commands also used the operations policy, which only audits a caller that is not an administrator unless `EnforceAdministratorPolicyForOperations` is set, while the queries used the enforced administrator policy. The same surface was therefore enforced on reads and audit-only on writes.

## Decision

1. **One capability for managing hosted federation.** `chronicle.federation.manage` decides creating, listing and revoking enrollments, listing issued certificates, setting, suspending, resuming and revoking subject policies and their directions, and reading policies and link status. The administrator role includes it, so administrators keep everything they had.
2. **Configuration grants it to named applications, and nothing else.** `Federation:Hosted:Applications` lists client ids. The decision point grants an application on the list this one capability, from the validated token and the configuration alone, exactly as `Federation:Enrollment:Applications` grants `chronicle.federation.enroll`: no I/O, no memoization, a principal with a user marker is never an application, the internal authority's client id is the name claim and an external authority's is `client_id` or `azp`. The two lists are independent; neither implies the other. An empty list, the default, grants nothing.
3. **The management surface is enforced.** These commands and queries use the `Chronicle.Federation.Manage` policy, which requires an authenticated caller and the capability, for reads and writes alike. A caller that is neither an administrator nor a listed application is refused even when operations are in audit mode. Pinned grants and bindings (decision 0027) are not part of this surface and keep their policies.

## Out of scope

- Granting other capabilities to applications by configuration. Each grant stays one capability per list.
- Scoping the capability to a subset of subjects. A listed application manages every subject of the authority.

## Consequences

- A platform can operate hosted federation with its own client credential and nothing more.
- On a kernel with operations in audit mode, a caller that is not an administrator, which could previously create or revoke enrollments and change policies with only an audit log entry, is now refused. Hosted federation shipped days before this decision, so the change is made in major 19 as a security fix.
- Any holder of a listed client's secret can issue enrollment codes for the authority, which admits new peers once a policy is set. Operators list only the client that hands out codes and protect its secret like an administrator credential.
