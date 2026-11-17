<!-- cratis-ai-managed: skills/cratis-engineering-post-mortem/references/examples/silent-backup-failure.md -->
# Example: backups failed silently for two months

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

**What this example shows.** A long, undetected failure caused by a monitoring
blind spot: a secondary backup stayed green while the primary one failed every
hour. It shows how to state a mitigation with its verification and limits, how
to keep a restore claim that was never tested Unverified, and how
every action ends up with one owner and one linked issue. Product, host and
person names are generalized.

---

# Post-mortem: Production database backups failing for 62 days

Blameless. Status: **Final**. Times UTC, on the day of discovery
(`<date>`, about 03:00 to 04:20). Every claim carries a confidence: Verified, Inferred
or Unverified.

## Summary
The production database cluster is backed up hourly by a managed backup tool
to S3-compatible object storage. The newest snapshot with status `ready`
before the incident was taken 62 days earlier. Every one of the roughly 1,500
snapshots after it ended in `error`. The failure was invisible to monitoring:
the watchdog checked only a separate hourly dump job, which kept succeeding.
The old snapshots predate a database major-version change and cannot be
restored onto the current cluster. The backup tool is healthy again: the first
`ready` snapshot since the gap finished at about 04:20 and the watchdog
finding cleared. Hourly dumps to in-cluster storage continued throughout and
passed a restore test on the day.

## Status and scope
Incident window: 62 days, ending `<date>` about 04:20Z. Severity: high (no
usable restore point from the managed tool; data itself was not lost).
Systems: the production database cluster, the backup tool, object storage, the
watchdog. Over. Remaining exposure is listed under Impact.

## Impact
There was no restore point from the managed backup tool for 62 days (backup
tool snapshot list). No data was lost. The hourly dump job completed every
hour and was the only usable copy. **Verified restore, 03:09 to 03:12:** the
newest dumps restored into a scratch namespace with every restore command
exiting 0, across six database groups from about 100 to several million
documents each; the event-log tail in the restored data was about an hour old.
**Limits:** per-database logical restore, no replay of the change log, not a
cross-database point-in-time restore. Remaining exposure: no off-cluster copy
of the dumps until the encryption key for it exists.

**Affected**
- Everything stored on the production database cluster: no managed-tool
  restore point (Confidence: Verified)
- Recoverability from the dumps: current and restorable, so data was
  recoverable (Confidence: Inferred, from the restore test, which covers
  logical restore only)
- Restoring from a managed-tool snapshot (Confidence: Unverified; no restore of
  a snapshot has been run)
- A feature that depends on backups is not enabled in production, so it had no
  gap; a fix must land before it is enabled (Confidence: Verified)

## Timeline (UTC)
| Time (UTC) | Event | Source | Confidence |
|---|---|---|---|
| <date-62> 23:00Z | Last `ready` snapshot of the managed tool | Backup tool list | Verified |
| **<date-62> (approximate; the day after the last good snapshot)** | **Outage start.** Operator upgrade chain in one day: the backup tool across seven versions, plus a database major version and a feature-compatibility change. From a middle version the tool switched its object-storage client library | Change history, tool release notes | Unverified (the exact upgrade times are not anchored) |
| <date-62> onward (hourly, approximate) | Hourly snapshots all end in `error`. Earlier failures were a connection-pool exhaustion, fixed upstream later | Backup tool list, upstream report | Unverified (time not anchored) |
| <date-62> to <date> 02:52Z (approximate; whole window) | The watchdog monitors only the dump job; no check exists for the managed tool, so no alert was possible | Watchdog configuration | Unverified (time not anchored) |
| <date-48> and <date-33> (approximate) | Two earlier watchdog jobs are in a Failed state; whether the watchdog was down while backups failed | Job list | Unverified |
| <date> about 02:52Z (approximate) | Deploy of the tool upgrade plus the first watchdog check for the managed tool, every 5 minutes, alerting by mail and chat for new findings | Deploy run | Unverified (approximate time) |
| <date> after 02:52Z (approximate) | An on-demand backup fails in 43 seconds with a `PutObject` 400 checksum mismatch | Backup tool log | Unverified (time not anchored) |
| <date> 03:09Z to 03:12Z | Restore test of the dumps into a scratch namespace: pass | Restore command output | Verified |
| <date> after 03:12Z (approximate) | Storage type changed to the S3-compatible client; merged and deployed | Change request | Unverified (time not anchored) |
| <date> 03:13Z | Operator-triggered resync walks the whole bucket root, because no prefix is set. It holds the tool's lock and blocks backups; the agent is busy, not hung | Backup tool log | Verified |
| <date> before 04:09Z (approximate) | New empty storage prefix deployed; old objects left unlisted in the bucket root. Lock cleared by restarting the agents on two pods, then a one-second forced resync | Change request, tool status | Unverified (time not anchored) |
| <date> 04:09:04Z | First `ready` snapshot since the gap starts | Backup tool list | Verified |
| **<date> 04:20:04Z** | **Outage end.** Snapshot finished (about 2.4 GiB, logical); watchdog run healthy, finding cleared | Backup tool list, watchdog log | Verified |

Durations: gap 62 days; discovery to first good snapshot about 1h20m.

## Evidence
- Backup tool status and snapshot list, read through its CLI.
- Watchdog configuration and job list.
- Deploy runs and change requests for the three fixes.
- Restore command output from the scratch namespace.
- Upstream issue tracker report of the same version split.

**Evidence gaps:** why the two older watchdog jobs failed; whether anyone
looked at the snapshot list during the 62 days.

## Verified facts
- Every snapshot after the last `ready` one has status `error` (backup tool
  list).
- The watchdog had no managed-tool check before the deploy (watchdog
  configuration).
- Changing the storage type produced a `ready` snapshot (backup tool list).

## Unverified claims
- A snapshot from the managed tool restores correctly (raised by the
  infrastructure engineer). Confidence: Unverified. Why unverified: no restore
  of a snapshot has been run; the test used the dump job. How to verify: run
  the restore drill action.
- The two failed watchdog jobs hid the gap (raised by a reviewer). Confidence:
  Unverified. Why unverified: job logs are gone. How to verify: inspect
  retained job history, or accept as unknown.

## Detection
Found by adding a check after an unrelated review of the backup setup, not by
an alert. Why it was not found sooner:
1. Monitoring covered the wrong mechanism. The watchdog checked only the dump
   job; the managed tool had no check.
2. The upgrade chain (seven tool versions plus a database version change in one
   day) had no post-upgrade check that a backup completes and restores.
3. No restore test existed, so a `ready` to `error` flip had no second signal.
4. Two failed watchdog jobs were not surfaced. Whether this affected detection
   is Unverified.

## Root causes
- **Trigger:** the upgrade chain on day -62.
- **Root cause 1:** **Primary** Backups failed for two months because the
  tool's uploads were rejected by the storage, because the new client library's
  default request checksums are incompatible with the S3-compatible storage,
  because the tool moved to that library in the upgrade chain and no check
  followed the upgrade. Confidence: Inferred, from failures starting at the
  version that introduced the library, an upstream report of the same split
  and uploads succeeding after the storage type changed.
- **Root cause 2:** **Primary** The failure stayed unseen because the watchdog
  watched the wrong signal (the dump job's success), because backup health was
  defined as the existence of dumps, not the age of the last good backup of the
  tool in use.
- **Root cause 3:** The usual workaround, an environment variable restoring the
  old checksum behavior, could not be injected, because the operator uses a
  fixed environment list and the tool has no option for it.
- **Contributing factors:**
  - An earlier, separate failure was already fixed upstream and masked this one
    until the tool was upgraded.
  - The post-fix resync stall had its own cause: the new storage config had no
    prefix, so the tool walked the whole bucket root.
- **Detection and response gaps:** no alert on backup age; findings that stay
  open are repeated but never escalated.

## What went well
- The new watchdog check fired correctly once it ran.
- The dump job ran every hour and its restore was proven the same day.
- The first deploy exposed the real error within minutes; two more changes
  produced a `ready` snapshot within about two hours.

## What went poorly
- Two months with no usable managed-tool restore point.
- A healthy secondary backup hid a failing primary one.

## Where we were lucky
- No data loss or cluster failure happened during the gap.
- The dump job's output was current and restorable.

## Actions
| # | Action | Type | Owner | Issue | Status |
|---|---|---|---|---|---|
| 1 | Upgrade the tool and add the watchdog check for it | detect | @infrastructure-team | <owner/repo>#<n> | Done |
| 2 | Set the storage type to the S3-compatible client | fix | @infrastructure-team | <owner/repo>#<n> | Done |
| 3 | Use an empty dedicated prefix, confirm a `ready` snapshot, clear the finding | fix | @infrastructure-team | <owner/repo>#<n> | Done |
| 4 | Restore the dumps into a scratch namespace | verify | @infrastructure-team | <owner/repo>#<n> | Done (pass) |
| 5 | Automate a managed-tool restore drill (the manual test needs bucket credentials in a scratch namespace) | verify | @infrastructure-team | <owner/repo>#<n> | Open |
| 6 | Clean up the bucket root: old objects from failed runs and the old snapshots | mitigate | @infrastructure-team | <owner/repo>#<n> | Open |
| 7 | Surface failed watchdog jobs, record first-seen time per finding, escalate findings open over about 24 hours | detect | @infrastructure-team | <owner/repo>#<n> | Open |
| 8 | Fix the dependent feature before enabling it in production | prevent | @platform-team | <owner/repo>#<n> | Open, before enable |
| 9 | Create the key pair for off-cluster encryption | prevent | @security-owner | <owner/repo>#<n> | Open |
| 10 | Copy the hourly dumps off-cluster, encrypted | prevent | @infrastructure-team | <owner/repo>#<n> | Open, after action 9 |
| 11 | Verify backup and restore after every backup-tool or database upgrade | prevent | @infrastructure-team | <owner/repo>#<n> | Open |

## Lessons
- Monitoring must cover every backup mechanism in use.
- A backup that reports `error` every hour is the same as no backup; alert on
  the age of the last good backup.
- A finding that stays open needs escalation, not only repeat alerts.
- A backup is proven only by a restore; verify one after any upgrade of the
  tooling or the database.
- Move backup tooling and database versions in separate, verified steps.
- Give object storage a dedicated prefix; a resync of an unprefixed bucket
  walks everything in it.
- Keep a copy outside the failure domain of the cluster.

## Redaction note
Generalized: product, provider and host names, tool and operator versions,
snapshot identifiers, counts that do not carry the lesson, tracker identifiers,
and the names and roles of people. Action owners and issue links are
placeholders.
