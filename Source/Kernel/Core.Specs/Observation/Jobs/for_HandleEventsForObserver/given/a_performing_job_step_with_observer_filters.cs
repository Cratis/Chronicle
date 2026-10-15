// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation.Jobs.for_HandleEventsForObserver.given;

public class a_performing_job_step_with_observer_filters : a_performing_job_step
{
    protected bool _succeeded;
    protected HandleEventsForPartitionResult _result;

    protected static AppendedEvent CreateTaggedEvent(EventSequenceNumber sequenceNumber, EventSourceId eventSourceId, params Tag[] tags)
    {
        var @event = CreateEvent(sequenceNumber, eventSourceId);
        return @event with { Context = @event.Context with { Tags = tags } };
    }

    protected async Task PerformStep()
    {
        var performed = await _jobStep.InvokePerformStep(_performState);
        performed.TryGetResult(out var stepResult);
        _succeeded = stepResult.TryGetFullResult(out _result, out _, new JsonSerializerOptions());
    }

    void Establish() =>
        _observer.GetSubscription().Returns(new ObserverSubscription(
            "observer-id",
            new ObserverKey("observer-id", "event-store", "event-store-namespace", EventSequenceId.Log),
            [],
            typeof(ISomeObserverType),
            SiloAddress.Zero,
            Filters: new ObserverFilters(["audited"])));
}
