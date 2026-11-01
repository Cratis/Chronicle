---
title: Operate a read model rebuild
description: Watch a read model rebuild, know what readers see, deal with one that stalls or fails, and roll a projection change back.
---

**Goal:** you are about to rebuild a read model in a running system, or one is already running. You want to know how to start it, how to watch it, what to do when it does not finish, and how to back out. This page covers what Chronicle does today. For the rebuild itself, see [Rebuild a read model](./rebuild-a-read-model.md).

## Before you start

- A rebuild reprocesses **all** matching events, so the time it takes grows with the size of the history.
- While it runs, the observer handles no live events and on the sinks that use a replay container (MongoDB, SQL and in-memory) readers keep getting the pre-rebuild read model. The event-sequence sink has no replay container, so what it publishes during the replay is visible at once. Decide whether your readers can live with that for the whole run. See [What readers see during a rebuild](./rebuild-a-read-model.md#what-readers-see-during-a-rebuild).
- Observers that Chronicle owns itself cannot be replayed. The Workbench disables the action for them and the Kernel refuses the request.
- Replaying a **global-scope** projection is refused. See [Observers](../concepts/observers.mdx).

## Start the rebuild

Replay the observer from the [Cratis CLI](/cli/) or from the observers view in the [Chronicle Workbench](/chronicle/workbench/):

```bash
cratis chronicle observers replay <observer-id>
```

The replay runs as a Kernel [job](../jobs/index.mdx). If a replay job for that observer is still live, Chronicle does not start a second one.

## Watch it

- **Observer state.** An observer that is rebuilding shows the running state **Replaying**. Check it in the Workbench observers view, or with [`cratis chronicle observers show`](/cli/chronicle/observers/#show) and [`cratis chronicle observers list`](/cli/chronicle/observers/#list).
- **The job.** The Workbench jobs view and the [jobs API](../jobs/index.mdx) list the replay job, its status and its steps. A job that finished with failures or failed shows that status there.
- **Readers.** A read model that has stopped changing is expected during a rebuild. It does not mean the rebuild is stuck.

When the replay finishes, the observer leaves **Replaying**, routes again and catches up on the events appended in the meantime. Query the read model afterwards and confirm it reflects the new logic. If a rebuild finished but the read model did not change, see [A rebuild that produces nothing](./rebuild-a-read-model.md#a-rebuild-that-produces-nothing).

Fast forwarding is not available for an observer while it replays. It has to finish first.

## A rebuild that stalls or fails

A replay that does not complete successfully is not promoted. On the sinks that use a replay container (MongoDB, SQL and in-memory), readers keep the pre-rebuild model. The event-sequence sink has no replay container and no rollback, so facts it already published during the replay stay published. See [Reads during a replay](../read-models/getting-single-instance.mdx#reads-during-a-replay). Work through these in order:

1. **Look at the job.** In the Workbench jobs view or through the [jobs API](../jobs/index.mdx), check the replay job's status and steps. Jobs can be stopped, resumed and deleted there.
2. **Look at failed partitions.** Use [`cratis chronicle failed-partitions list`](/cli/chronicle/failed-partitions/#list) and [`cratis chronicle failed-partitions show`](/cli/chronicle/failed-partitions/#show) for the error and the failing sequence number. The Workbench shows the same under the event store's failed partitions.
3. **Fix the cause and deploy.** A handler that throws on an event keeps failing until the code is fixed.
4. **Resume what did not recover.** Use [`cratis chronicle observers retry-partition`](/cli/chronicle/observers/#retry-partition) for a partition, and [`cratis chronicle observers replay-partition`](/cli/chronicle/observers/#replay-partition) if the partition's state is corrupt. [My reactor throws and the stream seems stuck](../troubleshooting/index.mdx#my-reactor-throws-and-the-stream-seems-stuck) has the details.
5. **Check for quarantine.** A quarantined observer does not route again after a replay. Clear it with `cratis chronicle observers clear-quarantine` or from the Workbench. See [Quarantined observers](../concepts/observers.mdx#quarantined-observers).
6. **Start over.** Once the cause is fixed, replay the observer again.

Beyond the job actions above, Chronicle offers no dedicated command to cancel a replay.

## Roll back

A projection change is a registration. To go back to the previous definition, redeploy the previous code **with a higher version**, or set `Cratis:Chronicle:SoftwareVersion` on the rolled-back deployment to a version above the one you are leaving. Redeploying an older image with its old version is ignored, because it is an older registration. See [Rolling back](../projections/registration-lifecycle.md#rolling-back).

Rolling back changes the definition, not the data. Whether the read model is rebuilt again depends on the observer configuration, as described in [definition evolution](../hosting/configuration/observers.md#definition-evolution). Replay the observer yourself when the previous definition needs a rebuild.

## Coming with projection generations

Chronicle is working on a way to evolve projections without readers seeing the pre-rebuild state during the whole rebuild. It is tracked in [Cratis/Chronicle#5155](https://github.com/Cratis/Chronicle/issues/5155). Nothing described there is available today, and this page describes only what you can do now.

## See also

- [Rebuild a read model](./rebuild-a-read-model.md) — the rebuild itself and what readers see.
- [Jobs](../jobs/index.mdx) — inspect and control long-running Kernel operations.
- [Observers](../concepts/observers.mdx) — replay, failed partitions and quarantine.
- [Projection registration lifecycle](../projections/registration-lifecycle.md) — versions and rollback.
