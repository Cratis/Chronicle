---
name: add-cratis-docs-page
description: Use this skill when creating a NEW Cratis documentation page under `Documentation/**` (tutorial, how-to, explanation, reference, or recipe). Handles where the file goes (which product repo), sidebar wiring (toc.yml + Diátaxis bucket), and verifying it renders. Trigger on add/create/write a new docs page, document a new feature, or add a new section to a product's docs.
---

# Adding a new Cratis documentation page

> Scope this skill to new source files under `Documentation/**` in the product or contributing repo. Site-level pages in `Documentation/web` are owned by the Documentation repo.

The site builds its navigation from each product's `toc.yml`, regrouped into Diátaxis buckets. A new page must be created in the right repo AND wired into the nav, or it builds but is unreachable.

## 1. Decide the type and the home

- **Which product?** Put the file in that product's repo: `Chronicle/Documentation/`, `Arc/Documentation/`, `Components/Documentation/`, `cli/Documentation/`, `Fundamentals/Documentation/`. A cross-product / site-level page belongs in the Documentation repo's `web/src/content/docs/`, not in this product repo.
- **Which Diátaxis type?** It decides the bucket and the voice — write *only* that type:
  - **Tutorial** (Get started) — learning by doing, steps with visible results.
  - **How-to / Guide** (Guides) — a recipe for a specific task; assume competence.
  - **Explanation / Concept** (Understand) — the *why*, trade-offs, a diagram.
  - **Reference** — exhaustive, terse, tables/signatures.

## 2. Create the file

- Author in the **tour voice** (open with a scenario, name the friction, narrate the code, show the result, forward-link at the end). The Chronicle tutorial (`Chronicle/Documentation/tutorial/*`) is the reference voice.
- **Format it to fit the site** — frontmatter (`title` + `description`, **no body H1**), H2-only structure, `:::note` asides, language-tagged code fences, GFM tables. The full spec is the **`documentation-structure-and-formatting`** rule.
- **`.mdx`** unlocks `<Steps>`, `<Tabs>`, `<Aside>` (from `@astrojs/starlight/components`) and the shared `@components` (`FullStackTabs`, `TopicHero`, `SimpleCard`, `StackDiagram`).
- Add a **Mermaid diagram** for any non-trivial concept (they're pre-rendered to SVG at build time).
- Verify every framework API in code examples against real source (the `writing-correct-examples` rule).

## 3. Wire it into the nav

- **Product page:** add the entry to that repo's `Documentation/toc.yml`, then add the entry's `name` to the correct bucket's `sections` in `Documentation/web/scripts/sync-content.mjs` (`PRODUCTS[].buckets`). Buckets order: **Get started → Guides → Understand → Reference**.
- **Site-level page:** use the Documentation repo copy of this skill; site-level pages are wired in `astro.config.mjs` there.
- **Links:** to a `.mdx` page use an **extension-less** link; `.md` keeps the extension; cross-product links are root-relative (`/chronicle/...`).

## 4. Verify

```bash
cd Documentation/web && npm run check
```

Must end **0 error(s)** and **0 broken** links, and your page must appear in the nav (the converter drops `toc.yml` entries whose slug doesn't match a built page — check the sync output for "broken toc entries dropped"). Preview at http://localhost:4321 (`npm run dev`); screenshot it with the `qa-cratis-docs` skill. Commit in the owning product repo (+ the site repo if you touched buckets/`astro.config`).

→ To edit an existing page instead, use `edit-cratis-docs`. The content craft — tour voice + how to use Starlight's authoring tools (`<Steps>`, `<Tabs>`, `<FullStackTabs>`, diagrams) — is the **`writing-cratis-docs`** rule. Read it before writing.
