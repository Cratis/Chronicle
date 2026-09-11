<!-- cratis-ai-managed: skills/cratis-engineering-docs-authoring/references/site-format.md -->
# Cratis documentation site format

Use these rules for a page that will render on the Cratis Astro Starlight site.
The owning repository remains authoritative when it defines a stricter format.

## Frontmatter and headings

- Include `title` and `description` frontmatter.
- Do not add a body H1; the site renders the title as H1.
- Start body sections at H2.
- Use sentence case and no trailing punctuation in headings.
- Keep the page's main workflow visible in H2 sections.

## Code and commands

- Tag every code fence with its language.
- Dedent copied snippets to their natural source indentation.
- Use complete, runnable examples without ellipses.
- Verify examples against first-party product source.
- Use the client-owned multi-language snippet mechanism when shared product docs
  support more than one client; do not hand-translate unsupported clients.

## Links and navigation

- Use descriptive link text, never "here" or "read more."
- Use root-relative links between products.
- Keep site-level links extensionless.
- Preserve the owning product repository's source-link convention.
- Do not edit generated synchronized pages; edit the owning source repository.

## Tables, asides, and diagrams

- Use GitHub-Flavored Markdown tables with a spaced separator row.
- Use Starlight or owning-repository note/caution syntax for boundaries and
  security warnings.
- Use Mermaid for architecture, sequence, or state explanations.
- Give images meaningful alternative text.

## File hygiene

- Use American English.
- End the file with one newline.
- Keep project paths, credentials, local endpoints, and private data out of
  shared documentation.
- Run the owning repository's build, lint, snippet, and link checks before
  calling the page complete.
