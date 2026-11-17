<!-- cratis-ai-managed: skills/cratis-documentation-writing/references/writing-guidance.md -->
# Documentation writing guidance

## Contents

- Use reader feedback as evidence
- Writing style
- AI-assisted drafting
- Maintenance
- Contextual awareness
- Keep editorial research private

Detail that supports the workflow in `SKILL.md`: reader feedback, voice,
AI-assisted drafting, maintenance and editorial hygiene. The completion
checklist stays in `SKILL.md`.

## Use reader feedback as evidence

Use support questions and user feedback as evidence: ask what they searched
for, where they looked, and which step failed. Repair the entry link, wording,
or example as appropriate; adding another FAQ entry alone may not fix discovery.
The docs are also the answer surface for Prompter and Chronicle MCP, so treat
a repeated question as a signal to investigate the page, its entry route, or
the product behavior behind it. Reduce a report to the page and the
sentence-level change the reader expected. Make an in-scope fix when the
evidence supports one; otherwise report the gap and suggest an issue, opening
one only when asked.
[Cratis site specifics](cratis-site.md) lists the signals Prompter
actually records.

## Writing style

The voice is **direct, practical, and opinionated** — an experienced colleague
explaining something to a capable developer, confident but never condescending.

- **Active voice, present tense.** "Chronicle appends the event", not "The
  event is appended by Chronicle."
- Address the reader when giving an action: "You configure…". In reference
  and explanation, let the API or component be the subject when that is clearer.
- **Lead with the reader's problem and payoff.** Explain why a capability
  matters before its configuration, except in a terse how-to or reference.
- Develop explanations in connected paragraphs. Use headings to locate them,
  lists for real steps, and tables for comparisons; do not fragment reasoning
  into a series of labeled boundaries.
- Focus on public behavior. Explain what happens behind an example when the
  reader needs it to understand the result, without turning the page into an
  internal implementation manual.
- Teach the intended idioms: show the conventional shape of a solution and
  explain why a tempting non-idiomatic approach causes trouble. Distinguish a
  framework requirement from a house convention; avoid making a workaround the
  first example a newcomer copies.
- State limitations, maturity, compatibility, and when the simpler approach
  is preferable. Link to third-party docs instead of retelling them. Say that
  an experimental or preview surface is experimental on its own first screen;
  a sidebar link or published guide does not tell the reader.
- Use a [Mermaid](https://mermaid-js.github.io/mermaid/#/) diagram where
  architecture, a sequence, or state transitions are clearer drawn than
  told. A diagram replaces topology prose; it does not decorate a page.
- Vary sentence and paragraph rhythm; avoid templated openings and filler.
  A voice edit must never weaken a technical caveat or invent experience.
- **American English only**: `color` not `colour`, `behavior` not `behaviour`,
  `organize` not `organise`, `initialize` not `initialise`.

## AI-assisted drafting

Use AI to find gaps, compare terminology and review a draft, but do not let
plausible generated prose decide what problem the product solves. Ground the
reader journey in observed use cases and have the owning maintainer review
structural changes. A style pass cannot establish technical correctness.
Do not rewrite an entire corpus into one repeated template.

An AI-drafted narrative is a first draft, not a finished page. Give the model
the reader, the scenario, the terminology and the source evidence up front;
then revise the result against that evidence and the
**cratis-writing-voice-and-cadence** constructions before it ships. Tell the
reviewer the narrative was AI-drafted (in the review request, a PR comment or
the commit message, never in the PR description, which is published as the
release note) so they read it as prose, not only as a diff.

For machine-readable delivery and retrieval checks, use
**cratis-llm-friendly-documentation**; publishing `llms-full.txt` alone does
not prove that an assistant can find the right page or cite it accurately.

## Maintenance

Edit the *authored* file, never a synced copy. The Cratis site derives each
product page's edit link from that source path, so don't hand-author
`editUrl` in product frontmatter. Recheck examples against the supported
version when that version changes. A review of docs is not complete just
because the site builds: syntax and behavior are different checks.

A page is done for this change, not finished forever. Expect to revisit its
wording, structure and examples as readers hit them.

## Contextual awareness

- Read the existing documentation around the page you are writing first, and
  match its tone, style, and terminology. If it is "event source" there, it is
  "event source" everywhere.
- Do not copy content from existing pages unless explicitly asked; link
  instead.
- Do not fabricate URLs or version numbers — link only to resources you can
  verify exist.

## Keep editorial research private

Keep stylistic influences and editorial research in private working notes,
not documentation, GitHub issues, pull requests, release notes or other public
text. Develop each reader journey from Cratis product evidence rather than
copying another project's structure or claims. This does not remove third-party
integration names, necessary API identifiers or links readers need to use them.
