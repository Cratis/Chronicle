---
agent: agent
description: Validate AI framework setup integrity, canonical source conventions, and symlink health.
---

# Verify AI Setup

Validate the repository AI setup by running:

```bash
bash .ai/hooks/scripts/validate-ai-setup.sh
```

If anything fails:

1. List every failure with the exact file path.
2. Explain whether the issue is canonical-source drift, missing metadata, or broken links.
3. Propose the smallest safe fix.
4. Apply fixes if requested.
