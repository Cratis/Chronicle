---
name: cratis-rtk
description: Use before choosing command-output wrappers, or when a wrapped command fails and exact diagnostics or parsed output are needed. Not for execution permission, CI-gate selection or rewriting repository scripts around rtk.
license: LICENSE
compatibility: Local shell; rtk is optional and must already be installed.
---
<!-- cratis-ai-managed: skills/cratis-rtk/SKILL.md -->

<!-- Copyright (c) Cratis. All rights reserved. -->
<!-- Licensed under the MIT license. See LICENSE file in the project root for full license information. -->

# Token-optimized commands

Follow [rtk.md](../../rules/rtk.md): exact output and real exit codes win over
compression. A wrapper is not verification and never expands authority.

1. If `rtk` is not on `PATH`, use normal tools; do not install it or block work.
2. For human-readable output, let the configured PreToolUse hook rewrite Bash
   commands (for example `git status` → `rtk git status`). Without that hook,
   prefix each supported command, including each command in an `&&` chain.
3. For parsed output, exact paths/content/Git state, structured diagnostics or
   output that decides the next action, use `rtk proxy <cmd>` (or the plain
   command when no hook rewrites it). Never sacrifice the producer's exit status.
4. If a wrapped command fails, read the actual error and retry through
   `rtk proxy` before concluding the tool, path or repository is missing.
5. Keep native commands in scripts, hooks and CI; do not add rtk to them.

Examples:

```sh
rtk git status
rtk proxy git diff --cached
rtk proxy git status --porcelain
```

For builds, lint and specs use the repository's authorized gate command. Do not
pipe a build or test into `grep`, `sort`, `head` or `tail` as gate evidence.
A filtered zero-error summary is not a successful check.

## Supported output families

- Files: `ls`, `tree`, `read`, `find`, `grep`, `diff`.
- Git and GitHub: `git status/log/diff/add/commit/push/pull`, `gh pr/issue/run …`.
- Tests: `dotnet test`, Jest, Vitest, Playwright, pytest, Go, Cargo, RSpec.
- Build and lint: `dotnet build`, `tsc`, ESLint, Biome, Prettier, Cargo Clippy,
  Ruff, golangci-lint, Rubocop.
- Package managers: pnpm/npm/yarn, pip, Bundler, Prisma.
- Cloud and containers: AWS CLI, Docker, Kubernetes, OpenShift.

## Built-in tools and audits

Bash hooks do not wrap built-in Read/Grep/Glob tools. For broad exploration use
`rtk read`, `rtk grep` or `rtk find` in the terminal to reduce output. For small,
precise reads and exact replacement text, use the built-in file tools: rtk can
filter away the line needed for an edit. Raw output remains mandatory whenever
it drives an operation.

Use `rtk gain` to inspect savings and `rtk discover` to find commands that missed
compression. These are optimization aids, not quality-gate evidence. The rtk
project is at https://github.com/rtk-ai/rtk; setup is a human's responsibility.
