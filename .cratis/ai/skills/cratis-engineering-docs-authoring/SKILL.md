---
name: cratis-engineering-docs-authoring
description: Draft accurate Cratis product or engineering documentation once the reader's goal, owning source, and product evidence are known. Use for tutorials, how-to guides, explanations, and references; use the owning repository's navigation and visual QA workflows for placement and rendering.
license: LICENSE
---
<!-- cratis-ai-managed: skills/cratis-engineering-docs-authoring/SKILL.md -->

# Cratis documentation authoring

Draft one accurate Cratis documentation page in the voice and structure required
by its document type. This skill owns **content**. It does not decide which
repository owns a page, wire site navigation, locate an existing source page, or
perform visual QA.

## Required inputs

Before drafting, establish:

- the owning repository and destination page;
- document type: tutorial, how-to, explanation, or reference;
- target reader and the outcome they need;
- authoritative product source for every API, command, version, and capability;
- explicit scope and important exclusions.

Use repository evidence to resolve routine details. Ask only when materially
different document types, audiences, or product choices remain plausible.

## Route near misses

- New-page placement or navigation is unresolved: read the owning repository's
  source map, site configuration, and local documentation instructions first.
- The source of an existing page is unresolved: find its authored source before
  editing; never patch a synchronized copy.
- Rendering or visual layout is requested: use the owning site's preview and
  screenshot workflow, checking both light and dark presentation when relevant.
- A product/API claim lacks first-party source evidence: stop and identify the
  missing authority instead of drafting the claim.
- The subject is not Cratis product or engineering documentation: do not apply
  this skill.

## Write to one primary reader need

Use Diátaxis to choose the page's main job. Include brief context needed to make the task work; link out instead of embedding a different page's full lesson or lookup:

| Type | Reader need | Shape |
| --- | --- | --- |
| Tutorial | Learn by completing a guided outcome | Ordered steps with visible results |
| How-to | Solve one concrete problem | Prerequisites, direct procedure, completion check |
| Explanation | Understand why and when | Concepts, boundaries, trade-offs, diagram |
| Reference | Look up exact information | Exhaustive tables, fields, commands, signatures |

For the detailed mechanical format, read
[site-format.md](references/site-format.md).

## Drafting workflow

1. Open with the reader's concrete friction and the Cratis capability that
   relieves it.
2. Organize by the reader's workflow, not by implementation namespaces or an
   alphabetical API dump.
3. Use active voice, present tense, second person, and American English.
4. Explain the invisible behavior after each example: what the framework does
   and why the boundary matters.
5. Use **cratis-technical-examples** for code and samples: verify every API
   and command against first-party source at the applicable revision, and
   never translate a C# example into another client language by guess.
6. State maturity, authorization, side effects, unsupported surfaces, and when a
   simpler approach is better.
7. Show a visible result in tutorials and procedures. Use Mermaid for a
   non-trivial explanation.
8. End with the natural next page or workflow.

## Correctness boundary

Never invent product APIs, customer claims, versions, support commitments,
marketplace availability, or private implementation details. Do not copy a code
sample from memory. If the source cannot prove a claim, omit it or mark the gap
for the owning maintainer.

A successful build proves rendering, not technical correctness. The owning
repository still runs its documentation, snippet, link, and product gates.

## Output

Return or write the page content only at the already approved destination. Do
not modify navigation, generated copies, project context, credentials, package
manifests, or unrelated documentation. Report the authoritative source checked
and the verification that still remains.
