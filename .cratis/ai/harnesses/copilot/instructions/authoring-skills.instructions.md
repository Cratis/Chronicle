---
applyTo: ".cratis/ai/skills/**,.cratis/ai/rules/**,.cratis/ai/agents/**,.cratis/ai/prompts/**"
paths:
  - ".cratis/ai/skills/**"
  - ".cratis/ai/rules/**"
  - ".cratis/ai/agents/**"
  - ".cratis/ai/prompts/**"
---
<!-- cratis-ai-managed: harnesses/copilot/instructions/authoring-skills.instructions.md -->
<!-- cratis-ai: generated Copilot instructions. Do not edit; rules/ is the source. -->

<!-- Copyright (c) Cratis. All rights reserved. -->
<!-- Licensed under the MIT license. See LICENSE file in the project root for full license information. -->

# Authoring skills and instructions

## 1. What a skill is

Trigger (when to use / when not) · the how (steps, patterns, one minimal correct example each) · choosing (use X when…, not Y) · pitfalls (with the diagnostic code you'll see) · verification (command + what a pass looks like). Timeless: describe current behavior without saying it is current.

A code-breaking prerequisite belongs in one `Requires` line with the version-check command and the fallback. This is a compatibility gate, not a release history. Use American English in authored text.

## 2. Frontmatter and routing

Use portable fields: `name`, `description`, `license`, `compatibility`, `metadata`, `allowed-tools`. Keep extension metadata under `metadata` so other harnesses can load the skill.

- Match `name` to the directory: 1–64 lowercase letters, digits and single hyphens; no edge hyphen or reserved `anthropic`/`claude` words. These constraints keep discovery portable.
- Lead the description with what the skill does, then `Use when…` and `Not for…` (name the sibling to use). Include concrete words from the user's request so the router can distinguish neighbors.
- Use neutral imperative or third-person phrasing, not `I`/`you`/`your`; descriptions enter instruction context, where shifting voice obscures who acts. Quoted command text and inline code are not the author's voice.
- Aim below 700 characters, within the hard 1–1024 limit. No tag-like text, versions or research-process vocabulary: the listing is for routing, not evidence.

An explicit trigger clause (`Use when`, `Load when`, `Read when` or an equivalent) and neutral voice are required even with `SKILL_AUTHORING_MODE=warn`. Negative routing and meta-language findings follow that switch; the 700-character target is guidance, not a gate.

## 3. Budget and progressive disclosure

Target at most 400 body lines and 16,000 characters; the hard limits remain 500 lines and 20,000 characters. A warning starts above the 16,000-character target, so a body that outgrows it is split before it nears the hard limit; characters only approximate tokens.

Keep decision rules, the core workflow and pre-action traps in `SKILL.md`. Move conditional detail to `references/`, with a direct `Read references/x.md when…` pointer for every file, including assets and scripts. `LICENSE`, `NOTICE` and `verification.json` are exempt as licensing/verifier metadata; existing reasoned asset exceptions fail when stale. Direct pointers prevent partial nested reads from hiding required context. References over 100 lines open with generated `## Contents`; a single fenced example with an early introduction is exempt because it has no sections to navigate. Use forward-slash paths for portability.

## 4. Timeless text

Describe behavior, syntax and diagnostics without dates or relative-time claims. Remove release/probe results, issue/PR/decision numbers, source-line citations, commit hashes, bundled versions and research-only pages; these age without helping the next task. No provenance, contradictions or release-baseline pages, nor verified-source headings in skills. Preserve a diagnostic's identifier, not the investigation history.

Before removing a version, decide what it marks; deleting it blindly turns a true statement into a false one:

- **History** (when something was verified, probed or shipped): remove it.
- **The release that introduced a behavior**: keep it as a `Requires` line naming that release, confirmed in the product's release tags, never the version something happened to be checked at.
- **A behavior that changed, or a defect fixed, in a release**: keep a `Requires` line that also says what older releases do, or remove it only when no supported user can run an older release.

Keep pins in `product-sync.json` and compatibility documentation under `Documentation/`, not repeated prose. Track product-documentation conflicts in the product's issues. A minimum-construct table is useful only when it is short and contains actual code-breaking prerequisites.

## 5. Instruction style

Give one default and the condition for choosing an alternative, rather than an equal-options menu. Put the reason beside an absolute so the agent can recognize its boundary; volume and ALL-CAPS add no evidence. Prefer concrete, checkable directions over vague quality requests. Use the terms in [glossary.md](./glossary.instructions.md); define an unfamiliar term on first use to avoid competing meanings. Show one minimal correct example per pattern and an exact verification command with the observable pass condition.

## 6. One source of truth

Link the owning rule instead of copying it into skills, agents or prompts; copies diverge on the next edit. A pointer may state when to read that rule, but does not redefine it. Resolve contradictions at their source rather than relying on a harness's precedence behavior.

## 7. MCP tools

Name the server once, then its protocol tool: for example, the `screenplay` MCP server's `propose-ast` tool. Resolve the harness-native invocation name from the connected client's tool listing instead of assuming one naming scheme works in every harness. For servers the corpus does not register, resolve the user's configured alias; a fixed prefix can address the wrong server.

## 8. Deterministic scripts

Put repeatable mechanical operations in `scripts/` when a script can eliminate repeated reasoning. Say explicitly to execute it (or, when needed, read it as reference), with prerequisites, inputs, errors and the pass signal. Handle known errors in the script and preserve the command's exit status; apparent success after an unperformed check is not verification. Keep judgment in prose.

## 9. Evaluations

Give new skills both trigger and output evaluation inputs. Existing unevaluated skills are a shrinking baseline, not permission to exempt new ones. Each evaluation set has 16–20 unique trigger queries: 8–10 positive (`shouldTrigger: true`) and 8–10 negative (`shouldTrigger: false`), plus 3–5 output tasks with 3–5 nonempty assertions each. Make negatives tricky near-misses and name the sibling that should win in their notes; that note quality is review guidance, not a shape check. Compare with-skill outputs to a without-skill baseline to show what the instructions improve.

When changing a description, run trigger evaluations before and after when authorized to spend model usage. Keep results untracked in `.ai-work/skill-evaluations/` and summarize them for reviewers, not in the skill. CI validates inputs without calling models; missing model runs remain explicitly unverified.

## 10. Review checklist

- What, when, when-not and sibling routing are explicit; voice is neutral and routing text has no meta/history claims.
- Body has headroom; every reference, asset and script is directly named; long references have Contents.
- Text is timeless; no research-only pages or verified-source headings remain.
- Defaults, escape conditions, adjacent reasons, glossary terms and correct examples are clear.
- Rules are linked, not duplicated; MCP server and native naming are resolved.
- Mechanical steps use scripts where warranted; verification names the command and pass condition.
- Evaluation inputs cover positive and near-miss routing and baseline output behavior; report any runs not performed.
- Always-on content meets the budget below. Apply the same timelessness and clarity review to rules, agents and prompts.

## 11. Always-on content

Keep `general.md` to facts needed in every session, at most 200 lines. Warn above 24,000 bytes and above 91,000 combined bytes for rules without `paths`; line length alone can hide context bloat. Review globally applied Copilot instructions above 4,000 characters. These budgets leave room for source and task context, rather than promising a token count.

Use path-scoped rules for file-specific invariants and skills for particular operations. Hooks and CI enforce mechanical constraints; natural-language rules do not replace permission controls.

## Sources

- https://agentskills.io/specification
- https://platform.claude.com/docs/en/agents-and-tools/agent-skills/best-practices
- https://github.com/anthropics/skills/blob/main/skills/skill-creator/SKILL.md
- https://code.claude.com/docs/en/memory
- https://learn.chatgpt.com/docs/agent-configuration/agents-md
- https://developers.openai.com/cookbook/examples/skills_in_api
- https://docs.github.com/en/copilot/tutorials/customize-code-review
- https://cursor.com/docs/context/rules
