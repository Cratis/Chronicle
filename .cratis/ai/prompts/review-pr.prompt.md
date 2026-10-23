---
agent: agent
description: Review a pull request against all Cratis project standards and produce a structured review report.
---
<!-- cratis-ai-managed: prompts/review-pr.prompt.md -->

# Review Pull Request

Produce a structured review of a pull request against all Cratis standards.

## Confirm first

- **PR number or branch**, and the affected repos/projects.

## Process

1. **Gather context** — list and read every changed file; identify the slice type(s).
2. **Architecture & quality** — run the **Code Reviewer** agent (it checks `.cratis/ai/rules/` and folds in the performance pass).
3. **Security** — run the **Security Reviewer** agent.
4. **Spec coverage** — confirm each slice has specs (happy path + each failure); confirm tests pass if runnable.
5. **Docs** — confirm public-facing changes updated documentation.

## Output

Read and use `.cratis/ai/rules/review-reports.md` before writing the report.
Open with the PR number and a short summary. Keep one report per reviewer using
the shared Blockers / Suggestions / Dismissed (with reason) / Verdict sections
and Not examined line. Include specification-coverage and documentation checks
in the relevant reviewer's scope, with any missing evidence made explicit.

End with one **Combined verdict** using the shared aggregation rule, preserving
each reviewer's findings, dismissals, and scope limits.
