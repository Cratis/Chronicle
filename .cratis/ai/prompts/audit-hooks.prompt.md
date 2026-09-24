---
agent: agent
description: Audit hook files for correctness, portability, and enforcement coverage.
---
<!-- cratis-ai-managed: prompts/audit-hooks.prompt.md -->

# Audit Hooks

Review `.cratis/ai/hooks/` — the write guard, store-mutation guard, pattern scan, and quality gate — and report whether hooks are:

- enforcing the intended policy (including the `cratis chronicle` read-only allowlist and its fail-closed behavior)
- portable across environments
- aligned with canonical source rules
- using bash-first commands for script execution

Focus on gaps, risks, and missing checks. If improvements are obvious and low-risk, propose exact edits.
