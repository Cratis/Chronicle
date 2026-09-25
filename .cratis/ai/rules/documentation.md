---
applyTo: "**/Documentation/**/*.{md,mdx}"
paths:
  - "**/Documentation/**/*.{md,mdx}"
---
<!-- cratis-ai-managed: rules/documentation.md -->

# How to write documentation

Documentation exists for one audience: **developers who need to use the framework** — not the team that built it. Write from the reader's perspective. They want to know *what this does*, *why they should care*, and *how to use it* — in that order.

Every page should answer: “If I were a developer encountering this concept for the first time, what would I need to understand to use it correctly?”

The site is built with [Astro Starlight](https://starlight.astro.build/). Documentation lives in the `Documentation/` folder of each product repository as [GitHub Flavored Markdown](https://github.github.com/gfm/); a converter synchronizes product `.md`/`.mdx` into the Starlight site, and all repositories are aggregated into one published site. Readers experience it as a single place — write for that whole, not for one repository in isolation. For the authoritative rendering contract, see [Documentation Structure and Formatting](./documentation-structure-and-formatting.md).

## Give each page one primary reader purpose

Use [Diátaxis](https://diataxis.fr/) as a compass: decide whether the reader is primarily learning, solving a task, looking up a contract, or understanding a concept. Brief context that helps someone complete a tutorial or how-to is welcome. Move an exhaustive lookup or a long conceptual detour to its own linked page; do not split a coherent reader task merely to keep categories pure.

| Type | The reader is… | Reads like | Rule |
|---|---|---|---|
| **Tutorial** | learning by doing | a guided lesson | Steps that each produce a visible result. Briefly explain the invisible effect after a step; link out for deeper theory rather than stopping the lesson. The reader must succeed even before they fully understand. |
| **How-to guide** | solving a specific problem | a recipe | Assume competence. Goal → prerequisites → steps → done. No teaching. |
| **Reference** | looking something up | a dictionary | Exhaustive and terse. Tables, signatures, attributes, configuration. No narrative. |
| **Explanation** | trying to understand | a discussion | Concepts, trade-offs, architecture, *why*. No steps. Lean on diagrams. |

Diátaxis governs a page's purpose and voice, not a universal set of sidebar labels. Each product has navigation buckets suited to its domain; read `PRODUCTS[].buckets` in the Documentation site's sync script before placing a new section.

For a reader-centered writing workflow, use the **cratis-documentation-writing** skill; for source-verified snippets use **cratis-technical-examples**.

## Onboarding is the most important documentation you write

Most readers decide whether to adopt Cratis in the first ten minutes. Protect that path.

- **One clear 'start here' entry per product**, not a menu of competing front doors. Let it direct readers to separate language or host procedures when those are genuinely different; do not force incompatible readers through one runnable recipe.
- Drive to a **visible payoff fast** — something running the reader can see. State it up front: “By the end you'll have X running.”
- **Thread tutorials when the product warrants a multi-chapter journey.** Use one realistic domain across chapters, each adding one concept and visible result. A small tool may need only a short guided lesson; don't create filler chapters to satisfy a template.
- **One cross-product capstone tutorial** builds a real full-stack feature using the relevant Cratis products together. This is the connective tissue between products — keep it current and runnable.

## Connect the products

Readers do not care about repository boundaries — they are building one application.

- The site has **one front door** stating what Cratis is, with a one-sentence definition and a “start here” link for each product.
- Every product index opens with a **one-sentence definition** and a **“without vs. with” framing** of the problem it removes — lead with the pain, then the relief.
- **Cross-link at the seams** rather than re-explaining: show how the products meet in the reader's workflow.
- Maintain a **glossary** of shared terms and link to it instead of redefining terms per page. One term, one concept, everywhere.

## Writing style

The project's voice is **direct, practical, and opinionated**. Write like an experienced colleague explaining something to a capable developer — confident but never condescending.

- **Active voice, present tense, second person.** “You append the event,” not “The event is appended.”
- **Lead with *why* before *how*.** A reader who understands the reasoning handles edge cases the docs do not cover.
- **Do not document the obvious.** If the API is self-explanatory, a complete code example is enough.
- Use headings, lists, tables, and code blocks — dense paragraphs lose readers.
- **Be honest about trade-offs.** A “when this is the wrong fit” section builds more trust than omitting the limits.
- Focus on public APIs and behavior. Explain internals only where they clarify a supported boundary or failure mode; link to third-party documentation rather than retelling it.

## Diagrams

- Use [Mermaid](https://mermaid-js.github.io/mermaid/#/) where a concept has structure that prose conveys poorly — architecture, event and command flow, state transitions, projection and reactor pipelines. Many strong pages carry their load in prose, real code, and real output instead; add a diagram because it explains something, not because the page type seems to require one.

## Code examples

- Prefer `record` types for events, commands, and read models — match the codebase.
- Use argument-free `[EventType]` for new events. A new generation or an explicit legacy identifier is valid only when documenting evolution of an existing stored-event contract.
- A standalone example must be **complete and correct** at its stated scope. Mark a partial excerpt as an excerpt, state its prerequisites, and never leave an essential step behind `// ...`. Do not offer an incomplete block as click-to-copy code.
- **Short illustrative snippets** may be purpose-built after checking their framework APIs against source. Derive substantial or runnable examples from compiled, tested sample or spec code; extract or check displayed snippets with the owning repository's tooling so they do not drift. Never substitute a bare “see the repository” link for showing the code.
- Where a feature spans products or languages, show both sides when both matter. Keep causal explanations sequential; use tabs only for alternatives.

## Links

- **Link text must describe the destination.** Write `[Event types](...)`, never `[see documentation](...)`, `[here](...)`, or `[click here](...)`. Non-descriptive link text is a defect.
- Use relative links for internal product-source references. Verify every link resolves — broken links and links to non-existent folders fail review.

## Cover the reader's needs at the product's scale

- Every navigable product needs an **index** that defines it and points to a first useful outcome.
- When a product has enough tutorials or how-to guides to warrant an index, list each by the **situation it solves**, with a one-line description (for example, "Dealing with concurrency" or "Multi-tenancy end to end"), not only by chapter number or API name. Don't add a landing page merely to satisfy this rule, or restructure navigation as a side effect of one page.
- Document the **diagnostic surface**: the CLI commands, MCP tools, and observable state that tell a reader why the product behaves as it does. A capability with no documented way to inspect it becomes a support request, and an AI assistant helping the reader cannot use it.
- Provide a **getting-started route**, exact reference, limitations, and recovery information where the product's complexity requires them. A small tool need not manufacture a multi-chapter tutorial, FAQ, glossary, or architecture diagram.
- Connect related products with links instead of duplicating the same explanation. Share task-area and product indexes through the site's AI-facing exports where useful; don't require a separate full-text download per product or claim that an export alone proves answer quality.

## File rules

- Follow [Documentation Structure and Formatting](./documentation-structure-and-formatting.md) for frontmatter, landing pages, `toc.yml`, Markdown/MDX, links, and rendering.
- End every Markdown file with a single trailing newline.
- Run the owning repository's local documentation gate when present; use `cd ../Documentation/web && npm run check` for full-fidelity site verification when the sibling checkout is available.
