// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Observation.Jobs;
using Cratis.Chronicle.Observation.Jobs.for_HandleEventsForPartition;
using Cratis.Chronicle.Storage.EventSequences;
using Cratis.Orleans.Jobs;
using Orleans.TestKit;

namespace Cratis.Chronicle.Observation.for_Observer.when_partition_replayed.given;

/// <summary>
/// Runs prepared partition steps against a deterministic, range-filtered event sequence.
/// </summary>
/// <param name="createSilo">Creates an isolated silo for each step grain.</param>
/// <param name="observer">The forwarding observer probe.</param>
/// <param name="subscriber">The recording subscriber.</param>
/// <param name="subscription">The actual observer subscription.</param>
public class partition_step_delivery(Func<TestKitSilo> createSilo, IObserver observer, IObserverSubscriber subscriber, ObserverSubscription subscription)
{
    readonly List<AppendedEvent> _events = [];

    /// <summary>
    /// Adds stored events that are available to replay and catch-up readers.
    /// </summary>
    /// <param name="events">The events to store.</param>
    public void Store(params AppendedEvent[] events) => _events.AddRange(events);

    /// <summary>
    /// Creates a cursor honoring the requested partition, range and event types.
    /// </summary>
    /// <param name="from">The lower bound.</param>
    /// <param name="to">The upper bound.</param>
    /// <param name="source">The partition.</param>
    /// <param name="types">The selected event types.</param>
    /// <returns>A deterministic cursor.</returns>
    public IEventCursor Read(EventSequenceNumber from, EventSequenceNumber to, EventSourceId source, IEnumerable<EventType> types)
    {
        var selectedTypes = types.ToArray();
        var batch = _events.Where(_ => _.Context.SequenceNumber >= from && _.Context.SequenceNumber <= to &&
            _.Context.EventSourceId == source && selectedTypes.Contains(_.Context.EventType)).OrderBy(_ => _.Context.SequenceNumber).ToArray();
        var cursor = Substitute.For<IEventCursor>();
        cursor.Current.Returns(batch);
        var moved = false;
        cursor.MoveNext().Returns(_ =>
        {
            var hasEvents = !moved && batch.Length > 0;
            moved = true;
            return Task.FromResult(hasEvents);
        });
        return cursor;
    }

    /// <summary>
    /// Executes the production delivery step using the job's prepared arguments.
    /// </summary>
    /// <param name="details">The prepared step.</param>
    /// <returns>The real step result.</returns>
    public async Task<JobStepResult> Perform(JobStepDetails details)
    {
        var arguments = (HandleEventsForPartitionArguments)details.Request;
        var state = new HandleEventsForPartitionState
        {
            ObserverKey = arguments.ObserverKey,
            Partition = arguments.Partition,
            StartEventSequenceNumber = arguments.StartEventSequenceNumber,
            EndEventSequenceNumber = arguments.EndEventSequenceNumber,
            EventObservationState = arguments.EventObservationState,
            EventTypes = arguments.EventTypes,
            ReaderScope = arguments.ReaderScope
        };
        var silo = createSilo();
        var step = await silo.CreateGrainAsync<TestableHandleEventsForPartition>(details.Id, details.Key);
        step.SetupForTesting(observer, subscriber, arguments.Partition.ToString(), subscription);
        var result = await step.InvokePerformStep(state);
        result.TryGetResult(out var completed).ShouldBeTrue();
        completed.TryGetFullResult<HandleEventsForPartitionResult>(out _, out _, new JsonSerializerOptions()).ShouldBeTrue();
        return completed;
    }
}
