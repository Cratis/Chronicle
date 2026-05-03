---
on:
  preToolUse:
    tool: runInTerminal
---

# Pre-commit — Run Specs

Before executing any `git commit` terminal command, automatically run the specs for every affected project to ensure nothing is broken before changes are recorded in version control.

## When this hook applies

This hook fires before **every** `runInTerminal` call. Check whether the command being run is a `git commit` (including `git commit -m`, `git commit --amend`, etc.). If it is not a commit command, do nothing and let the tool proceed.

## Steps

1. **Detect a git commit command** — inspect the terminal command string. If it does not start with `git commit`, skip all steps below and proceed normally.

2. **Identify affected projects** from the staged changes:
   ```
   git diff --name-only --cached
   ```
   Collect unique project roots using the same rules as the `agentStop` hook:
   - `.cs` files → walk up to the nearest `.csproj`.
   - `.ts` / `.tsx` files → walk up to the nearest `package.json` with a `"test"` script.

3. **Run specs for each affected .NET project**:
   ```
   dotnet test <specs-project-path> --no-build
   ```
   If the specs project cannot be identified, run `dotnet test` from the repository root.

4. **Run specs for each affected TypeScript project**:
   ```
   yarn test
   ```
   Run from the package root that owns the changed files.

5. **If any spec fails**:
   - Report the full test output including which specs failed and why.
   - **Do not proceed with the `git commit`** — block the tool call and fix the failures first.
   - Re-run the relevant specs to confirm they pass before retrying the commit.

6. **If all specs pass** — proceed with the `git commit` as originally requested.

## Rules

- Never skip the spec run before a commit, even for "minor" or "documentation-only" changes.
- A commit must not be made while any spec is failing.
- If a spec was already failing before the current changes (pre-existing failure), report it but do not block the commit — note the pre-existing failure clearly in the session output.
