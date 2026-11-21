---
name: cratis-ux-review
description: Use when judging from rendered screenshots whether an existing screen, dialog or short journey works for its user, before implementation handoff or after a meaningful UI change. Not for creating a visual identity, debugging broken requests, claiming WCAG compliance or replacing behavior specs.
license: MIT
---
<!-- cratis-ai-managed: skills/cratis-ux-review/SKILL.md -->

# Cratis UX review

A screenshot-grounded, severity-rated critique of a rendered UI against a stated user goal. Code that compiles, passes specs and follows the component rules can still have weak hierarchy, a missing empty or error state, unclear copy or inconsistent patterns. This skill looks at the result the way the user does.

It reviews; it does not build. Implement fixes with **cratis-arc-react-page**, **cratis-components-styling** and the repository's own frontend rules, then return here for after-captures.

## Boundaries

- Not a WCAG conformance claim. Accessibility risks seen in screenshots are reported as risks; keyboard, focus order, screen-reader names and measured contrast are verified separately (**cratis-components-accessibility**, a contrast checker or a DOM check) and otherwise marked UNVERIFIED.
- Not a behavior specification. A finding that needs a behavior decision goes back to the model or the specs, not into this report.
- Not request or data debugging. A screen that is blank because a request failed is a defect to hand to debugging, not a UX finding.
- Not a visual-identity exercise. Judge the screen against the project's templates, tokens and type scale, never against a preferred style.

## Input contract

Ask for what is missing and stop until the goal is known; severity is anchored to it.

1. **User goal and journey**: who the user is, what they are trying to finish, and the ordered screens or states of the journey.
2. **Project rules**: the repository's frontend rules, page templates, state conventions, tokens and type scale. Read them before judging. Project rules win over this skill (see below).
3. **Captures** produced by the `browser-operator` agent, following the permit, artifact and evidence rules of `.cratis/ai/rules/browser.md`. Hand it the URL or story, scope, permit and artifact directory; the main session does not drive the browser.

Capture matrix:

| Axis | Values |
| --- | --- |
| Viewport | 1280×720 and 390×844 |
| Theme | light and dark |
| State | populated, empty, loading, error and validation, where the screen can reach them |

Each capture records URL or story, build, viewport, theme, state and timestamp, and is saved under `.ai-work/<task>/browser/`. Use a local or staging copy; make no production writes and keep secrets and tokens out of captures, URLs and logs. A sign-in wall stops the run: record `UI UNVERIFIED — needs human sign-in` and review only what was captured.

A state the matrix names but the captures lack is listed as a gap, never assumed fine.

## Evidence rule

Every finding points at something visible in a named capture, with a region. No inferred behavior is stated as fact. A claim that screenshots cannot support (focus order, keyboard use, animation, latency, screen-reader output, a measured contrast ratio, behavior on a state not captured) is marked **UNVERIFIED** with the check that would settle it. UNVERIFIED items are never counted as defects. Illegible text is reported as illegible, never guessed. When a locator was verified in the DOM, add it to the evidence; otherwise omit it.

## Workflow

1. **Intake**: confirm goal, journey order and project rules; confirm the capture matrix and list gaps.
2. **View every capture** fully before judging. Per screen note the primary action, the visible states and anything illegible.
3. **Goal walk**: walk the journey as the stated user. Note where friction, doubt or a dead end appears. A problem that does not touch the goal rarely exceeds S2.
4. **Checklist pass**: run [references/checklist.md](references/checklist.md) screen by screen. It holds the ten Nielsen heuristics as task questions and the Cratis passes for hierarchy, density, consistency, states, accessibility risks and copy. Read it before the first pass.
5. **Journey pass**: consistency across screens, progress visibility, memory load across steps, and the worst moment plus the final screen.
6. **Calibrate**: apply the severity scale, merge the same issue seen on several screens into one finding that lists its occurrences, and remove anything without evidence.
7. **Report** using [references/report-template.md](references/report-template.md): findings, strengths, UNVERIFIED items and the handoff.

## Severity

Rate by impact on the stated goal, how many users meet it and whether they meet it repeatedly, following [Nielsen's severity ratings](https://www.nngroup.com/articles/how-to-rate-the-severity-of-usability-problems/).

| Level | Meaning |
| --- | --- |
| S4 | Blocks the goal, risks loss or harm; fix before release |
| S3 | Major friction; many users struggle or abandon |
| S2 | Minor; users recover quickly |
| S1 | Cosmetic polish |
| 0 | Not a defect: an observation, preference or open question |

S3 and S4 need visible evidence and a named basis. Never raise severity to look thorough.

## Findings format

Each finding has: an id (`F-01`, ordered by severity), a severity, evidence (capture and region, plus a locator when verified), an observation of what is visible, the impact on the user task, its basis (a Nielsen heuristic or a project rule), the smallest recommendation that uses existing Cratis Components, and an observable acceptance outcome with a confidence level (high, medium or low). The template is in [references/report-template.md](references/report-template.md).

- **Strengths** are listed separately (`P-01`), with what to keep.
- **No quota.** A review with two findings or none is a valid result; padding to a count is a defect of the review.
- The recommendation is the smallest change: a prop, a template, a token or a copy edit before any new component or layout.

## Project rules win

A recommendation never contradicts the repository's own frontend rules: page templates, state conventions, token and type scale, naming and copy rules. When the skill's checklist and a project rule disagree, follow the rule and record the disagreement as a preference (0) or a question for the owner, not a defect. When a rule itself causes the problem, report it as a question about the rule, with evidence.

## Handoff

Close every review with the handoff described in [references/report-template.md](references/report-template.md): prioritized findings with affected components and acceptance criteria, split into confirmed defects, preferences and verification requests, plus the after-captures needed to confirm each fix. Re-run only the affected captures after a fix; a fix is confirmed when the after-capture meets the finding's acceptance outcome.

## Sources

- [Nielsen's ten usability heuristics](https://www.nngroup.com/articles/ten-usability-heuristics/) and [severity ratings](https://www.nngroup.com/articles/how-to-rate-the-severity-of-usability-problems/): cited; checklist wording is original.
- The evidence rule, journey review and severity structure adapt ideas from `EliaAlberti/ux-audit-skill` (MIT); the notice is in this skill's `LICENSE`.
