---
name: cratis-documentation-writing
description: Use when planning a Cratis documentation reader journey, choosing a page's purpose or reviewing its structure. Not for example execution, release notes or publication; use the corresponding specialist.
license: MIT
metadata:
  cratis-hint-paths: "**/Documentation/**/*.{md,mdx}"
---
<!-- cratis-ai-managed: skills/cratis-documentation-writing/SKILL.md -->

# Documentation writing

A developer arrives with a job to do, not with an interest in our repository
structure. Start from that job: what did they try, what stopped them, and what
would let them know they succeeded? Use [Diátaxis](https://diataxis.fr/)
as a compass for each page's primary purpose, not a purity test. This skill
governs content and reader journeys; the owning repository governs page
placement, navigation, and rendering.

## Use the specialists at their boundary

Read this skill to plan the reader journey, then read
**cratis-engineering-docs-authoring** to draft from product evidence,
**cratis-technical-examples** to verify code, commands, configuration and output,
and **cratis-llm-friendly-documentation** when discovery or exported representations
are involved. Read **cratis-writing-voice-and-cadence** before revising existing
prose. Skill availability is not evidence that its instructions were read.
Before drafting or revising explanations beside examples, read and apply
[Explaining code](../cratis-engineering-docs-authoring/references/explaining-code.md).
Use its worked transformations to choose the detail that carries the explanation;
use its multi-client guidance before choosing shared prose, tabs or separate
procedures. Read the resulting paragraphs for connected reasoning, not just
coverage of accuracy questions.
The [documentation rule](../../rules/documentation.md) owns Platform naming and
product-claim constraints; do not turn an umbrella label into a dependency on
all components.

## Completion checklist

A page is done when:

- The page has a deliberate primary purpose; short supporting context helps
  the reader proceed, while substantial digressions link to their own pages.
- The audience and their goal were identified before writing, and the first
  screen serves that goal. The first working path needs no unexplained setup
  or up-front choice among configuration alternatives, and it is safe to run
  on a developer machine as written.
- Examples teach intended idioms and identify relevant traps without turning
  a tutorial into an exhaustive list of alternatives.
- Examples are source-verified, complete at their stated scope, and show a
  checkable result; longer examples come from compiling sample or spec source.
  Explanations of what happened match the backend the reader built.
- Security, privacy and data-loss claims ("never logged", "encrypted", "safe to
  retry") are verified against the code that implements them, not only against
  other docs; narrow or drop one that cannot be shown.
- A page that describes a UI was checked by exercising a seeded system and
  comparing what appears with the prose; recommended, not required.
- The entry point, terminology, and next-step links fit the surrounding docs.
- A new or materially changed journey has fresh-context reader evidence, or
  explicitly reports that review as unavailable. Missing steps and guessed
  prerequisites are gaps, not reader success.
- All internal links resolve; all external links are real.
- Mermaid blocks are syntactically valid.
- The file ends with a single trailing newline.

## Classify before writing

Determine which quadrant the page belongs to before drafting:

| Type | Orientation | Analogy | When to use |
| --- | --- | --- | --- |
| **Tutorial** | Learning | A lesson | Guide a newcomer step-by-step to a successful first outcome |
| **How-to guide** | Problem-solving | A recipe | Show an experienced user how to accomplish a specific task |
| **Reference** | Information | A dictionary | Describe the technical machinery — APIs, attributes, configuration |
| **Explanation** | Understanding | A discussion | Clarify *why* something works the way it does, trade-offs, architecture |

Rules per type:

- **Tutorial** — lead a newcomer through one realistic, threaded outcome.
  Each step has a visible result. Briefly explain the invisible effect of a
  step, then link to an explanation for deeper theory; do not interrupt the
  lesson with a reference dump. Show the smallest *safe* configuration that
  runs before presenting options: bind local services to loopback, label
  development credentials as such, and put host and configuration
  alternatives in linked how-to guides. Make the first snippet the convention
  path, with no registration or wiring a convention already does, then name
  what the framework did that the reader cannot see.
- **How-to guide** — assume competence. State the goal, list prerequisites,
  give the steps and a completion check. Explain necessary concepts, effects,
  and recovery beside the step; link sustained teaching elsewhere.
- **Reference** — exhaustive within its declared scope and terse. For each
  public option or API, cover type, required/optional status, default, valid
  values, return behavior, errors and compatibility where applicable. State
  scope and link to a working usage example; do not turn it into a lesson.
- **Explanation** — discuss concepts, trade-offs, and design decisions. Short
  steps or examples can make an effect concrete without becoming a full
  procedure. Diagrams are welcome when they clarify a relationship.

A brief prerequisite or explanation may serve the main journey. If a reader
also needs an exhaustive lookup, link a reference page rather than burying it
in the lesson. Resolve routine audience or type choices from the request
and neighboring pages; ask only when the alternatives change the outcome.

## Work from the reader outward

1. Find the authored source, neighboring pages, existing user entry points,
   and current product behavior. Do not edit generated site copies. Identify
   whether the reader is new, migrating, troubleshooting, or looking up an API.
2. Identify the question the reader came with and what would answer it.
   Let the title and opening serve that task: a tutorial can introduce a concrete
   problem and payoff; a reference entry can state the requested contract directly.
   Include context when it helps the reader act or understand.
3. Trace a short route from that page to one clear 'start here' entry,
   targeted recipes, and exact reference. Give different languages or hosts
   their own procedures when a shared path cannot be run as written. A
   'coming from X' bridge should map familiar concepts to the new workflow,
   not replace it. When comparison helps readers choose among backends, use a
   "how the backends differ" table near the start for relevant distinctions:
   identity source, id and response wire types, default authorization, live versus
   snapshot queries, and unsupported features with their tracking issue when
   available. Across a series, keep backend limitations in one table on the series index
   and link it, while keeping action-changing differences beside the affected
   example. Keep essential shared context and warnings outside tabs. Check each
   client independently: shared concepts do not imply shared setup, semantics or
   support. Give divergent setup its own coherent procedure.
4. Link required tools to their canonical installation procedures before the
   first command. For cross-product prerequisites, use a verified public route;
   a named Markdown reference is defined on this same page, not in another
   repository. Do not revive DocFX `xref:` symbols or duplicate another
   product's setup recipe. See the documentation formatting rule for syntax.
5. Draft in workflow order. For a tutorial, use one working domain throughout
   and show what to run and what appears. Recap only when a longer thread needs
   reorientation before another concept. For a how-to, keep only what the
   specific task needs. Link out for details.
   Many Cratis readers are adopting a paradigm (event sourcing, event
   modeling, vertical slices), not only a tool: link the explanation from the
   start route, but let the reader reach a first success before the theory.
6. Read the rendered page as a newcomer: could they find it, begin without
   hidden prerequisites, recover from a common mistake, and recognize success?
   For a new or materially changed journey, read and use
   [fresh-context reader review](references/reader-review.md). An author's
   reread is useful but is not independent reader evidence. Check claims,
   examples, accessibility, links, and the owning gates separately.

## Guide the next useful action

Give enough context to choose a relevant path, then place each prerequisite
beside its first use. Distinguish optional integration from required setup.
For a procedure, show a small meaningful action, its visible result and any
invisible effect before linking the next task or exact reference. Develop the
explanation from that action using **Explaining code**, rather than publishing
a separate list of responsibilities, defaults and limits. Link
source-backed recovery at likely failure points; do not invent error messages
or recovery commands. Keep lookup pages concise and explanations purpose-led,
not a compulsory tutorial template.

For Astro pages, deliberately choose supported presentation from
[the site guide](references/cratis-site.md) when it helps the task: ordered
steps, consequential callouts, peer-choice cards, or diagrams and code emphasis.
A prose-only pass can miss useful aids; uniform decoration can obscure them.
Preserve semantic order, accessibility and text/AI export meaning. Select the
relevant perspectives in [reader review](references/reader-review.md),
proportionately, without requiring a reviewer for every lens.

## Examples

Use **cratis-technical-examples** to choose between a short verified
illustration and a snippet extracted from compiling, tested sample source.
Never transcribe an API from memory, hand-translate an unsupported client, or
claim a pasted block is runnable when it requires unstated setup. Show the
command and observable output for a substantial walkthrough.

Adding a backend or client to a product also changes pages the product's own
checks don't scan, such as cross-product and site-level pages. Search them for
single-language claims ("in C#", C#-only APIs presented as universal) and for
series indexes whose description, reading order or production claims no longer
hold.

## Cratis platform specifics

Read [Cratis site specifics](references/cratis-site.md) before writing a
Cratis product page. It covers the teaching components (`YouWillLearn`,
`Recap`, client tabs, including on site-owned pages), the variant-docs audit,
maturity and preview labeling, cross-product compatibility, the Prompter
feedback signal, and who decides page structure.

## More guidance

Read [writing guidance](references/writing-guidance.md) when you draft or
revise prose (voice, American English, diagrams, AI-assisted drafting), when
you handle reader feedback or support questions, when you edit or maintain an
existing page (authored source, `editUrl`, matching neighboring terminology),
or before putting editorial research anywhere public.
