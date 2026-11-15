---
agent: agent
description: "Write reader-centered documentation using the Cratis documentation skills."
---
<!-- cratis-ai-managed: prompts/write-documentation.prompt.md -->

# Write Documentation

Write documentation for a feature, component, or concept. Follow
`.cratis/ai/rules/documentation.md` and use **cratis-documentation-writing**.

## Read the applicable skills first

Read the actual applicable `SKILL.md` files before drafting, in this order:

1. `.cratis/ai/skills/cratis-documentation-writing/SKILL.md` for reader journey
   and page purpose.
2. `.cratis/ai/skills/cratis-engineering-docs-authoring/SKILL.md` for
   source-grounded authoring of a Cratis product page.
3. `.cratis/ai/skills/cratis-technical-examples/SKILL.md` when including code,
   commands, configuration or observable output.
4. `.cratis/ai/skills/cratis-llm-friendly-documentation/SKILL.md` when discovery,
   exports, mirrors or citations are involved.
5. `.cratis/ai/skills/cratis-writing-voice-and-cadence/SKILL.md` before revising
   existing prose, after establishing its claims.

Read referenced material when its condition applies. Use the installed canonical
equivalents if these repository paths are absent; report an unavailable skill
instead of claiming it was read. Skill availability or invocation alone is not a read.
In the private author/change note, record paths, source revision, and observed read order
when transcript evidence exists; otherwise say what is unavailable. Do not add
a public verification receipt or infer hidden reads from the skill list.
For a new or materially changed journey, use the reader-review procedure linked
by **cratis-documentation-writing**. Do not spawn a reader agent or run a live
model evaluation without authorization; report unavailable independent review.

Follow the documentation rule's naming contract: Cratis is the organization,
Cratis Platform the umbrella, and named products are independently adoptable
components or supporting tools. Keep literal APIs and package IDs unchanged.

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
claimed scope, explain ownership and observable handoffs between components, and run the owning repository's content and link gates. Add a
new page to the owning product's `toc.yml`
(a page missing from it silently drops out of the sidebar); otherwise update
navigation only when the page's placement or route changes.
