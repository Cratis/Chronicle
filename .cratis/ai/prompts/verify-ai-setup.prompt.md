---
agent: agent
description: Check the installed Cratis AI corpus for drift, conflicts, and healthy harness adapters.
---
<!-- cratis-ai-managed: prompts/verify-ai-setup.prompt.md -->

# Verify AI Setup

Check the repository's installed Cratis AI setup by running:

```bash
cratis ai status
```

It reports the configured harnesses, profiles and languages, the installed corpus revision against the
available one (`updateAvailable`), and every managed file that was modified locally — exiting non-zero
when there is any. (A user-owned path that collides with a managed one is reported and refused by
`cratis ai install` / `cratis ai update`, not by `status`.) Then confirm
the harness adapters this repository uses (`.claude/`, `.agents/`, `.github/`, `.pi/`, `.cursor/`,
`.opencode/` as applicable) still resolve into `.cratis/ai/` — a broken or dangling symlink is a setup
fault, not corpus drift.

If anything is reported:

1. List every finding with the exact file path.
2. Explain whether it is a hand-edited managed file, a pending update, or a broken adapter.
3. Propose the smallest safe fix. A managed file is never patched by hand — `cratis ai update`
   replaces it (with `--force` only for content already recorded as Cratis-managed); a user-owned
   file is the repository's and stays.
4. Apply fixes if requested.
