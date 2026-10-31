<!-- cratis-ai-managed: skills/cratis-screenplay-legacy-extraction/references/verified-sources.md -->
# Where the product facts come from

Check a fact against the released source before relying on it. Repositories live under the
organization's GitHub account; read the file at the tag of the tool you run
(`cratis-screenplay-toolchain` `SKILL.md` lists the pins).

| Fact used here | Repository and path |
| --- | --- |
| `generate` options, stdout when `--file` is absent, spec-project exclusion by name, the `CLI*` diagnostics | `cli`: `Source/Cli/Commands/Screenplay/` (`GenerateScreenplayCommand`, `GenerateScreenplaySettings`, `ScreenplayProjectSelection`, `ScreenplayDiagnosticCodes`) |
| Generator diagnostics (`SP0016` condition lost, `SP0024` source did not compile) | `Arc`: `Source/DotNET/Screenplay/ScreenplayDiagnosticCodes.cs`, `Documentation/backend/csharp/generating-a-screenplay.md` |
| Generated strategies, parameters, pins and the loss inventory; enum and response recovery; compliance and self-check | `Arc`: `Source/DotNET/Screenplay/Emission/`; summarized in [generated recovery](generated-recovery.md) and [generate notes](generate-notes.md) |
| `prologue start` is an interactive wizard that writes `cratis-prologue.json`; `interpret --no-llm` never creates a chat client; a local `llm.enabled` setting wins over the global configuration; the provider is announced before evidence is sent | `cli`: `Source/Cli/Commands/Prologue/` (`StartPrologueCommand`, `InterpretPrologueSettings`, `InterpretPrologueCommand`, `LlmOptionsResolver`), `Documentation/reference/prologue.md` |
| `generate` and `conform` evaluate the project with MSBuild (a design-time build that may run project targets and write `obj/`); `conform` exits 0, 1 or 2 and is structural only | `cli`: `Documentation/reference/screenplay.md`, `Source/Cli/Commands/Screenplay/ConformScreenplayCommand.cs` |
| Extractor effects (CDC enable, publication and replication slot), POST, PUT and DELETE only, the correlation window, the interpret payload and its caps, reuse of an existing slot or publication | `Prologue`: `Source/Extractor/Sources/`, `Source/Configuration/`, `Source/Interpretation/EvidenceFormatter.cs` |
| `then error`, `then denied`, `unique ... released by` | `Screenplay`: `Documentation/screenplay/specifications.md`, `constraints.md` |
| Model notes: anchors, kinds, the committed `.screenplay/notes.json` sidecar, the `--open-questions` gate | `Screenplay`: `Documentation/screenplay/model-notes.md`, `Documentation/screenplay/mcp/reference.md` |
| `semantic-diff` compares canonical workspace exports; structural comparison is not execution equivalence | `Screenplay`: `Documentation/screenplay/mcp/reference.md` |

Every complete `screenplay` fence in this skill compiles with the pinned standalone compiler
(`--warnaserror`) unless its fence says otherwise.
