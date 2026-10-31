<!-- cratis-ai-managed: skills/cratis-screenplay-model-authoring/references/layout.md -->
# Choosing a readable layout

This reference holds file-layout options and import-barrel rules for split models.

## Choose a readable layout

Use `recommend-layout` for size-admissible options, then review an `expand-layout`
proposal. Layout is not application semantics.

- Keep one `application.play` while it remains readable.
- Use one file per module when modules are simple.
- Use one file per feature when nested features need separate review.
- Use one file per slice when a large model needs isolated diffs.

Compose split files with quoted imports (`import "<path or glob>"`).
Each level above the slices is a barrel file: `application.play` holds `domain`
and imports `Shared/*.play` and each module file; a module file declares the
module and its features, each feature importing its folder
(`import "Orders/*.play"`); a slice file holds only its `slice`, because the
import places it in its feature. When you write or reorganize files yourself,
do not restate `module`/`feature` in a slice file, and do not leave a barrel
that declares a scope but imports nothing. `expand-layout` writes
import barrels and slice-only leaves, preserving sibling and interleaved member
order. It refuses a proposal that would change that order; without an ordering
root its review discloses the path-order fallback. When choosing file placement, read the
[language reference](language-reference.md) for placement rules.

Parent scaffolding carries no duplicated module forms or contributions. Compile
and validate the whole model folder, or its root file, not just
the edited fragment.
