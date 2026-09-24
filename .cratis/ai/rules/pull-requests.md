---
applyTo: "**/*"
---
<!-- cratis-ai-managed: rules/pull-requests.md -->

# How to Do Pull Requests

PR descriptions serve two purposes: they help reviewers understand the change *now*, and they become the release notes that users read *later*. Write them with both audiences in mind.

**The description is the release note — it is published verbatim.** Write it as the note you want the person upgrading to read, in the repository template's sections. A generic development write-up (`## Summary`, `## Verification`, `## Testing`, a list of the files you touched, a description of how you arrived at the change) is not a release note, and shipping one makes the release history unreadable. The same applies wherever a release is produced by hand: release-notes text typed into a manual workflow run, or written straight into a published release, carries exactly the same shape and the same audience as a PR description. There is no path to a release whose notes are allowed to describe the work instead of the change.

## Description

- Follow the repository's pull request template (`.github/pull_request_template.md`).
- Focus on the **Added**, **Changed**, **Fixed**, **Removed**, **Security**, and **Deprecated** sections. Remove sections that are empty — don't leave blank headings.
- Each bullet should be short, self-contained, and release-note ready.
- **Write for users of the framework, not for internal developers.** Only include changes that have an impact on anyone using what we build — new APIs, changed behavior, fixed bugs, removed features. Do not list internal implementation details like storage changes, converter updates, gRPC contract internals, or spec additions. If a change is purely internal plumbing, it does not belong in the PR description. For a user-visible fix, a brief root cause and what now prevents a regression, stated as observable behavior rather than a list of specs, are user-facing and may be included: they tell the upgrader whether to trust the fix. Credit an external contributor by name or handle, unless they asked not to be named.
- Add the associated issue reference at the end of a bullet when there is a real GitHub issue for the change (e.g. `(#351)`). Keep it a bare reference — **no closing keywords** (`Closes #351`, `Fixes #351`) anywhere in the body, because the published release notes are the PR description verbatim. If there is no associated issue, omit the reference entirely. Never use a placeholder like `(#issue)` or leave the example number `(#123)` literally, and never invent a random issue number. **Always verify the issue number read-only using the repository source — never guess or invent a number.** Comment on or close an issue when the user's request includes that effect; otherwise prepare a bounded post-merge disposition without performing it.
- Include a summary only if there is a cohesive theme across the changes. If you find yourself restating individual bullets in slightly different words, the summary adds no value — remove it.
- Never include Copilot prompt content in the PR description. Remove any "Original prompt" / coding agent transcript blocks before publishing.

## Commits

See the full [Git Commits guide](./git-commits.md) for rules on logical grouping, message format, and staging discipline.

Quick reminders:

- Imperative mood: "Add author registration" not "Added author registration".
- Each commit = one logical unit of work. No WIP commits in the final PR.
- Never mix unrelated changes in a single commit.

## Labels

Confirm the current repository workflow contract before selecting release intent because release-intent labels can trigger publication. A direct request to ship with a named label authorizes applying that label and completing the repository's standard pull-request workflow, including the release it normally triggers. Do not ask for separate authorization at each step. If the user did not request shipping or publication, a proposed semantic label describes impact but does not grant authority.

- Label the PR according to semantic versioning impact:
  - **major** — breaking changes to public APIs
  - **minor** — new features, new slices, non-breaking additions
  - **patch** — bug fixes, refactoring with identical behavior

### A major release needs a human's explicit go-ahead

A `major` label is the one release-intent label a ship request does not, by
itself, authorize through to merge. Breaking a public API is the most
consequential and hardest-to-reverse thing a release does — every downstream
consumer eventually has to act on it — so it gets a checkpoint the other two
intents do not.

- Prepare the branch, commits, push, and PR carrying the `major` label exactly
  as any other ship request would.
- Before merging, stop and ask a human to confirm the major bump specifically:
  name the exact breaking change(s), who is affected and how, and the
  resulting version number. A generic "ready to ship?" is not this checkpoint —
  say plainly that this release breaks compatibility and needs a yes.
- Proceed to merge only on an explicit, affirmative answer to that question.
  A prior general instruction to "ship" or "land this" does not answer it,
  even when it named `major` as the intended label.
- This checkpoint is per release, not per conversation — a human confirming
  one major release does not pre-authorize the next one.

This narrows the general ship-changes authorization in
[`ship-changes.prompt.md`](../prompts/ship-changes.prompt.md) for exactly this
label; every other step of that workflow proceeds under its existing
authority.

### A pull request that changes nothing outward-facing carries `no-release`

**If nothing in the PR can change what a consumer compiles against, runs, or observes, propose repository-supported non-release intent, ordinarily `no-release`** — not `patch`. Confirm that the current workflow supports the label, requires exactly one release-intent label, and suppresses publication as intended before applying it with authorization.

Where the repository requires release intent, `no-release` is a decision, not an omission. Missing required labels are blockers, not a reason to bypass the check.

This covers, whenever the PR touches *only* these:

- **Documentation** — anything under `Documentation/**`, READMEs, the `.cratis/ai/` corpus.
- **CI and repository automation** — `.github/workflows/**`, `.github/scripts/**`, `.github/CODEOWNERS`, issue/PR templates.
- **Tests and specs** — `*.Specs/**`, `when_*/**`, `for_*/**`, `Integration/**`, and test-only fixtures.
- **Build and tooling configuration** that produces no shipped artifact difference — lint config, editor config, local scripts.

The test is **outward-facing effect, not file location**. A change under `Source/**` that only touches specs is not shippable; a one-line change to a published package's behavior is, however small. If a consumer could not tell the difference by upgrading, there is nothing to version. When genuinely unsure, ask rather than defaulting to `patch` — an unnecessary release is not free: it burns a version number, ships release notes describing nothing, and buries the releases that matter.

A non-release pull request must satisfy the relevant required checks like any other. Confirm label acceptance and publication suppression against the current workflow contract; do not assume external CI or API state.

### Group small related changes into one pull request

Do not open a pull request per task when the tasks belong to the same body of work. Several small merged PRs become several releases, and a stream of near-empty patch releases makes the release history useless for the people it is written for. Collect related work — a set of CI gates, a group of fixes in one area, the steps of one refactor — onto **one branch, as separate commits**, and open **one** pull request. Commits stay one-logical-unit-each; the pull request is the release boundary, and the release boundary should be a coherent, describable change.

**Before consolidating open PRs, review each PR’s release intent and workflow effects.** Integration may trigger completion/publication behavior on an absorbed PR. Use supported non-release intent where appropriate. A direct request to consolidate the named pull requests authorizes the necessary relabeling and merge; it does not authorize unrelated notifications.

Split into separate pull requests when the changes are genuinely unrelated, when one is urgent and the others are not, or when one is risky enough to want its own revert.

## Quality Gates

Documentation-only changes use repository-supported non-release intent, ordinarily `no-release`; confirm the workflow contract rather than assuming a label or API state. Run relevant content, link, frontmatter, and corpus checks instead of unrelated application builds, and satisfy every repository-required check, including release-intent checks where supported. Documentation is never a blanket exemption from red CI.

**`no-release` does not otherwise excuse a PR from this section.** A CI, tooling, or spec-only pull request ships nothing, but it is exactly the kind of change that can break the build or the pipeline for everyone else — a broken workflow or a deleted spec does its damage without ever being released. Hold it to every gate below.

Before marking code/automation work ready, select the affected-project gates that apply from the following list; run wider checks for cross-cutting changes and repository-required merge/release gates:

- `dotnet build` — zero errors, zero warnings
- `dotnet test` — all specs pass
- `yarn lint` — zero errors
- `npx tsc -b` — zero TypeScript errors
- Code follows all project coding standards and conventions
- **Required CI checks pass.** After an authorized push, inspect checks and failure logs. Diagnose within a bounded attempt, fix in-scope causes, and re-run relevant gates after each fix. Report unrelated/environmental failures and missing authority as blockers rather than retrying indefinitely or silently waiving required checks.
