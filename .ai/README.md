# Shared AI Assistant Configuration

This folder is the single source of truth for shared AI assistant artifacts.

## Structure

- `rules/` contains shared instruction files.
- `prompts/` contains reusable prompt templates.
- `agents/` contains reusable agent definitions.

## Tool integration

- GitHub Copilot files under `.github/` are symlinks to files in `.ai/`.
- Claude Code files under `.claude/` are symlinks to files in `.ai/`.

## Scoped rule frontmatter

Scoped rules include both:

- `applyTo` for GitHub Copilot instruction matching.
- `paths` for Claude Code rule matching.

When adding or changing a shared rule, update the file in `.ai/rules/` only. See `rules/managing-ai-rules.md` for the full guide on adding, updating, and renaming rules.
