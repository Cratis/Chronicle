<!-- cratis-ai-managed: skills/cratis-screenplay-legacy-extraction/references/ui-observation.md -->
# Bounded UI observation

Use this branch when watching a running application answers an extraction question. It is
evidence gathering, not screen redesign and not application testing. Adapted from Martin Dilger and Nebulit GmbH's
discover-storyboard (see provenance), with the board and HTML-reconstruction steps removed.

## Before navigating
- **Capability check first.** Look for browser tools (navigate, read page content, click, fill,
  screenshot). Interaction and image tools are needed only for the actions and captures the
  plan uses. No tools or no access blocks only this evidence source: say so in one message
  (what is missing, which source it blocks), record the gap and continue with source,
  document and expert evidence. Never install tools to fill the gap.
- **Discovery parameters.** Reuse answers already in the brief. Ask only for what is missing,
  in two questions at most:
  1. "What URL should I open to start?" (start URL, actor or role, permitted environment)
  2. "Any guidance for the discovery?" Offer the options: focus on one flow ("only the berth
     booking flow"); ignore sections ("skip the admin panel, ignore login pages"); follow a
     named journey ("register a member, then reserve a berth"); or leave blank to sample the
     visible flows. A blank answer is recorded as "sample all visible flows" within the budget.
  Record the answers and the observation budget (default: 15 observed states) in
  `legacy/INTERVIEW.md`.
- Approval is needed for non-local apps and for any action that writes data or triggers an
  effect (mail, payment, records). A local page can still call production services.
- Use approved test identities and data. Never guess credentials, fill credentials seen in a
  URL, or reuse a token found in one. Stop on unexpected production data, access boundaries
  or mutations.
- Follow the hosted-model disclosure policy in `../SKILL.md`, under 'Rules', before sending page text or captures to a
  hosted model.

## Walk
Process one state at a time: navigate, read, understand, decide the next action.

1. Start at the entry URL and wait for the page to settle. Record the initial state with how
   you got there ("initial load").
2. Follow the guidance. A named flow means follow only that path closely; ignored sections
   are skipped entirely; a named journey is followed in sequence, filling forms only as the
   approval allows; with no guidance, cover the primary navigation one main section at a time.
3. Priority for the next interaction: primary navigation (each main section once), then the
   main call-to-action on the current page (for example "Reserve berth", "Sign up"), then
   form submissions with approved example values, then state transitions (after "Reservation
   confirmed", the confirmation page). Do not submit a form just to see what happens.
4. Capture before-and-after states around each approved action: the empty and the populated
   list ("no reservations" and "one reservation"), the form before and after submit. Useful
   states: initial, populated, rejected, changed, absent or removed. Other roles or
   branches only where approved. Capture a loading or empty state only when it is a
   meaningful step.
5. **Meaningful transitions.** A new state is a URL change, or a major content change (a
   modal opens, a form is submitted, the page reloads, a denial appears). Tell states apart by
   actor, workflow and visible state, not by URL alone: the same URL can show a rejection, a
   modal or a completion. Cosmetic changes (tooltip, menu expansion, spinner) are not states.
6. **Be systematic, not exhaustive.** Visit each in-scope section at least once; follow
   pagination to one example page; sample repeated layouts once unless their differences
   answer a named question. Skip external destinations, logout and destructive actions, and
   settings or admin-only sections unless the brief asks for them.
7. **Progress.** On a long walk give a short milestone when a flow starts or finishes: the
   current flow, observed states against the budget, and observations kept versus captures that
   failed. Do not narrate every click.
8. Stop at the budget, the workflow boundary, when no new relevant states appear, or on an
   access or safety blocker. List the paths left over; never call the walk exhaustive.

## Record each meaningful observation
One evidence row (evidence-table-template.md) with:
- stable observation id, actor, environment and observation date;
- redacted route and capture or text locator when one is kept;
- the three-part description: **what it shows** (content and purpose), **how the user got
  here** ("initial load", "clicked 'Reserve berth'", "submitted the sign-in form"), and
  **what actions are possible**, phrased as intents ("user can confirm the reservation",
  "user can filter berths by pier"), not UI labels;
- the before/after difference, including the exact visible rejection text;
- the proposed command, view or event reading, marked as inference;
- the code or expert follow-up needed to establish the rule, authorization or cause.

Keep only what the question needs, under `legacy/captures/`. Redact sensitive values. A
recreated HTML page is a design interpretation, not raw evidence; do not make one the default.

## Group into flows
After the walk, group the observations by the workflow they belong to, in observed order,
with branches and actor changes kept. One linear journey is one flow; distinct sections are
one flow each. Tell the user, for example: "Observed 8 states across 2 flows: 'Reserve a berth'
(5), 'Renew a membership' (3)". A flow is an evidence grouping, not a slice or feature
boundary: a page is not a slice boundary.

## Reconcile
- Trace each displayed value to a command input, session context, query result or display-only
  derivation. Unresolved origins stay `unknown`.
- A screen proves what was shown, not the server rule, authorization or event behind it.
  Never infer atomic uniqueness, replay behaviour or idempotency from a UI success.
- Pass evidenced updates, removals, denials and branch differences to cratis-screenplay-scenario-coverage.
- **Partial failure.** A failed capture is not a recorded observation. Continue independent
  safe paths; stop the affected branch on an auth, safety or unexpected-mutation blocker.
  Report observed, skipped, blocked and failed paths separately, and list failures by name.
- Next steps after the walk: re-slice by business decision (`cratis-screenplay-discovery`
  for events, `cratis-screenplay-slice-design` to map each action to a command), then the
  candidate inventory in `references/candidate-inventory.md`.
