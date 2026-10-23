<!-- cratis-ai-managed: skills/cratis-ci-pipeline-performance/references/sources.md -->
# Sources and attribution

The structure and method of this skill adapt two MIT-licensed skills. The text
here is new; no passage is copied.

- `github-actions-efficiency` from github/awesome-copilot, MIT license:
  https://github.com/github/awesome-copilot/blob/main/skills/github-actions-efficiency/SKILL.md.
  Adapted: measure before changing, guardrails against hiding required
  validation, separating repository changes from organization settings, a
  static-only analysis label, expected against measured savings, and wall clock
  reported apart from runner-minutes.
- `ci-speedup` from starslingdev/skills, MIT license:
  https://github.com/starslingdev/skills/blob/main/skills/ci-speedup/SKILL.md.
  Adapted: the critical path over required checks, queue delay as a separate
  signal, shard imbalance, and an always-run aggregator that checks every
  `needs.*.result`, including for `merge_group`.

Primary documentation consulted for factual claims:

- GitHub Docs, dependency caching, secure use of Actions, concurrency control and
  merge queue.
- Docker Docs, GitHub Actions cache backend (custom `scope`).
- Microsoft Learn, `dotnet test` blame options and Microsoft.Testing.Platform
  retry and crash/hang extensions.
- The `actionlint` and `zizmor` project documentation.

- Studio CI investigation (Einar Ingebrigtsen, Cratis, 2026): the method of counting
  compiles per push, sizing cores before job count, finding test time hidden outside
  per-test timing, testing each guarantee where it can break, and layering
  content-addressed caches before warm workspaces. Reduced here to generic levers.

The Cratis-specific levers come from the shared `Cratis/Workflows` cache actions
and the build-once, artifact and shard layout used in the Arc repository.
