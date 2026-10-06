---
applyTo: "**/*"
---
<!-- cratis-ai-managed: rules/pull-requests.md -->

# How to Do Pull Requests

## The description is the release note

**The PR description is published verbatim as the GitHub release. In repositories released by `cratis/release-action`, it also closes every issue written as `(#n)`; elsewhere, follow the repository's own release and issue-closing process.** Write the note the person upgrading should read, not a development write-up. The same contract applies to release notes typed into a manual workflow run or written straight into a release. Where it is installed, the `verify-release-notes` check enforces it on release-bound PRs into the default branch carrying `major`/`minor`/`patch` (it re-runs when the label is added); it warns on `no-release` and unlabelled PRs, which must still be written this way because any of them may become releasable. `release-action` does not run for a `no-release` PR, so `(#n)` closes nothing there. Editing the description re-runs the check. HTML comments are not shown on the release page and the check ignores them for headings, keywords and placeholders, but a `(#n)` inside a comment still closes the issue (`release-action` skips only fenced and inline code) and the check fails it, so never put an issue reference in a comment. The template's own comment can stay: its `(#123)` examples are inline code.

### Allowed shape

- Follow the repository's pull request template (`.github/pull_request_template.md`) within this contract.
- An optional summary, first, only when one cohesive theme spans the bullets: **either** a `## Summary` section (short, user-facing prose) **or** one unheaded lead paragraph (1–3 sentences), never both. `Summary` is a level-2 heading; `# Summary` is the wrong level. No bullets before the first section.
- Then only these `##` sections, each at most once, in this order after any `## Summary`: `## Added`, `## Changed`, `## Fixed`, `## Removed`, `## Security`, `## Deprecated`. Keep only the sections that have bullets: a section holding only prose, a code example or `None.` fails the check, so delete it. `###` sub-headings inside a section are fine unless they use a forbidden name; inside a section even `### Added` is only a sub-heading, and a section itself is always level 2.
- A `major`/`minor`/`patch` PR needs at least one bullet under an allowed section.
- Bullets are short, self-contained and user-facing: what a consumer compiles against, runs or observes. A user-visible fix may add one sentence of root cause or regression guard, stated as observable behavior. Credit an external contributor by name or handle, unless they asked not to be named.
- Breaking changes and upgrade actions are bullets in `## Changed` or `## Removed` that state the action. A longer migration story goes in the docs, linked by absolute URL.
- Do not list internal plumbing (storage, converters, gRPC internals, specs, file lists, refactor narration).

### Issue references

| Write | Meaning |
| --- | --- |
| `(#351)` at the **end of the bullet that delivers it** | Delivered: in a repository released by `cratis/release-action`, it closes it at release |
| `(part of #351)` or prose `see #351` | Related, partial or follow-up: never closes |
| `Cratis/Repo#351` | Other repository: never closed |
| one `(#351)` per issue, never `(#351, #352)` | `(#351, #352)` closes nothing: write `(#351) (#352)` |

- Nothing but more issue references (also `(part of #n)`, `(see #n)` and `(Cratis/Repo#n)`), closing emphasis, `<br>` and sentence punctuation (a `:` may introduce nested bullets) may follow the delivering `(#n)`. Write one `(#n)` per issue: release-action only matches `(#n)`, so `(#56, #57)` closes nothing; write `(#56) (#57)`.
- Never use a linking or closing keyword before a number, anywhere: `Close`, `Closes`, `Closed`, `Fix`, `Fixes`, `Fixed`, `Resolve`, `Resolves`, `Resolved`, `Refs`, `Ref`, `References`, also as `Keyword: #n`, with `owner/repo#n`, followed by an issue URL, and wrapped in bold, italics or a link (`**Closes** #n`, `Closes [#n](url)`, `[Closes #n](url)`, and `[Closes](url)` where the link starts a clause). A `Refs #93` line closes nothing and is not a delivery marker: write `(#93)` at the end of the delivering bullet, or `(part of #93)` if it must stay open.
- Comment on or close an issue when the user's request includes that effect; otherwise prepare a bounded post-merge disposition without performing it. In repositories released by `cratis/release-action`, a delivered `(#n)` is closed by release-action at release, so do not close it by hand; for a `no-release` PR release-action does not run, so any disposition follows this same rule. Elsewhere, follow the repository's own release and issue-closing process.
- Use a bare `(#n)` only for an issue this PR fully delivers. No issue means no reference. Never use a placeholder such as `(#issue)` or the template's `(#123)`, and verify every number exists in the right repository; never guess.

### Forbidden anywhere outside code

- **Headings** (any level, also written as HTML `<h2>`, `<b>`, `<i>` or a possibly multi-line `<summary>`, or as an `*italic*` line; a heading line is checked like any other, so a keyword, relative link or `(#n)` in one fails too) other than a first `## Summary`, such as Overview, Description, What, Why, How, Context, Changes, What changed, Test plan, Testing, Tests, Verification, Verified, Validation, Quality, Review, Notes, Notes for reviewers, Limitations, Known follow-up, Acceptance, Details, and any `#`/`##` heading not in the allowed list.
- **Review, verification, testing and provenance notes**: `Review:`, `Reviewed:`, a stand-alone `Reviewed by` line, `Verification:`, `Tested:`, `Testing:`, `Validation:` lines that stand alone or report a result (`Tests: 400 passed`, `Review: approved`); same-provider, cross-provider, Opus-only or Anthropic-only review remarks; the review workflow having passed, returned or run; CI or gate results (`CI green`, `all tests passed`); and a line stating which agent or model wrote the description (`Generated with Claude Code`, `Co-Authored-By:`). A bullet that describes a product change and only mentions review, validation, tests or an AI model (`- Validation: rules now apply to commands (#3)`, `- Reviewed by status is now shown on the dashboard (#4)`) is fine: inside a bullet, a summary or the lead paragraph the check reads a note only when it ends the clause (`- Cross-provider review pending`).
- **Verification and scope notes**: how or where something was checked (`verified by building …`, `verified in Storybook`, `tested locally`, `Storybook only`), what was not checked (`not yet verified`, `Not verified against a real kernel`), and results (`492/492`, `9 of 9 runs`, `Tier 1 PASS`, `0 warnings, 0 errors`, a `Local:`, `CI:`, `Checks:` or `Checked locally:` line).
- **Reviewer, merge and deploy instructions**: `Stacked on #207`, `retarget to main once #207 merges`, `This PR should be deployed separately`, `Draft:`, `do not merge`, merge or deploy order. If a consumer must upgrade in a set order, write that as an upgrade bullet ("Upgrade the Chronicle kernel to 19.25 before this client").
- **Provenance**: which branch, commit or build something was checked against (`verified against Arc.TypeScript main (v0.34.0)`). If compatibility matters to the reader, state it as a fact about the release: "Requires `@cratis/arc` 0.34 or later."
- **Internal state** that is not a consumer change (for example "npm publication remains disabled").
- **Relative links** (`](Documentation/x.md)`, `](./x)`, `](Source/...)`): they 404 on the release page. Use `https://github.com/Cratis/<Repo>/blob/main/<path>`, a `#anchor` or `mailto:`.
- **Placeholders and transcripts**: template text, empty sections, Copilot "Original prompt" blocks, agent transcripts.

Reviewer-facing information (test plan, verification, what was not verified, review provenance, stacking, merge or deploy order) goes in a **PR comment** posted right after creating the PR: `gh pr comment <n> --body-file .ai-work/pr-notes.md`. Never put it in the description, even when the PR is labelled `no-release`.

### Keep the note true to the diff

After merging or rebasing the base branch, and before every body edit, run `git diff --stat origin/main...HEAD`. Delete each bullet whose change is now on main through another PR, or was dropped in conflict resolution. Check every version, package name and registry against the diff and the current `.github/workflows/publish*.yml`; never name a registry the publish workflow does not push to.

### Bad to good

Bad (Cratis/Arc.TypeScript v0.48.0 as first published, bullets abridged):

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

"npm publication remains disabled" is internal status, the link was relative (it 404s on the release page), the review line is provenance for reviewers, and `Refs #93` is a keyword line. #93 was only partly delivered by this release, so its bullets say `(part of #93)`; `(#93)` would have closed it.

### Before you create or edit a PR

1. Run `node .cratis/ai/hooks/scripts/cratis-check-pr.mjs --body-file <file> --label <intent>`. It runs the exact `verify-release-notes` rules plus the label and diff checks, and the Bash hook runs it on `gh pr create` and `gh pr edit`.
2. Read the body against the forbidden list and the issue table above; delete every hit and verify every issue number.
3. Sections in order, none empty, at most one summary form; every bullet user-facing.
4. Test plan, verification and review notes are in a PR comment.

If `verify-release-notes` fails, fix it by editing the description, not by pushing code.

## Commits

See the full [Git Commits guide](./git-commits.md) for rules on logical grouping, message format, and staging discipline.

Quick reminders:

- Imperative mood: "Add author registration" not "Added author registration".
- Each commit = one logical unit of work. No WIP commits in the final PR.
- Never mix unrelated changes in a single commit.

## Labels

Confirm the current repository workflow contract before selecting release intent because release-intent labels can trigger publication. A direct request to ship with a named label authorizes applying that label and completing the repository's standard pull-request workflow, including the release it normally triggers. Do not ask for separate authorization at each step. If the user did not request shipping or publication, a proposed semantic label describes impact but does not grant authority.

- Exactly one of `major`/`minor`/`patch`/`no-release`, set in the creating command (`gh pr create --label patch`), in every repository, including samples, docs and tooling repositories (`no-release` when nothing is shipped). To change intent, swap labels in one command: `gh pr edit <n> --remove-label minor --add-label patch`. Never add a second.
- Dependabot PRs carry only `no-release`. release-action never releases them, and the `major`/`minor`/`patch` Dependabot adds describe the dependency's version, not this product's.
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
