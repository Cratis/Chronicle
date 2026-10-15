// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation.Jobs.for_HandleEventsForPartition.given;

public class a_performing_job_step_with_observer_filters : a_performing_job_step
{
    protected readonly List<EventSequenceNumber[]> _handledBatches = [];
    protected bool _succeeded;
    protected HandleEventsForPartitionResult _result;

    protected virtual ObserverFilters Filters => new(["audited"]);

    protected static AppendedEvent CreateEvent(EventSequenceNumber sequenceNumber, Tag[]? tags = null, EventSourceType? eventSourceType = null) =>
        AppendedEvent.EmptyWithEventSequenceNumber(sequenceNumber) with
        {
            Context = EventContext.Empty with
            {
                SequenceNumber = sequenceNumber,
                EventSourceId = "some-partition",
                Tags = tags ?? [],
                EventSourceType = eventSourceType ?? EventSourceType.Default
            }
        };

    protected static readonly Tag[] _audited = [new("audited")];

    protected async Task PerformStep()
    {
        var performed = await _jobStep.InvokePerformStep(_performState);
        performed.TryGetResult(out var stepResult);
        _succeeded = stepResult.TryGetFullResult(out _result, out _, new JsonSerializerOptions());
    }

    void Establish()
    {
        _observer.GetSubscription().Returns(new ObserverSubscription(
            "observer-id",
            new ObserverKey("observer-id", "event-store", "event-store-namespace", EventSequenceId.Log),
            [],
            typeof(ISomeObserverType),
            SiloAddress.Zero,
            Filters: Filters));

        _observerSubscriber.OnNext(Arg.Any<Key>(), Arg.Any<IEnumerable<AppendedEvent>>(), Arg.Any<ObserverSubscriberContext>())
            .Returns(call =>
            {
                var events = call.ArgAt<IEnumerable<AppendedEvent>>(1).ToArray();
                _handledBatches.Add([.. events.Select(_ => _.Context.SequenceNumber)]);
                return Task.FromResult(ObserverSubscriberResult.Ok(events[^1].Context.SequenceNumber));
            });
    }
}
