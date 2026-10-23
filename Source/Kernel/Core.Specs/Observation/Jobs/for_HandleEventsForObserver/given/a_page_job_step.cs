// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;
using Orleans.TestKit;

namespace Cratis.Chronicle.Observation.Jobs.for_HandleEventsForObserver.given;

public class a_page_job_step : a_performing_job_step
{
    protected IPageObserverType _pageSubscriber;
    protected readonly List<AppendedEvent[]> _pages = [];
    protected readonly List<ObserverSubscriberKey> _subscriberKeys = [];

    void Establish()
    {
        var subscription = new ObserverSubscription("observer-id", _performState.ObserverKey, [], typeof(IPageObserverType), SiloAddress.Zero);
        _observer.GetSubscription().Returns(subscription);
        _pageSubscriber = Substitute.For<IPageObserverType>();
        _silo.AddProbe<IPageObserverType>(key =>
        {
            _subscriberKeys.Add(ObserverSubscriberKey.Parse(key.ToString()));
            return _pageSubscriber;
        });
        _pageSubscriber.OnNextPage(Arg.Any<IEnumerable<AppendedEvent>>(), Arg.Any<ObserverSubscriberContext>())
            .Returns(call =>
            {
                var page = call.Arg<IEnumerable<AppendedEvent>>().ToArray();
                _pages.Add(page);
                return ObserverSubscriberResult.Ok(page[^1].Context.SequenceNumber);
            });
        _pageSubscriber.OnNext(Arg.Any<Key>(), Arg.Any<IEnumerable<AppendedEvent>>(), Arg.Any<ObserverSubscriberContext>())
            .Returns(call =>
            {
                var events = call.ArgAt<IEnumerable<AppendedEvent>>(1).ToArray();
                _handledBatches.Add(new(call.Arg<Key>(), events.Select(_ => _.Context.SequenceNumber).ToArray()));
                return ObserverSubscriberResult.Ok(events[^1].Context.SequenceNumber);
            });
    }
}
