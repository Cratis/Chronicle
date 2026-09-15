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

## Every page is exactly one Diátaxis type

We organize documentation with the [Diátaxis framework](https://diataxis.fr/). Before writing, decide which of the four types a page is — and write *only* that type. Mixing types is the most common way docs fail: a tutorial padded with reference detail overwhelms the learner; a how-to interrupted by concept digressions stops being a quick recipe.

| Type | The reader is… | Reads like | Rule |
|---|---|---|---|
| **Tutorial** | learning by doing | a guided lesson | Steps that each produce a visible result. Do not explain *why* — just *do this, then this*. The reader must succeed even before they fully understand. |
| **How-to guide** | solving a specific problem | a recipe | Assume competence. Goal → prerequisites → steps → done. No teaching. |
| **Reference** | looking something up | a dictionary | Exhaustive and terse. Tables, signatures, attributes, configuration. No narrative. |
| **Explanation** | trying to understand | a discussion | Concepts, trade-offs, architecture, *why*. No steps. Lean on diagrams. |

Diátaxis governs a page's purpose and voice, not a universal set of sidebar labels. Each product has navigation buckets suited to its domain; read `PRODUCTS[].buckets` in the Documentation site's sync script before placing a new section.

For authoring a single page step by step, use the `write-documentation` skill.

## Onboarding is the most important documentation you write

Most readers decide whether to adopt Cratis in the first ten minutes. Protect that path.

- **One canonical getting-started per product**, not a menu of competing quickstarts. Host variants are how-to guides linked *from* the canonical path — never rival front doors.
- Drive to a **visible payoff fast** — something running the reader can see. State it up front: “By the end you'll have X running.”
- **One threaded tutorial per product** builds a single realistic domain across chapters, each adding one concept. Open every chapter with what the reader will build or learn and close with a recap. The reader finishes with a working application, not a pile of snippets.
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
- Focus on public APIs and behavior — never internal implementation or third-party libraries.

## Diagrams

- Use [Mermaid](https://mermaid-js.github.io/mermaid/#/) for every non-trivial concept — architecture, event and command flow, state transitions, projection and reactor pipelines. A concept page without a diagram is usually incomplete.

## Code examples

- Prefer `record` types for events, commands, and read models — match the codebase.
- Use argument-free `[EventType]` for new events. A new generation or an explicit legacy identifier is valid only when documenting evolution of an existing stored-event contract.
- Every example must be **complete and correct** — no pseudo-code, no `// ...` elisions that leave the reader guessing.
- **Short illustrative snippets** may be purpose-built. **Longer or real samples must be embedded from compiled, tested source** when snippet tooling is available, so they cannot drift as APIs change. Never paste untested code, and never substitute a bare “see the repository” link for showing the code.
- Where a feature spans products or languages, show both sides when both matter. Keep causal explanations sequential; use tabs only for alternatives.

## Links

- **Link text must describe the destination.** Write `[Event types](...)`, never `[see documentation](...)`, `[here](...)`, or `[click here](...)`. Non-descriptive link text is a defect.
- Use relative links for internal product-source references. Verify every link resolves — broken links and links to non-existent folders fail review.

## What every product's docs must have

- A front-door **index** with a one-sentence definition and a “start here” link.
- A **“Why <product>”** explanation page covering the problem it solves and when *not* to use it.
- A canonical **getting started** with a visible payoff.
- A **threaded tutorial**.
- A **concepts/glossary** page and an **architecture diagram**.
- A **troubleshooting/FAQ** page.
- An **`llms.txt`** and **`llms-full.txt`** output so AI assistants can ground answers in the docs.

## File rules

- Follow [Documentation Structure and Formatting](./documentation-structure-and-formatting.md) for frontmatter, landing pages, `toc.yml`, Markdown/MDX, links, and rendering.
- End every Markdown file with a single trailing newline.
- Run the owning repository's local documentation gate when present; use `cd ../Documentation/web && npm run check` for full-fidelity site verification when the sibling checkout is available.
