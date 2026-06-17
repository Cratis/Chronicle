---
lifecycle: session-stop
---

# Agent Stop — Build, Specs, and Corpus Validation

> **This is lifecycle guidance, not a wired tool hook.** Markdown is not a hook format for Copilot or Claude Code. To *enforce* it, wire it per tool to run the repo's build/test command — Claude Code: a `Stop` hook in `.claude/settings.json`; GitHub Copilot: a `sessionEnd` entry in a `.github/hooks/*.json` file. The steps below are what that hook (or the agent) should do.

When the agent finishes a session, verify the work against **fresh signals** before stopping — never against self-assessment. Pick the path that matches the repository.

## Pick the path for this repository

- **AI corpus repo** — the changes are only under `.ai/`, `.github/`, or `.claude/` and there is no .NET solution or frontend to build (e.g. this `cratis/AI` repo). Run the AI-setup validator instead of a code build:
  ```
  .ai/hooks/scripts/validate-ai-setup.sh
  ```
  Stop only when it passes (symlinks/adapters healthy, frontmatter present, no broken cross-links). Skip the application gates below.

- **Application repo** — there is a .NET solution and/or a frontend. Run the application gates below.

## Application gates

1. **Clean** from repository root:
   ```
   dotnet clean
   ```
2. **Build Debug** from repository root — validates `#if DEBUG` spec code:
   ```
   dotnet build
   ```
3. **Build Release** from repository root — regenerates the TypeScript proxies:
   ```
   dotnet build -c Release
   ```
4. **Run specs/tests for every affected project** — use the project's test command; if you cannot isolate the affected scope, run the repository-level test command.
5. **Frontend** (when frontend files changed) — run lint, the type/build check, and frontend tests.

## If any gate fails

- Report the full output.
- Fix all errors, warnings, and failing specs before considering the session complete.
- Re-run the gate that failed and confirm it passes *this time*.

## Rules

- A session is not complete until both Debug and Release builds exit `0` with **zero** warnings, and the affected specs/tests exit `0`.
- Treat Release-only warnings (nullable annotations, analyzer findings) as errors — fix them.
- **Never** use `/clp:ErrorsOnly` or any flag that suppresses warning output — hidden warnings are warnings that never get fixed.
- A green build is not behavioral correctness — exercise the affected behavior (specs, or the running UI) and state plainly anything you could not verify.
