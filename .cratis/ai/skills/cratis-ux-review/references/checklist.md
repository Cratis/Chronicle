<!-- cratis-ai-managed: skills/cratis-ux-review/references/checklist.md -->
# UX review checklist

Run each question against every capture it applies to. A question that surfaces nothing yields no finding. Cite the heuristic number or the pass name as the finding's basis. Heuristic names follow [Nielsen's ten usability heuristics](https://www.nngroup.com/articles/ten-usability-heuristics/); the questions are written for Cratis screens.

## Nielsen heuristics as task questions

1. **Visibility of system status**: After a command runs, can the user see that it is running, succeeded or failed? Does a list, table or dialog show loading and busy state, and where in the journey am I?
2. **Match with the real world**: Do labels, column names and actions use the user's words rather than event, projection or type names?
3. **User control and freedom**: Can the user leave a dialog or step without committing? Is there an undo or a clear cancel for a destructive action?
4. **Consistency and standards**: Does the same action look and behave the same on every screen? Do dialogs, tables and forms follow Cratis Components and the project's templates?
5. **Error prevention**: Are risky actions confirmed, are impossible choices disabled with a reason, and do forms constrain input before submit?
6. **Recognition rather than recall**: Is what the user needs for the next step visible on that step, instead of remembered from an earlier screen?
7. **Flexibility and efficiency**: Can a frequent user reach the common action quickly (defaults, filters, search, sensible sort)?
8. **Aesthetic and minimalist design**: Does every element earn its place, or does information compete with the primary action?
9. **Help users recognize, diagnose and recover from errors**: Does each error say what happened, why and what to do, near the field it concerns?
10. **Help and documentation**: Where the task is not self-evident, is guidance short, findable and in context?

## Cratis passes

### Hierarchy

- Is there one clearly primary action per view, and is it visually distinct from secondary and destructive ones?
- Does the heading structure tell the user where they are?
- At 390×844, does the primary action stay reachable without losing the context it acts on?

### Density

- Are tables and forms scannable at 1280×720 without horizontal scrolling for the key columns?
- At 390×844, do dense regions collapse or scroll deliberately rather than clip or overlap?
- Are touch targets separated enough to hit without error?

### Consistency with Cratis Components, templates and tokens

- Is each control a Cratis Components control or the project's own template, not a hand-rolled lookalike?
- Do spacing, color and type come from the project's tokens and type scale in both light and dark themes?
- Does the same concept use one term across the journey?

### Loading, empty and error states

- Is there a loading state that is not a blank screen, an empty state that explains what to do next, and an error state with a way forward?
- Is a validation state shown at the field with text, not by color alone?
- A state the captures do not include is a gap to request, not a pass.

### Accessibility risks

Report only what screenshots show, as risks, and list the verification each needs. Never claim conformance.

- Text or controls that look low-contrast in either theme: UNVERIFIED until measured with a contrast checker.
- Information carried by color alone, a missing visible label or a placeholder used as the label.
- Small or crowded targets.
- Focus visibility, keyboard order, accessible names and screen-reader output cannot be seen in a screenshot. Hand them to **cratis-components-accessibility** checks or a DOM verification, and mark UNVERIFIED here.

### Copy

- Do buttons say what they do ("Save changes", not "OK")?
- Do errors say what happened and how to fix it?
- Is wording plain, front-loaded and free of internal jargon?
- Are costs, consequences and timings stated honestly where the user commits?
- Is the tone calm for errors and irreversible actions?

## Cross-check with project rules

Before reporting, check each candidate finding against the repository's frontend rules. If a rule decides the matter, drop the finding or downgrade it to 0 with the rule as basis.
