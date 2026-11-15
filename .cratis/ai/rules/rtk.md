---
applyTo: "**/*"
description: "Choose command wrappers without losing exact output or exit codes."
---
<!-- cratis-ai-managed: rules/rtk.md -->

# Using rtk (Token-Optimized Commands)

Read **cratis-rtk** before choosing wrappers. The Bash hook rewrites supported
commands; built-in tools bypass it. Exact output and real exit codes win:
use `rtk proxy <cmd>` for parsed output, diagnostics and evidence driving an
operation, including wrapped failures. Never use summaries as gate evidence.
If rtk is absent, use normal tools. Keep scripts, hooks and CI native.
