---
applyTo: "**/Documentation/**/*.{md,mdx}"
paths:
  - "**/Documentation/**/*.md"
  - "**/Documentation/**/*.mdx"
---

# Writing Cratis Documentation — tour voice + Starlight authoring

The Cratis docs must **take the reader on a tour, like a teacher** — the way [Marten](https://martendb.io), [Wolverine](https://wolverinefx.net), and [aspire.dev](https://aspire.dev) docs do — **not** state facts like a reference dump. This is the project's strongest qualitative bar and it's been flagged repeatedly. The differentiator of those docs is **pedagogical structure**, not looks. Match it.

## The bar

- **Pain → relief.** Open by naming the friction the reader feels, then reveal the feature as the relief. (Aspire's "without vs. with".)
- **Why before how.** A reader who understands the reasoning handles edge cases the docs don't cover.
- **Active voice, present tense, second person.** "You append the event," not "the event is appended."
- **Be honest about limits.** A "when this is the wrong fit" section builds more trust than omitting it.

## One page = one Diátaxis type (it also picks the nav bucket)

| Type | Reader is… | Reads like | Nav bucket |
|---|---|---|---|
| **Tutorial** | learning by doing | a guided lesson — each step a visible result | Get started |
| **How-to** | solving a specific problem | a recipe — assume competence, no teaching | Guides |
| **Explanation** | trying to understand | a discussion — concepts, trade-offs, *why*, a diagram | Understand |
| **Reference** | looking something up | a dictionary — exhaustive, terse, tables/signatures | Reference |

Never mix types. A tutorial padded with reference detail overwhelms; a how-to interrupted by concept digressions stops being a recipe.

## The tour-voice checklist (apply to tutorials, getting-started, and explanations)

1. **Open with a concrete scenario** ("a book shows up — let's record that"), not a definition of the tool.
2. **Name the friction first**, then the feature as its relief.
3. **"Let's…" with chronological verbs** (define → append → project → query).
4. **After every code block, explain the invisible** — what happens under the hood and why it matters.
5. **Recap before pivoting** ("this works well for X… however…").
6. **Anticipate the reader's doubt** with an inline aside ("in a real app you'd…").
7. **Show the result** — the output, the resulting read model/JSON — so success is visible.
8. **Organize by workflow**, not alphabetically (especially CLI/command docs).
9. **End every section with a forward link** to the natural next step.

`Chronicle/Documentation/tutorial/*` is the reference voice — read it before writing.

## Achieve the tour voice with Starlight's authoring tools

Author rich pages as **`.mdx`** (the converter keeps `.mdx`): frontmatter `title` + `description`, **no body H1**, then import what you need:

```mdx
import { Steps, Tabs, TabItem, Aside } from '@astrojs/starlight/components';
import { FullStackTabs, TopicHero, SimpleCard, StackDiagram, YouWillLearn, Recap } from '@components';
```

- **`<Steps>`** — procedures where each step produces a visible result (checklist point 1, 3).
- **`<Tabs>` / `<TabItem>`** — alternatives (OS, console-vs-web, C#-vs-fluent). Valid icons include `apple`, `linux`, `seti:c-sharp`, `seti:react`.
- **`<Aside>`** — anticipate doubt / call out a gotcha (checklist point 6).
- **`<FullStackTabs>`** — synced C# ↔ generated-TypeScript tabs. **This is the differentiator** (full-stack type safety) — use it wherever a feature spans the stack; show both sides, not just the backend.
- **`<TopicHero>` + `<SimpleCard>`** — the per-product Overview landings (pain→relief framing + a card grid).
- **`<StackDiagram>`, `<YouWillLearn>`, `<Recap>`** — the stack picture, a chapter's learning goals, and the closing recap (checklist points 1 and 5).
- **A Mermaid diagram for every non-trivial concept** — architecture, event/command flow, state transitions. A concept page without a diagram is usually incomplete. (They're pre-rendered to SVG at build — see the `documentation-rendering-and-qa` rule.)

## Two voices, and connect the products

- **Two voices per area:** the toured/educational layer (tutorial, getting-started, "Understanding…") **and** the terse, exhaustive reference. The narrative pages **link *down*** into the reference; the reference stays a dictionary.
- **Connect at the seams** rather than re-explaining: a command (Arc) appends events (Chronicle); a query reads a projection (Chronicle) rendered by a component (Components). Link to the **glossary** for shared terms instead of redefining them.
- **Coming-from-X bridges** map new concepts to what the reader knows (MediatR, MVC, EF/CRUD, Marten/other event stores).

## Before you call a page done

- Every framework API in a code example is **verified against real source** — see [Writing Correct Code Examples](./writing-correct-examples.md). Readers paste snippets verbatim.
- `npm run check` is green (0 errors · 0 broken links); the page sits in the right nav bucket.
- For a visual page, screenshot it in light **and** dark — see the `qa-cratis-docs` skill.

**Study the masters (cloned locally):** `~/src/repos/aspire.dev` (Starlight — great CLI docs + tour writing), `~/src/repos/marten`, `~/src/repos/wolverine`. Mine their *patterns*; never copy their text.

→ The mechanical format a page must follow (frontmatter, headings, asides, code fences, file layout): [Documentation Structure & Formatting](./documentation-structure-and-formatting.md). The edit→sync→verify loop and where pages live: [Editing Cratis Documentation](./editing-cratis-docs.md). Site build internals live in the Documentation repo.
