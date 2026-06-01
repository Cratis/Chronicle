---
name: qa-cratis-docs
description: Use this skill to visually QA the Cratis documentation site — screenshot pages headless in light AND dark, evaluate how they look against the aspire.dev bar, check diagrams/tables/code blocks render, and diagnose layout-shift ("flicker"/"twitch"/"pop") bugs. Trigger when someone asks to screenshot the docs, check how a docs page looks, review the docs visually, verify a diagram or table renders, compare the docs to aspire.dev, or investigate a flicker/jump/layout-shift on the docs site.
---

# Visual & layout QA for the docs site

`shot-scraper`/Playwright aren't dependencies, but Chrome is. Use the committed `Documentation/web/scripts/screenshot.mjs` (drives system Chrome over CDP) — it captures **light or dark**, full-page, with client-side rendering settled.

## Capture

```bash
cd Documentation/web && npm run dev        # serve at http://localhost:4321 (keep it running)
node scripts/screenshot.mjs http://localhost:4321/chronicle/concepts/event-source/ /tmp/es-dark.png dark
node scripts/screenshot.mjs http://localhost:4321/chronicle/concepts/event-source/ /tmp/es-light.png light
```

Then **Read the PNG** to evaluate it. Crop/zoom with the `sharp` already in `node_modules` (PIL/ImageMagick aren't installed):

```bash
node -e "require('sharp')('/tmp/es-dark.png').extract({left:300,top:600,width:900,height:500}).resize({width:1400}).toFile('/tmp/crop.png')"
```

Build the page list from `web/src/generated/topics.json` + the site-level slugs. **The bar is aspire.dev** (`~/src/repos/aspire.dev/src/frontend`) — study its `site.css`/`mermaid.css` for the depth cues (gradient glows, framed diagrams, lifted cards) that `cratis.css` is built from.

## What to check on each page

- **Diagrams** themed and correctly sized in both themes (pre-rendered SVG — should be present immediately, no pop).
- **Tables** render as real tables (not raw `|` pipes — that's the GFM/MDX bug).
- **Code blocks** not over-indented (no spurious leading whitespace in the source snippet) and lifted off the page.
- **Hero / cards** have depth (glow, lift), inline code is a brand-tinted chip.
- Light AND dark both read cleanly.

## Diagnosing a flicker / twitch / layout-shift

The cause is almost always one of: **font swap** (fixed — `font-display: optional` + preload in `Head.astro`), **client-side rendering settling**, or **Mermaid** (fixed — build-time pre-render). To measure:

- Inject a buffered `layout-shift` PerformanceObserver via CDP `Page.addScriptToEvaluateOnNewDocument` and sample `document.documentElement` height every ~16ms after navigate; print the timeline.
- Use a **fresh `--user-data-dir`** for a cold (uncached) load; reuse it for a warm load. Many shifts only show cold.
- For scroll-restoration flashes: scroll down, `Page.reload`, sample `window.scrollY` — if it lands short, content above is rendering late.
- Run all CDP scripts **serially** — they collide on the debug port.

## Caveats

- **Restart `npm run dev` after running `npm run check`** — the gate's re-sync degrades a live dev server (pages 500, tables vanish). A degraded dev server makes screenshots lie; restart and re-verify before trusting a "broken" result.
- The Astro dev toolbar appears mid-page in full-page captures — it's a dev-only overlay, not a real element.
- `prefers-color-scheme` emulation can trigger astro-mermaid's theme observer on any client-rendered fallback diagram; pre-rendered diagrams are unaffected.

→ Rendering internals: the `documentation-rendering-and-qa` rule.
