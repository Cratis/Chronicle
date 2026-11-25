---
id: 0029-bounded-busy-silo-wait-at-startup
title: Wait out a busy silo during startup without counting attempts, up to a configurable limit measured in hours
status: accepted
stage: none
class: product
reversibility: reversible
decided: 2026-11-11
decider: einari
applies-to:
  - Source/Kernel/Core/Setup/ChronicleServerStartupTask*.cs
  - Source/Kernel/Core/Setup/BusySilo*.cs
  - Source/Kernel/Core/Setup/IBusySilos.cs
  - Source/Kernel/Core/Configuration/Startup.cs
---

<!-- markdownlint-disable-next-line MD025 -->
# Wait out a busy silo during startup without counting attempts, up to a configurable limit measured in hours

Tracking issue: [Cratis/Chronicle#5676](https://github.com/Cratis/Chronicle/issues/5676).

## Context

Every required startup step is a grain call. Before this change, a step that kept timing out was retried for about ten
rounds (several minutes) and then the host was stopped with a Critical log. A silo restarted into a large backlog
answers its own calls late for longer than that, and a silo joining a cluster whose active member is still catching
up sees its calls into that member time out the same way. Stopping the process in either case restarts the same
backlog, and in a cluster each restart moves more work onto the busy member: a crash loop (#5676).

A timeout against this silo, or against a sibling the cluster reports as active, is therefore treated as "busy, wait"
and does not count towards the attempt budget. The same timeout is also what a grain that never answers produces: a
non-reentrant call cycle or deadlock, a grain stuck activating on storage that hangs (the storage driver's own server
selection timeout races Orleans' response timeout), or a sibling that drops messages. Waiting without any limit would
leave such a silo running and not ready forever. Liveness stays green, so an orchestrator never restarts it, and the
log blames saturation.

## Decision

A required startup step waits for a busy silo without counting attempts, with a backoff capped at one minute and a log
entry for every wait (a Warning, escalating to an Error after ten waits that also names a grain that never answers as a
possible cause). The wait is bounded by `startup.busySiloWaitLimit`, two hours by default and measured per step from its
first busy timeout. Once the limit is spent the step fails like any exhausted step: a Critical log and a failed host.
Timeouts against a silo that is not active, or whose target cannot be read, keep the bounded rounds.

## Options considered

- **Keep the bounded rounds for every timeout (the previous behavior).** Rejected: it is the crash loop in #5676.
- **Wait without a limit.** Rejected: a deadlock or a storage hang then never surfaces. The silo stays running and not
  ready behind a green liveness probe, which is worse than failing.
- **A limit measured in minutes.** Rejected: a large backlog takes longer than that to drain, so it brings the crash
  loop back for exactly the deployments that need the wait.
- **A configurable limit measured in hours (chosen).** Long enough for a backlog to drain, short enough that a silo
  that will never get there is restarted and noticed within a working shift. Operators who know their backlog can
  raise it, and zero turns the wait off.

## Default if unanswered

The implementation ships with the two-hour default. If the record is never accepted, the behavior stays as built. The
cost is that the limit was chosen by the implementer, not confirmed by an operator.

## Timeline and scope

In scope: the required steps of `ChronicleServerStartupTask` and the classification in `BusySilos`. Out of scope: the
optional pattern-capture subscription (one round, never waits) and kernel projection registration, which is retried in
the background. Holds until the startup path stops making blocking grain calls.

## Verification

- **Done when:** a busy-silo timeout no longer counts as a failed attempt, and a step still timing out after
  `startup.busySiloWaitLimit` fails the host with a Critical log.
- **Verify by:** the `for_ChronicleServerStartupTask` specifications `and_this_silo_is_busy_catching_up`,
  `and_a_busy_sibling_keeps_timing_out`, `and_a_busy_silo_never_answers_within_the_wait_limit` and
  `and_shutdown_cancels_a_busy_silo_wait`.

## Consequences

- A silo that is only busy keeps running, not ready, and catches up instead of crash-looping.
- A silo held by a grain that never answers fails after the limit rather than immediately. With the default, an operator
  learns about a deadlock or a storage hang up to two hours later than before, and the Error logs before that name it
  as a possible cause.
- The classification depends on the text Orleans writes for a timed-out request. If that changes, the specifications
  fail, and at runtime the server falls back to the bounded rounds.
