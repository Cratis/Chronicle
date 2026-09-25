---
agent: agent
description: "Write reader-centered documentation using the Cratis documentation skills."
---
<!-- cratis-ai-managed: prompts/write-documentation.prompt.md -->

# Write Documentation

Write documentation for a feature, component, or concept. Follow
`.cratis/ai/rules/documentation.md` and invoke **cratis-documentation-writing**.
For a Cratis product page, also use **cratis-engineering-docs-authoring**;
for samples or snippets, use **cratis-technical-examples**.

## Determine the reader's job

From the request and neighboring authored pages, identify the reader, the
question they have, the outcome they need, and the page's primary purpose:
tutorial, how-to, explanation, or reference. Ask only when plausible choices
would materially change the result; don't require an outline approval before
ordinary drafting. Find the owning source before editing, never a synced copy.

## Write and verify

Lead with the problem and payoff. Use active voice and show how the reader can
recognize success. A tutorial can briefly explain an observed result without
turning into a reference dump; link to deeper material. Verify framework APIs
against first-party source at the applicable version, check the sample at its
claimed scope, and run the owning repository's content and link gates. Add a
new page to the owning product's `toc.yml`
(a page missing from it silently drops out of the sidebar); otherwise update
navigation only when the page's placement or route changes.
