---
applyTo: "web/**"
paths:
  - "web/**"
---

# The Astro Starlight Docs Site

The published docs site lives in `Documentation/web/` — an **Astro Starlight** app that aggregates every product's docs into one site. Read this before changing anything in `web/`.

## Content is converted, not owned

**Source of truth = each product repo's `Documentation/` folder** (cloned as a sibling: `<parent>/{Chronicle,Arc,Components,Fundamentals,cli,.github}`). At build time `web/scripts/sync-content.mjs` converts that DocFX-style Markdown into Starlight content under `web/src/content/docs/<product>/`.

- **Never edit `web/src/content/docs/{chronicle,arc,components,cli,fundamentals,contributing}/`** — it's generated and git-ignored. Edit the product-repo source and re-sync (`npm run sync`).
- **Site-level pages** (owned by no product) ARE authored directly in `web/src/content/docs/` as `.mdx`: `index.mdx` (splash front door), `why-cratis.mdx`, `cratis-stack.mdx`, `adopting-cratis.mdx`, `ai-native-development.mdx`, `studio.mdx`, `comparisons/*`, etc.

## Navigation

Per-product `toc.yml` → Diátaxis **buckets** (defined in `sync-content.mjs`'s `PRODUCTS[].buckets`) → emitted to **`src/generated/topics.json`**, which `astro.config.mjs` imports. **`topics.json` is the real sidebar source — NOT the stale `src/generated/sidebar.json`** (don't be fooled inspecting it). To add a product page: create it in the product repo, add a `toc.yml` entry, and add that entry's `name` to the right bucket's `sections` array in `sync-content.mjs`. Site-level pages are wired in `astro.config.mjs` (`overviewTopic.items` + the `overview` glob).

## `.mdx` and links

The converter **keeps `.mdx`** (sync-content.mjs ~line 304). Author rich pages as `.mdx`: frontmatter `title`+`description`, no body H1, import `{ Steps, Tabs, TabItem, Aside }` from `@astrojs/starlight/components` and shared components from `@components` (`FullStackTabs` = synced C#/TS tabs, `TopicHero`, `SimpleCard`, `StackDiagram`). When renaming `.md`→`.mdx`, update the `toc.yml` href too.

- **Intra-doc links to a `.mdx` page must be EXTENSION-LESS** (`./validation`, not `./validation.mdx`) — the converter's slugify strips the dot and breaks the link. Links to `.md` keep the extension fine (the converter strips `.md`).
- Site-level `.mdx` pages do NOT go through the converter — use clean root-relative URLs (`/arc/...`), never `.md`.
- Astro slug rules (in `slugify`): lowercase, **strip dots**, keep `-`/`_`. Cross-product links are root-relative `/chronicle/...`.

## The QA gate

`npm run check` = `build` + `lint-docs` (fails on non-descriptive link text + leftover DocFX-isms; advisory Google/MS style warnings) + `check-links` (HARD gate: every internal link resolves) + `lint-prose` (Vale, graceful) + `lint-markdown` (markdownlint, graceful) + `check-external` (lychee, graceful). The three optional linters **skip when the tool isn't installed** (install in CI / locally to activate). Must end **0 error(s)** and **0 broken**. Run it after every change.

## Related rules

- **Where content lives + the edit→sync→verify loop across repos:** [Editing Cratis Documentation](./editing-cratis-docs.md).
- **Mermaid build-time pre-rendering, fonts, GFM tables, and headless visual QA:** [Documentation Rendering & QA](./documentation-rendering-and-qa.md).

## Gotchas (hard-won)

- **A running `npm run dev` degrades** (every page 500s / GFM tables stop rendering / `RenderError: slug "…" does not exist`) after a long session, or when **`npm run check`/`build` runs against the same tree while dev is live** (the build re-syncs content dev is watching). Fix: **restart `npm run dev`**. Re-verify a fresh dev server before trusting any "X doesn't render".
- **Stale Astro content cache** (`.astro/`, `node_modules/.astro`) silently serves a PARTIAL prior render. If a change won't take, `rm -rf .astro node_modules/.astro` and rebuild.
- Sections whose landing is `overview.md` (no `index.md`) **404 on the bare URL** (e.g. `/arc/backend/tenancy/`) — link to a specific page (`/arc/backend/tenancy/overview/`).
- **`SimpleCard`/`TopicHero` link props are JSX attributes — `check-links` does NOT validate them.** Verify card `link="…"` targets by hand.
- Mermaid diagrams are **pre-rendered to SVG at build time** (see the rendering rule); the front door splash diagram renders fine. `git reset --hard` is blocked by the harness — use `git revert` to undo a commit.
- Commit completed, green, logical units locally; **don't push or open PRs without explicit approval**.
- Verify a file with a fresh `Read` immediately before an `Edit`; a failed Edit means your read was stale — re-Read, don't force it.
