---
applyTo: "**/Documentation/**/*.{md,mdx}"
paths:
  - "**/Documentation/**/*.md"
  - "**/Documentation/**/*.mdx"
---
<!-- cratis-ai-managed: rules/editing-cratis-docs.md -->

# Editing Cratis documentation

Cratis documentation is split across product repositories and aggregated by the sibling `Documentation` repository. Find the authored source before editing; synchronized product copies under `Documentation/web/src/content/docs/` are disposable build output.

## Find the source of truth

Common routes map as follows:

| Public route | Authored source |
|---|---|
| `/chronicle/**` | `Chronicle/Documentation/**` |
| `/arc/**` | `Arc/Documentation/**` |
| `/components/**` | `Components/Documentation/**` |
| `/chronicle-mcp/**`, `/authproxy/**`, `/cli/**`, `/fundamentals/**`, `/screenplay/**`, `/prologue/**`, `/prompter/**`, and other product routes | The product or family-source repository selected by `PRODUCTS` |
| `/contributing/**` | The organization `.github` repository (legacy fallback: `GitHubLanding`) |
| Site-level routes such as `/`, `/why-cratis`, `/cratis-stack`, `/glossary`, `/ai/**`, and `/compare-event-sourcing-*` | `Documentation/web/src/content/docs/**` |

The definitive source map is `PRODUCTS` and its optional `familySources` in `Documentation/web/scripts/sync-content.mjs`. The site prefers sibling checkouts and falls back to configured submodules. Do not hard-code a shorter product list when the script can answer ownership.

Never edit a synchronized subtree under `Documentation/web/src/content/docs/`. Each `PRODUCTS[].key` is regenerated there even when Git state or `.gitignore` makes a particular directory look hand-authored. Site-level files that are not populated from `PRODUCTS` are authored directly in the Documentation repository. Check `PRODUCTS`, source paths, and the site configuration when ownership is unclear.

## Edit and verify

From the owning repository:

1. Edit the authored `.md` or `.mdx` file and preserve its existing frontmatter unless the task deliberately changes it.
2. Run that repository's local documentation gate when present, commonly `./Documentation/verify-markdown.sh`.
3. For full rendering, run the site from the sibling checkout:

   ```bash
   cd ../Documentation/web
   npm run check
   ```

4. Preview with `npm run dev` from that same `../Documentation/web` directory and inspect visual changes in light and dark.

The local gate validates authored content without requiring every sibling product. The full site check synchronizes every available product and can expose unrelated sibling or optional-tool failures; diagnose and report those separately. Report which checks actually ran when local prose, Markdown, or external-link tools skip because their executables are absent.

Restart `npm run dev` after a build/check. The build re-sync can degrade a running dev server, producing 500s or missing table rendering. If a change still appears stale, clear `web/.astro` and `web/node_modules/.astro`, restart, and recheck before blaming the source.

## Add, move, rename, or delete a page

- Product navigation comes from its `toc.yml`; site-level navigation comes from `astro.config.mjs`.
- Product navigation buckets are defined per product in `PRODUCTS[].buckets`. Read the actual names and section lists before changing them.
- Keep exactly one landing for a route. A sibling `<folder>.md[x]` collides with `<folder>/index.md[x]`; a legacy `.md` collision can move the directory index to `/overview/`, while other duplicate landing shapes can fail the build.
- Update inbound links and `toc.yml` together. For a published route change, inspect the Documentation site's redirect mechanism rather than assuming a source-file move preserves old URLs.
- Watch sync output for dropped toc entries and verify the built sidebar. External, `../`, and `/api/` toc targets are intentionally omitted; single-child groups collapse.

## Links

- Product source links to files keep the real `.md` or `.mdx` extension. The converter removes either extension for the public route.
- Directory links end in `/`.
- Site-level MDX and cross-product links use clean root-relative public routes such as `/arc/backend/commands/`.
- Slugification removes punctuation from path segments (`react.mvvm` becomes `reactmvvm`), so verify hand-authored site-absolute paths against the build.

## Content and rendering

- Match the page's Diátaxis type and the tour voice in [Writing Cratis Documentation](./writing-cratis-docs.md).
- Verify framework APIs against source using [Writing Correct Code Examples](./writing-correct-examples.md).
- Follow [Documentation Structure and Formatting](./documentation-structure-and-formatting.md) as the single authority for frontmatter, Markdown/MDX boundaries, asides, components, navigation behavior, and gates.

Commit in the repository that owns the authored source. Touch the Documentation repository only when the task deliberately changes site-level content, navigation composition, components, styling, redirects, or build behavior.
