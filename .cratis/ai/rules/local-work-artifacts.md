---
applyTo: "**/*"
---
<!-- cratis-ai-managed: rules/local-work-artifacts.md -->

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
- A genuine follow-up that must survive the session is **not** a work record — suggest
  opening a GitHub issue for it (or open one when asked) so future work is tracked
  where everyone can see it, instead of leaving a planning file behind.
- Knowledge that must outlive the session (real documentation, ADRs, operator
  guides) is written deliberately into the repository's documentation structure
  through normal review — not left behind as a work record.
- **A worktree is a work record too.** Remove the worktree you created for a task
  once its branch is merged to the default branch — pushed is not merged. Do it from
  the parent checkout, after `git status --ignored` in the worktree, because a nested
  `.ai-work/`, `.env` files and local databases are ignored and are deleted with it.
  If the branch must outlive the task unmerged, leave the worktree and say so in the
  handoff. Never `--force` a removal; a refusal means look, not push harder.
- **A decision log is not a work record.** A decision — a durable choice with a
  decider and a date — is documentation: it lives in **`decisions/`** (or the
  repository's documented decisions folder) and is reviewed like any other
  documentation. A handover may summarize decisions; it never holds the only
  copy. If a session produced a real decision, land the record in `decisions/`
  before the session's `.ai-work/` files are discarded.
- The record's shape (front matter, status and stage values, supersession
  pointers) is defined by the decision-record skill and the shared vocabulary
  once this repository carries them; until then use the repository's existing
  decisions folder and keep decider and date explicit.
