<!-- cratis-ai-managed: skills/cratis-screenplay-modeling-lifecycle/references/opt-in.md -->
# Opt-in evidence

The decision rule is in `SKILL.md` ("Decide the level first"). This file holds the evidence.

## Why an empty directory or an MCP entry is not opt-in
Up to cratis CLI `v3.27.1`, `cratis ai install`/`update` created an empty `.cratis/screenplay`
model directory for the `cratis/screenplay` profile (and composed profiles such as Stage) and
registered a Screenplay MCP entry for it. From `v3.28.x` (read at `v3.28.2`) install no longer
creates a model directory by default: `AiMcpDescriptor.Root(configuration)` returns only an
explicitly configured `mcpServers.screenplay.root` (null otherwise; there is no `DefaultRoot`), and
`AiMcpPlan` creates a directory only for a configured root. The MCP entry is still registered,
and uninstall keeps any model directory. With no configured root, `cratis screenplay mcp` locates the model
itself (`Source/Cli/Commands/Screenplay/ScreenplayModelLocation.cs`): a legacy `.cratis/screenplay`
that already holds `.play` files, else the common ancestor of existing `.play` files, else a `Source`
or `src` folder, else a new `Screenplay/` folder at the project root. Nothing is chosen under
`.cratis` for new work. The MCP entry and any directory a tool created therefore appear in every
repository that merely installed the language skills, so neither can signal consent. The old
documentation text (`Documentation/ai/index.md` at `v3.28.2`: "Install/update creates the selected
empty model directory (normally `.cratis/screenplay`)") is stale against that source.

## The explicit signal
`mcpServers.screenplay.root` in `.cratis/ai.json` is the project-owned property that sets the
model directory (`Documentation/reference/screenplay-mcp.md` at `v3.28.2`: "The optional
project-owned `mcpServers` property in `.cratis/ai.json` overrides the model directory or
disables registration"; `AiConfiguration.McpServers`, read by `AiMcpDescriptor.Root`). Verify with
`git -C <cli checkout> show v3.28.2:Source/Cli/Commands/Ai/AiMcpDescriptor.cs`.

## Model root and the committed-`.play` test
The model root is the folder holding the project's `.play` files; absent an existing
model, use `Source/` or `src/`, else `Screenplay/` at the repository root, never
under `.cratis/`. An explicitly configured `mcpServers.screenplay.root` selects
the root. A committed `.play` there is rule (a)'s opt-in signal without requiring
the setting; setting a root is independently rule (b), even when empty. The legacy
CLI discovery behavior above is historical evidence, not a default corpus root.
Do not count unrelated documentation, samples or scratch files as the project model.

## Edge cases
- `.play` files only under `.ai-work/`, a docs folder or a sample: not under the root, so not opt-in.
- Acceptance is visible in the repository: the model root holds at least one `.play` file in
  the committed tree (`git ls-tree -r --name-only HEAD` lists a `.play` file there, narrowed to `-- <root>` when a root is configured). Committing a model
  under the root is the team's act of acceptance and opts the repository in. P6 commits the
  accepted `.play` files and `.screenplay/identities.json` when present.
- Staged or untracked files under the root are drafts: they do not opt the repository in and
  are not a contract for code agents. The modeler writes drafts into the root as before; they
  stay drafts until committed. A local STATE.md alone proves nothing to another clone.
- A committed file with uncommitted working-tree edits is a model change in progress. Its HEAD
  version is the contract until the change is committed; `git diff --quiet HEAD -- <file>`
  detects whether the working copy differs.
- An explicitly configured root (`mcpServers.screenplay.root`) that is empty still counts as
  opted in; rule (b) is independent of rule (a).
- Declined proposals: a team that wants a lasting "no model" answer writes it in its own
  repository instructions, never in managed corpus files.

## Acceptance test cases
Assume no other committed `.play` file under the root and no explicit root configuration,
except in the last row. A committed model is a contract only for the slice it covers.

| Case under the model root | Opted in? | Contract for code agents |
|---|---|---|
| Untracked `.play` file | No | None; draft |
| Staged-only `.play` file, absent from HEAD | No | None; draft |
| Committed `.play` file | Yes | Its HEAD version |
| Committed `.play` file with working-tree edits | Yes | Its HEAD version; edits are a model change in progress until committed |
| Explicitly configured empty root | Yes | None yet; model the slice first |
