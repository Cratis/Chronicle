---
agent: agent
description: Audit hook files for correctness, portability, and enforcement coverage.
---
<!-- cratis-ai-managed: prompts/audit-hooks.prompt.md -->

# Audit Hooks

Review `.cratis/ai/hooks/` and report whether hooks are:

- enforcing the intended policy
- portable across environments
- aligned with canonical source rules
- using bash-first commands for script execution

Focus on gaps, risks, and missing checks. If improvements are obvious and low-risk, propose exact edits.
