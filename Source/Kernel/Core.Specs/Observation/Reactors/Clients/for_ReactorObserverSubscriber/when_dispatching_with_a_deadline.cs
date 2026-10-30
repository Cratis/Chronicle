// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Clients;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation.Reactors;

namespace Cratis.Chronicle.Observation.Reactors.Clients.for_ReactorObserverSubscriber;

public class when_dispatching_with_a_deadline : given.a_subscriber
{
    Task<ObserverSubscriberResult> _delivery;

    void Because() => _delivery = _subscriber.OnNext("partition", [], _context);

    [Fact] void should_tell_the_mediator_how_long_the_caller_waits() =>
        _mediator.Received(1).OnNext(
            Arg.Any<ReactorId>(),
            Arg.Any<ConnectionId>(),
            Arg.Any<EventStoreName>(),
            Arg.Any<EventStoreNamespaceName>(),
            Arg.Any<Key>(),
            Arg.Any<IEnumerable<AppendedEvent>>(),
            Arg.Any<TaskCompletionSource<ObserverSubscriberResult>>(),
            TimeSpan.FromSeconds(30));

    async Task Destroy()
    {
        _completion?.TrySetResult(ObserverSubscriberResult.Ok(EventSequenceNumber.First));
        await _delivery;
    }
}
