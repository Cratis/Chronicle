<!-- cratis-ai-managed: skills/cratis-commits-and-pull-requests/references/pr-labels-and-release.md -->
<!-- Copyright (c) Cratis. All rights reserved. -->
<!-- Licensed under the MIT license. See LICENSE file in the project root for full license information. -->

# Labels, release intent, grouping and quality gates

The contract (exactly one release-intent label, the major go-ahead, `no-release` for non-outward changes, required gates) stays always-on in [pull-requests.md](../../../rules/pull-requests.md). This reference holds the procedure and detail.

## Commits in a PR

See [commits.md](./commits.md) for logical grouping, message format and staging discipline.

- Imperative mood: "Add author registration", not "Added author registration".
- Each commit is one logical unit of work. No WIP commits in the final PR.
- Never mix unrelated changes in a single commit.

## Labels

Confirm the current repository workflow contract before selecting release intent because release-intent labels can trigger publication. A direct request to ship with a named label authorizes applying that label and completing the repository's standard pull-request workflow, including the release it normally triggers. Do not ask for separate authorization at each step. If the user did not request shipping or publication, a proposed semantic label describes impact but does not grant authority.

- Exactly one of `major`/`minor`/`patch`/`no-release`, set in the creating command (`gh pr create --label patch --assignee @me`), in every repository, including samples, docs and tooling repositories (`no-release` when nothing is shipped). To change intent, swap labels in one command: `gh pr edit <n> --remove-label minor --add-label patch`. Never add a second: with more than one of them, release-action releases nothing and reports `reason=error`. Re-running a `pull_request` workflow replays the original label payload, so fix a label mistake by editing the PR or pushing; a `workflow_dispatch` run needs an explicit `version` and the PR description as `release-notes`, and without notes it closes no issues.
- Dependabot PRs carry only `no-release`. release-action never releases them, and the `major`/`minor`/`patch` Dependabot adds describe the dependency's version, not this product's.
- Label the PR according to semantic versioning impact:
  - **major**: breaking changes to public APIs
  - **minor**: new features, new slices, non-breaking additions
  - **patch**: bug fixes, refactoring with identical behavior

### A major release needs a human's explicit go-ahead

A `major` label is the one release-intent label a ship request does not, by itself, authorize through to merge. Breaking a public API is the most consequential and hardest-to-reverse thing a release does (every downstream consumer eventually has to act on it), so it gets a checkpoint the other two intents do not.

- Prepare the branch, commits, push, and PR carrying the `major` label exactly as any other ship request would.
- Before merging, stop and ask a human to confirm the major bump specifically: name the exact breaking change(s), who is affected and how, and the resulting version number. A generic "ready to ship?" is not this checkpoint; say plainly that this release breaks compatibility and needs a yes.
- A human merges a `major` PR, after answering that question with an explicit yes. An agent never merges it, even after the confirmation. A prior general instruction to "ship" or "land this" does not answer it, even when it named `major` as the intended label.
- This checkpoint is per release, not per conversation: a human confirming one major release does not pre-authorize the next one.

This narrows the general ship-changes authorization in [`ship-changes.prompt.md`](../../../prompts/ship-changes.prompt.md) for exactly this label; every other step of that workflow proceeds under its existing authority.

### A pull request that changes nothing outward-facing carries `no-release`

**If nothing in the PR can change what a consumer compiles against, runs, or observes, propose repository-supported non-release intent, ordinarily `no-release`**, not `patch`. Confirm that the current workflow supports the label, requires exactly one release-intent label, and suppresses publication as intended before applying it with authorization.

Where the repository requires release intent, `no-release` is a decision, not an omission. Missing required labels are blockers, not a reason to bypass the check.

This covers, whenever the PR touches *only* these:

- **Documentation**: anything under `Documentation/**`, READMEs, the `.cratis/ai/` copy installed in a consumer repository.
- **CI and repository automation**: `.github/workflows/**`, `.github/scripts/**`, `.github/CODEOWNERS`, issue/PR templates.
- **Tests and specs**: `*.Specs/**`, `when_*/**`, `for_*/**`, `Integration/**`, and test-only fixtures.
- **Build and tooling configuration** that produces no shipped artifact difference: lint config, editor config, local scripts.

**In Cratis/AI the corpus is the product.** Its skills, rules, prompts and hooks reach users through `@cratis/pi`, so a corpus change there is labelled by its effect on those users, like code:

- `patch`: a fix to existing guidance, for example correcting a wrong command or a broken link in a skill.
- `minor`: new or changed guidance, for example a new skill or a new required step in an existing one.
- `no-release`: only when users cannot see the change, for example specs, evaluations or CI.

The test is **outward-facing effect, not file location**. A change under `Source/**` that only touches specs is not shippable; a one-line change to a published package's behavior is, however small. If a consumer could not tell the difference by upgrading, there is nothing to version. When genuinely unsure, ask rather than defaulting to `patch`: an unnecessary release is not free. It burns a version number, ships release notes describing nothing, and buries the releases that matter.

A non-release pull request must satisfy the relevant required checks like any other. Confirm label acceptance and publication suppression against the current workflow contract; do not assume external CI or API state.

## Release-action windows and workflow drift

Batching is opt-in; success with `batched` or `nothing-pending` is not publication. Read **cratis-release-notes** before configuring windows, handling a pending-major hotfix, or recovering a workflow-drift 403. Major confirmation remains required; a workflow-drift recovery differs from the concurrent-tag retry in the quality gates below.

## Group small related changes into one pull request

Do not open a pull request per task when the tasks belong to the same body of work. Several small merged PRs become several releases, and a stream of near-empty patch releases makes the release history useless for the people it is written for. Collect related work (a set of CI gates, a group of fixes in one area, the steps of one refactor) onto **one branch, as separate commits**, and open **one** pull request. Commits stay one-logical-unit-each; the pull request is the release boundary, and the release boundary should be a coherent, describable change. Joining an open candidate, folding superseded PRs and the cut are in `cratis-engineering-release-trains`.

**Before consolidating open PRs, review each PR's release intent and workflow effects.** Integration may trigger completion/publication behavior on an absorbed PR. Use supported non-release intent where appropriate. A direct request to consolidate the named pull requests authorizes the necessary relabeling and merge; it does not authorize unrelated notifications.

Before a consolidation or a re-land of reverted work merges, prove it dropped nothing with `cratis-engineering-consolidation-guard`.

Split into a separate pull request only for the exceptions in `cratis-engineering-release-trains`: a named major awaiting human confirmation, an urgent security fix authorized in writing, or an accepted outsider issue. Unrelated or riskier work rides a later train instead.

## Quality gates

Documentation-only changes use repository-supported non-release intent, ordinarily `no-release`; confirm the workflow contract rather than assuming a label or API state. Run relevant content, link, frontmatter, and corpus checks instead of unrelated application builds, and satisfy every repository-required check, including release-intent checks where supported. Documentation is never a blanket exemption from red CI.

**`no-release` does not otherwise excuse a PR from these gates.** A CI, tooling, or spec-only pull request ships nothing, but it is exactly the kind of change that can break the build or the pipeline for everyone else: a broken workflow or a deleted spec does its damage without ever being released. Hold it to every gate below.

Before marking code/automation work ready, select the affected-project gates that apply from the following list; run wider checks for cross-cutting changes and repository-required merge/release gates:

- `dotnet build`: zero errors, zero warnings
- Built C# spec executables: all specs pass; `dotnet test <project> --no-build` only for VSTest (see specs.csharp)
- `yarn lint`: zero errors
- `npx tsc -b`: zero TypeScript errors
- Code follows all project coding standards and conventions
- **Required CI checks pass.** After an authorized push, inspect checks and failure logs. Diagnose within a bounded attempt, fix in-scope causes, and re-run relevant gates after each fix. Report unrelated/environmental failures and missing authority as blockers rather than retrying indefinitely or silently waiving required checks. Waiting, cancelling and tiered local gates are in `cratis-engineering-ci-gates`. If a release job fails after a concurrent merge (the release for the tag exists for another commit), re-run it: it releases under the next version (release-action fails the job rather than reporting success).
