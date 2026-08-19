---
applyTo: "**/*"
---

# How to Write Git Commits

Commits are the permanent record of how the codebase evolved. Each commit should tell a clear story: *what* changed and *why*. A reviewer reading `git log --oneline` should understand the arc of the work without opening any diffs.

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

**Merging is part of this rule, and it is the easiest place to get it wrong.** A squash merge feels
like an integration step rather than a rewrite, which is exactly why it slips past: the branch is
deleted straight afterwards, so the commits it collapsed have no ref left and `gc` eventually takes
them. **Always merge with a true merge commit** — `git merge --no-ff`, or `gh pr merge --merge`.
Never `--squash`, never `--rebase`, and never the equivalent buttons in the GitHub UI.

If a repository's settings only allow squash or rebase merges, that is a **setting to raise with the
owner, not a licence to squash.** Stop and ask.

**This is not stylistic.** A commit can exist as an object (`git cat-file -t <sha>` succeeds) while being absent from every branch *and* from the working tree — reachable only through `git reflog`, and only until `gc` runs. Work has already been lost in this repository exactly this way: a branch checkout carried another session's commit away, the branch was deleted, and the file changes silently reverted in the tree. It was recovered from the reflog by luck, because someone asked the right question in time.

### What to do instead

- **Made a mistake in the last commit?** Add a new commit that corrects it. The wrong state stays in history, and that is fine — history is a record of what happened, not a curated story of what you wish had happened.
- **Need to undo a commit?** `git revert <sha>` — it records the undo as a new commit and loses nothing.
- **Messy commits before a PR?** Leave them. A reviewer reading a coherent series of small commits is better served than by one squashed blob, and the project does not require a linear history.
- **Landing a PR?** `gh pr merge --merge` (a real merge commit). **Not `--squash`, not `--rebase`.** The commits on the branch are the record of how the change was actually built; squashing throws that away permanently in exchange for a tidier `main`, which is not a trade this project makes.
- **Need someone else's changes?** `git merge`, never `git rebase`.
- **Need to move a commit to another branch?** `git cherry-pick` — it copies, leaving the original reachable.
- **Working alongside another agent or session?** Use `git worktree add` so each has its own checkout and branch. Never two sessions committing in one working tree.

### Before you finish

Verify your own commits are still reachable on the branch **and** that their content is still in the working tree — those are two different things. `git log --oneline -5` plus a `grep` for something the commit introduced. If a commit has gone missing, `git reflog` is the recovery tool: find the SHA, `git tag` it immediately so `gc` cannot take it, then `git cherry-pick` it back.

## Logical Grouping

Every commit must be a **single logical unit of work**. Group related changes together; separate unrelated changes into distinct commits.

### What belongs in one commit

- A bug fix and the spec that proves it.
- A new file and the changes to existing files needed to integrate it (imports, registrations, wiring).
- A refactor that moves or renames code — only the mechanical transformation, nothing else.
- An interface change together with all implementation updates required to compile.

### What does NOT belong in one commit

- A bug fix mixed with an unrelated feature.
- Source code changes mixed with unrelated spec additions for a different area.
- Formatting or style cleanups bundled with behavioral changes.
- Multiple independent fixes or features squashed into a single commit.

### Deciding where to split

Ask: "If I needed to revert this commit, would I lose exactly one coherent change?" If reverting would undo two unrelated things, it should be two commits.

Common split points:

1. **Infrastructure / plumbing first** — interface additions, new types, or schema changes that later commits build on.
2. **Core logic second** — the behavioral change that uses the new infrastructure.
3. **Specs / tests third** — the specs that prove the behavioral change works. Specs may also be combined with the core logic commit when they are tightly coupled (e.g., a TDD red-green cycle or a bug fix with its regression test).
4. **Integration or wiring last** — connecting the new behavior to the rest of the system (DI registration, routing, UI hookup).

When a task produces both source fixes and new integration specs, prefer separate commits for the source changes and the specs — unless the specs are inseparable from the fix (e.g., a single bug fix + its regression test).

## Commit Messages

### Format

```text
<imperative summary of what this commit does>

<optional body — why the change was made, context, trade-offs>
```

- **Subject line**: imperative mood, present tense. Start with a verb: `Add`, `Fix`, `Remove`, `Rename`, `Extract`, `Update`, `Support`.
- **No period** at the end of the subject line.
- **72-character limit** on the subject line. If you cannot describe the change in 72 characters, the commit is probably too large — split it.
- **Body**: separated from the subject by a blank line. Explain *why*, not *what* (the diff shows the what). Use bullet points for multi-part changes.

### Good examples

```text
Fix duplicate key crash in IdentityStorage.Populate

The upsert used InsertOne which threw on existing identities.
Replace with ReplaceOne using upsert: true.
```

```text
Add type-safe event migration API with expression-based builders

Introduce EventTypeMigration<TUpgrade, TPrevious> base class with
typed property builders for Split, Join, Rename, and DefaultValue
operations. Migrators are discovered automatically by convention.
```

```text
Add integration specs for observer replay on redaction
```

### Bad examples

- `Fix stuff` — meaningless.
- `WIP` — never commit work-in-progress; stage and commit when the unit is complete.
- `Add files` — says nothing about what or why.
- `Fix bug and add new feature and update docs` — three unrelated things.
- `Changed Observer.Handling.cs` — describes a file, not a behavior.

## When to Commit

- **After each logical unit passes the build** — `dotnet build` with zero errors and zero warnings, or `yarn compile` with zero errors.
- **Before starting a different kind of work** — about to switch from fixing a bug to adding a feature? Commit the bug fix first.
- **After completing specs for a change** — if the specs are a separate commit from the source change.
- **Never commit code that does not compile.** Every commit must be a buildable, working state of the codebase.

## Staging Discipline

- Use `git add <specific files>` rather than `git add .` or `git add -A`. Only stage files that belong to the current logical unit.
- Review `git diff --cached` before committing to verify nothing unrelated was staged.
- If you realize mid-commit that unrelated changes are mixed in, unstage them and commit only the related subset.
