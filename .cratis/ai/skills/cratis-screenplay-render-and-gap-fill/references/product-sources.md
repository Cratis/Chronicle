<!-- cratis-ai-managed: skills/cratis-screenplay-render-and-gap-fill/references/product-sources.md -->
# Product sources

Sources behind the render and gap-fill claims.

Tool versions are covered in `cratis-screenplay-toolchain` `SKILL.md`; read it before
selecting the compiler and renderer for delivery: the CLI bundles its own Screenplay and Stage;
its compiler version differs from the standalone pin. Render options (`--scope`,
`--check`, `--package-set`) and exit codes: cli `Documentation/reference/screenplay.md`
and `cratis render --help`; admission: Stage `SemanticVersionFeatures.cs`,
`SemanticVersionFeatureAdmission.cs`, `SemanticCratisAdmission.Automation.cs`. Stage's scaffold pins Arc and
Chronicle releases, which are not necessarily the latest (`cratis-stage-rendering-and-sandbox`
`references/rendered-application.md`). Source reading is not a new render run.

The worked example (`references/worked-example.md`) was probed with the public CLI (observed). The
canonical `ScreenComposition` corpus was rendered and published successfully, and an MCP-edited copy
was rendered, run and checked in the browser. The AI
repository's `screenplay-mcp-transcript.ts` harness proves the real stdio proposal/apply path against the
packaged CLI. Guarded Close actions and double-click interactions are refused by an older Stage
(`STAGE-SCENE-ACTION-001`, `STAGE-SCENE-INTERACTION-001`); a current Stage renders and runs them, and the codes
remain for a guard Stage cannot evaluate, which is never emulated. UI-profile
rejection (`CLI-RENDER-007` to `-010`) and render-then-recover are CLI behavior. The
renderer facts are owned by `cratis-stage-rendering-and-sandbox`; this skill links them and never
restates the admission table.
