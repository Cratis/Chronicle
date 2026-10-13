<!-- cratis-ai-managed: skills/cratis-screenplay-toolchain/references/provenance.md -->
# Provenance

## Origin

This skill is an original Cratis work, condensed from verified research on the Screenplay
language, the standalone tool, the cratis CLI and Stage, then re-verified against product
source at the tags in `versions.md` and probed on the installed tools (Screenplay 4.66.0 and
cratis 3.28.2). Every version, diagnostic and exit-code row cites its evidence there or in
the file that holds it.

## External material

| Source | Use | Licence handling |
| --- | --- | --- |
| TrogonStack/agentskills `7b249d3ee42d8b7e11fa564141ebd5fbc37aadf1` (MIT, Copyright (c) 2025 Straw Hat, LLC) | none: no passage of this skill is adapted from it | no third-party notice needed in `LICENSE` |

## Adapted closely (Martin Dilger and Nebulit GmbH)

Source: https://github.com/Nebulit-GmbH/agentic-engineer at commit `07b0f30648d663cb588d7e2c7aa031af9dfc21f2`,
by Martin Dilger and Nebulit GmbH (https://nebulit.de), used with the explicit consent of Martin Dilger and Nebulit GmbH.

| Source file | Used in | How |
|---|---|---|
| `.claude/skills/connect/SKILL.md`: resolve the connection once per session, reuse it, re-resolve only when a value actually needs to change | `SKILL.md` "Ground rules" 8, `references/verdicts.md` V2 start-up | Now adapted closely; board token/config prompts not adopted, translated to tool, version and model root |
| `.claude/skills/load-slice/SKILL.md`: scoped, identity-first reads instead of loading everything | `SKILL.md` "Edit strategy" | Idea; our own wording kept |
| `.claude/skills/learn-eventmodelers-api/SKILL.md`: load the tool reference on demand; prefer the typed tool over raw calls | `SKILL.md` "Ground rules" 8, "Edit strategy" | Idea; our own wording kept; the 934-line API catalog has no Screenplay equivalent to adopt |

## Sources read for facts

- Historical public/keys/routes/version source check: Screenplay `v4.135.0` (`b4e46130`),
  unchanged release contract, `Semantics/Versions.cs`, `Documentation/screenplay/{events,slices,captures,readmodels,queries,event-sources,diagnostics,interoperability}.md`,
  decisions 0049–0051 and 0060–0066. Composite view keys remain PLAY0268 (#599);
  v11 literal lowering is admitted despite interoperability's unreleased label.
  CLI `v3.50.0` (`546dea12`) pins Stage 4.67.0 and Screenplay 4.131.0;
  `RenderedSemanticVersions.cs` and Stage's version-feature admission were read,
  not executed as a new render/browser vector.
- Previous pin update: Screenplay `v4.131.0` (`71f3ef37`), published
  `screenplay-contract.json`, `Source/DotNET/Screenplay.Mcp/{McpRevisionDiff,McpWorkspaces.Comparison,McpToolSchemas}.cs`
  and `Documentation/screenplay/mcp/reference.md`; CLI `v3.46.1` (`73578fbf`)
  `Directory.Packages.props`, `RenderedSemanticVersions.cs`, `RenderSettings.cs`,
  `CratisRenderTarget.cs`, `RunSettings.cs` and `StageContainer.cs`. The CLI
  source checks are not a new render/browser probe.
- Identity/compliance/route-literal guidance: Screenplay `v4.127.0` (`e70435a4`)
  `identity.md`, `context.md`, `concepts.md`, `purposes.md`, `events.md`,
  `event-sources.md`, `reactions.md`, `completeness.md`, `diagnostics.md`,
  `editor-diagnostics.md`, MCP reference/authoring tools and `Semantics/Versions.cs`.
  Installed 4.127.0 version and changed examples were checked separately. Historical
  execution and renderer probes retain their labels, including CLI 3.41.0.
- Earlier baseline: Screenplay `v4.125.0` `Documentation/screenplay/{tool,diagnostics,editor-diagnostics,grammar,event-sources,specifications,policies}.md`, MCP reference/authoring guides and `Semantics/Versions.cs`; CLI `v3.41.0` dependency pins, `RenderedSemanticVersions.cs` and its v7 planning specification. Installed versions/CLI help and changed examples were checked separately. Pins are in `versions.md`; the older probes below retain their original labels.
- Screenplay `v4.66.0` (`c89198b`): `Documentation/screenplay/` (grammar, commands, queries,
  specifications, reactions, diagnostics, mcp), `decisions/0017`, `0020`, `0022` to `0024`,
  `Source/DotNET/Screenplay/Semantics/` (binder, versions, execution),
  `Source/DotNET/Screenplay.Mcp/` (connection, catalog, schemas, workspaces, visualization),
  `Source/DotNET/Tool/Program.cs`. Also `v4.60.1` and `v4.63.1` for the differences from the compiler bundled in cratis 3.27.1
  and earlier.
- Stage `v4.24.0` (`fa48546`, unchanged in `Rendering.Cratis` admission at `v4.24.1`): `Source/Rendering.Cratis/` (surface ledger, planner, pure
  transition admission, scaffold), `Documentation/guides/customize-rendered-application.md`.
- cratis `v3.28.2` (`141c499`; `Render/`, `Run/`, `Prologue/` and `Screenplay/Generate*` are unchanged since `v3.27.1`, `a327e89`): `Source/Cli/Commands/Render/`, `ScreenplayMcpRoot.cs`,
  `Documentation/reference/screenplay.md`.
- Arc `v22.50.5` and Chronicle `v19.32.0` for the code-level facts cited in `versions.md`.
- Historical issue inventory at the old probes: cli#242, #243, #244, #245;
  Stage#79, #165, #178; Chronicle#3744, #4123, #4131. Current invocation actor and
  reference-execution availability are verified against v4.125.0, not old open-issue status.

## Changes from the earlier toolkit

- Folded the version facts into one `versions.md` (the only version table in the corpus).
- Moved the pin to Screenplay 4.68.0 (ESM v7: generated values and responses, decision 0026, `generated-responses-example.md`); re-ran the example compile and reference-runner gates unchanged; replaced every fixed number for unadmitted features with "not admitted by any supported ESM version" (decision 0025).
- Replaced the readiness script with the MCP view and field that carry the same result; no
  script ships.
- Moved the MCP loop to `cratis-screenplay-model-authoring`; this skill keeps only the facts
  needed to run a verdict and the edit strategy summary.
- Re-checked every documentation contradiction at v4.66.0; resolved the clock boundary and the
  ESM v7 and v11 allocations; added numbers-exact, concurrency, approval-policy, visualization,
  bodied-reducer, file-mode and cascade rows.
- Corrected the code-attachment rule (a handler never binds, with or without `hint`), the
  historical automation rendering rule (Stage 4.24 rendered no Automation or Translate slices; current admission is in `renderable-subset.md`) and the
  Stage output description (a React/Vite scaffold is emitted too).
- Verdict names follow the frozen V1 to V5 (V2 is executable diagnostics, V3 binding-ready);
  the earlier "authoring accepted" meaning of V2 was dropped because it proves only that the
  source compiled.
