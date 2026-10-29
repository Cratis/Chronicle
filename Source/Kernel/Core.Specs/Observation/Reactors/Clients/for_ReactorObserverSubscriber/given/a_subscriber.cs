// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Clients;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation.Reactors;
using Cratis.Chronicle.Observation.given;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.Observation.Reactors.Clients.for_ReactorObserverSubscriber.given;

public class a_subscriber : Specification
{
    protected readonly DeliveryClock _clock = new();
    protected IReactorMediator _mediator;
    protected ReactorObserverSubscriber _subscriber;
    protected ObserverSubscriberContext _context;
    protected TaskCompletionSource<ObserverSubscriberResult> _completion;

    void Establish()
    {
        _mediator = Substitute.For<IReactorMediator>();
        _mediator.When(mediator => mediator.OnNext(Arg.Any<ReactorId>(), Arg.Any<ConnectionId>(), Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName>(), Arg.Any<Key>(), Arg.Any<IEnumerable<AppendedEvent>>(), Arg.Any<TaskCompletionSource<ObserverSubscriberResult>>(), Arg.Any<TimeSpan?>()))
            .Do(call => _completion = call.Arg<TaskCompletionSource<ObserverSubscriberResult>>());
        _subscriber = new(_mediator, NullLogger<ReactorObserverSubscriber>.Instance, _clock);
        _context = new ObserverSubscriberContext(new ConnectedClient { ConnectionId = "A" }).WithDeadlines(TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(300), _clock);
    }
}
