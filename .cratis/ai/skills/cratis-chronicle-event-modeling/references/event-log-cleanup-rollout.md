<!-- cratis-ai-managed: skills/cratis-chronicle-event-modeling/references/event-log-cleanup-rollout.md -->
<!-- Copyright (c) Cratis. All rights reserved. -->
<!-- Licensed under the MIT license. See LICENSE file in the project root for full license information. -->

# Rolling out a cleanup that removes or reshapes many event types

Read this when a release deletes or reshapes many event types in place (no generations) and the
stored data must be repaired to match. The mechanics of repair are repository-specific; these are
the traps that apply to any store.

## Contents

- Procedure
- Traps in the new code
- Traps in the repair

## Procedure

1. Scale every registrant of the store to zero so nothing appends or registers while data changes.
2. Back up every collection the repair writes, before the first write.
3. Run a **dry run** and review each count. Give every step a **fuse** (the most documents it may
   touch, sized from the reviewed dry run) so a step over its fuse stops before writing; see
   [guards-and-fuses.md](../../../rules/guards-and-fuses.md). Make the repair idempotent: a re-run skips work already done.
4. Apply, then restart the kernel if it caches the event type registry, then scale the application
   back up.
5. **Wipe the whole schema registry** for the store rather than a hand-picked subset; the application
   registers every current type again on start. A hand-picked list misses stale registrations and the
   application crash-loops against them.
6. Verify the pages that read each reshaped model, then re-run the dry run: every step reports zero.

## Traps in the new code

- **A reactor added by the release catches up the whole history on first registration, and
  `[OnceOnly]` does not cover that**: a first catch-up is not a replay. A side-effect reactor (jobs,
  AI calls, external calls) therefore starts at the tail of the log, or its handlers ignore facts
  that predate its registration. Specify that a new side-effect reactor does nothing for history.
- **Reconciliation sweeps must not decide from read models that are replaying.** A rebuilding model
  looks like "nothing was ever processed" and the sweep starts the work again for everything. Decide
  from the events or a `[Passive]` model, or refuse to run while the model's observer is replaying or
  behind; specify the replaying case. See **cratis-chronicle-reactor**.
- **Re-recorded facts retrigger downstream consumers.** Recording facts again in a new shape (for
  example a synchronizer re-emitting per-fact events once the broad event was removed) looks like new
  evidence to gates and evaluations and supersedes concluded work. Make consumers follow what the
  evidence says, not that an event arrived, and prefer rewriting the stored events over re-recording.

## Traps in the repair

- **Numbers written by `mongosh` are BSON doubles** and break `int64` event schemas. Write integers
  with `NumberLong`.
- **Every observer whose read model is reset must actually replay.** After a reset, check each
  observer's state and position: a projection left at position 0 without a running replay job leaves
  its read model empty (counter projections were missed this way). Request the replay again and verify.
- **A dry run is evidence only for the data it saw.** Wait out the quiet period, run it again just
  before applying, and investigate any count that grew before raising a fuse.
