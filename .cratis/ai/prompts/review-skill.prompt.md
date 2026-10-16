---
agent: agent
description: Review one skill for clarity, trigger quality, and maintainability.
---
<!-- cratis-ai-managed: prompts/review-skill.prompt.md -->

# Review Skill

Read [authoring-skills.md](../rules/authoring-skills.md), then apply its section 10
checklist to the requested skill folder under `.cratis/ai/skills/`. Check routing
against neighboring skills and workflow correctness against the owning product.

Report blocking findings separately from suggestions, with the file and concrete
consequence. Name any evaluation runs or verification not performed. Return findings
first, then proposed edits; do not edit during the review.
