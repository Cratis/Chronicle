---
title: Get alerted when observers stop processing
description: Find out within minutes, not days, when a Chronicle partition keeps failing or an observer is quarantined — using the metrics and CLI you already have.
---

A reactor throws on one event, and its partition stops. Chronicle retries it, then gives up and waits for someone to act. Meanwhile, every other partition keeps flowing, the application looks healthy, and nobody is told. A failed partition can sit like that for days — the emails that reactor should have sent are simply never sent.

Chronicle does not send alerts itself yet. This guide sets up two ways to be told today: an alert rule on the metrics Chronicle already exports, and a scheduled health check with the [Cratis CLI](/cli/). Use one or both.

## What to watch

An observer — a reactor, reducer or projection — processes events per **partition**, one partition per event source. When the handler fails, only that partition stops:

1. **The partition fails and retries.** Chronicle retries it with an exponential backoff: waits of 2 seconds, doubling each time, capped at 10 minutes. It is still recoverable on its own, for example when a dependency comes back.
2. **The partition runs out of retries and is quarantined.** After `MaxRetryAttempts` (10 by default) Chronicle stops retrying it. With the default settings that happens roughly half an hour after the first failure. From here on, the partition does not resume until an operator acts — see [My reactor throws and the stream seems stuck](/chronicle/troubleshooting/#my-reactor-throws-and-the-stream-seems-stuck).
3. **The whole observer can be quarantined.** For example, when `QuarantineOnFailedPartitionCount` or `QuarantineOnFailedPartitionPercentage` is set and crossed, Chronicle stops the entire observer.

A quarantined partition is the signal that needs a person. A partition that keeps failing is an early warning. The retry and quarantine settings are described in [observer configuration](/chronicle/hosting/configuration/observers/).

## Alert from metrics

Chronicle exports its metrics over [OpenTelemetry](/chronicle/hosting/configuration/open-telemetry/) from the `Cratis.Chronicle` meter. Three counters describe failing observers. Each has one series per observer, tagged `EventStore`, `Namespace`, `ObserverId` and `EventSequenceId`. An observer has a series on them from its first failure, not before:

| Instrument | Counts |
| --- | --- |
| `chronicle-observer-partitions-failed` | Every failed attempt, including each retry. It is not a count of partitions. |
| `chronicle-observer-partitions-quarantined` | Partitions that ran out of retries |
| `chronicle-observer-quarantined` | Times the whole observer was quarantined |

With the default translation of the OpenTelemetry Collector's Prometheus exporter, or Prometheus's own OTLP receiver, the counters appear as `chronicle_observer_partitions_failed_total`, `chronicle_observer_partitions_quarantined_total` and `chronicle_observer_quarantined_total`, and the tags keep their names. Look the series up in your backend before relying on the names below.

A series is created by the first failure and starts at `1`; Chronicle does not record a `0` beforehand, so healthy observers cost no series. The first value your backend receives for an observer is therefore already `1`. A rule built only on `increase()` needs two samples in its window and reports nothing for that series. Each rule below therefore has two halves, joined with `or`: the first catches series that went up inside the window, the second catches series that did not exist at the start of the window and are already above the threshold. Both aggregate to the same labels, so an observer produces one series, and so one alert, whichever half caught it.

The rules select each counter by its name prefix, for example `{__name__=~"chronicle_observer_partitions_quarantined.*"}`, so the same rules work for the long names that earlier Kernels export. These Prometheus alerting rules report, per observer, partitions that ran out of retries and observer quarantines in the last 30 minutes, and repeated failed attempts in the last 15 minutes:

```yaml title="chronicle-observer-alerts.yml"
groups:
  - name: chronicle-observers
    rules:
      - alert: ChroniclePartitionsQuarantined
        expr: |
          sum by (EventStore, Namespace, ObserverId) (
            increase({__name__=~"chronicle_observer_partitions_quarantined.*"}[30m])
          ) > 0
          or
          count by (EventStore, Namespace, ObserverId) (
            {__name__=~"chronicle_observer_partitions_quarantined.*"} > 0
            unless {__name__=~"chronicle_observer_partitions_quarantined.*"} offset 30m
          )
        labels:
          severity: critical
        annotations:
          summary: "Partitions of {{ $labels.ObserverId }} in {{ $labels.EventStore }}/{{ $labels.Namespace }} ran out of retries"
          description: "Chronicle will not retry them again. Inspect with: cratis chronicle failed-partitions list -e {{ $labels.EventStore }} -n {{ $labels.Namespace }}"

      - alert: ChronicleObserverQuarantined
        expr: |
          sum by (EventStore, Namespace, ObserverId) (
            increase({__name__=~"chronicle_observer_quarantined.*"}[30m])
          ) > 0
          or
          count by (EventStore, Namespace, ObserverId) (
            {__name__=~"chronicle_observer_quarantined.*"} > 0
            unless {__name__=~"chronicle_observer_quarantined.*"} offset 30m
          )
        labels:
          severity: critical
        annotations:
          summary: "{{ $labels.ObserverId }} in {{ $labels.EventStore }}/{{ $labels.Namespace }} was quarantined and processes no events"

      - alert: ChroniclePartitionsFailing
        expr: |
          sum by (EventStore, Namespace, ObserverId) (
            increase({__name__=~"chronicle_observer_partitions_failed.*"}[15m])
          ) >= 3
          or
          count by (EventStore, Namespace, ObserverId) (
            {__name__=~"chronicle_observer_partitions_failed.*"} >= 3
            unless {__name__=~"chronicle_observer_partitions_failed.*"} offset 15m
          )
        labels:
          severity: warning
        annotations:
          summary: "{{ $labels.ObserverId }} in {{ $labels.EventStore }}/{{ $labels.Namespace }} has failed repeatedly in the last 15 minutes"
```

Route the alerts with Alertmanager or Grafana to wherever your team works — email, Slack, Microsoft Teams, PagerDuty. Other backends that receive OTLP, such as Azure Monitor, Datadog or Grafana Cloud, can express the same conditions in their own alert languages. The two halves of each rule are what to carry over: a series that appears already above zero is as much a failure as one that climbs.

:::caution[Older Kernels export different series]
Earlier versions export these counters with the description appended to the name, for example `chronicle_observer_partitions_failed_Number_of_failed_partitions_per_observer_in_a_given_event_store_and_namespace_total`, which the prefix selectors above match. They also tag every measurement with `partition` — the event source id, which can identify a person — and have no `chronicle-observer-quarantined` counter, so `ChronicleObserverQuarantined` never fires for them. With the `partition` tag each partition has its own series that starts at its first failure, and the rules evaluate the second half per partition series. Consider dropping the `partition` label at your collector if your telemetry store must not hold personal data.
:::

Know the limits of counters before you depend on them:

- **They say "recently", not "still".** The quarantine alerts fire when something is quarantined, then resolve after 30 minutes even if it is still stuck. The failing alert cannot tell a partition that is still failing from one that recovered after its latest failure. Keep the scheduled check below for the current state.
- **They reset when the Kernel restarts.** `increase()` handles resets, and a partition that fails again after a restart is counted again.
- **A gap in your own data raises false alerts.** The second half of each rule fires for every series that is above the threshold now and did not exist 30 minutes ago (15 for the failing alert). After Prometheus or the collector restarts, a scrape outage, or a series dropped by retention, every observer that has ever failed qualifies, and the alerts resolve once the window has passed. A `for:` clause does not help, because the condition stays true for the whole window. Inhibit these alerts in Alertmanager while your monitoring itself is recovering, for example with an alert on `time() - process_start_time_seconds < 1800` for the Prometheus job.

## Alert from a scheduled health check

[`cratis chronicle diagnose`](/cli/chronicle/diagnose/) checks one event store and namespace and exits with a non-zero status when it finds failed partitions, quarantined ones included. Run it on a schedule, and the scheduler's own failure notification becomes your alert — no metrics pipeline needed.

Authenticate with client credentials in the connection string: `chronicle://<client-id>:<client-secret>@<host>:35000` — see [server connection strings](../connection-strings/server.md) for the format. The CLI reads the connection string from `CHRONICLE_CONNECTION_STRING`; see [connecting the CLI](/cli/reference/connection/).

A GitHub Actions workflow — replace `my-store` and the namespaces with your own — that checks every 15 minutes and fails — notifying whoever watches the repository's workflow runs — when a partition is failing:

```yaml title=".github/workflows/chronicle-health.yml"
name: Chronicle health
on:
  schedule:
    - cron: "*/15 * * * *"
  workflow_dispatch:

jobs:
  diagnose:
    runs-on: ubuntu-latest
    strategy:
      fail-fast: false
      matrix:
        namespace: [Default]
    steps:
      - uses: actions/setup-dotnet@v4
        with:
          dotnet-version: "10.0.x"
      - name: Install the Cratis CLI
        run: |
          dotnet tool install -g Cratis.Cli
          echo "$HOME/.dotnet/tools" >> "$GITHUB_PATH"
      - name: Check for failed partitions
        env:
          CHRONICLE_CONNECTION_STRING: ${{ secrets.CHRONICLE_CONNECTION_STRING }}
        run: cratis chronicle diagnose -e my-store -n ${{ matrix.namespace }} -o json
```

The same command works from a Kubernetes CronJob or any cron-style scheduler with the CLI installed. Add one run per namespace you care about; the matrix above does that.

:::caution[Check what `diagnose` did not look at]
`diagnose` currently counts a check it could not run — for example a failed-partitions query that returned an error — as zero failures, and it does not treat a quarantined observer as unhealthy unless that observer also has failed partitions. Treat a green run as "no failed partitions were reported", and rely on the `ChronicleObserverQuarantined` alert or `cratis chronicle observers list` for quarantined observers.
:::

## When an alert fires

1. See which partitions failed and why: `cratis chronicle failed-partitions list -e <store> -n <namespace>`, then `failed-partitions show` for the error and the failing sequence number. The Workbench shows the same under the event store's failed partitions.
2. Fix the cause and deploy.
3. Resume what did not recover on its own. A partition that ran out of retries is not retried by an ordinary retry — follow [My reactor throws and the stream seems stuck](/chronicle/troubleshooting/#my-reactor-throws-and-the-stream-seems-stuck).
