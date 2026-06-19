---
agent: agent
description: Check for drift between AI assets and documentation inventory.
---

# Check Documentation Drift

Check whether AI assets and docs are in sync:

- `.ai/rules/` vs documented instruction inventory
- `.ai/skills/` vs documented skill inventory
- `.ai/agents/` vs documented agent roster
- `.ai/hooks/` vs architecture docs

Report:

1. Missing documentation entries.
2. Stale documentation entries.
3. Suggested updates by file.

Apply updates only if asked.
