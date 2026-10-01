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

Chronicle exports its metrics over [OpenTelemetry](/chronicle/hosting/configuration/open-telemetry/) from the `Cratis.Chronicle` meter. Two counters describe failing partitions:

| Instrument | Counts | Tags |
| --- | --- | --- |
| `chronicle-observer-partitions-failed` | Every failed attempt, including each retry. It is not a count of partitions. | `EventStore`, `Namespace`, `ObserverId`, `EventSequenceId`, `partition` |
| `chronicle-observer-partitions-quarantined` | Partitions that ran out of retries | `EventStore`, `Namespace`, `ObserverId`, `EventSequenceId`, `partition` |

Observer quarantine has no metric yet. The scheduled check below catches it only when the quarantined observer also has failed partitions; for the rest, check `cratis chronicle observers list` for observers in the `Quarantined` state.

With the default translation of the OpenTelemetry Collector's Prometheus exporter, or Prometheus's own OTLP receiver, the counters appear as `chronicle_observer_partitions_failed_total` and `chronicle_observer_partitions_quarantined_total`, and the tags keep their names. Look the series up in your backend before relying on the names below.

:::caution[`increase()` alone misses a newly quarantined partition]
Each partition has its own series, and a series only exists from its first increment. A quarantined partition's counter is born at `1` and stays there, so `increase()` — which needs two samples inside its window — reports nothing for it, and a rule built only on `increase()` never fires. Also select series that did not exist at the start of the window, as the rules below do.
:::

These Prometheus alerting rules report, per observer, how many partitions ran out of retries in the last 30 minutes, and how many have recorded at least five failed attempts with at least one in the last 15 minutes:

```yaml title="chronicle-observer-alerts.yml"
groups:
  - name: chronicle-observers
    rules:
      - alert: ChroniclePartitionsQuarantined
        expr: |
          count by (EventStore, Namespace, ObserverId) (
            (chronicle_observer_partitions_quarantined_total
              unless chronicle_observer_partitions_quarantined_total offset 30m)
            or (increase(chronicle_observer_partitions_quarantined_total[30m]) > 0)
          )
        labels:
          severity: critical
        annotations:
          summary: "{{ $value }} partition(s) of {{ $labels.ObserverId }} in {{ $labels.EventStore }}/{{ $labels.Namespace }} ran out of retries"
          description: "Chronicle will not retry them again. Inspect with: cratis chronicle failed-partitions list -e {{ $labels.EventStore }} -n {{ $labels.Namespace }}"

      - alert: ChroniclePartitionsFailing
        expr: |
          count by (EventStore, Namespace, ObserverId) (
            (chronicle_observer_partitions_failed_total >= 5)
            and (
              (chronicle_observer_partitions_failed_total
                unless chronicle_observer_partitions_failed_total offset 15m)
              or (increase(chronicle_observer_partitions_failed_total[15m]) > 0)
            )
          )
        labels:
          severity: warning
        annotations:
          summary: "{{ $value }} partition(s) of {{ $labels.ObserverId }} in {{ $labels.EventStore }}/{{ $labels.Namespace }} have failed at least five times, most recently within 15 minutes"
```

Route the alerts with Alertmanager or Grafana to wherever your team works — email, Slack, Microsoft Teams, PagerDuty. Other backends that receive OTLP, such as Azure Monitor, Datadog or Grafana Cloud, can express the same two conditions in their own alert languages.

Know the limits of counters before you depend on them:

- **They say "recently", not "still".** The quarantine alert fires when a partition runs out of retries, then resolves after 30 minutes even if the partition is still stuck. The failing alert cannot tell a partition that is still failing from one that recovered after its latest failure. Keep the scheduled check below for the current state.
- **They reset when the Kernel restarts.** `increase()` handles resets, and a partition that fails again after a restart is counted again.
- **They carry the partition as a tag.** Always aggregate over the observer tags, as the rules do. The partition is the event source id, so it can identify a person — consider dropping the `partition` label at your collector if your telemetry store must not hold personal data.

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
`diagnose` currently counts a check it could not run — for example a failed-partitions query that returned an error — as zero failures, and it does not treat a quarantined observer as unhealthy unless that observer also has failed partitions. Treat a green run as "no failed partitions were reported", and use `cratis chronicle observers list` to find quarantined observers.
:::

## When an alert fires

1. See which partitions failed and why: `cratis chronicle failed-partitions list -e <store> -n <namespace>`, then `failed-partitions show` for the error and the failing sequence number. The Workbench shows the same under the event store's failed partitions.
2. Fix the cause and deploy.
3. Resume what did not recover on its own. A partition that ran out of retries is not retried by an ordinary retry — follow [My reactor throws and the stream seems stuck](/chronicle/troubleshooting/#my-reactor-throws-and-the-stream-seems-stuck).
