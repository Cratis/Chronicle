---
name: edit-cratis-docs
description: Use this skill when changing, fixing, or improving Cratis documentation whose source file is under `Documentation/**` in a product or contributing repo. The content is split across repos (each product owns its docs in its own `Documentation/` folder; the `Documentation` repo aggregates them), so this skill finds the real source file, edits it, syncs, and verifies. Trigger whenever someone asks to edit/update/fix/reword a docs page, fix a broken link, correct a code example, update a guide or tutorial or reference page, or says a docs page is wrong/outdated — for Chronicle, Arc, Components, CLI, Fundamentals, or the contributing docs.
---

# Editing a Cratis documentation page

> Scope this skill to source files under `Documentation/**` in the product or contributing repo. Site-level pages in `Documentation/web` are owned by the Documentation repo.

Cratis docs content lives in **each product's own repo** under that repo's `Documentation/` folder; the published site (in the `Documentation` repo at `Documentation/web/`) aggregates them. Editing the wrong copy wastes the work — the per-product folders under `web/src/content/docs/` are **generated and overwritten**.

## 1. Find the source of truth

Map the page URL to its owning repo:

| URL | Source file |
|---|---|
| `/chronicle/**` | `Chronicle/Documentation/**` |
| `/arc/**` | `Arc/Documentation/**` (the `ApplicationModel` repo, cloned as `Arc`) |
| `/components/**` | `Components/Documentation/**` |
| `/cli/**`, `/fundamentals/**`, `/contributing/**` | the matching repo's `Documentation/` |
| `/`, `/why-cratis`, `/cratis-stack`, `/glossary`, `/comparisons/**`, `/adopting-cratis`, … | Site-level pages: use the Documentation repo; source lives in `Documentation/web/src/content/docs/*.{md,mdx}`. |

Example: `/chronicle/concepts/event-source/` → `Chronicle/Documentation/concepts/event-source.md`. If unsure, `grep -rl "<a distinctive sentence>" */Documentation Documentation/web/src/content/docs/*.md*`.

**Never edit `Documentation/web/src/content/docs/{chronicle,arc,components,cli,fundamentals,contributing}/`** — generated and git-ignored.

## 2. Edit the source

- Match the page's **Diátaxis type** (tutorial / how-to / explanation / reference) and the **tour voice** (teach, don't dump) — see the **`writing-cratis-docs`** rule (the tour-voice checklist + Starlight authoring tools). Don't mix types.
- **Verify every framework API in a code example against real source** before writing it — readers paste them verbatim. (See the `writing-correct-examples` rule; grep Studio `*.cs`/`*.tsx` and the product `Source/` trees.)
- Link rules: product `.md` may use `./foo.md`; links to a `.mdx` page must be **extension-less** (`./foo`); site-level `.mdx` uses clean root-relative URLs (`/arc/...`).

## 3. Sync, preview, verify

```bash
cd Documentation/web
npm run dev      # serves http://localhost:4321 (re-syncs the product repos)
npm run check    # the gate: build + lint + link-check
```

`npm run check` MUST end **0 error(s)** and **0 broken** links (≈187 advisory style warnings are expected). Fix anything it flags. **Restart `npm run dev` after running the gate** — the gate's re-sync degrades a live dev server.

For visual changes, screenshot the page in light and dark and read the result — use the `qa-cratis-docs` skill.

## 4. Commit

Commit the change in the **product repo** that owns the page (the site repo only changes if you touched a site-level page, the nav buckets in `sync-content.mjs`, or the build). Keep commits to one logical unit; don't push without explicit approval.

→ Site build and rendering internals live in the Documentation repo. To create a *new* page (not edit an existing one), use the `add-cratis-docs-page` skill.
