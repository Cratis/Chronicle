---
applyTo: "Documentation/**/*.md"
---

# How to Write Documentation

Documentation exists for one audience: **developers who need to use the framework** — not the team that built it. Write from the reader's perspective. They want to know *what this does*, *why they should care*, and *how to use it* — in that order.

Every page should answer: "If I were a developer encountering this concept for the first time, what would I need to understand to use it correctly?"

The site is built with [DocFX](https://dotnet.github.io/docfx/) using [GitHub Flavored Markdown](https://github.github.com/gfm/). Documentation lives in the `Documentation/` folder of each product repository and is aggregated into one published site. Readers experience it as a single place — write for that whole, not for one repo in isolation.

## Every Page Is Exactly One Diátaxis Type

We organize documentation with the [Diátaxis framework](https://diataxis.fr/). Before writing, decide which of the four types a page is — and write *only* that type. Mixing types is the most common way docs fail: a tutorial padded with reference detail overwhelms the learner; a how-to interrupted by concept digressions stops being a quick recipe.

| Type | The reader is… | Reads like | Rule |
|---|---|---|---|
| **Tutorial** | learning by doing | a guided lesson | Steps that each produce a visible result. Don't explain *why* — just *do this, then this*. The reader must succeed even before they fully understand. |
| **How-to guide** | solving a specific problem | a recipe | Assume competence. Goal → prerequisites → steps → done. No teaching. |
| **Reference** | looking something up | a dictionary | Exhaustive and terse. Tables, signatures, attributes, configuration. No narrative. |
| **Explanation** | trying to understand | a discussion | Concepts, trade-offs, architecture, *why*. No steps. Lean on diagrams. |

The top-level navigation of **every product** mirrors these types in the same order, so the whole set feels like one product: **Get started** (tutorials) → **Guides** (how-to) → **Concepts** (explanation) → **Reference**. Use this structure for Chronicle, Arc, Components, and every other product — never invent a per-product layout or organize a section purely around internal technical concepts.

→ For authoring a single page step by step, use the **write-documentation** skill.

## Onboarding Is the Most Important Documentation You Write

Most readers decide whether to adopt Cratis in the first ten minutes. Protect that path.

- **One canonical getting-started per product**, not a menu of competing quickstarts. Host variants (ASP.NET Core, Worker, console) are how-to guides linked *from* the canonical path — never rival front doors.
- Drive to a **visible payoff fast** — something running the reader can see (a live-updating read model, generated proxies wired into a UI). State it up front: "By the end you'll have X running."
- **One threaded tutorial per product** builds a single realistic domain across chapters, each adding one concept. Open every chapter with "what you'll build/learn" and close with a recap. The reader finishes with a working application, not a pile of snippets.
- **One cross-product capstone tutorial** builds a real full-stack feature using Arc + Chronicle + Components together. This is the connective tissue between products — keep it current and runnable.

## Connect the Products

Readers don't care about repository boundaries — they're building one application.

- The site has **one front door** stating what Cratis is, with a one-sentence definition and a "start here" link for each product.
- Every product index opens with a **one-sentence definition** and a **"without vs. with" framing** of the problem it removes — lead with the pain, then the relief.
- **Cross-link at the seams** rather than re-explaining: a command (Arc) appends events (Chronicle); a query reads a projection (Chronicle) rendered by a component (Components).
- Maintain a **glossary** of shared terms (event, event source, stream, event log, projection, reducer, reactor, observer, read model, sink, consistency boundary, tenant) and link to it instead of redefining terms per page. One term, one concept, everywhere.

## Writing Style

The project's voice is **direct, practical, and opinionated**. Write like an experienced colleague explaining something to a capable developer — confident but never condescending.

- **Active voice, present tense, second person.** "You append the event" not "The event is appended by Chronicle."
- **Lead with *why* before *how*.** A reader who understands the reasoning handles edge cases the docs don't cover.
- **Don't document the obvious.** If the API is self-explanatory, a complete code example is enough.
- Use headings, lists, tables, and code blocks — dense paragraphs lose readers.
- **Be honest about trade-offs.** A "when this is the wrong fit" section builds more trust than omitting the limits.
- Focus on public APIs and behavior — never internal implementation or third-party libraries.

## Diagrams

- Use [Mermaid](https://mermaid-js.github.io/mermaid/#/) for every non-trivial concept — architecture, event and command flow, state transitions, projection and reactor pipelines. A concept page without a diagram is usually incomplete.

## Code Examples

- Prefer `record` types for events, commands, and read models — match the codebase.
- When specifying `[EventType]`, never add a name argument — just `[EventType]`.
- Every example must be **complete and correct** — no pseudo-code, no `// ...` elisions that leave the reader guessing.
- **Short illustrative snippets** may be purpose-built. **Longer or real samples must be embedded from compiled, tested source** (the Samples repo) via the snippet tooling, so they cannot drift as APIs change. Never paste untested code, and never substitute a bare "see the repo" link for showing the code.
- Where a feature spans the stack, show **both sides** — the C# slice and the generated TypeScript consumer — with tabs. Full-stack type safety is the story; show it.

## Links

- **Link text must describe the destination.** Write `[Event Types](...)`, never `[see documentation](...)`, `[here](...)`, or `[click here](...)`. Non-descriptive link text is a defect.
- Use relative links for internal references. Verify every link resolves — broken links and links to non-existent folders fail review.

## What Every Product's Docs Must Have

- A front-door **index** with a one-sentence definition and a "start here" link.
- A **"Why <product>"** explanation page covering the problem it solves and when *not* to use it.
- A canonical **getting-started** with a visible payoff.
- A **threaded tutorial**.
- A **concepts/glossary** page and an **architecture diagram**.
- A **troubleshooting / FAQ** page.
- An **`llms.txt`** (curated index) and **`llms-full.txt`** (full corpus) so AI assistants ground answers in the docs — these are first-class build outputs, not an afterthought.

## File Rules

- Every folder has a `toc.yml` for navigation and an `index.md` landing page. In `toc.yml`, link to a subfolder's `toc.yml`, not its `index.md`.
- End every markdown file with a single trailing newline.
- Never use shell commands to modify files after writing them.
- Run `verify-markdown.sh` in the `Documentation/` folder to validate links and formatting before considering a page done.
