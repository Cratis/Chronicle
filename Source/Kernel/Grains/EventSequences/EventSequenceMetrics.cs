// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Events;
using Cratis.Metrics;

namespace Cratis.Kernel.Grains.EventSequences;

#pragma warning disable SA1600 // Elements should be documented
#pragma warning disable MA0048 // File name must match type name
#pragma warning disable SA1402 // File may only contain a single type

internal static partial class EventSequenceMetrics
{
    [Counter<int>("cratis-event-sequences-appended-events", "Number of events appended to the event sequence")]
    internal static partial void AppendedEvent(this IMeterScope<EventSequence> scope, EventSourceId eventSourceId, string eventName);

    [Counter<int>("cratis-event-sequences-duplicate-event-sequence-numbers", "Number of duplicate event sequence numbers")]
    internal static partial void DuplicateEventSequenceNumber(this IMeterScope<EventSequence> scope, EventSourceId eventSourceId, string eventName);

    [Counter<int>("cratis-event-sequences-failed-appended-events", "Number of events that failed to be appended to the event sequence")]
    internal static partial void FailedAppending(this IMeterScope<EventSequence> scope, EventSourceId eventSourceId, string eventName);
}
internal static partial class EventSequenceMetrics
{
    internal static partial void AppendedEvent(this IMeterScope<EventSequence> scope, EventSourceId eventSourceId, string eventName)
    {
        throw new NotImplementedException();
    }

    internal static partial void DuplicateEventSequenceNumber(this IMeterScope<EventSequence> scope, EventSourceId eventSourceId, string eventName)
    {
        throw new NotImplementedException();
    }

    internal static partial void FailedAppending(this IMeterScope<EventSequence> scope, EventSourceId eventSourceId, string eventName)
    {
        throw new NotImplementedException();
    }
}

internal static class EventSequenceMetricsScopes
{
    internal static IMeterScope<EventSequence> BeginEventSequenceScope(this IMeter<EventSequence> meter, EventStoreName eventStore, EventStoreNamespaceName @namespace) =>
        meter.BeginScope(new Dictionary<string, object>
        {
            ["EventStore"] = eventStore,
            ["Namespace"] = @namespace
        });
}
