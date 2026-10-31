// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Storage.Sinks;
using Orleans.TestKit;

namespace Cratis.Chronicle.Observation.Reducers.Clients.for_ReducerObserverSubscriber.when_the_sink_write_fails.given;

public class a_subscriber_whose_sink_write_fails : for_ReducerObserverSubscriber.given.a_subscriber_with_a_cached_pipeline
{
    protected const string Partition = "the-partition";
    protected readonly FailedPartition _other = new("another-partition", 7UL) { Reason = "dropped with the batch" };
    protected IObserver _observer;
    protected ObserverSubscriberResult _result;

    protected virtual IReadOnlyCollection<FailedPartition> FailedPartitions => [_other];

    void Establish()
    {
        _observer = Substitute.For<IObserver>();
        _silo.AddProbe(_ => _observer);

        // The client reduces the batch, then the sink reports the failures - as the reducer pipeline does.
        _firstPipeline.Reduce(Arg.Any<ReducerContext>(), Arg.Any<ReducerDelegate>()).Returns<Task<Concepts.ReadModels.ReadModelChangeNotification?>>(async callInfo =>
        {
            await callInfo.Arg<ReducerDelegate>()([], null);
            throw new ReducerSinkWriteFailed(FailedPartitions);
        });
    }

    protected async Task Deliver() => _result = await _subscriber.OnNext(
        Partition,
        [AppendedEvent.EmptyWithEventTypeAndEventSequenceNumber(new EventType("the-event", 1), 42UL)],
        new ObserverSubscriberContext(new Cratis.Chronicle.Concepts.Clients.ConnectedClient()));
}
