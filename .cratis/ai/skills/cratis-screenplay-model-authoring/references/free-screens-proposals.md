<!-- cratis-ai-managed: skills/cratis-screenplay-model-authoring/references/free-screens-proposals.md -->
# Free screens through typed proposals

Read this when a proposal adds, changes or removes a `shape`, a screen declared outside a slice, a need, a slice's adoption of a screen or the provision that supplies a need. The syntax, severities and diagnostics are in `cratis-screenplay-ui-composition` ([free screens](../../cratis-screenplay-ui-composition/references/free-screens.md)); the design decisions are in `cratis-screenplay-slice-design`.

Requires a Screenplay release whose MCP `syntax-schema` lists `ShapeSyntax` and `UsesScreenSyntax` (check `screenplay --version`; discover the members before constructing typed input).

## The nodes

| Construct | `read-ast` kind | Edit it with |
| --- | --- | --- |
| A shape, the typed contract of a screen's data | `ShapeSyntax` | `add` a `PropertySyntax` to `properties`; `replace` a field's name or type; `remove` a field or the shape |
| A free screen at the top level, in a module or in a feature | `ScreenSyntax` | `add` to `screens` on an `ApplicationSyntax`, `ModuleSyntax` or `FeatureSyntax`; `replace` its directives; `remove` |
| A need a screen leaves to its adopters | `ScreenNeedSyntax` | `replace` the `type` or the `alias`; `add` to a screen's `directives`; `remove` |
| A slice's adoption of a screen | `UsesScreenSyntax` | `add` to `usedScreens` on a `SliceSyntax`; `replace` the `screen`; `remove` to stop adopting |
| The query that provides one need | `ScreenProvisionSyntax`, `ScreenProvisionMappingSyntax` | `add` to a `UsesScreenSyntax`'s `provisions` or a provision's `mappings`; `replace` the `need`, `query`, `by`, a mapping's `field` or `path`; `remove` |

Every member is omitted from the AST when empty, so a model with no free screens reads and writes the same AST as before. A screen a slice declares in its own body is a `ScreenSyntax` of that slice, as always.

## One coherent proposal

1. **Read first.** Read the existing shapes, screens and adoptions, and the target slice's read model and queries (`read-ast`, `declaration-details`, `find-references`), so a need is named after what exists and a provision targets a query that returns the right cardinality.
2. **Propose the whole change together:** the shape, the screen with its needs, the adoption with its provisions. A reviewer cannot judge a need without the provision that supplies it, and a half-proposal reports `PLAY1155` for the missing part.
3. **Use typed edits, not text.** `propose-ast` keeps identities and the proposal reports exact diagnostics; `propose-source` is for a whole-file rewrite. Never hand-edit a free screen's text to move or rename it.
4. **Read the diagnostics by code and typed `arguments`, never by message text.** `read-proposal` shows them before and after.
5. **Apply only the reviewed proposal** and fetch fresh handles afterwards: handles are revision-bound.

## What a proposal accepts and refuses

- A proposal that adopts a screen without providing every need is **accepted with warning** `PLAY1155`, as it is on disk. Report it as unfinished, not as done; only the executable-model export refuses it, with a reason naming the screen and the shape.
- A proposal that provides a need wrongly (an unknown need, a query of the wrong cardinality, a field the read model lacks, a type that differs from the explicit shape, a `by` the query does not declare) reports `PLAY1156` to `PLAY1163` and **cannot be applied** under `Authoring` validation. Repair the slice (read model, query, mapping), not the shape.
- A screen no slice adopts reports information `PLAY1164`: design-only, never an error and never in the executable model.
- A free screen's identity includes the module and feature it is declared in. Moving it to another scope changes its identity, so treat it as an identity-affecting edit and review the proposal's identity effects before applying.

## Verify and report

Run `screenplay <model> --warnaserror` on the applied model: it must report no unprovided need. Report separately what is only authored (a design-only screen, a screen whose slice does not compile to an executable model) and what is executable. Never drop a need or a provision to make a diagnostic disappear.
