---
applyTo: "**/Documentation/**/*.{md,mdx}"
paths:
  - "**/Documentation/**/*.md"
  - "**/Documentation/**/*.mdx"
---
<!-- cratis-ai-managed: rules/documentation-structure-and-formatting.md -->

# Documentation structure and formatting

This is the authoritative rendering contract for product documentation consumed by the Astro Starlight site. For content and teaching voice, see [Writing Cratis Documentation](./writing-cratis-docs.md). For source ownership and the edit loop, see [Editing Cratis Documentation](./editing-cratis-docs.md).

Product `.md` and `.mdx` files are copied through the Documentation repository's `web/scripts/sync-content.mjs`; the converter preserves the extension and rewrites the content before Starlight renders it.

## Frontmatter

```yaml
---
title: Append an event
description: Append a domain event to one event source and inspect the result.
tableOfContents: false             # optional per-page override
sidebar:
  badge: { text: New, variant: tip }
---
```

- Product pages should declare `title` and `description`. The title becomes the page H1; the description feeds metadata and AI-facing exports.
- Preserve existing frontmatter when editing unless the task deliberately changes it. The converter preserves only `title`, `description`, `sidebar`, and `tableOfContents`; it drops DocFX keys and other Starlight keys. Features such as `template`, `hero`, `banner`, `head`, `prev`, `next`, `slug`, and `draft` work only on site-level pages authored directly in the Documentation repository.
- Product navigation comes from `toc.yml`, not Starlight autogeneration. `sidebar.badge` works, but `sidebar.order`, `sidebar.label`, and `sidebar.hidden` do not control product navigation.
- A frontmatter-less page falls back to its first H1, but that loses the description and relies on converter inference. Do not add new pages that way.

## Headings

- Do not put an H1 in the body; frontmatter supplies it.
- The global “On this page” list shows H2 headings only. Organize the page around a short, flat set of `##` sections; use H3/H4 only inside them.
- Use sentence case and no trailing punctuation.
- Keep a real H2 when a section needs a stable URL anchor. A card or aside title is presentation, not document structure.

## Files, folders, and navigation

- A navigable product folder normally has `toc.yml` plus one landing page: `<folder>/index.md[x]` or a sibling `<folder>.md[x]`.
- Never keep both `<folder>.md[x]` and `<folder>/index.md[x]`. A legacy `<folder>.md` sibling makes the sync move the directory index to `/overview/`; other duplicate landing shapes can fail the build. Either outcome can orphan the intended landing page or make compatibility links point back to themselves.
- A folder with neither an index nor a sibling landing has no page at its bare URL.
- Product bucket names are product-specific. Read that product's `PRODUCTS[].buckets` entry in `web/scripts/sync-content.mjs`; do not assume generic “Get started / Guides / Understand / Reference” labels.
- A missing built slug is dropped from the sidebar and counted as a broken toc entry. Keep that count at zero.
- `toc.yml` entries with external URLs, `../`, or `/api/` are intentionally dropped. A group with one child collapses to the child link. Check the generated sidebar rather than inferring it from YAML alone.
- Sync slugification lowercases path segments and removes characters outside `[a-z0-9_-]`; for example, `react.mvvm` becomes `reactmvvm`. Verify hand-authored site-absolute URLs against the built route.

## Choose Markdown or MDX

Use the least powerful format that communicates the idea:

- Keep `.md` for headings, prose, links, GFM tables, fenced code, Mermaid/EventModeling diagrams, images, and Starlight aside directives.
- Use `.mdx` only when the page needs imported Astro components, expressions, props, or named slots.
- Imports and JSX in `.md` fail silently: the import can render as visible prose and the component as an inert element. Permissive Markdown HTML allowlists can hide this mistake. A page using a component must be `.mdx`.
- Do not rename a page to `.mdx` merely for a callout or diagram. Renames require checking `toc.yml`, inbound links, generated routes, and AI-facing Markdown output.
- Do not add raw HTML, inline styling, scripts, or one-off visual components to decorate a product page. Reuse an established component or make an explicit reusable site change in the Documentation repository.

## Callouts and asides

The complete Starlight directive set is `note`, `tip`, `caution`, and `danger`. These work in both `.md` and `.mdx` and support a custom title:

```markdown
:::caution[Do not use a raw Guid as the event source id]
Chronicle treats a raw `Guid` as an ordinary response value.
:::
```

| Variant | Meaning |
|---|---|
| `note` | Neutral context or an important clarification |
| `tip` | A recommendation or easier path |
| `caution` | A likely mistake, compatibility trap, or behavior that produces the wrong result |
| `danger` | Destructive, security-sensitive, or data-loss consequences |

The set is closed. Do not use `warning`, `important`, `info`, or `success`: an unknown container directive silently renders as an unstyled `<div>` rather than failing the build.

Legacy DocFX alerts are converted as follows; prefer titled native directives when editing the surrounding content:

| DocFX | Starlight |
|---|---|
| `> [!NOTE]` / `> [!IMPORTANT]` | `:::note` |
| `> [!TIP]` | `:::tip` |
| `> [!WARNING]` | `:::caution` |
| `> [!CAUTION]` | `:::danger` |

In MDX, `<Aside type="tip" title="A specific title">…</Aside>` is available when component composition requires it. Directive asides can also take a Starlight icon attribute, but verify the icon name first; a bad aside icon fails the build.

## Code blocks

- Always tag the language: `csharp`, `tsx`, `typescript`, `bash`, `yaml`, and so on.
- Expressive Code supports useful metadata such as ``title="Program.cs"`` and line/text markers. Use them to orient the reader or focus a diff, not to decorate every snippet.
- DocFX-era aliases include `env`, `pdl`, `ebnf`, `pql`, `gitignore`, `flow`, and `screenplay`; use a real language where one exists.
- Dedent snippets to column zero while preserving their internal indentation.
- Show both sides of a full-stack contract, but do not automatically hide sequential C#→generated-TypeScript explanations behind tabs. Use `FullStackTabs` only when the snippets are alternatives that remain understandable independently.
- The converter's DocFX-alert and link rewriting is not fully code-fence-aware. Literal `> [!NOTE]`, Markdown-link targets, or `href="…"` examples can be rewritten; inspect the synced output when documenting those syntaxes.

## Tables, images, links, and diagrams

- Use GFM tables with a separator row and a blank line before the table. `remarkGfm` in the Documentation site's Astro config is load-bearing for `.mdx`; raw pipe text in a rendered page indicates that integration is missing or degraded.
- Keep images beside the source page, use meaningful alt text, and rely on the site's click-to-zoom behavior.
- In product source, relative links to files keep their real `.md` or `.mdx` extension. The converter strips either extension for the public route. Directory URLs end in `/`.
- Site-level MDX uses clean root-relative routes such as `/arc/backend/commands/`. Cross-product links are also root-relative.
- Link text describes the destination; `here`, `click here`, and `see documentation` are hard lint errors.
- Use `mermaid` for architecture, sequence, flow, and state diagrams. Use `eventmodeling` for EventModeling diagrams. Both are pre-rendered to responsive SVG at build time.

## MDX component surface

Place imports immediately after frontmatter, with a blank line before the first body content. Import only what the page uses. Starlight exports exactly `Aside`, `Badge`, `Card`, `CardGrid`, `Code`, `FileTree`, `Icon`, `LinkButton`, `LinkCard`, `Steps`, `TabItem`, and `Tabs`:

```mdx
import { Aside, Steps, TabItem, Tabs } from '@astrojs/starlight/components';
```

Shared Cratis components are default imports through exact `@components/<Name>.astro` paths; there is no bare `@components` barrel:

```mdx
import FullStackTabs from '@components/FullStackTabs.astro';
import Recap from '@components/Recap.astro';
```

Common shared components include `FullStackTabs`, `OsAwareTabs`, `TopicHero`, `SimpleCard`, `StackDiagram`, `YouWillLearn`, and `Recap`. Inspect the current component and an existing page before using its props or slots; do not infer an API from the component name.

Icon names must come from the installed Starlight set. Use `seti:windows`, not `windows`. Invalid icons in `Icon`, `TabItem`, `SimpleCard`, and `TopicHero` can produce an empty SVG without a build failure, so visual verification is mandatory.

## AI-facing Markdown

The published `.md` mirror behind page actions such as “Copy Markdown” is copied from synchronized Markdown/MDX rather than reconstructed from rendered HTML. Synced frontmatter and converter rewrites are present, but MDX imports and JSX remain visible in that raw Markdown surface. Prefer plain Markdown unless a component creates a real teaching advantage, and inspect the emitted `.md` artifact when changing a page's authoring format. Generated AI indexes can process content separately; do not infer their exact representation from the page action.

## Verification

Run the owning repository's local documentation gate first when it has one. Many product repositories expose:

```bash
./Documentation/verify-markdown.sh
```

For full-fidelity rendering, use the sibling Documentation checkout:

```bash
cd ../Documentation/web
npm run check
```

The local gate validates the authored repository in isolation. The full site check builds and syncs every available sibling product, runs site linting and rendered-link checks, and can expose unrelated sibling failures; diagnose those separately rather than silently waiving them. Some optional local tools skip when not installed, so name what actually ran.

A successful build proves syntax, not presentation. For any aside, diagram, tabs, cards, or custom component change, use the `qa-cratis-docs` skill to inspect light and dark screenshots.

End every file with a single trailing newline.
