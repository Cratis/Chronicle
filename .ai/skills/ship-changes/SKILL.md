---
name: ship-changes
description: >
  Ship staged or unstaged local changes: create a branch, make logical commits,
  push to origin, open a PR with the correct description and label, merge it,
  and delete the branch locally and on origin. Use whenever the user asks to
  commit, push, create a PR, ship, or land changes.
---

# Ship Changes

This skill takes local modifications from the working tree and lands them on
`main` via a properly structured branch, commits, and PR — following all
project conventions for commits, PR descriptions, and labels.

## Inputs

Collect the following before starting:

- **Semantic label** — `patch`, `minor`, `major`, or none. Skip labeling entirely if the user says no label or omits the label. Do not ask — infer from the nature of the change or follow the user's explicit instruction.
- **Branch name suffix** — short kebab-case description of the work, e.g. `fix/testing-orleans-runtime-assemblies`. Determine from the nature of the changes if not provided.
- **Related GitHub issue** — search GitHub issues if the change likely relates to one; use the real number or omit the reference when none exists. Never invent or reuse example numbers.

## Step 1 — Review the working tree

```
git status
git diff
```

Read the full diff. Understand every changed file before touching git.
Do **not** start staging until you know exactly how the commits will be split.

## Step 2 — Create the branch

Branch off of current `main`. Always use a prefix:

| Prefix | When to use |
|--------|-------------|
| `fix/` | bug fixes, runtime errors, incorrect behavior |
| `feat/` | new features, new slices, new capabilities |
| `chore/` | build infra, tooling, docs, refactoring |

```
git checkout -b <prefix>/<short-description>
```

## Step 3 — Make logical commits

### Commit splitting rules

Split commits so that each one is a single logical unit of work:

1. **Infrastructure / plumbing first** — new types, interfaces, MSBuild targets, shared build props — anything that later commits build on.
2. **Core behavior second** — the actual fix or feature that uses the infrastructure.
3. **Specs / tests third** — only when specs are clearly separate from the behavior change (e.g. new integration spec added after the source fix). Combine with behavior commit when tightly coupled.
4. **Integration or wiring last** — DI registration, routing, UI hookup.

Never mix unrelated changes in a single commit.

### Staging discipline

Stage files explicitly — never `git add .` or `git add -A`:

```
git add <file1> <file2>
git diff --cached          # verify staged content before committing
git commit -m "<message>"
```

### Commit message format

```
<imperative summary — 72-char max, no trailing period>

<optional body: WHY the change was made, context, trade-offs>
<use bullets for multi-part changes>
```

- Subject starts with a verb: `Add`, `Fix`, `Remove`, `Rename`, `Extract`, `Update`, `Support`.
- Body separated from subject by a blank line.
- Body explains *why*, not *what* — the diff shows the what.

**Good examples:**

```
Add _PackPrivateAssemblyGlobs target for runtime-only NuGet package embedding

Extend the shared client build infrastructure with a new MSBuild target.
The new PrivatePackageAssemblyGlob item type globs $(OutputPath) at pack
time and embeds matching DLLs into lib/{tfm}/ without a nuspec dependency.
```

```
Fix duplicate key crash in IdentityStorage.Populate

The upsert used InsertOne which threw on existing identities.
Replace with ReplaceOne using upsert: true.
```

**Bad examples** (never do these):

- `Fix stuff`
- `WIP`
- `Added files`
- `Fix bug and add feature and update docs`

## Step 4 — Push the branch

```
git push -u origin <branch-name>
```

## Step 5 — Create the PR

Use `mcp_github_github_create_pull_request` with:

- `owner` / `repo`: **the current repository** — derive it from the `origin` remote (`git remote get-url origin`); never hardcode a specific repo
- `head`: the branch name
- `base`: `main`
- `title`: short imperative sentence describing the overall change
- `body`: PR description (see below)

### PR description format

Follow `.github/pull_request_template.md`. Include only non-empty sections.

```markdown
## Added
- <release-note bullet> (#<actual-issue-number>)

## Changed
- <release-note bullet> (#<actual-issue-number>)

## Fixed
- <release-note bullet> (#<actual-issue-number>)
```

Rules:
- Bullets are short, release-note ready, written for a user reading the changelog.
- End every bullet with `(#<N>)` using the **real** GitHub issue number. Search issues first. If there is no issue, omit the reference entirely — never write `(#issue)` or reuse an example number.
- Remove any empty sections — no blank headings.
- Include a `# Summary` paragraph only when the bullets alone don't tell the full story.
- Never include any Copilot prompt transcript or "Original prompt" block.

### Searching for a related issue

```
mcp_github_github_search_issues  query="<keywords> repo:<owner>/<repo>"
```

(Use the current repository's `<owner>/<repo>`, derived from the `origin` remote.)

If nothing relevant is found, omit the issue reference from affected bullets.

## Step 6 — Add the label (optional)

Skip this step entirely if the user said no label or did not provide one.

Otherwise use `gh pr edit <number> --add-label "<label>"` with one of:

| Label | When |
|-------|------|
| `patch` | bug fixes, docs, refactoring with identical behavior |
| `minor` | new features, new slices, non-breaking additions |
| `major` | breaking changes to public APIs |

## Step 7 — Wait for CI to pass

Before merging, poll the PR checks with `pull_request_read`:

- Use `method: get_check_runs` (and `get_job_logs` on any failure).
- Merge **only** when all required checks are green. If a check fails, investigate, fix, and push again.
- Do not merge while red unless the user explicitly accepts confirmed pre-existing flakes unrelated to the change.

## Step 8 — Merge the PR

Use `mcp_github_github_merge_pull_request` with:

- `merge_method`: `merge`
- `owner` / `repo`: the current repository (same as step 5)
- `pullNumber`: the PR number returned in step 5

## Step 9 — Clean up the branch

```
git checkout main && git pull && git branch -d <branch-name> && git push origin --delete <branch-name>
```

Run all four commands in one shell invocation to avoid partial state.
After this completes, `main` is fully up to date locally.

## Full example sequence

An illustrative end-to-end run for a new slice (paths and numbers are placeholders — use the current repo's):

```
# 1. Review
git status
git diff

# 2. Branch
git checkout -b feat/authors-registration

# 3a. First commit — backend slice
git add Authors/Registration/Registration.cs
git diff --cached
git commit -m "Add author registration slice

Register command + AuthorRegistered event + uniqueness constraint."

# 3b. Second commit — specs
git add Authors/Registration/when_registering_an_author/
git commit -m "Add specs for author registration"

# 4. Push
git push -u origin feat/authors-registration

# 5. PR (via mcp_github_github_create_pull_request) — owner/repo from the origin remote

# 6. Label (optional)
gh pr edit <pr-number> --add-label "minor"

# 7. Wait for CI — pull_request_read get_check_runs until green

# 8. Merge (via mcp_github_github_merge_pull_request)

# 9. Clean up (pulls main as part of the same command)
git checkout main && git pull && git branch -d feat/authors-registration && git push origin --delete feat/authors-registration
```

## Common mistakes to avoid

- **Never `git add .`** — always stage specific files and verify with `git diff --cached`.
- **Never invent issue numbers** — search first; omit the reference if nothing matches.
- **Never leave placeholder text** in PR bodies (`(#issue)`, `(#123)`).
- **Never commit code that does not compile** — every commit must be a working state.
- **Never push directly to `main`** — always go through the branch + PR flow.
- **Never skip branch cleanup** — delete both the local and remote branch after merging.
