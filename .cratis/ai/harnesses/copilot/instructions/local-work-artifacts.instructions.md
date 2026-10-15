---
applyTo: "**/*"
---
<!-- cratis-ai-managed: harnesses/copilot/instructions/local-work-artifacts.instructions.md -->
<!-- cratis-ai: generated Copilot instructions. Do not edit; rules/ is the source. -->

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
- **A session prompt or handover states when it is valid.** A fresh-session prompt,
  continuation prompt or handover in `.ai-work/` opens with its preconditions: the
  repository, the branch, the expected `HEAD` commit, every worktree path it names,
  and the date it was written. Without them nothing tells a later session whether
  the prompt still describes reality.
- **Check the preconditions before acting on a prompt.** A session that starts from
  one compares them with `git rev-parse --show-toplevel`, `git rev-parse HEAD`,
  `git branch --show-current` and `git worktree list` first. A mismatch (a moved
  `HEAD`, a missing worktree, another branch) means **the prompt is stale — not
  that the work is lost.** Do not recreate
  a worktree, reset, or discard anything because of it; look at the branches, the
  reflog and the open pull requests for where the work is now, then update or
  replace the prompt.
- **A worktree is a work record too.** Create it outside the repository tree, for
  example in a sibling `../.worktrees/` directory. A nested worktree inherits the
  parent's `.globalconfig` alongside its own, causing .NET's `MultipleGlobalAnalyzerKeys`
  errors or silently changing analyzer severities. **Delivered work is cleaned up by
  `.cratis/ai/hooks/scripts/cratis-cleanup-worktrees.sh`, not by memory:** run it from the
  main checkout when a pull request merges or you finish a task (hooks also run it after
  `gh pr merge` and at session start where the harness supports them, and **ship-changes**
  ends with it). It removes a worktree and its branch only on evidence the work reached the
  default branch — pushed is not merged — and never forces; ignored files such as a nested
  `.ai-work/`, `.env` files and local databases are deleted with it, so move anything you
  still need out first. Do not hand-remove worktrees or `git branch -D` a delivered branch
  when the script can; and `gh pr merge --delete-branch` from inside a worktree leaves the
  worktree and branch behind, which is why the script exists. If the branch must outlive the
  task unmerged, leave the worktree and say so in the handoff. Never `--force` a removal; a
  refusal means look, not push harder.
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
