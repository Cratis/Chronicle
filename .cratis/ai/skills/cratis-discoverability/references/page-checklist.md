<!-- cratis-ai-managed: skills/cratis-discoverability/references/page-checklist.md -->
# Page checklist

## Contents

- Field targets
- Every page
- Docs pages
- Blog posts
- Landing pages
- READMEs
- Before publishing a site change
- Docs-site guardrails

## Field targets

Lengths are targets, counted in characters including any site suffix.

| Field | Target |
| --- | --- |
| Title | Distinguishing term first. Blog and landing pages 50-60; docs and reference pages 20-60; a README H1 that is the product name is fine |
| Description | One sentence, 60-160, never over 160; the point comes first |
| Lead answer passage | 40-60 words, names its subject, one key property, one limit |
| Heading | Under about 70 characters |
| `og:image` | 1200x630, under 200 KB, with `og:image:alt` |

## Every page

- [ ] One reader job. For docs, one page type: tutorial, how-to, reference or explanation.
- [ ] Title unique, distinguishing term near the front.
- [ ] Description written by hand; not the title, not another page's description.
- [ ] The `description` answers what the page is or achieves in one self-contained sentence; the first paragraph does too on landing, index, glossary and README pages.
- [ ] Canonical names; first mention uses the full "Cratis X" form.
- [ ] Facts, numbers and comparisons carry a source, and counts also carry a date; nothing beyond the claim gate.
- [ ] Links have descriptive text. Root-relative inside the docs site, absolute
      `https://www.cratis.io/...` elsewhere.
- [ ] Images have alt text (empty only when decorative) and sensible size.
- [ ] Linked from at least two other pages and from the right hub; no orphan.
- [ ] Read aloud once: if it sounds written for a crawler, rewrite it.

## Docs pages

- Voice and structure follow `writing-cratis-docs`, which wins on any conflict: tutorials,
  getting-started pages and explanations open with a concrete scenario (pain, then relief). Put the
  answer-first definition in the frontmatter `description`; one definition sentence may follow the
  scenario where it reads naturally.
- Product index page: opens with the one-sentence definition, then the without-versus-with framing.
- Concept page: scenario first; then why it exists, when to use it, when not to, how it works.
  H2s such as "What is X" and "When to use X" only where they fit. No H1 in the body.
- Tutorial: title names the outcome and starts with a verb; the scenario, then what the reader will
  have running; prerequisites and versions up front; one visible result per step; "Last updated" and the
  version it applies to.
- How-to: the task as the title; goal, prerequisites, numbered steps, a done check, the two likely
  failures; versions stated.
- Reference: the title is the exact name with its product; first sentence says what it is for;
  tables and signatures; stable anchors; no intro, no pitch. Merge or enrich near-empty stub pages.

## Blog posts

- [ ] Title states the topic; series name goes in a `series` field or at the end.
- [ ] Two-sentence conclusion up front; "when this is the wrong fit" where it applies.
- [ ] Named author where agreed (`cratis-team` only for company announcements), date, `lastUpdated`
      only on real changes.
- [ ] At least one internal link to the owning docs page; tags reused from the existing list, no
      singleton tags; a cover image with alt text for posts likely to be shared.

## Landing pages

- [ ] One primary term in title, `<h1>` or first paragraph; one-sentence definition above the fold.
- [ ] Description differs per page and is not a 300-character paragraph.
- [ ] No testimonials, numbers, customers, prices or maturity claims the claim gate does not allow.
- [ ] Norwegian pages are written natively, keep established English technical terms, link to the
      English page with `hreflang`, and are not machine-translated and published unreviewed.

## READMEs

- [ ] H1 may be the product name ("Cratis <Product>"); first sentence is the definition (15-35 words, no metaphor).
- [ ] Then install, a minimal working example, a link to `https://www.cratis.io/<product>/`,
      license and how it relates to the rest of Cratis.
- [ ] GitHub About text equals the README opener (80-160 characters); website is the docs page;
      topics include `cratis` plus category terms.

## Before publishing a site change

- [ ] Title, description, canonical, Open Graph and JSON-LD present and unique on changed pages.
- [ ] Sitemap lists new pages; `robots.txt` unchanged unless intended.
- [ ] JSON-LD validates; no `HowTo`; no new `FAQPage`.
- [ ] The content is in the raw HTML the server returns, not only rendered by JavaScript.
- [ ] Internal links resolve; no apex `cratis.io` links.
- [ ] A real HTTP 404 for unknown URLs; redirect stubs keep a canonical and stay out of the sitemap.

## Docs-site guardrails

- Keep missing-description and duplicate-title lint baselines at 0. Never raise a baseline to pass;
  fix authored metadata or its generator and rerun the site's lint with no new exceptions.
- Inspect rendered browser titles, not only frontmatter: retain the product name, add language
  context for variants and distinguish repeated generic titles without changing page headings.
- Measure UTF-8 bytes of `llms.txt` and `llms-small.txt`, including product/nested-product exports,
  against the site's configured budgets. Keep `llms.txt` an index and `llms-small.txt` curated
  starting points with one-line summaries, not a full inventory. Trim duplication and curation,
  not human documentation or useful code examples. Do not raise budgets to hide growth.
- Run the site's lint and enforced export-budget checks after its build; require exit 0 and
  measured files within their limits. The built-site checker alone does not enforce these gates.
  For export design, use **cratis-llm-friendly-documentation**.
