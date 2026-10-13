<!-- cratis-ai-managed: skills/cratis-screenplay-model-authoring/references/provenance.md -->
<!-- Copyright (c) Cratis. All rights reserved. -->
<!-- Licensed under the MIT license. See LICENSE file in the project root for full license information. -->

# Provenance

| Content | Source | How it is used |
| --- | --- | --- |
| `mcp-loop.md` (ownership, edit-request shape, edit routes, failure handling) | The project's own earlier event-modeling toolkit draft (MCP loop reference) | Restructured and re-verified against Screenplay v4.66.0 source and documentation; the roots-bug workaround and the identity rule (`id` pins are not enough when `identities.json` exists) were corrected or tightened |
| Connection facts (29/30 tools, `notifications/initialized`, 16 connection-local proposals, fixed root) | Screenplay `v4.66.0` `Source/DotNET/Screenplay.Mcp/McpToolCatalog.cs`, `McpConnection.cs`, `McpWorkspaces.cs`, `Documentation/screenplay/mcp/reference.md`, `install.md` | Read at the tag and probed against the standalone 4.66.0 tool (4.63.1 for the roots behavior) |
| Disposition table (personas, generated values, operations, streams, automation) | Screenplay `v4.66.0` and `v4.68.0` (decisions 0025 and 0026) `SemanticModelBinder.cs`, `commands.md`, `operations.md`, `event-sources.md`, `Versions.cs` | Probed through the MCP `executable-diagnostics` view of the 4.66.0 tool |

No TrogonStack or Nebulit text is reproduced here: neither covers the Screenplay
MCP surface.
