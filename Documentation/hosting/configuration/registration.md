---
title: Configure how Cratis Chronicle Server takes in registrations
description: Configure how Chronicle accepts registrations from clients, applies projections in the background, bounds the work and answers a busy kernel.
---
A client registers its artifacts every time it starts or reconnects. Chronicle accepts a projection registration durably, answers at once and applies it in the background, so a busy kernel never makes a client wait for the work behind its registration, and a client that retries does not add to that work.

## Example configuration

```json
{
  "registration": {
    "acceptAsynchronously": true,
    "maxConcurrentRegistrations": 4,
    "maxQueueAgeSeconds": 20,
    "busyRetryAfterSeconds": 5,
    "maxApplyAttempts": 3,
    "applyRetryDelaySeconds": 5
  }
}
```

| Property | Type | Default | Description |
| --- | --- | --- | --- |
| acceptAsynchronously | boolean | true | Accept a projection registration with one write and apply it afterwards. Every [storage provider](storage.mdx) keeps registration intents: MongoDB, SQL and in-memory. Set it to false to apply each registration before answering it. |
| maxConcurrentRegistrations | number | 4 | How many distinct projection registrations one event store applies at the same time. The rest wait in arrival order. Identical registrations share the one in flight |
| maxQueueAgeSeconds | number | 20 | How long a registration waits for a place before it is answered with a retryable "unavailable" response carrying a `retry-after` trailer, instead of waiting for the 30 second call timeout |
| busyRetryAfterSeconds | number | 5 | The `retry-after` the busy response carries |
| maxApplyAttempts | number | 3 | How many times an accepted registration is applied before it is recorded as failed |
| applyRetryDelaySeconds | number | 5 | Seconds between those attempts. A failing registration waits; the registrations behind it do not |

Watch `cratis.chronicle.registration.pending` and `cratis.chronicle.registration.oldest_pending_age` (see [OpenTelemetry](open-telemetry.md)) to see whether accepted registrations are being applied: an age that keeps growing means the backlog is not draining.

## What the kernel does

The kernel keeps one *registration intent* for each registrant that registers a full set of projections, and one for each distinct content of a partial registration. An intent holds the content hash, the content, a revision and its state: `Pending`, `Applied` or `Failed`.

- A registration that repeats what was last accepted for the same key changes nothing and touches no downstream grain.
- A newer full set from the same registrant replaces an intent that has not been applied yet.
- One worker per event store applies the pending intents in the order they were accepted. A kernel restart, or the loss of a silo, resumes the intents that were not applied: the worker is restarted when the event store is activated and from the silo supervisor every 30 seconds.
- Event types and constraints are not deferred. Appends depend on them, so registering them still returns after they are stored.
- A projection observes nothing until its registration has been applied; it then receives every event from its own position, in order.

## Asking where a registration is

`GetRegistrationStatus` on the projections service returns `Pending`, `Applied` or `Failed` for a registrant, with the latest and the last applied revision and, for a failure, why it failed. A registration that could not be applied after `maxApplyAttempts` stays `Failed` and is written to the log as an error; registering it again unchanged retries it. The .NET client waits for the status after it registers, with short calls, and throws `ProjectionRegistrationNotApplied` for a failure.

## Upgrading

This is a stop-start release for the grain contracts: the kernel adds the `IRegistrationInbox` grain and `IProjectionsManager.RegisterShared` takes a cancellation token. Stop every node, then start the new version. The client gRPC contract only gains `GetRegistrationStatus`; older clients keep working and are answered earlier, and a newer client talking to an older kernel does not wait for a status the kernel cannot give.
