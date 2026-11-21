---
name: cratis-session-handoff
description: Use when a long session nears its context limit, a milestone ends, or work must continue in a fresh session; also for keeping a running state note. Not for coordinating parallel lanes (cratis-orchestration-lanes).
license: LICENSE
---
<!-- cratis-ai-managed: skills/cratis-session-handoff/SKILL.md -->

<!-- Copyright (c) Cratis. All rights reserved. -->
<!-- Licensed under the MIT license. See LICENSE file in the project root for full license information. -->

# Hand off a session without losing state

A session ends sooner than the task does. The state that matters (running jobs,
other worktrees, unanswered questions, the user's exact words) lives only in
the session's context, so it is written down while the session is healthy, not
reconstructed after it degrades. This skill covers one agent continuing its own
task in a later session. Parallel lanes with claims, leases and inboxes use
`cratis-orchestration-lanes`, which reuses the template here.

Work records live under `.ai-work/` as defined in
[local-work-artifacts.md](../../rules/local-work-artifacts.md), which also owns
the preconditions every handoff opens with.

## Keep a state note

Default: keep `.ai-work/<task>/STATE.md` from the start of any task expected to
span more than one sitting. It holds the same sections as the handoff template,
so a handoff is a copy plus a verification, not a reconstruction.

Update it at each milestone and before any long wait. Also update it
immediately after:

- launching a job, subagent or CI watch (record its immutable id and what to do
  when it finishes);
- receiving a user instruction (quote it verbatim);
- asking or answering a question (record the question and its status).

A note updated only at the end is the one that is missing when the session is
cut off.

## When to hand off

Hand off at a milestone, or when the context is nearly full: by default at
about 60-70% used or when the harness warns. A repository or brief may set
another threshold; harnesses differ in how they report usage. Hand off at a
boundary, never mid-atomic-step: a half-finished edit or half-run command
cannot be described reliably.

When the context is nearly full:

1. Finish the current atomic step.
2. Update `STATE.md`.
3. Write the handoff from it. Read
   [references/handoff-template.md](references/handoff-template.md) for the
   fixed structure, the rules for filling it and a worked example.
4. Verify it: run `node .cratis/ai/hooks/scripts/cratis-check-handoff.mjs .ai-work/<task>/handoff.md`;
   fix every error and either fix or acknowledge each warning (a moved HEAD means
   update the table). Where the script is not installed, check by hand:
   preconditions against `git`, each section present, every running job with a
   full watch row.
5. End the session, or reset it with your harness's compaction or new-session
   command, starting from the handoff.

## Session hygiene

- One repository and one task per session. A second task makes the handoff
  ambiguous about what is current.
- At a milestone, start a fresh session from a handoff instead of running one
  session all day; quality drops as context fills.
- Delegate log, diff and CI digests where the harness allows it; reading raw
  output spends context on bytes the decision does not need.
- A truncated view is never gate evidence. Read the full result or the exit
  code before claiming a check passed.
- Explore an alternative design in a branch of the session or a fresh one, so
  the original plan is preserved.

## Resuming

Start from the handoff, not from memory:

1. Run `git rev-parse --show-toplevel`, `git rev-parse HEAD`,
   `git branch --show-current` and `git worktree list`; compare with the
   preconditions and the repositories table.
2. Read the finished, unread results first.
3. Reconcile every watch row against the real state of its target; decide each
   action from that state.
4. Re-read the verbatim constraints and instructions before the next command.

A stale handoff is not a lost one: a mismatch means the work moved. Find it
through branches, the reflog and open pull requests before recreating or
resetting anything.

## Pitfalls

- **Unrecorded jobs and subagents.** A job started and never written down is
  orphaned by the reset. Record it the moment it starts.
- **Other repositories and worktrees.** Work in a second checkout is invisible
  to a note that names only the current one; list every path in the table.
- **Questions from earlier sessions.** An open question forgotten is a
  decision made by default. Carry it forward with who asked and its status.
- **Paraphrased user words.** A summary drops the condition that mattered.
  Quote constraints and instructions exactly.

## Verification

A handoff is complete when every heading of the template is present in order,
the repositories and watch tables use the exact header rows, every running job,
CI watch and subagent has a row with an immutable target, and each user quote
is verbatim. Run `node .cratis/ai/hooks/scripts/cratis-check-handoff.mjs .ai-work/<task>/handoff.md`;
the checker exits 0. Fix or acknowledge its warnings before ending the session.
