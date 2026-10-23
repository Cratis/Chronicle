<!-- cratis-ai-managed: skills/cratis-ci-pipeline-performance/references/security-and-pinning.md -->
# Security alongside speed

Use this when a speed change adds or updates an action, edits `permissions:`,
changes a trigger, or adds a cache. Speed changes widen the blast radius of a
workflow more often than they narrow it. Run `zizmor` (security audits, including
`unpinned-uses` and `excessive-permissions`) and `actionlint` (syntax, expressions,
embedded shell; it does not check pinning) locally on every edited workflow when
installed. Do not assume CI runs them.

## Contents

- Pin third-party actions
- Minimal permissions
- Untrusted code and triggers
- Caches and artifacts
- Verification

## Pin third-party actions

A tag can be moved to different code; a full commit SHA cannot. Pin every
third-party action by its 40-character commit SHA and keep the version in a
comment so updates remain reviewable:

```yaml
- uses: actions/checkout@<40-character-sha> # v7
```

- Resolve a tag to its commit with `gh api repos/OWNER/REPO/commits/TAG --jq .sha`
  and confirm it matches the release you intend.
- Examples in this skill show a major tag with a "pin by full commit SHA" note;
  the major shown is not a recommendation to pin a tag.
- Actions of the `Cratis` organization follow the owning repository's convention.
- Let a dependency-update tool bump pinned SHAs so pinning does not freeze them.

## Minimal permissions

- Set `permissions:` at the job level, not the workflow level, and grant only
  what the job uses. Start from `contents: read`, or `permissions: {}` for a job
  that touches no repository content.
- Grant write scopes (`contents`, `packages`, `pull-requests`, `id-token`) only to
  the job that needs them, such as the publisher.
- Do not use a personal access token where the workflow token suffices.

## Untrusted code and triggers

- Never check out or run code from a pull request under `pull_request_target`
  or `workflow_run` with the base repository's credentials: it exposes secrets
  and a write token to fork code.
- `pull_request` from a fork has no secrets, which is the safe default for
  verification.
- Never route `pull_request` jobs of a public repository to self-hosted runners.
- Pass untrusted values (branch names, titles, comment bodies) to scripts through
  `env:`, never interpolated into `run:` with `${{ }}`.

## Caches and artifacts

- Caches must not contain secrets. A cache saved from the default branch is
  readable by every branch; a cache saved by a pull request is scoped to it.
- Do not restore a pull-request cache in a privileged workflow.
- Use `persist-credentials: false` on `actions/checkout` when the job does not
  push, so the token is not left in the checkout.
- Artifacts are not private from other jobs in the run; do not put credentials
  in them.

## Verification

```bash
actionlint .github/workflows/*.yml
zizmor .github/workflows/
```

Pass means exit `0` and no findings. Report a tool that is not installed as not
run. Resolve findings in the workflow; suppress one only with a recorded reason.
