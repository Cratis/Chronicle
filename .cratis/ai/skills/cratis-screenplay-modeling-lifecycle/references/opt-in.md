<!-- cratis-ai-managed: skills/cratis-screenplay-modeling-lifecycle/references/opt-in.md -->
# Opt-in evidence

The decision rule is in `SKILL.md` ("Decide the level first"). This file holds the evidence.

## Why an empty directory or an MCP entry is not opt-in
`cratis ai install`/`update` do not create a model directory by default:
`AiMcpDescriptor.Root(configuration)` returns only an
explicitly configured `mcpServers.screenplay.root` (null otherwise; there is no `DefaultRoot`), and
`AiMcpPlan` creates a directory only for a configured root. An older CLI created an empty `.cratis/screenplay`
model directory for the `cratis/screenplay` profile (and composed profiles such as Stage). The MCP entry is still registered,
and uninstall keeps any model directory. With no configured root, `cratis screenplay mcp` locates the model
itself (`Source/Cli/Commands/Screenplay/ScreenplayModelLocation.cs`): a legacy `.cratis/screenplay`
that already holds `.play` files, else the common ancestor of existing `.play` files, else a `Source`
or `src` folder, else a new `Screenplay/` folder at the project root. Nothing is chosen under
`.cratis` for new work. The MCP entry and any directory a tool created therefore appear in every
repository that merely installed the language skills, so neither can signal consent. Documentation that says install/update creates the selected
empty model directory (normally `.cratis/screenplay`) is stale against that source.

## The explicit signal
`mcpServers.screenplay.root` in `.cratis/ai.json` is the project-owned property that sets the
model directory (`Documentation/reference/screenplay-mcp.md`: "The optional
project-owned `mcpServers` property in `.cratis/ai.json` overrides the model directory or
disables registration"; `AiConfiguration.McpServers`, read by `AiMcpDescriptor.Root`). Verify with
`Source/Cli/Commands/Ai/AiMcpDescriptor.cs` in the CLI repository.

## Model root and edge cases
Run `cratis-screenplay-modeling-lifecycle`'s `scripts/model-first-status.mjs` for root resolution, the opt-in verdict, drafts and model changes in progress.
- Declined proposals: a team that wants a lasting "no model" answer writes it in its own
  repository instructions, never in managed corpus files.

## Frozen committed model
A project can mark a committed model as generated evidence with `modelFirst.frozen` (optional `reason`) in `.cratis/ai.json`. The script then reports reason `frozen-model`, `frozen` and `frozenReason`, `contract` null, and still lists the committed files so the evidence stays visible. A non-boolean value exits 2. The marker lives in the project configuration, not in the model root, so it does not travel with a model that moves. The `cratis screenplay mcp` server does not read it: it still resolves the same model root. Regeneration and edits follow `SKILL.md` step 1.

## Acceptance test cases
Run `scripts/model-first-status.mjs --self-test` for the executable verdict fixtures.
