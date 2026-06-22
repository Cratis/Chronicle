---
applyTo: "**/*"
description: "Use when running any shell command, reading files, or searching code. Route every command rtk supports through rtk (a hook auto-rewrites Bash commands); call rtk read/grep/find directly because the built-in Read/Grep/Glob tools bypass that hook."
---

# Using rtk (Token-Optimized Commands)

[rtk](https://github.com/rtk-ai/rtk) (Rust Token Killer) is a CLI proxy that filters and compresses command output *before it reaches the model* — typically **60–90% fewer tokens** on common dev commands, with no loss of the signal you actually need. The default in this corpus is to **run every command rtk supports through rtk**. The savings are real and compound across a session: build, test, lint, git, search, and file-read output are exactly the high-volume, low-signal outputs rtk trims.

## How it works — let the hook do its job

A `PreToolUse` hook **auto-rewrites Bash commands** to their `rtk` equivalent transparently and at zero token overhead (`git status` → `rtk git status`). For any supported command you do **nothing special** — run it normally and the hook wraps it.

- **Never bypass it.** Don't disable the hook, and reserve `rtk proxy <cmd>` for the rare case where you genuinely need the raw, unfiltered output (e.g. debugging what a filter dropped).
- **Audit coverage** with `rtk gain` (savings so far) and `rtk discover` (commands that slipped past rtk — missed opportunities to close).

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
