---
lifecycle: pre-commit
---

# Pre-commit — Run Specs

> **This is lifecycle guidance, not a wired tool hook.** To *enforce* it, wire it per tool — Claude Code: a `PreToolUse` hook in `.claude/settings.json` with a matcher on `Bash` (or your terminal tool) gating `git commit` (and its rtk-rewritten `rtk git commit` form — see [rtk](../rules/rtk.md)); GitHub Copilot: a hook in a `.github/hooks/*.json` file. The steps below are what that hook (or the agent) should do.

Before executing any `git commit` terminal command, automatically run the specs for every affected project to ensure nothing is broken before changes are recorded in version control.

## When this hook applies

This guidance applies before any `git commit` (including `git commit -m`, `git commit --amend`, etc.) — and equally to the rtk-rewritten `rtk git commit ...`, since the rtk hook prefixes terminal commands. If the command being run is not a commit, do nothing and proceed.

## Steps

1. **Detect a git commit command** — inspect the terminal command string for `git commit ...` (treat the rtk-rewritten `rtk git commit ...` as the same thing, since the rtk hook prefixes commands). If it does not match, skip all steps below and proceed normally.

2. **Identify affected projects** from the staged changes:
   ```
   git diff --name-only --cached
   ```
   Collect unique project roots using the same rules as the agent-stop guidance:
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
