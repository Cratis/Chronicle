<!-- cratis-ai-managed: skills/cratis-commits-and-pull-requests/references/pr-description.md -->
<!-- Copyright (c) Cratis. All rights reserved. -->
<!-- Licensed under the MIT license. See LICENSE file in the project root for full license information. -->

# The pull request description

The contract (the description is the release note, the issue-reference rules, the reviewer-note rule) stays always-on in [pull-requests.md](../../../rules/pull-requests.md). This reference holds the detailed shape, the forbidden list and a worked example.

**The PR description is published verbatim as the GitHub release. In repositories released by `cratis/release-action`, it also closes every issue written as `(#n)`; elsewhere, follow the repository's own release and issue-closing process.** Write the note the person upgrading should read, not a development write-up. The same contract applies to release notes typed into a manual workflow run or written straight into a release. Where it is installed, the `policy / verify` check enforces it on release-bound PRs into the default branch carrying `major`/`minor`/`patch` (it re-runs when the label is added); its release-note step warns on `no-release` and unlabelled PRs, which must still be written this way because any of them may become releasable. `release-action` does not run for a `no-release` PR, so `(#n)` closes nothing there. Editing the description re-runs the check. HTML comments are not shown on the release page and the check ignores them for headings, keywords and placeholders, and `release-action` removes them before publishing, so a `(#n)` only inside a comment closes nothing; the check fails it, so never put an issue reference in a comment. The template's own comment can stay: its example issue references are inline code, and the comment is not published.

**A change to a hook script or its wiring (`.cratis/ai/hooks/**`) needs a release-note line that tells consumers to run `cratis ai update`; a hook fix does not reach a repository until they do.**

## Allowed shape

- Follow the repository's pull request template (`.github/pull_request_template.md`) within this contract.
- An optional summary, first, only when one cohesive theme spans the bullets: **either** a `## Summary` section (short, user-facing prose) **or** one unheaded lead paragraph (1–3 sentences), never both. `Summary` is a level-2 heading; `# Summary` is the wrong level. No bullets before the first section.
- Then only these `##` sections, each at most once, in this order after any `## Summary`: `## Added`, `## Changed`, `## Fixed`, `## Removed`, `## Security`, `## Deprecated`. Keep only the sections that have bullets: a section holding only prose, a code example or `None.` fails the check, so delete it. `###` sub-headings inside a section are fine unless they use a forbidden name; inside a section even `### Added` is only a sub-heading, and a section itself is always level 2.
- A `major`/`minor`/`patch` PR needs at least one bullet under an allowed section.
- Bullets are short, self-contained and user-facing: what a consumer compiles against, runs or observes. A user-visible fix may add one sentence of root cause or regression guard, stated as observable behavior. Credit an external contributor by name or handle, unless they asked not to be named.
- Breaking changes and upgrade actions are bullets in `## Changed` or `## Removed` that state the action. A longer migration story goes in the docs, linked by absolute URL.
- Do not list internal plumbing (storage, converters, gRPC internals, specs, file lists, refactor narration).

## Issue references

| Write | Meaning |
| --- | --- |
| `(#n)` at the **end of the bullet that delivers it** | Delivered: in a repository released by `cratis/release-action`, it closes it at release |
| `(part of #n)` or prose `see #n` | Same issue, related, partial or follow-up: never closes |
| `Cratis/Repo#n` | Other repository: never closed |
| one `(#n)` per issue, never `(#n, #m)` | For distinct issues, `(#n, #m)` closes nothing: write `(#n) (#m)` |

- Nothing but more issue references (also `(part of #n)`, `(see #n)` and `(Cratis/Repo#n)`), closing emphasis, `<br>` and sentence punctuation (a `:` may introduce nested bullets) may follow the delivering `(#n)`. Write one `(#n)` per issue: release-action only matches `(#n)`, so `(#n, #m)` closes nothing; write `(#n) (#m)`.
- Never use a linking or closing keyword before a number, anywhere: `Close`, `Closes`, `Closed`, `Fix`, `Fixes`, `Fixed`, `Resolve`, `Resolves`, `Resolved`, `Refs`, `Ref`, `References`, also as `Keyword: #n`, with `owner/repo#n`, followed by an issue URL, and wrapped in bold, italics or a link (`**Closes** #n`, `Closes [#n](url)`, `[Closes #n](url)`, and `[Closes](url)` where the link starts a clause). A `Refs #n` line closes nothing and is not a delivery marker: write `(#n)` at the end of the delivering bullet, or `(part of #n)` if it must stay open.
- Comment on or close an issue when the user's request includes that effect; otherwise prepare a bounded post-merge disposition without performing it. In repositories released by `cratis/release-action`, a delivered `(#n)` is closed by release-action at release, so do not close it by hand; for a `no-release` PR release-action does not run, so any disposition follows this same rule. Elsewhere, follow the repository's own release and issue-closing process.
- Use a bare `(#n)` only for an issue this PR fully delivers. No issue means no reference. Never use a placeholder such as `(#issue)` or the template's example issue number, and verify every number exists in the right repository; never guess.

## Forbidden anywhere outside code

- **Headings** (any level, also written as HTML `<h2>`, `<b>`, `<i>` or a possibly multi-line `<summary>`, or as an `*italic*` line; a heading line is checked like any other, so a keyword, relative link or `(#n)` in one fails too) other than a first `## Summary`, such as Overview, Description, What, Why, How, Context, Changes, What changed, Test plan, Testing, Tests, Verification, Verified, Validation, Quality, Review, Notes, Notes for reviewers, Limitations, Known follow-up, Acceptance, Details, and any `#`/`##` heading not in the allowed list.
- **Review, verification, testing and provenance notes**: `Review:`, `Reviewed:`, a stand-alone `Reviewed by` line, `Verification:`, `Tested:`, `Testing:`, `Validation:` lines that stand alone or report a result (`Tests: 400 passed`, `Review: approved`); same-provider, cross-provider, Opus-only or Anthropic-only review remarks; the review workflow having passed, returned or run; CI or gate results (`CI green`, `all tests passed`); and a line stating which agent or model wrote the description (`Generated with Claude Code`, `Co-Authored-By:`). A bullet that describes a product change and only mentions review, validation, tests or an AI model (`- Validation: rules now apply to commands (#n)`, `- Reviewed by status is now shown on the dashboard (#m)`) is fine: inside a bullet, a summary or the lead paragraph the check reads a note only when it ends the clause (`- Cross-provider review pending`).
- **Verification and scope notes**: how or where something was checked (`verified by building …`, `verified in Storybook`, `tested locally`, `Storybook only`), what was not checked (`not yet verified`, `Not verified against a real kernel`), and results (`492/492`, `9 of 9 runs`, `Tier 1 PASS`, `0 warnings, 0 errors`, a `Local:`, `CI:`, `Checks:` or `Checked locally:` line).
- **Reviewer, merge and deploy instructions**: `Stacked on #n`, `retarget to main once #n merges`, `This PR should be deployed separately`, `Draft:`, `do not merge`, merge or deploy order. If a consumer must upgrade in a set order, write that as an upgrade bullet ("Upgrade the Chronicle kernel to 19.25 before this client").
- **Provenance**: which branch, commit or build something was checked against (`verified against Arc.TypeScript main (v0.34.0)`). If compatibility matters to the reader, state it as a fact about the release: "Requires `@cratis/arc` 0.34 or later."
- **Internal state** that is not a consumer change (for example "npm publication remains disabled").
- **Relative links** (`](Documentation/x.md)`, `](./x)`, `](Source/...)`): they 404 on the release page. Use `https://github.com/Cratis/<Repo>/blob/main/<path>`, a `#anchor` or `mailto:`.
- **Placeholders and transcripts**: template text, empty sections, Copilot "Original prompt" blocks, agent transcripts.

Reviewer-facing information (test plan, verification, what was not verified, review provenance, stacking, merge or deploy order) goes in a **PR comment** posted right after creating the PR: `gh pr comment <n> --body-file .ai-work/pr-notes.md`. Never put it in the description, even when the PR is labelled `no-release`.

## Keep the note true to the diff

After merging or rebasing the base branch, and before every body edit, run `git diff --stat origin/main...HEAD`. Delete each bullet whose change is now on main through another PR, or was dropped in conflict resolution. Check every version, package name and registry against the diff and the current `.github/workflows/publish*.yml`; never name a registry the publish workflow does not push to.

## Bad to good

Bad (a published release note, bullets abridged):

```markdown
Arc can now construct Chronicle reactors and reducers through its own dependency injection, one scope per delivered batch, as an opt-in preview. npm publication remains disabled.

## Added

- Preview option `withChronicle({ ..., activateArtifactsInScopes: true })` ... (#93)

See [Activate reactors and reducers in Arc scopes](Documentation/chronicle/reactors/scoped-activation.md).

Review: Opus-only (same-provider) review.

Refs #93
```

Good (as corrected):

```markdown
Arc can now construct Chronicle reactors and reducers through its own dependency injection, one scope per delivered batch, as an opt-in preview.

## Added

- Preview option `withChronicle({ ..., activateArtifactsInScopes: true })` ... See [Activate reactors and reducers in Arc scopes](https://github.com/Cratis/Arc.TypeScript/blob/main/Documentation/chronicle/reactors/scoped-activation.md). (part of #93)
```

"npm publication remains disabled" is internal status, the link was relative (it 404s on the release page), the review line is provenance for reviewers, and `Refs #n` is a keyword line. The example issue was only partly delivered by this release, so its bullets use the `(part of #n)` form; `(#n)` would have closed it.

## Before you create or edit a PR

1. Run `node .cratis/ai/hooks/scripts/cratis-check-pr.mjs --body-file <file> --label <intent>`. It runs the exact `policy / verify` release-note rules plus the label and diff checks, and the Bash hook runs it on `gh pr create` and `gh pr edit`.
2. Read the body against the forbidden list and the issue table above; delete every hit and verify every issue number.
3. Sections in order, none empty, at most one summary form; every bullet user-facing.
4. Test plan, verification and review notes are in a PR comment.

If the release-note rules fail, fix the description, not the code.
