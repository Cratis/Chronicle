<!-- cratis-ai-managed: skills/cratis-engineering-post-mortem/references/review-template.md -->
# Post-mortem review template

## Contents

- Template
- Column and value rules
- Synthetic example

Copy the template below, fill every section and delete the guidance in
parentheses. A section with nothing to say states that in one line; it is never
removed. The synthetic example at the end shows the level of detail.

## Template

```markdown
# Post-mortem: <short title>

Blameless. Status: **Draft | Final**. Times UTC. Every claim carries a
confidence: Verified, Inferred or Unverified. (State when tracker status was
read, for example "as read at write time".)

## Summary
(3-5 sentences, facts only: what failed, the exposure window with start, end
and total duration, who or what was affected, how it was fixed, how it was
found. Only Verified material is stated as fact.)

## Status and scope
(Incident window in UTC, severity, repositories and systems involved, review
date, whether the incident is over, what is still exposed.)

## Impact
(A short text: how long, how much and how, with the measure and source of each
number; estimates say so. Remaining exposure. Mitigations that held, how each
was verified and its limits.)

**Affected**
- <who or what> (Confidence: Verified)
- <who or what> (Confidence: Inferred, from <the evidence it is derived from>)
- <who or what> (Confidence: Unverified)

## Timeline (UTC)
| Time (UTC) | Event | Source | Confidence |
|---|---|---|---|
| <date> HH:MM:SSZ | <what happened> | <link> | Verified |
| **<date> HH:MM:SSZ** | **<outage start>** | <link> | Verified |
| <date> HH:MM:SSZ | <what follows from the evidence> | <link> | Inferred (from <evidence>) |
| <date> HH:MM:SSZ | <what someone reported> | <link> | Unverified |
| **<date> HH:MM:SSZ** | **<outage end>** | <link> | Verified |

(Durations: total, start to fix released, fix released to deployed.)

## Evidence
(Every source with a stable link and what was read.)

**Evidence gaps:** (sources that could not be read, and what is unknown as a
result.)

## Verified facts
(Each fact followed by its source.)

## Unverified claims
- <claim> (raised by <role>). Confidence: Unverified | Inferred (from
  <evidence>). Why unverified: <reason>. How to verify: <step>.

## Detection
- <how and when it was found>
- <a reason it was not found sooner: the check that could not see it, the
  status it accepted, the signal that does not exist>

## Root causes
- **Trigger:** (the event that started it.)
- **Root cause:** **Primary** (why-chain: <effect> because <cause> because
  <cause>, until the last cause is something a change can prevent.)
- **Root cause:** (another why-chain; mark it **Primary** only if it is one of
  the main causes. A cause shared by several actions is named once.)
- **Contributing factors:** (conditions that made it worse or longer.)
- **Detection and response gaps:** (what slowed finding or fixing it.)

## What went well
- <item>

## What went poorly
- <item>

## Where we were lucky
- <item>

## Actions
| # | Action | Type | Owner | Issue | Status |
|---|---|---|---|---|---|
| 1 | <outcome, one action> | prevent | @<team-or-person> | <owner/repo>#<n> | Open |

(Type: prevent, detect, mitigate, fix, verify or process. Status: Needs
decision, Open, In progress, Done or Declined with the reason. In a **Draft**
review the Owner may be a proposed owner (a role or team) and Status may be
"Needs decision"; the Issue may be pending. The review becomes **Final** only
when every action has one named accountable owner and one linked issue, in the
repository that owns the work. Never "TBD" or "everyone" in a Final review. Add
a Filing column with `linked-existing` or `filed-new` when actions are filed
by an agent.)

## Lessons
- <one line, phrased as a rule that can become a gate>

## Redaction note
(Categories removed or generalized, not their values.)
```

## Column and value rules

- **Time.** ISO-8601 UTC. When the source used a local zone, convert and keep
  the original in the Event cell ("09:15 local, converted").
- **Confidence.** Exactly `Verified`, `Inferred` or `Unverified`, in the
  timeline, the Affected list and the claims. `Verified` needs a source.
  `Inferred` means derived from verified evidence but not directly observed,
  and names what it is inferred from. Everything else is `Unverified`.
- **Type.** Prefer `prevent` and `detect` actions that add or change a gate
  over `process` reminders. A hold, freeze or override action carries an
  expiry and a watcher.
- **One action, one issue.** Never cover two actions with one issue; never file
  without a duplicate search.

## Synthetic example

A short, fictional review that shows the shape; the worked examples in
`examples/` show a full one.

```markdown
# Post-mortem: Nightly export skipped for three days

Blameless. Status: **Final**. Times UTC. Every claim carries a confidence.

## Summary
The nightly export did not run from <date> 02:00Z to <date+3> 02:00Z, a gap of
72 hours. A scheduler upgrade reset the job's enabled flag. A user noticed the
missing file and the job was re-enabled the same hour.

## Impact
Three nightly exports were missed (72 hours), counted from the scheduler log.

**Affected**
- The downstream report that reads the export (Confidence: Verified)
- Two partner dashboards (Confidence: Inferred, from their read of the same
  report)

## Timeline (UTC)
| Time (UTC) | Event | Source | Confidence |
|---|---|---|---|
| <date> 01:40Z | Scheduler upgraded | deploy run link | Verified |
| **<date> 02:00Z** | **Export job did not start** | scheduler log link | Verified |
| <date+3> 09:12Z | User reports missing file | support ticket link | Verified |
| **<date+3> 09:48Z** | **Job re-enabled; export ran** | scheduler log link | Verified |

## Unverified claims
- "The old version also dropped jobs" (raised by the on-call engineer).
  Confidence: Unverified. Why unverified: no log of earlier runs is retained.
  How to verify: check run history exports.

## Root causes
- **Root cause:** **Primary** The job did not run because its enabled flag was
  off, because the upgrade reset scheduler settings, because upgrades are not
  followed by a check that jobs are enabled.

## Actions
| # | Action | Type | Owner | Issue | Status |
|---|---|---|---|---|---|
| 1 | Alert when no export has finished in 26 hours | detect | @platform-team | <owner/repo>#<n> | Open |
```
