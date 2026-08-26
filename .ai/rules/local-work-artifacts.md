---
applyTo: "**/*"
---

# Local AI work artifacts belong in `.ai-work/` only

AI-assisted sessions produce working artifacts: plans, handover documents, session
notes, continuation prompts, status boards, TODO and scratch analyses, research
dumps, and similar coordination files. These are **work records, not documentation**.

- Create every such artifact inside **`.ai-work/`** at the repository root — never at
  the repository root itself, never under documentation folders, never anywhere else.
- `.ai-work/` is listed in `.gitignore` and must stay untracked. Never commit anything
  inside it, never `git add -f` anything inside it, and never remove the ignore entry.
- These artifacts must never enter git history or reach GitHub — not on any branch.
  If you find one tracked in git, move it into `.ai-work/` and remove it from
  tracking in a dedicated commit.
- Knowledge that must outlive the session (real documentation, ADRs, operator
  guides) is written deliberately into the repository's documentation structure
  through normal review — not left behind as a work record.
