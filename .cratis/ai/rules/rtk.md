---
applyTo: "**/*"
description: "Use when running any shell command, reading files, or searching code. Route commands through rtk for human-readable output (a hook auto-rewrites Bash commands; prefix rtk yourself where it does not); use raw output when the result is parsed, compared exactly, or drives another operation."
---
<!-- cratis-ai-managed: rules/rtk.md -->

# Using rtk (Token-Optimized Commands)

[rtk](https://github.com/rtk-ai/rtk) (Rust Token Killer) is a CLI proxy that filters and compresses command output *before it reaches the model* — typically **60–90% fewer tokens** on common dev commands, with no loss of the signal you actually need. The default in this corpus is to **run every command rtk supports through rtk**. The savings are real and compound across a session: build, test, lint, git, search, and file-read output are exactly the high-volume, low-signal outputs rtk trims.

## How it works — let the hook do its job

A `PreToolUse` hook **auto-rewrites Bash commands** to their `rtk` equivalent transparently and at zero token overhead (`git status` → `rtk git status`). For any supported command you do **nothing special** — run it normally and the hook wraps it. Where no hook is configured, prefix the command yourself — including each command in an `&&` chain: `rtk git add . && rtk git commit -m "msg"`.

- **Audit coverage** with `rtk gain` (savings so far) and `rtk discover` (commands that slipped past rtk — missed opportunities to close).

## When to use raw output instead

rtk output is a summary for a reader. It is never evidence of exact file contents, paths, Git state, or command semantics, and a filter can drop the line you needed. Use the raw command — `rtk proxy <cmd>`, or the plain command where nothing rewrites it — whenever:

- the output is parsed by a script, compared exactly, or fed to another command;
- the output is the evidence for a claim about content, paths, or Git state;
- the command's exit code or exact diagnostics decide what happens next (security checks, structured JSON, machine-readable diagnostics);
- a wrapped command failed. Read the actual error and retry through `rtk proxy` before concluding the tool, path, or repository is missing.

Do not add `rtk` to scripts, hooks, or CI steps that deliberately use native commands; their raw output is the point. Exit codes are never traded for token savings — see [`exit-codes-and-wrappers.md`](./exit-codes-and-wrappers.md).

## What rtk supports (route these through rtk)

- **Files** — `ls`, `tree`, `read`, `find`, `grep`, `diff`
- **Git & GitHub** — `git status/log/diff/add/commit/push/pull`, `gh pr/issue/run …`
- **Tests** — `dotnet test`, Jest, Vitest, Playwright, pytest, Go, Cargo, RSpec
- **Build & lint** — `dotnet build`, `tsc`, ESLint, Biome, Prettier, Cargo Clippy, Ruff, golangci-lint, Rubocop
- **Package managers** — pnpm/npm/yarn, pip, Bundler, Prisma
- **Cloud & containers** — AWS CLI, Docker, Kubernetes, OpenShift

In practice this means the Cratis **quality-gate commands** — `dotnet build`, `dotnet test`, `yarn lint`, `npx tsc -b`, `git`, `gh` — all flow through rtk automatically. Just run them.

## The one gap — built-in tools bypass the hook

The hook only sees **Bash** commands. Claude Code's built-in `Read`, `Grep`, and `Glob` tools (and the Copilot equivalents) do **not** pass through it, so they are **not** auto-rewritten — and those are some of the most token-heavy operations in a session.

- For **bulk reads and broad searches** where output volume is large, call **`rtk read` / `rtk grep` / `rtk find`** from the terminal so the savings are captured. This is a deliberate override of the usual "prefer the built-in file tools" default.
- Keep the **built-in** `Read`/`Grep`/`Glob` for **small, targeted reads** and when you need exact line references for an edit — rtk filters output, so it serves exploration and volume, not the precise content an `Edit` must match verbatim.

## Availability

This assumes the `rtk` binary is installed and the hook configured (`rtk init -g`). If `rtk` is **not** on `PATH`, ignore this rule and use the normal tools — never block work on rtk being present.
