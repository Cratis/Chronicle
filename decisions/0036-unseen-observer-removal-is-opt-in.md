---
id: 0036-unseen-observer-removal-is-opt-in
title: Make the removal of unseen client observers opt in, off by default, while still recording when each observer was last seen
status: accepted
stage: none
class: contract
reversibility: reversible
proposed: 2026-11-19
decided: 2026-11-19
decider: einari
supersedes: 0035-remove-unseen-client-observers
applies-to:
  - Source/Kernel/Core/Observation/Observer*.cs
  - Source/Kernel/Core/Observation/UnseenObserverRemoval*.cs
  - Source/Kernel/Core/Configuration/Observers.cs
  - Source/Kernel/Core/Configuration/ObserverRetryOverride.cs
  - Integration/Kernel/for_UnseenObserverRemoval/**
---
<!-- markdownlint-disable-next-line MD025 -->
# Make the removal of unseen client observers opt in, off by default, while still recording when each observer was last seen

Decided on 2026-11-19 by einari, the same day as [decision 0035](./0035-remove-unseen-client-observers.md) and before it reached a release. It replaces the part of 0035 that chose a default; everything else in 0035 (what is removed, what never is, how "seen" is recorded, the sweep, the clock start, idempotence, logging and the counter) stays in force and is read together with this record.

## Context

Decision 0035 chose to remove a client reactor or reducer after seven days without a subscriber, on by default, with `0` turning it off. The decider reconsidered the default: a removed reactor replays the whole history when its client returns, so its side effects run again, and a kernel that begins deleting records after an upgrade, with nobody having asked for it, is a risk out of proportion to the records it saves. The removal is "super critical" and must happen only where an operator chose it.

## Decision

1. **The removal is off unless an operator turns it on.** A new setting `Observers.UnseenRemovalEnabled` is a boolean that defaults to `false`. While it is `false` the sweep does no work: it reads no storage, activates no observer and removes nothing.
2. **The period stays `Observers.UnseenRemovalAfter`, defaulting to 7 days, which is the recommended period.** It only matters once the removal is on. A zero or negative value no longer means "off"; it falls back to 7 days, like the other period settings. Turning the removal on is therefore one setting: `observers.unseenRemovalEnabled = true`.
3. **The last-seen time is recorded whether or not the removal is on.** Decision 0035 wrote nothing while the setting was zero. That is withdrawn: an operator who turns the removal on after weeks of running must not find every clock starting from zero, which would postpone the first removal by a full period and make "unseen for seven days" untrue for the observers that had really been gone longer. The cost is the writes 0035 already accepted and sized (once per subscription, once when the last client leaves, and one refresh per subscribed observer at most once per `min(1 hour, period / 4)`), now paid by every kernel.
4. The sweep, the guards, the clock start for an observer without a recorded time, idempotence across silos, the log line and the `chronicle.observers.unseen_removed` counter are as in 0035. Dormant release (decision 0034) is unchanged and stays on by default.

## Options considered

- **On by default (decision 0035).** Replaced: see Context.
- **Keep `UnseenRemovalAfter` as the switch, with a zero default meaning off.** Rejected: turning it on would need the operator to know and type the recommended period, and a zero value would continue to mean "off" in one setting and "fall back" in the others. A separate flag keeps the period's default meaningful and the switch obvious.
- **Write the last-seen time only once the removal is on.** Rejected, see decision 3.
- **Record the time only for observers that have a subscription written by this release, and treat the rest as never seen.** Not needed: the clock start for an observer without a time (0035, decision 5) already covers them.

## Default if unanswered

Nothing is ever removed, which is what 0035's default-if-unanswered already said, and now is also what the configuration says.

## Timeline and scope

- **In scope:** the setting, its fallback rule, recording the time while off, documentation, specifications for the defaults, and integration specifications for the removal under `Integration/Kernel/for_UnseenObserverRemoval`.
- **Out of scope:** any change to what is removed or when it is safe to remove it; per-observer overrides of the switch.
- **Release:** `minor`, one new setting; the feature is delivered as an opt-in addition, not a change of default behavior.

## Verification

- **Done when:** with the default configuration an observer whose client has been gone far longer than the period is never removed, and its last-seen time is still recorded; with `UnseenRemovalEnabled` set, the behavior of 0035 holds.
- **Verify by:** configuration specifications for the defaults and binding; unit specifications for the sweep with the switch on and off and for recording the time while off; the integration specifications under `Integration/Kernel/for_UnseenObserverRemoval`, which run the disabled and the enabled cases against a kernel with real storage.

## Consequences

- An upgrade removes nothing, and no operator is surprised by deleted observers.
- Operators who want the cleanup set one setting; the seven-day period is already the default.
- Every kernel records last-seen times, so a later opt-in is immediately accurate.
- The memory and record cost of observers nobody uses stays until an operator opts in.
