---
applyTo: "**/*"
---

# How to Write Git Commits

Commits are the permanent record of how the codebase evolved. Each commit should tell a clear story: *what* changed and *why*. A reviewer reading `git log --oneline` should understand the arc of the work without opening any diffs.

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

```
<imperative summary of what this commit does>

<optional body — why the change was made, context, trade-offs>
```

- **Subject line**: imperative mood, present tense. Start with a verb: `Add`, `Fix`, `Remove`, `Rename`, `Extract`, `Update`, `Support`.
- **No period** at the end of the subject line.
- **72-character limit** on the subject line. If you cannot describe the change in 72 characters, the commit is probably too large — split it.
- **Body**: separated from the subject by a blank line. Explain *why*, not *what* (the diff shows the what). Use bullet points for multi-part changes.

### Good examples

```
Fix duplicate key crash in IdentityStorage.Populate

The upsert used InsertOne which threw on existing identities.
Replace with ReplaceOne using upsert: true.
```

```
Add type-safe event migration API with expression-based builders

Introduce EventTypeMigration<TUpgrade, TPrevious> base class with
typed property builders for Split, Join, Rename, and DefaultValue
operations. Migrators are discovered automatically by convention.
```

```
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
