---
lifecycle: pre-commit
---

# Pre-commit — Run Specs

> **This is lifecycle guidance, not a wired tool hook.** To *enforce* it, wire it per tool — Claude Code: a `PreToolUse` hook in `.claude/settings.json` with a matcher on `Bash` (or your terminal tool) gating `git commit` (and its rtk-rewritten `rtk git commit` form — see [rtk](../rules/rtk.md)); GitHub Copilot: a hook in a `.github/hooks/*.json` file. The steps below are what that hook (or the agent) should do.

Before an explicitly authorized commit, verify the staged scope with proportional checks. Reuse fresh passing results only when they cover the exact unchanged staged inputs; otherwise run the relevant checks. Never stage unrelated edits.

## When this guidance applies

Apply before an authorized `git commit`, including `rtk git commit` or `rtk proxy git commit`. Do not interpret recognizing a command as authorization. History rewriting (`commit --amend`, rebase, squash, or force-push) remains prohibited.

## Steps

1. **Confirm authorization and scope** — this guidance does not authorize a commit or create executable hook wiring. Select documentation/corpus checks for rule-only edits; do not run application tests without affected application code.

2. **Identify affected projects** from the staged changes:
   ```
   git diff --name-only --cached
   ```
   Collect unique affected project roots:
   - `.cs` files → walk up to the nearest `.csproj`.
   - `.ts` / `.tsx` files → walk up to the nearest `package.json` with a `"test"` script.

3. **Run specs for each affected .NET project**:
   ```
   dotnet test <specs-project-path> --no-build
   ```
   Use `--no-build` only when matching build outputs are current; otherwise incrementally build the affected specs project first. If the owning specs project cannot be identified, inspect project references or report the uncertainty; do not default to a root-wide test run.

4. **Run specs for each affected TypeScript project**:
   ```
   yarn test
   ```
   Run from the package root that owns the changed files.

5. **If a relevant check fails** — diagnose within a bounded attempt, fix only in-scope causes, and re-run the failed gate. Report unrelated/environmental failures as blockers instead of repeated retries or broad edits. Do not claim completion or bypass required gates.

6. **When relevant required checks pass** — proceed only with the originally authorized commit and staged scope. Report the exact verification and any checks not run.

## Rules

- Documentation/rule-only commits run relevant content, link, frontmatter, and corpus checks, not application builds/tests.
- Code changes run affected-project incremental checks and targeted regression specs after coherent changes. Wider suites and clean/Release builds require cross-cutting scope or repository merge/release gates.
- Do not bypass required failures, suppress diagnostics, or expand into unrelated cleanup. Missing prerequisites and pre-existing failures must be reported honestly.
