---
applyTo: "**/*"
---
<!-- cratis-ai-managed: rules/git-commits.md -->

# How to Write Git Commits

**Before writing any commit message, committing, pushing, or creating/editing a PR, read the `cratis-commits-and-pull-requests` skill.** It holds the commit format, grouping, staging and when-to-commit procedure. This rule keeps the history contract that must hold even before the skill is read.

Commits are the permanent record of how the codebase evolved. Each commit is a single logical unit of work: *what* changed and *why*, in an imperative subject of at most 72 characters, never WIP, never uncompilable. Stage specific files, never `git add .`.

## Never rewrite history

**Committed history is append-only. Never rewrite it — under any circumstances, on any branch, including your own.**

These commands are **forbidden** unless the human explicitly asks for that specific command on that specific branch in that specific message:

| Forbidden | Why |
|---|---|
| `git rebase` (any form, incl. `-i`, `--onto`, `--autosquash`) | rewrites every replayed commit; the originals become unreachable |
| `git commit --amend` | replaces the tip commit; the original is unreachable immediately |
| `git reset --hard`, `git reset` onto an earlier commit | discards commits and, with `--hard`, uncommitted work too |
| `git push --force`, `git push --force-with-lease`, `git push -f` | destroys the remote's copy — the one backup that survives local loss |
| `git branch -D` / `git branch -d` on a branch holding unmerged commits | strands those commits with no ref; `git gc` then deletes them |
| `git checkout`/`git switch` away while another agent's commits sit only on this branch | the commits leave with the branch and the working tree silently reverts |
| `git filter-branch`, `git filter-repo`, history-rewriting scripts | rewrites the entire history graph |
| `git gc --prune=now`, `git reflog expire` | destroys the recovery path for anything already stranded |
| `git merge --squash`, `gh pr merge --squash`, `--rebase`, the **Squash and merge** / **Rebase and merge** buttons | collapses or replays the branch's commits into new ones; every original commit becomes unreachable and the branch's real history is destroyed at the moment of merge |

**Merging is part of this rule, and it is the easiest place to get it wrong.** A squash merge feels like an integration step rather than a rewrite, which is exactly why it slips past: the branch is deleted straight afterwards, so the commits it collapsed have no ref left and `gc` eventually takes them. **Always merge with a true merge commit** — `git merge --no-ff`, or `gh pr merge --merge`. Never `--squash`, never `--rebase`, and never the equivalent buttons in the GitHub UI.

If a repository's settings only allow squash or rebase merges, that is a **setting to raise with the owner, not a licence to squash.** Stop and ask.

### What to do instead

- **Made a mistake in the last commit?** Add a new commit that corrects it.
- **Need to undo a commit?** `git revert <sha>` — it records the undo as a new commit and loses nothing.
- **Messy commits before a PR?** Leave them; the project does not require a linear history.
- **Landing a PR?** `gh pr merge --merge` (a real merge commit). **Not `--squash`, not `--rebase`.**
- **Need someone else's changes?** `git merge`, never `git rebase`.
- **Need to move a commit to another branch?** `git cherry-pick` — it copies, leaving the original reachable.
- **Working alongside another agent or session?** Use `git worktree add` so each has its own checkout and branch. Never two sessions committing in one working tree.

### Before you finish

Verify your own commits are still reachable on the branch **and** that their content is still in the working tree: `git log --oneline -5` plus a `grep` for something the commit introduced. If a commit has gone missing, recover it from `git reflog`, `git tag` it immediately, then `git cherry-pick` it back.
