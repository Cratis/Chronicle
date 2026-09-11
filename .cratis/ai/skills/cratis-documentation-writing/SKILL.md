---
name: cratis-documentation-writing
description: Write and structure documentation using the Diátaxis framework — decide whether a page is a Tutorial, a How-to guide, Reference, or Explanation, then draft it in that style with complete runnable examples. Use when creating or reworking documentation pages for a Cratis-based project, its product, or its samples. Do not use for code generation, release operations, or inventing API facts the code does not show.
license: MIT
---
<!-- cratis-ai-managed: skills/cratis-documentation-writing/SKILL.md -->

# Documentation writing

Documentation fails when it is written for the writer instead of the reader.
The [Diátaxis framework](https://diataxis.fr/) fixes that by separating
documentation into four types, each serving one distinct user need — and by
refusing to mix them. A page that teaches, instructs, describes, and explains
at once serves none of those needs well.

This skill is documentation-system-agnostic: it applies to a docs site, a
`docs/` folder in a repository, a wiki, or README files. Where a page goes and
how navigation is wired is your project's own convention; this skill governs
the *classification*, *structure*, and *prose* of what you write.

## Classify before writing

Determine which quadrant the page belongs to before drafting:

| Type | Orientation | Analogy | When to use |
| --- | --- | --- | --- |
| **Tutorial** | Learning | A lesson | Guide a newcomer step-by-step to a successful first outcome |
| **How-to guide** | Problem-solving | A recipe | Show an experienced user how to accomplish a specific task |
| **Reference** | Information | A dictionary | Describe the technical machinery — APIs, attributes, configuration |
| **Explanation** | Understanding | A discussion | Clarify *why* something works the way it does, trade-offs, architecture |

Rules per type:

- **Tutorial** — never explain *why*; focus on *do this, then this*. Each step
  must produce a visible, verifiable result. The reader must succeed even
  while not yet understanding the concepts.
- **How-to guide** — assume competence. State the goal, list prerequisites,
  give the steps, done. No teaching.
- **Reference** — exhaustive and terse. Tables, signatures, attribute lists.
  No narrative.
- **Explanation** — no steps. Discuss concepts, trade-offs, and design
  decisions. Diagrams are welcome here.

If a request seems to need two types at once, that is two pages linked to each
other. If the type cannot be determined from the request, ask before writing.

## Workflow

1. **Clarify** — decide the document type, the target audience (newcomer,
   experienced contributor, framework consumer, operator), the reader's goal,
   and the scope: what to include *and* what to exclude.
2. **Propose structure** — present an outline (headings plus a one-line
   description each) before writing full content.
3. **Write** — produce the full page in well-formatted Markdown, following the
   style rules below.
4. **Verify** — run the completion checklist at the end of this skill.

## Writing style

The voice is **direct, practical, and opinionated** — an experienced colleague
explaining something to a capable developer, confident but never condescending.

- **Active voice, present tense.** "Chronicle appends the event", not "The
  event is appended by Chronicle."
- **Second person.** "You configure…", not "One configures…" or "It is
  possible to configure…".
- **Lead with the most important information.** Do not bury the key point
  after three paragraphs of context.
- Use headings, lists, and code blocks to organize content; dense paragraphs
  lose readers.
- Focus on public APIs and features, never internal implementation.
- Do not document third-party libraries; link to their own docs instead.
- **American English only**: `color` not `colour`, `behavior` not `behaviour`,
  `organize` not `organise`, `initialize` not `initialise`.

## Code examples

Examples are where documentation credibility is won or lost.

- Every example must be **complete, correct, and runnable** — no pseudo-code,
  no `// ...` elisions. If it cannot be shown complete, show a smaller thing
  that can.
- Never copy code verbatim from a repository — APIs change under copied
  examples. Write purpose-built examples that demonstrate the documented
  behavior.
- Prefer the framework's canonical shapes. In a Cratis context that means
  `record` types for commands, events, and read models; attributes as the
  framework applies them; and the vertical-slice layout the project already
  uses.
- Show the outcome: expected output, the state change, or the query result an
  example produces, so the reader can verify their attempt.

## Diagrams

Use [Mermaid](https://mermaid-js.github.io/mermaid/#/) for architecture
(`graph TD` / `graph LR`), sequence flows (`sequenceDiagram`), and state
transitions (`stateDiagram-v2`). A diagram replaces a paragraph of topology
prose; it does not decorate one.

## Contextual awareness

- Read the existing documentation around the page you are writing first, and
  match its tone, style, and terminology. If it is "event source" there, it is
  "event source" everywhere.
- Do not copy content from existing pages unless explicitly asked; link
  instead.
- Do not fabricate URLs or version numbers — link only to resources you can
  verify exist.

## Completion checklist

A page is done when:

- The Diátaxis type is chosen deliberately and the page holds to that one
  type, linking out to the other types instead of drifting into them.
- The audience and their goal were identified before writing, and the first
  screen serves that goal.
- Every code example is complete, runnable, and purpose-built.
- Terminology is consistent with the surrounding documentation.
- All internal links resolve; all external links are real.
- Mermaid blocks are syntactically valid.
- The file ends with a single trailing newline.
