// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;
using Orleans.TestKit;

namespace Cratis.Chronicle.Observation.Jobs.for_HandleEventsForObserver.given;

public class a_partition_page_job_step : a_performing_job_step
{
    protected IPartitionPageObserverType _pageSubscriber;
    protected readonly List<IReadOnlyList<ObserverPartitionBatch>> _pages = [];

    void Establish()
    {
        var subscription = new ObserverSubscription("observer-id", _performState.ObserverKey, [], typeof(IPartitionPageObserverType), SiloAddress.Zero);
        _observer.GetSubscription().Returns(subscription);
        _pageSubscriber = Substitute.For<IPartitionPageObserverType>();
        _silo.AddProbe<IPartitionPageObserverType>(_ => _pageSubscriber);
        _pageSubscriber.OnNextPartitions(Arg.Any<IReadOnlyList<ObserverPartitionBatch>>(), Arg.Any<ObserverSubscriberContext>())
            .Returns(call =>
            {
                var batches = call.Arg<IReadOnlyList<ObserverPartitionBatch>>();
                _pages.Add(batches);
                return new ObserverPageResult(batches.ToDictionary(_ => _.Partition, _ => ObserverSubscriberResult.Ok(_.Events[^1].Context.SequenceNumber)));
            });
    }
}
