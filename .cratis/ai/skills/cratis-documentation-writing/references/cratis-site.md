<!-- cratis-ai-managed: skills/cratis-documentation-writing/references/cratis-site.md -->
# Cratis site specifics

## Contents

- Teaching components
- Preserve the text and citation path
- Maturity
- Cross-product compatibility
- Prompter as a documentation signal
- Bridges and comparisons
- Structure and publishing

## Teaching components

Confirm presentation against the owning Documentation repository's
`web/src/components`, installed Starlight exports, `web/astro.config.mjs`,
`web/scripts/sync-content.mjs` and `web/variant-docs.yml` before using it.

Choose a component because it helps the reader act, understand or recover,
not because every page needs decoration. Product `.md` pages support native
aside directives such as `:::note`, `:::tip` and `:::caution`. Imports and JSX
require `.mdx` and an owning pipeline that supports the component; do not copy
raw site imports into ordinary Markdown or convert whole chapters by default.

| Need | Use |
| --- | --- |
| Open a tutorial with its outcomes | `import YouWillLearn from '@components/YouWillLearn.astro'`; optional `title`, list in the body |
| Close a tutorial chapter | `import Recap from '@components/Recap.astro'`; optional `title` |
| Ordered procedure | `import { Steps } from '@astrojs/starlight/components'` around an ordered list |
| One example in every client language | `<ChronicleClientTabs snippet="…" />` or `<ArcBackendTabs snippet="…" />`, expanded at sync time from client-owned snippet files |
| Genuinely independent variants | Import Starlight `Tabs` and `TabItem`; give each variant a descriptive label and complete prerequisites |
| Backend and frontend roles in a full-stack example | `import FullStackTabs from '@components/FullStackTabs.astro'`, named `csharp` and `typescript` slots; these roles are not interchangeable alternatives |
| Consequential prerequisite, risk or limitation | Native aside directive, or Starlight `Aside` in supported MDX; state the boundary before its first consequence |
| Peer task choices and destinations | Starlight `Card`, `CardGrid` or `LinkCard`; use meaningful link text, not a compulsory card for every section |
| Relationships or state transitions | A Mermaid fence; the site is configured for prerendering with client fallback, not guaranteed rendering or accessibility; essential relationships also need readable text |
| A reader needs to locate code or a change | Language-tagged fences; use configured Expressive Code filename/highlight features only after confirming their exact syntax |

- The client-tab macros take `snippet`, and optionally `syncKey` and a
  `variants="java,kotlin"` subset. There is no `clients` property: a tab
  appears for each client whose repository has that snippet file.
- The site links each expanded tab to its exact snippet file in the owning
  repository. Don't add those links by hand.
- Arc's authoring check rejects `ArcBackendTabs` nested inside `Steps`.
  Tabs can't sit inside `Steps` anywhere, so write a backend procedure as
  numbered H2 steps and keep `Steps` for shared, untabbed procedures such as
  the React screen.
- Site-owned MDX under `web/src/content/docs/` can use `<ArcBackendTabs>` and
  `<ChronicleClientTabs>` without an import. The site expands them from the
  same manifest and client-owned snippets as product pages, including in the
  Copy Markdown mirror; check the mirror to confirm the macro expanded.
- The variant-docs audit (`web/variant-docs.yml`) also covers site-owned
  pages. Every site page is audited or explicitly excluded with a reason, and
  direct single-language backend fences are ratcheted.
- `TopicHero` declares `title` (required), `icon` and `eyebrow`. Don't copy
  props from a page that passes others; they are ignored.

Keep required backend-to-frontend actions sequential rather than hiding one
half as an optional tab. For Markdown children inside supported components,
retain blank lines and semantic heading/list order. Inspect relevant keyboard
navigation, focus, text alternatives and small-screen/light/dark presentation;
a component's availability is not accessibility evidence.

## Preserve the text and citation path

The Copy Markdown pipeline strips MDX imports/wrappers while retaining supported
static labels and children. It flattens syntax; it does not execute arbitrary
components or prove that every prop survives. Keep essential meaning in
prose/children and inspect the bounded emitted Markdown, not just the MDX source.
For example, a `LinkCard` destination can survive while its `description` prop
is lost; an `Aside` can retain a title but lose its type or computed default
heading; `FullStackTabs` can lose computed backend/frontend role labels.
Put the choice, warning and role in readable content as well. Preserve both
variants, code, links and a causal reading order.

Retain the canonical URL and heading/source context needed to cite the page.
Check relevant Markdown/LLM exports for lost boundaries or duplicate/misleading
labels. Rendering success does not establish export correctness, citation
resolution, runtime behavior or assistant answer quality. Use
[reader review](reader-review.md) for an uncoached task attempt, and the
**cratis-llm-friendly-documentation** skill for bounded retrieval/citation work.

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

Label a preview variant on the first screen of every page that shows it, not
only on its own getting-started page. An unsupported-variant snippet is one
sentence with its tracking issue; the surrounding prose doesn't repeat it.

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
