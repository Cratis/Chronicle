---
applyTo: "**/for_*/**,**/when_*/**,**/*.csproj,**/Directory.Build.props,**/Directory.Build.targets,**/*.Specs/**,**/.github/workflows/**"
paths:
  - "**/for_*/**"
  - "**/when_*/**"
  - "**/*.csproj"
  - "**/Directory.Build.props"
  - "**/Directory.Build.targets"
  - "**/*.Specs/**"
  - "**/.github/workflows/**"
---
<!-- cratis-ai-managed: harnesses/copilot/instructions/fast-feedback.instructions.md -->
<!-- cratis-ai: generated Copilot instructions. Do not edit; rules/ is the source. -->

# Fast feedback is a product requirement

Applies to every Cratis repository and every application built on Cratis. A build and a spec run are
measured in seconds. Each line is tagged **[contract]** (binding) or **[convention]** (the house
default) per [`general.md`](../../../rules/general.md).

- **[contract] Building and running specs is as fast as possible, locally and in CI.** A change
  that makes a build or a spec run slower is a regression: measure it (cold full-suite wall time
  plus a focused single-folder loop) and fix it in the same change.
- **[contract] Every spec is safe to run in parallel with every other spec.** No shared mutable
  static state, no ordering assumptions, no fixed ports, paths or names that two specs could both
  claim. Never serialize a suite or opt out of shared setup to hide an isolation bug.
- **[contract] No per-spec startup cost for convenience.** Setup is built once and shared
  (shared lifecycle stays on). Slow setup in a framework harness is fixed in the framework and
  reported upstream; do not work around it by merging unrelated assertions or skipping specs.
- **[contract] Analyzers and generators are cheap or they are not added.** A slow one is an
  upstream issue; see [`framework.md`](./framework.instructions.md) for how analyzers are written for speed.
- **[convention] Specify with the cheapest proof that is real.** Call a pure decision directly;
  use a scenario for what only the framework pipeline or engine proves. The per-artifact table is in
  [`specs.scenarios.csharp.md`](./specs.scenarios.csharp.instructions.md); the universal base is in
  [`specs.md`](./specs.instructions.md).

A cold full-suite run under a minute is the target for a repository's spec gate. It is a budget to
protect, not a reason to delete coverage.
