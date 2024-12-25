// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Metrics;

namespace Cratis.Chronicle.Grains.Observation;

#pragma warning disable SA1600 // Elements should be documented
#pragma warning disable MA0048 // File name must match type name
#pragma warning disable SA1402 // File may only contain a single type

internal static partial class ObserverMetrics
{
    [Counter<int>("chronicle-observer-successful-observations", "Number of successful observations per observer in a given event store and namespace")]
    internal static partial void SuccessfulObservation(this IMeterScope<Observer> meter);

    [Counter<int>("chronicle-observer-partitions-failed", "Number of failed partitions per observer in a given event store and namespace")]
    internal static partial void PartitionFailed(this IMeterScope<Observer> meter, Key partition);
}

internal static class ObserverMetricsScopes
{
    internal static IMeterScope<Observer> BeginObserverScope(this IMeter<Observer> meter, ObserverId observerId, ObserverKey observerKey) =>
        meter.BeginScope(new Dictionary<string, object>
        {
            ["ObserverId"] = observerId,
            ["EventStore"] = observerKey.EventStore,
            ["Namespace"] = observerKey.Namespace,
            ["EventSequenceId"] = observerKey.EventSequenceId
        });
}
