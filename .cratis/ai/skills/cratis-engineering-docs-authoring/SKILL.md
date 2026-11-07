---
name: cratis-engineering-docs-authoring
description: Use when drafting a Cratis tutorial, how-to, explanation or reference after its reader, owning page and product evidence are known. Not for locating pages, wiring navigation or visual QA.
license: LICENSE
metadata:
  cratis-hint-paths: "**/Documentation/**/*.{md,mdx}"
---
<!-- cratis-ai-managed: skills/cratis-engineering-docs-authoring/SKILL.md -->

# Cratis documentation authoring

Draft one accurate Cratis documentation page in the voice and structure required
by its document type. This skill owns **content**. It does not decide which
repository owns a page, wire site navigation, locate an existing source page, or
perform visual QA.

Read **cratis-documentation-writing** for the reader journey and the
[documentation rule](../../rules/documentation.md) for Platform naming and
product-claim constraints. Use those shared contracts rather than inventing
a second review procedure here. Before drafting, read and use
[Explaining code](references/explaining-code.md), including both worked
transformations. Choose the detail in your example that will carry its
explanation; apply the multi-client section before choosing shared prose,
tabs or separate guides.

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
| How-to | Solve one concrete problem | Prerequisites, direct procedure, necessary concepts and effects beside the step, completion check |
| Explanation | Understand why and when | Concepts, boundaries, trade-offs; useful diagrams or short examples, not a full procedure |
| Reference | Look up exact information | Exhaustive tables, fields, commands, signatures; short explanation where a default or interaction needs it |

Read [site-format.md](references/site-format.md) when drafting a page for the Cratis Astro Starlight site; the owning repository’s stricter format remains authoritative.

## Drafting workflow

1. Open where the reader's task starts. A tutorial can introduce a concrete
   problem and the capability that helps; a lookup can state its contract directly.
   Include context when it helps the reader act or understand.
2. Organize by the reader's workflow, not by implementation namespaces or an
   alphabetical API dump.
3. Use active voice, present tense and American English. Address the reader
   for actions; use the component or API as subject when explaining its contract.
4. Develop the explanation from the concrete example, following **Explaining
   code**. Establish what changes and why it matters, then build toward the next
   action or expectation. Check ownership, host boundaries, connecting identifiers,
   defaults, observable results and failure/recovery behavior as relevant. Weave
   the answers into the explanation where they help; they are not a public
   paragraph template. Include only components the task needs.
5. Use **cratis-technical-examples** for code and samples: verify every API
   and command against first-party source at the applicable revision, and
   never translate a C# example into another client language by guess.
6. Explain maturity, authorization, side effects, unsupported surfaces or a
   simpler alternative when they change the reader's choice, action or expectation.
   Keep an important warning before the risky action and a consequential limit
   beside the behavior it qualifies.
7. Show a visible result in tutorials and procedures. Use Mermaid where it
   clarifies a non-trivial relationship, not as a requirement on every explanation.
8. Read the resulting explanation as connected prose, using **Explaining code**
   to repair a missing connection rather than add stock labels. Keep shared
   concepts distinct from source-checked client behavior; code shape alone
   proves no parity. Link the next page or workflow when the reader has a useful
   next step; do not add a stock ending to a complete answer.

## Correctness boundary

Never invent product APIs, customer claims, versions, support commitments,
marketplace availability, or private implementation details. Do not copy a code
sample from memory. If the source cannot prove a claim, omit it or mark the gap
for the owning maintainer.

A successful build proves rendering, not technical correctness. The owning
repository still runs its documentation, snippet, link, and product gates.
For a new or materially changed journey, use the
[fresh-context reader review](../cratis-documentation-writing/references/reader-review.md)
owned by **cratis-documentation-writing**. Report unavailable independent review;
source checks and a successful build do not prove comprehension.

## Output

Return or write the page content only at the already approved destination. Do
not modify navigation, generated copies, project context, credentials, package
manifests, or unrelated documentation. Report the authoritative source checked
and the verification that still remains.
