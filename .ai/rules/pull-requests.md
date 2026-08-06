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

- Label the PR according to semantic versioning impact:
  - **major** — breaking changes to public APIs
  - **minor** — new features, new slices, non-breaking additions
  - **patch** — bug fixes, refactoring with identical behavior

### Documentation-only pull requests carry no label

**If every changed file is documentation, do not label the PR at all** — not `patch`, not anything. No label means the Publish run skips every publish step, which is exactly right: a docs change should not cut a release. A `verify`-style job that fails because the label is missing is the expected outcome for this kind of PR, not a problem to fix.

## Quality Gates

**A documentation-only pull request skips this section entirely.** Nothing it changes can break a build, a spec or a lint, so there is nothing to wait for: open it and merge it. Do not monitor its checks, do not wait for green, and do not treat a red `verify` from the deliberately absent version label as a failure. Verify the content instead — links resolve, anchors exist, every code example matches real source.

Before marking any other PR ready for review:
- `dotnet build` — zero errors, zero warnings
- `dotnet test` — all specs pass
- `yarn lint` — zero errors
- `npx tsc -b` — zero TypeScript errors
- Code follows all project coding standards and conventions
- **CI checks pass** — after pushing, use GitHub MCP tools (`pull_request_read` with `get_check_runs`, `get_job_logs`) to monitor CI results. If any checks fail, investigate the logs, fix the failures, and push again. The task is not complete until all CI checks pass or the remaining failures are confirmed to be pre-existing flaky tests unrelated to the PR changes.
