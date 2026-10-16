<!-- cratis-ai-managed: skills/cratis-screenplay-discovery/references/product-sources.md -->
# Product sources

Sources for the facts discovery relies on.

| Fact used here | Source |
| --- | --- |
| Persona syntax, report-only, policies must be declared | Screenplay `Documentation/screenplay/personas.md` |
| Description order; authored/import board order and order-preserving MCP layouts | Screenplay `Documentation/screenplay/grammar.md`, `folders.md`, `imports.md` |
| Single-line and fenced `description` (`FencedText`) | `Documentation/screenplay/slices.md#descriptions`, `grammar.md` |
| `<Type>[]`, shapes must be declared; `pii` and `reason` on concepts | `Documentation/screenplay/events.md`, `concepts.md` |
| `then denied`, `given caller`, `then error` | `Documentation/screenplay/specifications.md` |
| PII on an event-source id is rejected by Chronicle (CHR0034); nullable event properties warn (CHR0012) | Chronicle `Source/Clients/DotNET.CodeAnalysis/DiagnosticIds.cs` |
| Bare `pii` and `secret` on a concept bind as ESM v13; legacy `@`-prefixed markers are deprecated (PLAY0565); enum/identity compliance fails binding (PLAY0268); current markers are in `cratis-screenplay-command-surface` | `Source/DotNET/Screenplay/Semantics/SemanticModelBinder.Concepts.cs` |

Every complete `screenplay` fence in this skill compiles with the standalone compiler
(`--warnaserror`).
