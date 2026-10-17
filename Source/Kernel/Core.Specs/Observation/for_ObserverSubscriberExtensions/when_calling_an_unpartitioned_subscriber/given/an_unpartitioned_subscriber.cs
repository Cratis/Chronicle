// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;

namespace Cratis.Chronicle.Observation.for_ObserverSubscriberExtensions.when_calling_an_unpartitioned_subscriber.given;

public class an_unpartitioned_subscriber : Specification
{
    protected IObserverSubscriber _subscriber;
    protected List<int> _batchSizes;
    protected AppendedEvent[] _events;

    void Establish()
    {
        _subscriber = Substitute.For<IObserverSubscriber, IUnpartitionedObserverSubscriber>();
        _batchSizes = [];
        _events = [.. Enumerable.Range(0, 50).Select(i => AppendedEvent.EmptyWithEventSequenceNumber((ulong)i))];
        _subscriber.OnNext(Arg.Any<Key>(), Arg.Any<IEnumerable<AppendedEvent>>(), Arg.Any<ObserverSubscriberContext>())
            .Returns(call => HandleBatch(call.ArgAt<IEnumerable<AppendedEvent>>(1).ToArray()));
    }

    protected virtual Task<ObserverSubscriberResult> HandleBatch(AppendedEvent[] batch)
    {
        _batchSizes.Add(batch.Length);
        return Task.FromResult(ObserverSubscriberResult.Ok(batch[^1].Context.SequenceNumber));
    }
}
