---
applyTo: "**/Documentation/**/*.{md,mdx}"
paths:
  - "**/Documentation/**/*.{md,mdx}"
---
<!-- cratis-ai-managed: harnesses/copilot/instructions/documentation.instructions.md -->
<!-- cratis-ai: generated Copilot instructions. Do not edit; rules/ is the source. -->

# How to write documentation

Documentation exists for one audience: **developers who need to use the framework** — not the team that built it. Write from the reader's perspective. They want to know *what this does*, *why they should care*, and *how to use it* — in that order.

Every page should answer: “If I were a developer encountering this concept for the first time, what would I need to understand to use it correctly?”

## Name the Platform and its components accurately

In reader-facing documentation and discovery, **Cratis** is the organization and
**Cratis Platform** is the umbrella offering. Chronicle, Arc, Components,
Screenplay, Studio, CLI, and other named tools are components or supporting tools
with distinct roles, not competing platforms. When a reader or search query asks
about a Cratis framework, route to the relevant Platform component without
pretending its name, API or package identifier changed.

Readers may adopt one component independently or combine several. Never imply
that a component requires the whole Platform. For a cross-component task, name
which component owns each responsibility and show the handoffs, prerequisites
and result instead of using the umbrella name as an explanation. Preserve
verified product terminology, literal package IDs and API names.

Use authoritative source for capability, compatibility, dependency and maturity
claims; label unknown pairings as unverified. Do not infer support from branding
or navigation, and do not invent guarantees, performance or production-readiness
claims. Shared overview pages should link to component-owned procedures rather
than duplicate their setup.

The site is built with [Astro Starlight](https://starlight.astro.build/). Documentation lives in the `Documentation/` folder of each product repository as [GitHub Flavored Markdown](https://github.github.com/gfm/); a converter synchronizes product `.md`/`.mdx` into the Starlight site, and all repositories are aggregated into one published site. Readers experience it as a single place — write for that whole, not for one repository in isolation. For the authoritative rendering contract, see [Documentation Structure and Formatting](./documentation-structure-and-formatting.instructions.md).

## Give each page one primary reader purpose

Use [Diátaxis](https://diataxis.fr/) as a compass: decide whether the reader is primarily learning, solving a task, looking up a contract, or understanding a concept. Brief context that helps someone complete a tutorial or how-to is welcome. Move an exhaustive lookup or a long conceptual detour to its own linked page; do not split a coherent reader task merely to keep categories pure.

| Type | The reader is… | Reads like | Rule |
|---|---|---|---|
| **Tutorial** | learning by doing | a guided lesson | Steps that each produce a visible result. Briefly explain the invisible effect after a step; link out for deeper theory rather than stopping the lesson. The reader must succeed even before they fully understand. |
| **How-to guide** | solving a specific problem | a recipe | Assume competence. Goal → prerequisites → steps → completion check. Explain necessary concepts, effects and recovery beside the step; link sustained teaching elsewhere. |
| **Reference** | looking something up | a dictionary | Exhaustive and terse. Tables, signatures, attributes, configuration; concise explanation where a default or interaction would otherwise surprise the reader. |
| **Explanation** | trying to understand | a discussion | Concepts, trade-offs, architecture, *why*. A short example or sequence can clarify an effect without becoming a full procedure. Use diagrams when they explain a relationship. |

Diátaxis governs a page's purpose and voice, not a universal set of sidebar labels. Each product has navigation buckets suited to its domain; read `PRODUCTS[].buckets` in the Documentation site's sync script before placing a new section.

For a reader-centered writing workflow, use **cratis-documentation-writing**; for source-verified snippets use **cratis-technical-examples**. Before drafting or revising an explanation beside code, read and apply [Explaining code](../../../skills/cratis-engineering-docs-authoring/references/explaining-code.md): develop the point from the example instead of turning source-checking questions into public prose.

## Onboarding is the most important documentation you write

Most readers decide whether to adopt Cratis in the first ten minutes. Protect that path.

- **One clear 'start here' entry per product**, not a menu of competing front doors. Let it direct readers to separate language or host procedures when those are genuinely different; do not force incompatible readers through one runnable recipe.
- Drive to a **visible payoff fast** — something running the reader can see. State it up front: “By the end you'll have X running.”
- **Thread tutorials when the product warrants a multi-chapter journey.** Use one realistic domain across chapters, each adding one concept and visible result. A small tool may need only a short guided lesson; don't create filler chapters to satisfy a template.
- **One cross-product capstone tutorial** builds a real full-stack feature using the relevant Cratis products together. This is the connective tissue between products — keep it current and runnable.

## Connect the products

Readers do not care about repository boundaries — they are building one application.

- The site has **one front door** stating what Cratis is, with a one-sentence definition and a “start here” link for each product.
- Every product index makes the product's purpose clear near the beginning. Use a **one-sentence definition** or **“without vs. with” framing** where it helps the reader choose; do not force one opening or sequence onto every product.
- **Cross-link at the seams** rather than re-explaining: show how the products meet in the reader's workflow. Before a procedure uses another product's command, link its installation procedure as a prerequisite instead of assuming the command is already available. Prefer the canonical procedure over copying installation commands that can drift.
- Maintain a **glossary** of shared terms and link to it instead of redefining terms per page. One term, one concept, everywhere.

## Writing style

The project's voice is **direct, practical, and opinionated**. Write like an experienced colleague explaining something to a capable developer — confident but never condescending.

- **Active voice, present tense.** Address the reader when giving an action; let the API or component be the subject when it makes a contract clearer.
- Explain the reason where it helps the reader choose or understand an effect. A lookup can open directly with its contract.
- **Do not document the obvious.** If the API is self-explanatory, a complete code example is enough.
- Use connected paragraphs to develop an explanation, headings to locate it, and lists or tables for real steps and comparisons.
- **Be honest about trade-offs.** Put a limit where it changes the reader's action or expectation, before risk when relevant; a separate wrong-fit section is useful only when it has something to explain.
- Focus on public APIs and behavior. Explain internals only where they clarify a supported boundary or failure mode; link to third-party documentation rather than retelling it.

## Diagrams

- Use [Mermaid](https://mermaid-js.github.io/mermaid/#/) where a concept has structure that prose conveys poorly — architecture, event and command flow, state transitions, projection and reactor pipelines. Many strong pages carry their load in prose, real code, and real output instead; add a diagram because it explains something, not because the page type seems to require one.

## Code examples

- Prefer `record` types for events, commands, and read models — match the codebase.
- Use argument-free `[EventType]` for new events. A new generation or an explicit legacy identifier is valid only when documenting evolution of an existing stored-event contract.
- A standalone example must be **complete and correct** at its stated scope. Mark a partial excerpt as an excerpt, state its prerequisites, and never leave an essential step behind `// ...`. Do not offer an incomplete block as click-to-copy code.
- **State version requirements, not the test log.** Name minimum, fixed or breaking releases and meaningful compatibility or peer ranges. Ordinary installation follows the supported latest-release path, not universal exact-patch pins; latest does not imply every package combination is compatible. Keep resolved versions in maintained manifests/lockfiles, or pin deliberately reproducible tutorials when needed. Preserve required security/deployment constraints and production image policy. Do not end a page with verification provenance (SDK build, image digest or SHA, date, "tested with", unrun samples); that belongs in the change summary. See **cratis-technical-examples**.
- **Short illustrative snippets** may be purpose-built after checking their framework APIs against source. Derive substantial or runnable examples from compiled, tested sample or spec code; extract or check displayed snippets with the owning repository's tooling so they do not drift. Never substitute a bare “see the repository” link for showing the code.
- Where a feature spans products or languages, show both sides when both matter. Keep sequential backend-producer/frontend-consumer examples together. Tabs are alternatives for the same reader task, not evidence of client parity; apply the multi-client guidance in [Explaining code](../../../skills/cratis-engineering-docs-authoring/references/explaining-code.md) before choosing that shape.

## Links

- **Link text must describe the destination.** Write `[Event types](...)`, never `[see documentation](...)`, `[here](...)`, or `[click here](...)`. Non-descriptive link text is a defect.
- Named Markdown reference links (`[Install the CLI][cli-installation]` with a same-page `[cli-installation]: /cli/getting-started/` definition) can make cross-product prerequisites readable and reusable. These are page-local labels, not DocFX symbols; verify the target route in the aggregated site.
- Use relative links for internal product-source references and root-relative site routes (`/arc/backend/commands/`) for another product's pages — not absolute `cratis.io` or GitHub URLs, and not `xref:` symbol links, which the site does not support. Verify every link resolves — broken links and links to non-existent folders fail review.

## Cover the reader's needs at the product's scale

- Every navigable product needs an **index** that defines it and points to a first useful outcome.
- When a product has enough tutorials or how-to guides to warrant an index, list each by the **situation it solves**, with a one-line description (for example, "Dealing with concurrency" or "Multi-tenancy end to end"), not only by chapter number or API name. Don't add a landing page merely to satisfy this rule, or restructure navigation as a side effect of one page.
- Document the **diagnostic surface**: the CLI commands, MCP tools, and observable state that tell a reader why the product behaves as it does. A capability with no documented way to inspect it becomes a support request, and an AI assistant helping the reader cannot use it.
- Provide a **getting-started route**, exact reference, limitations, and recovery information where the product's complexity requires them. A small tool need not manufacture a multi-chapter tutorial, FAQ, glossary, or architecture diagram.
- Connect related products with links instead of duplicating the same explanation. Share task-area and product indexes through the site's AI-facing exports where useful; don't require a separate full-text download per product or claim that an export alone proves answer quality.

## File rules

- Follow [Documentation Structure and Formatting](./documentation-structure-and-formatting.instructions.md) for frontmatter, landing pages, `toc.yml`, Markdown/MDX, links, and rendering.
- End every Markdown file with a single trailing newline.
- Run the owning repository's local documentation gate when present; use `cd ../Documentation/web && npm run check` for full-fidelity site verification when the sibling checkout is available.
- For a new or materially changed reader journey, use the **cratis-documentation-writing** [fresh-context reader review](../../../skills/cratis-documentation-writing/references/reader-review.md). Report unavailable review rather than calling an author's reread independent evidence; a passing build is not a comprehension check.
