---
applyTo: "**/Documentation/**/*.{md,mdx}"
paths:
  - "**/Documentation/**/*.md"
  - "**/Documentation/**/*.mdx"
---
<!-- cratis-ai-managed: rules/writing-cratis-docs.md -->

# Writing Cratis documentation — tour voice and Starlight authoring

The Cratis docs must **take the reader on a tour, like a teacher** — the way [Marten](https://martendb.io), [Wolverine](https://wolverinefx.net), and [aspire.dev](https://aspire.dev) docs do — **not** state facts like a reference dump. The differentiator is pedagogical structure, not decoration. Match it.

## The bar

- **Pain → relief.** Open by naming the friction the reader feels, then reveal the feature as the relief.
- **Why before how.** A reader who understands the reasoning handles edge cases the docs do not cover.
- **Active voice, present tense, second person.** “You append the event,” not “the event is appended.”
- **Be honest about limits.** A “when this is the wrong fit” section builds more trust than omitting the limits.

## One primary purpose per page

| Type | Reader is… | Reads like |
|---|---|---|
| **Tutorial** | learning by doing | a guided lesson — each step produces a visible result |
| **How-to** | solving a specific problem | a recipe — assume competence, no teaching |
| **Explanation** | trying to understand | a discussion — concepts, trade-offs, *why*, a diagram |
| **Reference** | looking something up | a dictionary — exhaustive, terse, tables/signatures |

Use the page's primary reader need to guide its structure, not to ban a brief prerequisite or explanation. A tutorial padded with a reference dump overwhelms; a how-to interrupted by a long conceptual detour stops being a recipe. Link out when the other material deserves sustained attention. Diátaxis does not imply a universal navigation bucket; bucket names are product-specific.

## The tour-voice checklist

Apply this checklist to tutorials, getting-started pages, and explanations:

1. **Open with a concrete scenario**, not a definition of the tool.
2. **Name the friction first**, then the feature as its relief.
3. **Make the first snippet the convention path.** Show the shape that works without registration or wiring a convention already handles (no manual `.AutoMap()`, no hand-registered handlers). A first example that configures what a convention does teaches the reader to distrust the convention.
4. **Use chronological verbs** such as define → append → project → query.
5. **After every code block, explain the invisible**: what happens under the hood and why it matters. Verify that explanation against the setup the reader actually built; a backend without Chronicle appends no events, so "the command appended an event" would be false there.
6. **Recap before pivoting** to the next concept.
7. **Anticipate a likely mistake** near the example it affects. Use a caution aside where the risk would otherwise be missed, such as a convention that fails silently (proxy generation, service lifetimes, AutoMap): show the working shape and a verified recovery path, and name affected versions only when the product source establishes them.
8. **Show the result** — output, a resulting model, or another visible success signal.
9. **Organize by workflow**, not alphabetically.
10. **End each substantial section with the natural next step** when one exists.

Read a current, well-reviewed tutorial in the product or a closely related product before writing; do not assume one product's domain vocabulary fits every other product.

## Use presentation to support the tour

Choose the simplest authoring surface that preserves the reading flow. Use steps for real procedures, tabs for genuine alternatives, asides for meaningful context or risk, and diagrams for non-trivial flows. Do not turn sequential cause-and-effect examples into tabs merely because they use different languages; hiding one side can make the explanation harder to follow.

Full-stack type safety is a differentiator, so show both the backend contract and generated frontend shape when both matter. Use `FullStackTabs` only when each pane remains understandable independently.

The raw Markdown mirror behind page actions such as “Copy Markdown” comes from synchronized Markdown/MDX rather than rendered HTML. Converter rewrites and normalized frontmatter are present, but component imports and JSX remain visible. Prefer plain Markdown unless a component adds real teaching value.

The exact Markdown/MDX boundary, aside semantics, component contracts, import paths, and rendering checks live in [Documentation Structure and Formatting](./documentation-structure-and-formatting.md). Do not duplicate or infer that rendering API here.

## Two voices, connected products

- **Two voices per area:** the toured/educational layer and the terse, exhaustive reference. Narrative pages link *down* into the reference; the reference stays a dictionary.
- **Connect at the seams** rather than re-explaining. Show how neighboring products meet in the user's workflow and link to the glossary for shared terms.
- **Coming-from-X bridges** map new concepts to what the reader already knows without organizing the whole product around a competitor. Give each the same shape: the reader's current code, the Cratis equivalent beside it, then a short "what changed and why" list. Write one page per source technology. Keep the other technology's code accurate at a named version, or label it illustrative; state the trade-offs fairly and say when the reader's current approach remains the better fit.

## Before you call a page done

- Verify every framework API in a code example against real source — see [Writing Correct Code Examples](./writing-correct-examples.md). Readers paste snippets verbatim.
- The owning repository's local documentation gate passes when one exists; when available, the sibling Documentation site's full check has zero hard lint errors and zero broken rendered links attributable to the change.
- For a visual page, preview it and inspect light **and** dark screenshots using the owning site's screenshot workflow.

Study the **aspire.dev** docs for strong Starlight information architecture and tour writing.

The edit/sync/verify loop and source ownership live in [Editing Cratis Documentation](./editing-cratis-docs.md).
