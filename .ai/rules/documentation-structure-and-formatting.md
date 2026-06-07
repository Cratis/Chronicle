---
applyTo: "**/Documentation/**/*.{md,mdx}"
paths:
  - "**/Documentation/**/*.md"
  - "**/Documentation/**/*.mdx"
---

# Documentation Structure & Formatting (so a page fits the site)

The mechanical conventions a page must follow to slot into the Astro Starlight site correctly. (For *what* to write — the tour voice — see [Writing Cratis Documentation](./writing-cratis-docs.md). For *where* a page lives — see [Editing Cratis Documentation](./editing-cratis-docs.md).) Product `.md` is run through `web/scripts/sync-content.mjs` before it reaches Starlight; this is what that converter expects and does.

## Frontmatter

```yaml
---
title: Append an event          # required-ish; becomes the page H1
description: One sentence …      # strongly recommended; SEO/meta + AI export
sidebar:                         # optional: order / label / badge
  order: 2
  badge: { text: New, variant: tip }
---
```

- The converter **keeps** `title`, `description`, `sidebar`; it **drops** DocFX keys (`uid`, `applyTo`, `storybook`, …). For product `.md` with no frontmatter, it derives a `title` from the first H1.
- **Starlight renders `title` as the page H1 — so do NOT put an H1 in the body.** Start the body at `##` (H2). A leading body H1 is stripped, but don't rely on that; structure from H2.

## Headings

- **No body H1** (the title is the H1).
- **The "On this page" ToC shows H2 only** (`tableOfContents: { min:2, max:2 }`). Structure each page as a flat list of `##` sections — sub-points go under them as `###`/prose, not as ToC entries.
- Use sentence case in headings; no trailing punctuation.

## File & folder layout

- Every folder has a **`toc.yml`** (navigation) and an **`index.md`** (landing). A folder whose landing is `overview.md` (no `index.md`) **404s on the bare URL** — give it an `index.md` or link to a specific page.
- A `toc.yml` entry pointing to a **missing page is dropped** from the sidebar (the sync prints "N broken toc entries dropped" — keep it 0).
- `toc.yml` sections are regrouped into Diátaxis **buckets** (Get started / Guides / Understand / Reference) via `PRODUCTS[].buckets` in `sync-content.mjs`. Sections named **"Getting started…"** auto-get a *Quickstart* badge; **"Tutorial"** gets a *Tutorial* badge.

## Callouts / asides

Write DocFX alerts in product `.md` (converted automatically) **or** Starlight directives directly:

| DocFX | Becomes |
|---|---|
| `> [!NOTE]` / `> [!IMPORTANT]` | `:::note` |
| `> [!TIP]` | `:::tip` |
| `> [!WARNING]` | `:::caution` |
| `> [!CAUTION]` | `:::danger` |

```
:::note
Body of the callout.
:::
```

In `.mdx` you may also use `<Aside type="tip">…</Aside>` (import from `@astrojs/starlight/components`).

## Code blocks

- **Always tag the language** for highlighting: ` ```csharp `, ` ```tsx `, ` ```bash `, ` ```yaml `, etc. DocFX-era custom fences (`env`, `pdl`, `ebnf`, `pql`, `gitignore`, `flow`) are aliased to plain text so they don't warn — but use a real language where one exists.
- **No spurious common leading indentation** — if a snippet is lifted from inside a method, dedent it to column 0 (the whole block shifted right reads as "indented"). Keep only the code's own internal indentation.
- Where a feature spans the stack, show **both sides** with `<FullStackTabs>` (synced C# ↔ generated TS) rather than backend-only.
- `[!INCLUDE [x](./x.md)]` is inlined; `[Snippet source](…)` links stay as-is.

## Tables, images, links, diagrams

- **Tables** are GitHub-Flavored Markdown (`| … |` with a `| --- |` separator row, blank line before). They render as real tables (the site tints the header). A table that shows as raw `|` pipes means GFM isn't applying — check the Documentation repo's site-rendering rule.
- **Images** use relative paths next to the page; they're click-to-zoom automatically. Give meaningful alt text.
- **Links:** product `.md` may use `./foo.md`; links to a `.mdx` page must be **extension-less** (`./foo`); site-level `.mdx` uses clean root-relative URLs (`/arc/…`). Cross-product links are root-relative. **Link text must describe the destination** — never `[here]` / `[see documentation]` (the gate fails on these).
- **Diagrams:** ` ```mermaid ` fences for any non-trivial concept (pre-rendered to SVG at build).

## `.mdx` specifics

- Frontmatter `title` + `description`, **no body H1**, then imports:
  ```mdx
  import { Steps, Tabs, TabItem, Aside } from '@astrojs/starlight/components';
  import { FullStackTabs, TopicHero, SimpleCard, StackDiagram } from '@components';
  ```
- Valid `<TabItem>` icons include `apple`, `linux`, `seti:c-sharp`, `seti:react`.
- The splash front door uses `template: splash` in frontmatter.

## File hygiene

- **End every file with a single trailing newline.**
- Don't use shell commands to modify a doc after writing it.
- Verify before done: `cd Documentation/web && npm run check` must end **0 error(s)** and **0 broken** links.
