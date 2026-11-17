---
applyTo: "**/contracts/**,**/vectors/**,**/*vectors*.json,**/*.proto,**/*.schema.json"
paths:
  - "**/contracts/**"
  - "**/vectors/**"
  - "**/*vectors*.json"
  - "**/*.proto"
  - "**/*.schema.json"
---
<!-- cratis-ai-managed: rules/published-contracts.md -->

# Published contracts

A published contract is a specification other repositories implement: a wire format, a
protocol profile, or an authorization or trust-boundary specification, usually with
conformance vectors. This rule applies when a change alters behavior at such a surface.
Every line is tagged **[contract]** (binding) or **[convention]** (the house default) per
the Three Levels of Authority in [`general.md`](./general.md). The router is the
`general.md` "Where to Look" row; the rule loads by path, so look it up before editing
source on a trust boundary or wire surface.

- **[contract] Find the governing contract before changing the behavior.** Locate the
  published specification and its vectors for the surface (authentication, authorization,
  tokens, serialization, protocol handshakes) and cite both, by name and version or
  revision, in the issue and in the pull request comment. If none exists, say so
  explicitly in both; silence is not the same as "no contract".
- **[contract] The contract wins over the implementation.** When the change conflicts with
  a vector, follow the contract, or change the contract first through its owner and
  re-pin. Never change the implementation alone to disagree with it, and never edit a
  pinned copy of a vector to make a spec pass.
- **[contract] Implementers run the pinned vectors as executable specs in CI.** A
  repository that implements a published contract pins a revision of its vectors and runs
  every vector as a spec on each build; reading the vectors is not verification. A spec
  that loads zero vectors fails (see [`guards-and-fuses.md`](./guards-and-fuses.md)).
- **[contract] Review checks the change against the cited contract.** Reviewers, including
  the security lens, compare the diff to the cited specification and vectors. An uncited
  behavior change on such a surface is a blocking finding, as is a change that conflicts
  with a vector.
- **[convention] Bump the pin deliberately.** Update the pinned vectors in their own
  commit so the behavior change they carry is visible and reviewable.
