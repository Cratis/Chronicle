---
profile: cratis/engineering
---
<!-- cratis-ai-managed: rules/local-verification-and-delivery.md -->

# Verify locally, deliver once

CI/CD is shared and finite: it confirms, it is not a feedback loop.

- Build, run specs and lint locally; never push to find out if it works, and reproduce a CI failure locally before pushing again.
- Push and open a PR only when ready: local gates passed on the exact head. No draft, placeholder or WIP PRs.
- One PR per repository. As orchestrator, brief sub-agents to commit verified work to the shared branch and never push or open a PR; you verify the combination and open the one PR ([pull-requests.md](./pull-requests.md) lists the split exceptions).
