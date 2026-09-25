<!-- cratis-ai-managed: skills/cratis-documentation-writing/references/cratis-site.md -->
# Cratis site specifics

Facts about the Cratis documentation platform that change how a page should be
written. They describe the site as of September 2026; confirm against the
Documentation repository (`web/src/components`, `web/scripts/sync-content.mjs`,
`web/variant-docs.yml`) before relying on a detail.

## Teaching components

Product pages that import components must be `.mdx`. Every component costs
machine readability: the raw Markdown mirror behind "Copy Markdown" keeps its
imports and JSX. Use one where it clearly teaches better than plain Markdown.

| Need | Use |
| --- | --- |
| Open a tutorial with its outcomes | `import YouWillLearn from '@components/YouWillLearn.astro'`; optional `title`, list in the body |
| Close a tutorial chapter | `import Recap from '@components/Recap.astro'`; optional `title` |
| Ordered procedure | `import { Steps } from '@astrojs/starlight/components'` around an ordered list |
| One example in every client language | `<ChronicleClientTabs snippet="…" />` or `<ArcBackendTabs snippet="…" />`, expanded at sync time from client-owned snippet files |
| Two independently readable alternatives (C# and TypeScript) | `import FullStackTabs from '@components/FullStackTabs.astro'`, named `csharp` and `typescript` slots |

- The client-tab macros take `snippet`, and optionally `syncKey` and a
  `variants="java,kotlin"` subset. There is no `clients` property: a tab
  appears for each client whose repository has that snippet file.
- The site links each expanded tab to its exact snippet file in the owning
  repository. Don't add those links by hand.
- Arc's authoring check rejects `ArcBackendTabs` nested inside `Steps`.
- `TopicHero` declares `title` (required), `icon` and `eyebrow`. Don't copy
  props from a page that passes others; they are ignored.

## Maturity

The site has no single maturity convention. The sidebar badges some surfaces
"Soon", some pages use a note aside, and the model-first layer (Studio,
Screenplay, Stage, Scene, Prologue) is described as experimental in the
site's AI-facing overview (`llms.txt`) but not on every product index. Check the owning product's current
maturity and production guidance. If it is verified as experimental or
preview, say so prominently on its index and getting-started page, for
example in a `:::caution` aside, and state only the change and production
limits the product source supports. Don't infer those limits from a
site-wide label.

## Cross-product compatibility

There is no numbered compatibility matrix across the Chronicle kernel and its
clients, Arc and Chronicle, or Components and Arc; the roadmap lists it as
being hardened. `compatibility.mdx` describes baselines and dependency
relationships, Components' supported Arc range is its `peerDependencies`, and
Chronicle's `upgrading/major-versions.md` marks where kernel and clients must
move together, and says "unknown" where it has not been established. Follow
that practice: state the pairings you verified, and say when a pairing is
unverified instead of implying it works or fails.

## Prompter as a documentation signal

Prompter, the Discord documentation assistant, stores anonymous rows per
answer: surface, cited page URLs, answered or refused, confidence, and a
thumbs-up/down verdict. It does not store question or answer text. Readers can
report a missing or hard-to-find page through its `/issue` command, and an
opted-in repository can notify a maintainer channel when it could not answer.
Use refusals, low confidence and negative verdicts grouped by cited page to
decide which pages to investigate. Don't claim individual questions or a
reporting dashboard exist.

## Bridges and comparisons

Chronicle's `coming-from-crud`, Arc's `coming-from-mediatr-and-mvc` and
Components' `coming-from-primereact` are product-owned. The site's .NET and
JVM event-sourcing comparisons name the compared versions, link first-party
sources, avoid ranking, and commit to a refresh interval. Keep that standard,
and label another technology's code as illustrative unless it was compiled
at a named version.

## Structure and publishing

- No repository has a path-specific `CODEOWNERS` entry for
  `Documentation/**`; a few, such as Chronicle.Python and Chronicle.Wolverine,
  have catch-all owners that include it. Check the owning repository rather
  than assuming a documentation reviewer exists.
- What is part of writing a page, what is a separate navigation decision,
  and how a merged change reaches the site are in
  [Editing Cratis Documentation](../../../rules/editing-cratis-docs.md).
