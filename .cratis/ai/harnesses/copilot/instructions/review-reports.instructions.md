---
applyTo: ".cratis/ai/skills/cratis-code-review/**,.cratis/ai/skills/cratis-security-review/**,.cratis/ai/skills/cratis-performance-review/**,.cratis/ai/agents/**,.cratis/ai/prompts/**"
paths:
  - ".cratis/ai/skills/cratis-code-review/**"
  - ".cratis/ai/skills/cratis-security-review/**"
  - ".cratis/ai/skills/cratis-performance-review/**"
  - ".cratis/ai/agents/**"
  - ".cratis/ai/prompts/**"
---
<!-- cratis-ai-managed: harnesses/copilot/instructions/review-reports.instructions.md -->
<!-- cratis-ai: generated Copilot instructions. Do not edit; rules/ is the source. -->

<!-- Copyright (c) Cratis. All rights reserved. -->
<!-- Licensed under the MIT license. See LICENSE file in the project root for full license information. -->

# Shared review reports

Use this report shape for code, security, performance, and repository-investigation
reviews. Domain-specific risk labels refine findings, not the report or verdict
vocabulary. Review the change within the declared scope, not unrelated existing debt.

## Report shape

Keep these sections in order, writing `None` when a section has no entries:

- **Blockers** — findings that must be fixed before merge. Each names a file and
  line or evidence reference, the problem, its concrete consequence, and a proposed
  correction. A blocker names a consequence, not a rule number: "returns HTTP 500
  instead of a validation error" explains harm; "violates rule 4" does not.
- **Suggestions** — non-blocking improvements, with their benefit and any relevant
  conditions. Do not turn a preference or a hypothetical risk into a merge gate.
- **Dismissed (with reason)** — candidate findings considered and rejected, each
  with the evidence or reasoning for dismissal so another reviewer need not raise
  it again. Do not invent candidates to populate this section.
- **Verdict** — one of the values below, scoped to the evidence actually examined.

An empty Blockers section is a valid, complete review. Never manufacture a finding
to make a review look useful. Reserve **[BLOCKING]** exclusively for
must-fix-before-merge findings; suggestions and dismissed findings never carry it.

Every review includes a **Not examined:** line naming the paths, surfaces, checks,
and axes not examined, including anything that could not be reached. Name what was
not measured: code-shape inspection is not a benchmark, a build is not behavioral
proof, and an unexamined surface is not verified. State `None` only when the entire
declared scope was examined; otherwise give concrete limits and remaining checks.

## Verdict vocabulary

| Verdict | Meaning |
| --- | --- |
| **Approved** | No blockers or suggestions within the examined scope; required review checks are complete. |
| **Approved with suggestions** | No blockers; non-blocking suggestions remain and required review checks are complete. |
| **Changes requested** | At least one blocker must be corrected before merge. |
| **Incomplete** | No confirmed blocker, but missing or inconclusive required evidence prevents approval. |

Skipped optional axes belong on the Not examined line and do not alone prevent a
scoped approval. Missing required evidence does. When blockers and evidence gaps
coexist, use Changes requested and record the gaps on the Not examined line; never
reinterpret a failed or blocked gate as passing.

When combining reviews, retain one report per reviewer with the shared sections
and scope line, then give one combined verdict. Any blocker means Changes requested;
otherwise any Incomplete review means Incomplete; otherwise any remaining suggestion
means Approved with suggestions; otherwise use Approved. Do not drop dismissals or
scope limits when combining reports.

When a workflow requires a typed review envelope, preserve its schema and express
these sections, verdict meanings, and scope limits through its existing fields.
Do not append a competing Markdown report or invent envelope fields.

## Reviewer independence

- **[contract]** Run a reviewer on a different provider family than the author's model
  where the harness can select one. The reviewer agents declare
  `reviewIndependence: different-provider` and the `reviewer` model tier.
- **[contract]** Where the harness cannot select a provider, state `single-provider` in the
  report header and on the Not examined line.
- **[contract]** A provider or tool error makes the review **Incomplete**. Fall back once
  to the other family. Never report a failed verdict, or Approved, because the reviewer
  could not run.
