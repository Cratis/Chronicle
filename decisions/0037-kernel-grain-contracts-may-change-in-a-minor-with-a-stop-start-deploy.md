---
id: 0037-kernel-grain-contracts-may-change-in-a-minor-with-a-stop-start-deploy
title: Let kernel-internal grain contracts change in a minor release, marked as a stop-start deploy, while the client wire contract and persisted formats stay compatible
status: proposed
stage: none
class: contract
reversibility: moderate
proposed: 2026-11-20
applies-to:
  - Source/Kernel/Core/**
  - .github/grain-compatibility-waivers.txt
  - .github/workflows/grain-compatibility.yml
  - .github/scripts/integration_lane_exclusions.py
  - Documentation/hosting/rolling-deploy.md
  - Documentation/contributing/kernel/grain-interfaces.mdx
---

<!-- markdownlint-disable-next-line MD025 -->
# Let kernel-internal grain contracts change in a minor release, marked as a stop-start deploy, while the client wire contract and persisted formats stay compatible

Proposed on 2026-11-20. It is accepted when the pull request that introduces it is approved. It narrows the promise that every minor release can share a cluster with the one before it, and amends statements in [decision 0017](./0017-generation-enablement-compatibility-and-recovery.md), [decision 0018](./0018-partition-ownership-protocol.md) and [decision 0034](./0034-release-dormant-subscribed-observers.md) (see [Amended statements](#amended-statements)). It is part of the plan tracked by [#6105](https://github.com/Cratis/Chronicle/issues/6105).

## Context

Until now a grain interface or a grain payload type only grew within a major release, so silos of two minors could share an Orleans cluster during a rolling upgrade. That promise has a price. Methods are kept next to their replacements and forward to them, `[Version]` is raised to route calls, and 172 of the 297 payload types in `GrainPayloadContracts.txt` still travel as System.Text.Json because changing a serializer is a break.

The grains are the kernel's own implementation. No client sees them: clients speak the gRPC contract. The owner has decided to go all in on Orleans and give up the rolling-compatible promise for kernel-internal grain changes, so the kernel can use Orleans the way it is meant to be used. The CI gates (`grain-compatibility.yml`, the `RollingUpgrade` integration shard) would otherwise reject every such change.

## Decision

1. **Kernel-internal grain contracts may change in a minor release.** This covers grain interfaces and their methods, grain keys, grain payload types and their serializers, grain class and interface names, and placement. No forwarding methods, duplicate overloads or dual code paths are required to keep two releases talking.
2. **Such a release is a stop-start deploy.** If a change means the release cannot share a cluster with the previous one, the release is marked **Requires a stop-start deploy**. Every node is stopped, then the new version is started. Replacing nodes one at a time, or any overlap of the two versions, is not supported for that release.
3. **What stays guaranteed in every minor and patch:**
   - the client gRPC contract, guarded by `wire-compatibility.yml` and `grpc-compatibility.yml`;
   - persisted storage formats, so a stop-start deploy reads what the previous release wrote and a rollback is not blocked by storage; and
   - the Cratis.Choreography job contracts: a Choreography major is still taken only with a Chronicle release that says so.
4. **The marker has one human form and one machine-readable form.**
   - Human: the pull request's Changed or Removed bullet, and therefore the release notes, carries the exact sentence `Requires a stop-start deploy: this release cannot share a cluster with <previous release>.` Operators search release notes for `requires a stop-start deploy`.
   - Machine: a line in `.github/grain-compatibility-waivers.txt` naming the previous release tag, which is the baseline the release cannot share a cluster with. The Grain Compatibility workflow reads it and skips the comparison. The same file makes the `RollingUpgrade` integration lane skip, with a log line, when the release it would upgrade from is waived; the lane still runs and is required for every other release. The waiver file is the single list tooling reads; the release-note sentence is for people.
5. **Batch the changes.** Every minor may now be a stop-start release, but each one costs operators a full stop. Contract changes are grouped into as few releases as practical, and a break that is not needed is not made. There is no limit of one per N minors.
6. **Operators' deploy tooling must treat a release with the marker as stop-start**: no overlap and no rolling replace. This is stated in the rolling-deploy documentation and in the release notes of the first stop-start release. A node that is started beside an incompatible one still stops at startup with the documented critical message, so a missed marker stalls a rollout instead of corrupting it.
7. **Rolling-compatible changes remain the better choice when they are cheap.** The guidance on what makes a method identity, and on never moving a type a custom codec names, stays as the way to make a change that keeps the release rolling-compatible.

## Amended statements

- **Decision 0017**, bridge release and exclusion: the bridge release it requires is still how a store reaches the generation floor, but the preparatory step is no longer necessarily a rolling upgrade; a release may require a stop-start deploy instead. The floor, the exclusion of incompatible binaries and the refusal of unsafe downgrade are unchanged.
- **Decision 0018**, mixed versions: the statement that the oldest kernel a minor must coexist with is 19.106.0 holds only for minors not marked as stop-start. A stop-start release need not coexist with an older kernel, which makes the observer-state additions this record verified optional for compatibility, but persisted formats stay readable by the previous release as in point 3.
- **Decision 0034**, DP7 rolling-upgrade compatibility: "Grain interfaces only grow within a major" is replaced by this record. Additive grain methods remain allowed and still make the release rolling-compatible; they are no longer required.
- Decision 0022 repeats the same project rule as a consequence; it is read together with this record.

## Options considered

- **Keep the rule and take grain-contract changes only in a major.** Rejected by the owner: it blocks the plan to use Orleans fully for years.
- **Support rolling upgrades with dual code paths for every change.** Rejected: that is the cost this decision removes.
- **Drive the marker from prose only.** Rejected: tooling must read a list, not parse release notes.
- **Limit stop-start releases to one per N minors.** Not adopted; batching is the guidance.

## Default if unanswered

The previous rule stays in force and no grain-contract change can ship in a minor.

## Timeline and scope

- **In scope:** this record, the project concern, the operator and contributor documentation, and the `RollingUpgrade` lane skip for a waived release.
- **Out of scope:** the grain-contract changes themselves, and any change to the gRPC contract or storage formats.
- **Release:** `no-release` for the policy and CI change. Each later grain-contract change is a `minor` that carries the marker.

## Verification

- **Done when:** a release that changes a grain contract can pass CI with a waiver entry and the marker sentence, the `RollingUpgrade` lane logs that it is skipped for it, and the lane still runs for every other release.
- **Verify by:** the specs in `.github/scripts/test-sparse-integration-workflows.py` for the lane skip, the self-test of the GrainCompatibility tool, and `bash Documentation/verify-markdown.sh`.

## Consequences

- Kernel-internal grain work no longer carries forwarding methods and duplicate paths.
- Operators lose rolling upgrades for the releases that carry the marker, and must read release notes before each upgrade.
- The client wire contract and stored data keep their guarantees, so clients and storage are not part of a stop-start deploy.
- Operators whose deploy tooling replaces nodes one at a time must teach it the marker, or the rollout stops at the first new node.
