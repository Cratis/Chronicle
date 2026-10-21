<!-- cratis-ai-managed: skills/cratis-screenplay-model-review/references/verified-sources.md -->
# Where the product facts come from

Check a fact against the released source before relying on it. Repositories live under the
organization's GitHub account; read the file at the tag of the tool you run
(`cratis-screenplay-toolchain` `SKILL.md` lists the pins). Compilation (V1) does not establish
reference execution.

| Used for | Repository and path |
|---|---|
| The MCP tool catalog (`describe-application`, `search-declarations`, `dependency-graph`, `find-assertion-gaps`, `read-workspace`, `declaration-details`) | `Screenplay`: `Source/DotNET/Screenplay.Mcp/McpToolCatalog.cs`, `Documentation/screenplay/mcp/reference.md` |
| Diagnostics PLAY0029, PLAY0191, PLAY0268, PLAY0271, PLAY0350, PLAY0381, PLAY0469; one data subject per event | `Screenplay`: `Source/DotNET/Screenplay/Diagnostics/DiagnosticCodes.cs`, the decision record on data subjects |
| `find-specification-obligations` (SPEC001 to SPEC008, authored presence) and `find-modeling-smells` (SMELL001 to SMELL005, advisory questions); neither proves executed behavior or complete lineage | `Screenplay`: `Documentation/screenplay/mcp/reference.md` |
| `semantic-diff` source objects and revision-pinned pages | `Screenplay`: `Source/DotNET/Screenplay.Mcp/` (`McpRevisionDiff`, `McpWorkspaces.Comparison`), `Documentation/screenplay/mcp/reference.md` |
| Model notes: anchors, kinds, the committed sidecar, the open-question gate | `Screenplay`: `Documentation/screenplay/model-notes.md`; notes tools in model-authoring `references/model-notes.md` |
| Property links and their roles for lineage and journey tracing | `Screenplay`: `Documentation/screenplay/mcp/reference.md`; model-authoring `references/property-links.md` |
