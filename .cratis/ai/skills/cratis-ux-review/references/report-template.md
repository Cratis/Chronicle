<!-- cratis-ai-managed: skills/cratis-ux-review/references/report-template.md -->
# Report template

Write the report under `.ai-work/<task>/` unless the user names a location. Keep it short enough to act on.

## Header

- **Goal and user**: the stated goal and who is pursuing it.
- **Project rules read**: the files consulted.
- **Captures reviewed**: a table of capture file, URL or story, build, viewport, theme, state and timestamp.
- **Gaps**: matrix cells with no capture.
- **Not assessable from screenshots**: the items marked UNVERIFIED below, collected.

## Finding

```markdown
### F-01 · S3 · Short title
- **Evidence**: capture `checkout-populated-1280-light.png`, region: order summary, lower right. Locator: `[data-testid=order-total]` (only when verified in the DOM).
- **Observation**: what is visible, without interpretation.
- **Impact on the task**: how this slows, misleads or blocks the user's goal.
- **Basis**: Nielsen heuristic 5 (error prevention), or the project rule by file and heading.
- **Recommendation**: the smallest change that uses an existing component, template or token.
- **Acceptance**: an observable outcome in a named capture, for example "the delete dialog opens with Cancel focused and the confirm button uses the destructive variant at both viewports".
- **Confidence**: high | medium | low, with the reason when not high.
```

Rules for findings:

- One issue per finding; merge repeats across screens and list each capture.
- The recommendation never introduces a new component when a prop, template or token fixes it, and never contradicts a project rule.
- An acceptance outcome that nobody could observe in a capture is rewritten until someone could.

## Strengths

```markdown
### P-01 · What works
- **Evidence**: capture and region.
- **Keep because**: the heuristic or project rule it satisfies.
```

List strengths separately from findings. Do not add strengths to balance the findings.

## UNVERIFIED

```markdown
- **U-01**: Focus order in the create dialog. Not visible in a screenshot. Verify by tabbing through the dialog; see cratis-components-accessibility.
```

Each item names the claim and the check that would settle it. These are never defects until verified.

## Handoff

1. **Confirmed defects**, ordered S4 to S1. For each: id, severity, affected components (the Cratis Components control, template or page), the acceptance outcome and the after-capture needed to confirm it.
2. **Preferences**: severity 0 observations and rule disagreements, with the owner who decides.
3. **Verification requests**: each UNVERIFIED item with the check, who can run it and the capture or DOM evidence expected.
4. **After-captures**: a list of capture names (URL or story, viewport, theme, state) to retake after fixes. Each fix is confirmed only when its after-capture meets the finding's acceptance outcome.

Close with the three highest-priority fixes in chat. Do not open issues or tickets unless asked.
