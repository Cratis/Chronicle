// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;

namespace Cratis.Chronicle.Observation.Jobs.for_HandleEventsForPartition.when_flushing_bulk_writes;

public class and_the_next_batch_loses_the_window : given.a_buffered_projection_job_step
{
    int _batch;

    void Establish()
    {
        var eventType = _performState.EventTypes.Single();
        var first = AppendedEvent.EmptyWithEventType(eventType) with { Context = EventContext.Empty with { EventType = eventType, SequenceNumber = 1UL } };
        var second = first with { Context = first.Context with { SequenceNumber = 2UL } };
        _eventCursor.MoveNext().Returns(_ => Task.FromResult(++_batch <= 2));
        _eventCursor.Current.Returns(_ => _batch == 1 ? [first] : [second]);
        _projection.OnNext(Arg.Any<Key>(), Arg.Any<IEnumerable<AppendedEvent>>(), Arg.Any<ObserverSubscriberContext>()).Returns(
            Task.FromResult(ObserverSubscriberResult.Ok(1UL)), Task.FromResult(ObserverSubscriberResult.Ok(2UL)));
        _observerServiceClient.FlushAllFor(Arg.Any<ObserverDetails>(), Arg.Any<bool>()).Returns(
            Task.FromResult(Windows(_window)), Task.FromResult(Windows(_window)), Task.FromResult(Windows()));
    }

    async Task Because() => _result = await _jobStep.InvokePerformStep(_performState);

    [Fact] void should_fail_the_step() => _result.Match(result => !result.TryGetError(out _), _ => true).ShouldBeFalse();
    [Fact] void should_keep_the_durable_batch_checkpoint() => _stateStorage.State.LastSuccessfullyHandledEventSequenceNumber.Value.ShouldEqual(1UL);
    [Fact] void should_only_count_the_durable_batch() => _observer.ReceivedCalls().Count(call => call.GetMethodInfo().Name == nameof(IObserver.ReportHandledEvents)).ShouldEqual(1);
}
