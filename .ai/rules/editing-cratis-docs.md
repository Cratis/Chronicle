---
applyTo: "**/Documentation/**/*.{md,mdx}"
paths:
  - "**/Documentation/**/*.md"
  - "**/Documentation/**/*.mdx"
---

# Editing Cratis Documentation (the content is split across repos)

Cratis documentation is **not** one folder. The content lives in **each product's own repo** under that repo's `Documentation/` folder, and a single published site (in the **`Documentation` repo**, at `Documentation/web/`) aggregates them all at build time. Know where a page actually lives before you edit it, or you'll edit a generated copy that gets overwritten.

## Where each page actually lives

| Page | Source of truth |
|---|---|
| `/chronicle/**` | `Chronicle/Documentation/**` |
| `/arc/**` | `Arc/Documentation/**` (the `ApplicationModel` repo, cloned as `Arc`) |
| `/components/**` | `Components/Documentation/**` |
| `/cli/**`, `/fundamentals/**`, `/contributing/**` | the matching repo's `Documentation/` |
| Site-level pages (`/`, `/why-cratis`, `/cratis-stack`, `/glossary`, `/comparisons/**`, …) | `Documentation/web/src/content/docs/*.{md,mdx}` (authored directly in the site) |

**Map a URL to a file:** `/chronicle/concepts/event-source/` → `Chronicle/Documentation/concepts/event-source.md`. The site reads each product repo as a **sibling clone** (`<parent>/{Documentation,Chronicle,Arc,Components,Fundamentals,cli,.github}`), all on the same branch.

## The one rule that prevents wasted work

**NEVER edit `Documentation/web/src/content/docs/{chronicle,arc,components,cli,fundamentals,contributing}/`.** Those are **generated and git-ignored** — `web/scripts/sync-content.mjs` regenerates them from the product repos on every build. Edit the **product-repo source** and re-sync. (Site-level pages directly under `web/src/content/docs/` *are* hand-authored — only the per-product subfolders are generated.)

## The loop

1. **Edit** the source `.md`/`.mdx` in the owning product repo (or the site-level page in `web/src/content/docs/`).
2. **Sync + preview:** from `Documentation/web`, `npm run dev` (runs the sync, serves http://localhost:4321). The sync also runs automatically in `predev`/`prebuild`.
3. **Verify:** `npm run check` — the full gate. It MUST end **0 error(s)** and **0 broken** links (≈187 advisory style warnings are expected and fine). Run it after every change.
4. Commit the change in the **product repo** (content) — the site repo only changes if you touched site-level pages, nav buckets, or the build.

## Adding or moving a page

- Create the file in the product repo's `Documentation/`, add a `toc.yml` entry, and add that entry's `name` to the right Diátaxis **bucket** in `web/scripts/sync-content.mjs` (`PRODUCTS[].buckets`). Site-level pages are wired in `astro.config.mjs` instead.
- Buckets are the Diátaxis order: **Get started** (tutorials) → **Guides** (how-to) → **Understand** (explanation) → **Reference**. Keep every product on this shape.

## Links (the rules differ by file type)

- **Product `.md`** goes through the converter — `[x](./foo.md)` is fine (it strips `.md`).
- **Intra-doc links to a `.mdx` page must be EXTENSION-LESS** (`./validation`, not `./validation.mdx`) — slugify strips the dot and breaks `.mdx`.
- **Site-level `.mdx`** does NOT go through the converter — use clean root-relative URLs (`/arc/...`), never `.md`.
- Cross-product links are root-relative: `/chronicle/...`. Sections whose landing is `overview.md` (no `index.md`) 404 on the bare URL — link to a specific page.

## Hard gotchas (these will bite you)

- **A long-running `npm run dev` degrades** (every page 500s / tables stop rendering) — and **running `npm run check`/`build` while dev is live** corrupts it (the build re-syncs content the dev server is watching). Fix: **restart `npm run dev`** (kill `lsof -ti tcp:4321`). Re-verify a fresh dev server before trusting any "X doesn't render".
- **The Astro content cache (`.astro/`, `node_modules/.astro`) silently serves a PARTIAL prior render** between iterations. If a change "didn't take" or a build looks half-done, `rm -rf .astro node_modules/.astro` and rebuild.
- **Code examples are copied verbatim by readers** — verify every framework API against real source before writing it. See [Writing Correct Code Examples](./writing-correct-examples.md).
- Match the **tour voice** (teach, don't dump) and the right **Diátaxis** type — see [Writing Cratis Documentation](./writing-cratis-docs.md).

→ Rendering pipeline (Mermaid, fonts, tables) + visual QA: [Documentation Rendering & QA](./documentation-rendering-and-qa.md). Site build internals live in the Documentation repo.
