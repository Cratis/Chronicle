---
applyTo: "**/*"
---
<!-- cratis-ai-managed: harnesses/copilot/instructions/pull-requests.instructions.md -->
<!-- cratis-ai: generated Copilot instructions. Do not edit; rules/ is the source. -->

# How to Do Pull Requests

**Before writing any commit message, committing, pushing, or creating/editing a PR, read the `cratis-commits-and-pull-requests` skill.** It holds the allowed description shape, the forbidden list, worked examples, label procedure, grouping and release-action windows. This rule keeps the contract that must hold even before the skill is read. Commit rules are in [git-commits.md](./git-commits.instructions.md).

## The description is the release note

**The PR description is published verbatim as the GitHub release. In repositories released by `cratis/release-action`, it also closes every issue written as `(#n)`; elsewhere, follow the repository's own release and issue-closing process.** Write the note the person upgrading should read, not a development write-up.

- Follow the repository's pull request template within this contract: an optional summary first, then only `## Added`, `## Changed`, `## Fixed`, `## Removed`, `## Security`, `## Deprecated`, each with short, user-facing bullets and none empty.
- Never put review, verification, test or CI results, provenance, merge or deploy instructions, internal state, relative links or placeholders in the description. **Reviewer-facing information goes in a PR comment** posted right after creating the PR (`gh pr comment <n> --body-file .ai-work/pr-notes.md`), even when the PR is labelled `no-release`.
- A change to a hook script or its wiring (`.cratis/ai/hooks/**`) needs a release-note line that tells consumers to run `cratis ai update`; a hook fix does not reach a repository until they do.

### Issue references

- `(#n)` at the end of the bullet that delivers it closes the issue at release: use it **only** for an issue this PR fully delivers. One `(#n)` per issue: `(#351, #352)` closes nothing, write `(#351) (#352)`.
- `(part of #n)`, prose `see #n` or `Cratis/Repo#n` relate without closing. No issue means no reference; never use a placeholder or guess a number.
- Never use a linking or closing keyword before a number, anywhere (`Close`, `Closes`, `Fix`, `Fixes`, `Resolve`, `Resolves`, `Refs`, `References`, in any form). `Refs #n` closes nothing: write `(#n)` or `(part of #n)`.
- Comment on or close an issue only when the user's request includes that effect. A delivered `(#n)` in a repository released by `cratis/release-action` is closed at release; do not close it by hand.

## Labels

- **Exactly one** of `major`/`minor`/`patch`/`no-release`, set in the creating command (`gh pr create --label patch`), in every repository. Never add a second: release-action then releases nothing. Swap in one command with `gh pr edit --remove-label … --add-label …`. Dependabot PRs carry only `no-release`.
- Confirm the repository workflow before choosing, because the label can trigger publication. A direct request to ship with a named label authorizes applying it and the standard workflow; without a ship request a proposed label describes impact but grants no authority.
- **A major release needs a human's explicit go-ahead, and a human merges it.** Prepare the branch and PR with `major`, then stop and ask a human to confirm the major bump: name the breaking changes, who is affected and the resulting version. A general "ship" instruction does not answer it. An agent never merges a `major` PR, even after confirmation; the checkpoint is per release.
- **If nothing in the PR can change what a consumer compiles against, runs or observes, use `no-release`**, not `patch`: documentation, CI and automation, tests and specs, tooling configuration. The test is outward-facing effect, not file location. When unsure, ask rather than defaulting to `patch`.

## Before you create or edit a PR

- Run `node .cratis/ai/hooks/scripts/cratis-check-pr.mjs --body-file <file> --label <intent>`; it applies the exact `policy / verify` release-note rules and the Bash hook runs it on `gh pr create` and `gh pr edit`. If it fails, fix the description, not the code. Before every body edit, run `git diff --stat origin/main...HEAD` and delete bullets the diff no longer supports.
- Run the affected-project gates before marking work ready (build, built specs, lint and type-check; zero warnings), and the repository's required CI checks after an authorized push. `no-release` and documentation-only PRs still satisfy every required check; none is a blanket exemption from red CI.
- The release train is the PR: fold finished branches into the repository's one train (see the skill). Open it once, when verified, as the orchestrator (`cratis-engineering-release-trains`) with `--assignee @me`. Split only for a named major, an urgent security fix authorized in writing, or an accepted outsider issue (patch or minor, own release).
