---
applyTo: "**/*.cs,**/*.ts,**/*.tsx,**/*.go,**/*.kt,**/*.kts,**/*.java,**/*.ex,**/*.exs"
paths:
  - "**/*.cs"
  - "**/*.ts"
  - "**/*.tsx"
  - "**/*.go"
  - "**/*.kt"
  - "**/*.kts"
  - "**/*.java"
  - "**/*.ex"
  - "**/*.exs"
---
<!-- cratis-ai-managed: harnesses/copilot/instructions/code-health.instructions.md -->
<!-- cratis-ai: generated Copilot instructions. Do not edit; rules/ is the source. -->

# Code Health floor

Applies when CodeScene tools (`code_health_review`, `code_health_score`,
`pre_commit_code_health_safeguard`, `analyze_change_set`) are available. Without them or
an account, say so in the report and continue; never block. MCP setup, approval-gated
tools and tool-output handling are in [codescene.md](../../../rules/codescene.md).

## The floor

- Every file a change creates or modifies ends at Code Health **9.5 or higher**; new files aim for 10. A touched file never declines.
- A touched file that starts below 9.5 is raised to 9.5 in the same change by behavior-preserving refactoring with specs behind it (`guiding-refactoring-with-code-health` where the profile provides it).
- If that is too large for the task (a red or yellow legacy file), raise it as far as is safe, report before and after, and add the file to the repository's Code Health uplift issue ("Code Health: raise all files to 9.5"; create it if none exists).
- Urgent security and production fixes may ship below 9.5, but report the gap and file the uplift.
- Never refactor generated code. A "could not determine" Code Health score is not a failure. Other Cratis rules win over a finding.
- CodeScene has no floor setting, so the check is ours.

## Checking

- Run `code_health_review` on each changed file while shaping it, then `code_health_score` on each changed file before delivery, then `pre_commit_code_health_safeguard` (or `analyze_change_set` against the base branch).
- The delivery report has a table: file, score before, score after; name each exception with its reason.

## No fix loops

- **Once, before the gate.** Handle Code Health once per touched file: review while shaping the code (design for the score, do not write first and fix later), refactor, then one `code_health_score`. The gate and review then run once on the final shape.
- **Never a cycle.** No gate, score, refactor, gate loops. A score check never triggers an extra build or test run on its own; batch score fixes with the other fixes.
- **One refactor pass per file per change.** If it does not reach 9.5, report the before and after and the reason, add the file to the uplift issue, and stop.
- **A reported reason closes the item.** Reviewers flag only a touched file below 9.5 with no reason, together with the round's other findings, never as a review round of its own.
- **A second Code Health round is itself a finding.** Name the cause (wrong approach, file too large for the task, tool problem) instead of iterating.
