<!-- cratis-ai-managed: skills/cratis-engineering-post-mortem/references/examples/unintended-release-regression.md -->
# Example: an unrelated release shipped a regression to production

## Contents

- Summary
- Status and scope
- Impact
- Timeline (UTC)
- Evidence
- Verified facts
- Unverified claims
- Detection
- Root causes
- What went well
- What went poorly
- Where we were lucky
- Actions
- Lessons
- Redaction note

**What this example shows.** A regression introduced in a dependency reached
production by an accident of how the deploy works, then stayed hidden behind
checks that could not see it. It shows a UTC timeline with `Verified`,
`Inferred` and `Unverified` rows, durations computed from the outage
start and end, shared root causes, a numbered list of detection gaps, and
actions with one owner and one linked issue each. Names, identifiers and
versions are generalized.

---

# Post-mortem: Live data broken after an edge proxy upgrade

Blameless. Status: **Final**. Times UTC; `<date>` is the day of the outage and
`<date-n>` is n days earlier. A time marked approximate is never Verified. Every claim carries a confidence: Verified, Inferred or Unverified.
Evidence was gathered read-only from pipeline runs, change requests and
issues; no cluster access, so pod facts come from cited run data.

## Summary
A change to the edge proxy made it answer 404 to every path under a reserved
prefix that it did not own. The application serves its live queries, commands
and queries under that prefix, so signed-in users got no live data. The change
had been merged six minutes after opening, with no review, and released three
days earlier. It reached production because the pipeline for an unrelated
release checked out the moving main branch instead of the release commit, and
that branch already carried the new proxy version. Smoke and health checks
stayed green. A fix was released at 15:39 and the fixed proxies ran at 18:21.
Exposure was about 8h13m.

## Status and scope
Window: 10:08:12Z to 18:21:28Z. Severity: high (a core feature unavailable for
signed-in users). Systems: the edge proxy, the application deploy pipeline,
the smoke checks. Over; remaining exposure is under Impact.

## Impact
Live data was unavailable to signed-in users for 8h13m: 5h31m to the fix
release and 2h42m from the fix release to the reported deployment (change
request timestamps). How much of that was queueing alone is unknown: 1h53m
passed before the deploy pipeline started (build, specs, schema
acknowledgement, replica recovery).

**Affected**
- Signed-in users relying on live data (Confidence: Verified, from the fix
  change request); the number affected is Unverified
- Plain HTTP commands and queries, which also returned 404 (Confidence:
  Verified, per the fix change request)
- Two other products pinned an older proxy version below the affected range
  (Confidence: Inferred, from repository configuration, not from running pods)
- Users of the public image outside the organization (Confidence: Unverified)

## Timeline (UTC)
| Time (UTC) | Event | Source | Confidence |
|---|---|---|---|
| <date-3> 19:48Z | Proxy change opened by an automated agent identity, no linked issue | Change request | Verified |
| <date-3> 19:54:52Z | Merged with 0 reviews; released. The release note does not disclose the routing refusal | Change request, release | Verified |
| <date-3> to <date> (approximate; no single instant) | Several later proxy releases all carry the bug | Release list | Unverified (release times not anchored here) |
| <date-1> (approximate; time of day unknown) | A targeted upgrade review of the proxy exists and missed the routing regression | Review notes | Unverified (review time not recorded) |
| <date> 09:49:55Z | Application change merged with 0 reviews; moves the production proxy pin to an affected version | Change request | Verified |
| **<date> 10:08:12Z** | **The pipeline for an unrelated release checks out the main branch (not the release commit) and updates both proxy deployments. Outage start** | Pipeline run | Verified |
| <date> 10:29Z to 10:31Z | A later pipeline run updates only the application; smoke passes | Pipeline run | Verified |
| <date> 10:47Z to 11:08Z | A manual production deploy fails rollout verification; unrelated to the bug | Pipeline run | Inferred (from the failure being in rollout verification of a different deployment) |
| <date> morning (approximate) | Agents report production healthy; the health endpoint and TCP probes cannot see the path | Incident notes | Unverified (time not anchored) |
| <date> 15:33Z | Fix change request opened by a maintainer, with the earliest diagnosis comment found | Fix change request | Verified |
| <date> 15:33Z | A first user report is mentioned; its author and time are not cited | Fix change request | Unverified |
| <date> 15:39:17Z | Fix merged; released at 15:39:31Z | Fix change request | Verified |
| <date> 15:46Z | Application pin change opened; root-cause comment at 15:47Z | Change request | Verified |
| <date> 16:11Z | An event-schema acknowledgement is added; without it the pipeline stops at the schema guard | Change request comment | Verified |
| <date> about 16:20Z (approximate) | A manual break-glass patch is blocked: it needs an override from the owner, and the proposed rollback target was the broken version | Incident notes | Unverified (approximate time) |
| <date> about 16:25Z (approximate) | The application deployment is scaled to zero by an unknown actor; a comment says another operator changed replicas | Change request comment | Unverified |
| <date> 16:41Z | Scaled back to one | Change request comment | Verified |
| <date> 17:32:32Z | Pin change merged; pipeline starts 17:32:36Z, behind two queued runs | Change request | Verified |
| **<date> 18:21:28Z** | **Both proxies on the fixed version; application ready 18:21:30. Outage end** | Change request comment | Verified |
| <date> 18:31:49Z | Anonymous public check: live paths now answer 401 instead of 404 | Change request comment | Verified |
| <date> 18:31:49Z | Signed-in confirmation of the fix | Change request comment | Unverified |

Durations: total 8h13m16s; to fix release 5h31m; fix release to deployed 2h42m.

## Evidence
- Pipeline runs for the unrelated release, the later application run and the
  manual deploy, with the workflow definition.
- The proxy change request, its release, the fix change request and the
  application pin change requests, with their comments.
- The smoke script and the release list.
- Incident notes kept by the people responding.

**Evidence gaps:** proxy access logs, the cluster audit log and any user-report
records were not read, so the first failed request, the actor behind the scale
to zero and the number of affected users are unknown.

## Verified facts
- The release commit's proxy pin was not what the pipeline deployed; the
  pipeline deployed the main branch (pipeline run, workflow definition).
- The smoke step accepts either 401 or 404 for anonymous requests under the
  reserved prefix (smoke script).
- The deep live-query check runs in-cluster against the application and
  bypasses the proxy (smoke script).

## Unverified claims
- The first failed user request was after the deploy (raised by a reviewer).
  Confidence: Unverified. Why unverified: no access log was read. How to
  verify: read proxy access logs for the window.
- No data corruption occurred (raised by a maintainer). Confidence: Inferred,
  from the bug failing closed on live paths only. Why not verified: writes were
  not examined. How to verify: compare writes before and after.
- Another operator scaled the deployment to zero (raised by an engineer).
  Confidence: Unverified. Why unverified: no audit entry is cited. How to
  verify: read the cluster audit log.

## Detection
Found by a human, hours after rollout; no alert, smoke or agent check fired.
Why it was not found sooner:
1. The smoke sign-in step accepts 401 or 404, so the bug's 404 passed.
2. The deep check runs in-cluster against the application, bypassing the
   proxy.
3. The health endpoint and root path never touch the reserved prefix; proxy
   probes are TCP-only.
4. No external synthetic probe and no alert on live data.
5. Smoke cannot roll anything back.
6. The proxy had no consumer test against a real application; its own specs
   encoded the bug.
7. The upgrade review read release notes, which omitted the change.

## Root causes
- **Trigger:** the application pipeline for an unrelated release deployed the
  main branch, not the release commit, picking up the new proxy pin.
- **Root cause 1:** **Primary** Production ran a bad proxy because an unrelated
  release deployed it, because the pipeline deploys a moving branch instead of
  an immutable release reference, so an unrelated release ships every merge
  made since.
- **Root cause 2:** **Primary** The bad change existed because a routing change
  to security-critical infrastructure was merged in minutes, because required
  review was not enforced on the repository.
- **Root cause 3:** It stayed live for hours because the checks accepted
  "something answered" and could not see the failing path, because smoke
  accepted 401 or 404 and the deep check bypassed the proxy.
- **Contributing factors:**
  - Release notes did not disclose the behavior change, so a notes-based review
    could not find it.
  - No rollback path independent of building the application; the manual
    override was blocked and named the broken version as the target.
  - Incident fixes queued behind normal runs in the shared pipeline.
- **Detection and response gaps:** see Detection; reports of "healthy" did not
  name what the check could not cover.

## What went well
- The maintainer diagnosed and shipped the fix within six minutes of opening;
  it uses an explicit owned-path list rather than a revert.
- Cause and affected paths are clearly written in the fix and the pin change.
- The other products' pins sat below the affected range.

## What went poorly
- About 8h13m of exposure; 5h31m before a fix existed, 2h42m after.
- The smoke check was shaped so the failure passed.
- An unreviewed merge to an authentication component, and a deploy that
  carried unreviewed main-branch content inside an unrelated release.
- The scale-to-zero by an unknown actor cost about 16 minutes.

## Where we were lucky
- The bug failed closed (404), not as an authentication bypass.
- No data corruption is known (Inferred, from live paths only).
- A maintainer could diagnose from a signed-in browser.

## Actions
| # | Action | Type | Owner | Issue | Status |
|---|---|---|---|---|---|
| 1 | Forward service-owned paths in the proxy | fix | @proxy-maintainer | <owner/repo>#<n> | Done |
| 2 | Pin the fixed version in the application | fix | @release-owner | <owner/repo>#<n> | Done |
| 3 | Deploy the release commit's configuration and record the image and version per deploy | prevent | @release-owner | <owner/repo>#<n> | Open |
| 4 | Add consumer tests against a real application and a breaking-change release-note rule | prevent | @proxy-maintainer | <owner/repo>#<n> | In progress |
| 5 | Assert the exact anonymous status and add a rollback step to smoke | detect | @release-owner | <owner/repo>#<n> | In progress |
| 6 | Add a signed-in smoke check from CI | detect | @release-owner | <owner/repo>#<n> | Open, blocked on a CI identity |
| 7 | Move auth infrastructure pins one minor version at a time, or prove them by smoke | prevent | @release-owner | <owner/repo>#<n> | Open |
| 8 | Audit every proxy reference against the affected range | verify | @release-owner | <owner/repo>#<n> | Open |
| 9 | A rollback procedure for the proxy independent of building the application, with a recorded last-known-good | mitigate | @platform-lead | <owner/repo>#<n> | In progress |
| 10 | Make the review workflow a required check on security repositories | prevent | @platform-lead | <owner/repo>#<n> | Open |
| 11 | External synthetic probe and alert on authenticated live queries | detect | @infrastructure-team | <owner/repo>#<n> | In progress |
| 12 | Identify who scaled the deployment to zero | verify | @platform-lead | <owner/repo>#<n> | Open |

## Lessons
- Deploy what was released; a deploy from a moving branch ships every merge
  since.
- A check that accepts "something answered" proves nothing; assert the exact
  status and a real payload.
- A probe that bypasses the component under change does not test it.
- Routing and authentication changes are breaking whatever the version says; a
  notes-based review cannot find what the notes omit, so test the consumer
  contract.
- A rule that depends on someone choosing to ask for review is not a rule;
  enforce it.
- Have a rollback you can run without building the thing that is broken.
- "Healthy" in a report must name what was and was not checked.

## Redaction note
Generalized: product, repository and host names, people and agent identities,
run, job and change-request identifiers, commit hashes, versions, file and
line citations, unrelated incidents seen in the window, and internal
coordination details. Action owners and issue links are placeholders.
