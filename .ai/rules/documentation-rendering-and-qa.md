---
applyTo: "**/Documentation/web/**"
paths:
  - "**/Documentation/web/**"
---

# Documentation Rendering & Visual QA

How the docs site turns Markdown into pixels, and how to verify it looks right. These mechanisms exist because the obvious defaults each caused a real, user-reported bug. Don't undo them without understanding why.

## Mermaid diagrams are rendered at BUILD time

`web/scripts/mermaid-prerender.mjs` (a remark plugin wired into `astro.config.mjs` `markdown.remarkPlugins`, before astro-mermaid) renders every ```mermaid block to SVG **at build time** using the **system Chrome over CDP — no npm dependency** — and inlines it. This is why diagrams ship in the HTML: no client-side draw delay, no layout shift, correct scroll restoration.

**Rules / gotchas (each one cost a debugging round):**
- **`autoTheme` MUST stay `false`** on the `astro-mermaid` integration. Its theme-change observer strips `data-processed` from every `pre.mermaid` and re-renders client-side — which **clobbers the pre-rendered SVGs**. Light/dark still works because `cratis.css` themes the SVG via CSS variables that flip with the theme; the JS re-render isn't needed.
- The inlined SVG gets an inline **`aspect-ratio` (from its viewBox) + `width:100%`** (`makeResponsive()`), so the browser reserves its height at first paint — otherwise it settles/reflows, or squishes to a thin strip with naive `height:auto`.
- **Clear `.astro`/`node_modules/.astro` between iterations** — stale Astro content cache serves a PARTIAL prior render (looks like only some diagrams rendered).
- **Graceful fallback:** no Chrome / a render error → the block falls through to astro-mermaid's client-side rendering, so the build can't break. In CI set `CHROME_PATH` if Chrome isn't at a standard path (GitHub `ubuntu-latest` has it).
- **Invalid Mermaid falls back silently.** Watch for syntax the parser rejects — e.g. `()` in an *unquoted* edge label (`-->|.use() hook|`) breaks it; quote the label (`-->|".use() hook"|`).
- Diagram colors/frame are themed entirely in `cratis.css` (the "Mermaid diagrams" section), using the brand accent + Starlight gray CSS vars. Mermaid's own SVG rules use `#id` selectors but **no `!important`**, so class selectors with `!important` win. Never bump label `font-weight` via CSS post-render — it widens text past the measured box and clips.

## Fonts: `font-display: optional`, not `swap`

The brand fonts are declared in `web/src/components/Head.astro` (a Starlight `<Head>` override) with **`font-display: optional` + `<link rel="preload">`**, and the `@fontsource/*/index.css` imports are removed from `astro.config` `customCss`. This is deliberate: @fontsource ships `font-display: swap`, which **re-wraps text when the web font arrives after first paint** — the "page grows then pops" twitch on load/refresh. `optional` means the browser uses the (preloaded) font if it's ready in a short window, else keeps the metric-matched fallback for that load — **never a mid-page swap**. Metric-matched fallback `@font-face`s in `cratis.css` cover the brief first-paint case. If you change font loading, re-verify with `document.fonts.check(...)` that Inter/JetBrains Mono still load AND that there's no reflow.

## GFM tables need `remark-gfm` explicitly

`markdown.remarkPlugins` includes `remarkGfm`. Without it, **tables render as raw `|` pipe text on every `.mdx` page** (the front door's comparison table, why-cratis, …) while plain `.md` is fine — because astro-mermaid injects plugins via the deprecated `markdown.remarkPlugins` path, which leaves `@astrojs/mdx`'s own `gfm` flag falsy. Diagnose with `grep -c '<table' dist/<page>/index.html` after a build.

## Visual + layout-shift QA (headless, no extra deps)

`shot-scraper`/Playwright aren't installed, but Chrome is. Use the committed **`web/scripts/screenshot.mjs`** to capture any page in **light or dark**, full-page:

```bash
cd Documentation/web && npm run dev    # serve at http://localhost:4321
node scripts/screenshot.mjs http://localhost:4321/chronicle/concepts/event-source/ /tmp/es.png dark
node scripts/screenshot.mjs http://localhost:4321/chronicle/concepts/event-source/ /tmp/es-light.png light
```

Then **read the PNG** to evaluate it, and crop/zoom with the `sharp` already in `node_modules`:

```bash
node -e "require('sharp')('/tmp/es.png').extract({left:300,top:600,width:900,height:500}).resize({width:1400}).toFile('/tmp/crop.png')"
```

- The **bar is aspire.dev** (`~/src/repos/aspire.dev/src/frontend`) — study its `site.css`/`mermaid.css` for depth (gradient glows, framed diagrams, lifted cards). Much of `cratis.css` is adapted from it.
- For **layout-shift / "twitch"** bugs, inject a buffered `layout-shift` PerformanceObserver via CDP `Page.addScriptToEvaluateOnNewDocument` and sample `documentElement` height over time; use a **fresh user-data-dir** for a cold (uncached) load, reuse it for warm. Run CDP scripts **serially** (they share the debug port).
- Run the gate (`npm run check`) and restart `npm run dev` afterward — the gate's re-sync degrades a running dev server (see [Editing Cratis Documentation](./editing-cratis-docs.md)).
