---
applyTo: "**/*"
---

# How to Do Pull Requests

PR descriptions serve two purposes: they help reviewers understand the change *now*, and they become the release notes that users read *later*. Write them with both audiences in mind.

## Description

- Follow the repository's pull request template (`.github/pull_request_template.md`).
- Focus on the **Added**, **Changed**, **Fixed**, **Removed**, **Security**, and **Deprecated** sections. Remove sections that are empty — don't leave blank headings.
- Each bullet should be short, self-contained, and release-note ready.
- **Write for users of the framework, not for internal developers.** Only include changes that have an impact on anyone using what we build — new APIs, changed behavior, fixed bugs, removed features. Do not list internal implementation details like storage changes, converter updates, gRPC contract internals, or spec additions. If a change is purely internal plumbing, it does not belong in the PR description.
- Add the associated issue reference at the end of a bullet when there is a real GitHub issue for the change (e.g. `(#351)`). Keep it a bare reference — **no closing keywords** (`Closes #351`, `Fixes #351`) anywhere in the body, because the published release notes are the PR description verbatim. An issue the PR resolves is closed explicitly after the merge (`gh issue close <N> --comment "Fixed in #<pr>."`), and the close is verified. If there is no associated issue, omit the reference entirely. Never use a placeholder like `(#issue)` or leave the example number `(#123)` literally, and never invent a random issue number. **Always verify the issue number using the `search_issues` or `list_issues` GitHub MCP tool — never guess or invent a number.**
- Include a summary only if there is a cohesive theme across the changes. If you find yourself restating individual bullets in slightly different words, the summary adds no value — remove it.
- Never include Copilot prompt content in the PR description. Remove any "Original prompt" / coding agent transcript blocks before publishing.

## Commits

See the full [Git Commits guide](./git-commits.md) for rules on logical grouping, message format, and staging discipline.

Quick reminders:
- Imperative mood: "Add author registration" not "Added author registration".
- Each commit = one logical unit of work. No WIP commits in the final PR.
- Never mix unrelated changes in a single commit.

## Labels

**The label is what cuts the release.** A `major`/`minor`/`patch` label makes the merge publish packages, push images and create a GitHub release; no label makes the Publish run skip every publish step. So the label is not paperwork describing the change — it is the decision to ship. Choose it accordingly.

- Label the PR according to semantic versioning impact:
  - **major** — breaking changes to public APIs
  - **minor** — new features, new slices, non-breaking additions
  - **patch** — bug fixes, refactoring with identical behavior

### A pull request that changes nothing outward-facing carries `no-release`

**If nothing in the PR can change what a consumer of the framework compiles against, runs, or observes, label it `no-release`** — never `patch`. It is a fourth release-intent label alongside `major`/`minor`/`patch`, and exactly one of the four is required. Merging a `no-release` pull request publishes nothing, which is the correct outcome: there is nothing to release.

`no-release` is a decision, not an omission. Leaving the label off entirely is indistinguishable from forgetting it, so it stays an error.

This covers, whenever the PR touches *only* these:

- **Documentation** — anything under `Documentation/**`, READMEs, the `.ai/` corpus.
- **CI and repository automation** — `.github/workflows/**`, `.github/scripts/**`, `.github/CODEOWNERS`, issue/PR templates.
- **Tests and specs** — `*.Specs/**`, `when_*/**`, `for_*/**`, `Integration/**`, and test-only fixtures.
- **Build and tooling configuration** that produces no shipped artifact difference — lint config, editor config, local scripts.

The test is **outward-facing effect, not file location**. A change under `Source/**` that only touches specs is not shippable; a one-line change to a published package's behavior is, however small. If a consumer could not tell the difference by upgrading, there is nothing to version. When genuinely unsure, ask rather than defaulting to `patch` — an unnecessary release is not free: it burns a version number, ships release notes describing nothing, and buries the releases that matter.

A `no-release` pull request should be **green like any other** — `verify-semver-label` accepts the label, and the publish run skips its "nothing was published" alarm because nothing was meant to be.

### Group small related changes into one pull request

Do not open a pull request per task when the tasks belong to the same body of work. Several small merged PRs become several releases, and a stream of near-empty patch releases makes the release history useless for the people it is written for. Collect related work — a set of CI gates, a group of fixes in one area, the steps of one refactor — onto **one branch, as separate commits**, and open **one** pull request. Commits stay one-logical-unit-each; the pull request is the release boundary, and the release boundary should be a coherent, describable change.

Split into separate pull requests when the changes are genuinely unrelated, when one is urgent and the others are not, or when one is risky enough to want its own revert.

## Quality Gates

**A documentation-only pull request skips this section entirely.** Nothing it changes can break a build, a spec or a lint, so there is nothing to wait for: open it and merge it. Do not monitor its checks, do not wait for green, and do not treat a red `verify` from the deliberately absent version label as a failure. Verify the content instead — links resolve, anchors exist, every code example matches real source.

**`no-release` does not otherwise excuse a PR from this section.** A CI, tooling, or spec-only pull request ships nothing, but it is exactly the kind of change that can break the build or the pipeline for everyone else — a broken workflow or a deleted spec does its damage without ever being released. Hold it to every gate below.

Before marking any other PR ready for review:
- `dotnet build` — zero errors, zero warnings
- `dotnet test` — all specs pass
- `yarn lint` — zero errors
- `npx tsc -b` — zero TypeScript errors
- Code follows all project coding standards and conventions
- **CI checks pass** — after pushing, use GitHub MCP tools (`pull_request_read` with `get_check_runs`, `get_job_logs`) to monitor CI results. If any checks fail, investigate the logs, fix the failures, and push again. The task is not complete until all CI checks pass or the remaining failures are confirmed to be pre-existing flaky tests unrelated to the PR changes.
