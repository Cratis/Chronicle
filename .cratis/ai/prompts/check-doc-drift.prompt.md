---
agent: agent
description: Check for drift between AI assets and documentation inventory.
---
<!-- cratis-ai-managed: prompts/check-doc-drift.prompt.md -->

# Check Documentation Drift

Check whether AI assets and docs are in sync:

- `.cratis/ai/rules/` vs documented instruction inventory
- `.cratis/ai/skills/` vs documented skill inventory
- `.cratis/ai/agents/` vs documented agent roster
- `.cratis/ai/hooks/` vs architecture docs

Report:

1. Missing documentation entries.
2. Stale documentation entries.
3. Suggested updates by file.

Apply updates only if asked.
