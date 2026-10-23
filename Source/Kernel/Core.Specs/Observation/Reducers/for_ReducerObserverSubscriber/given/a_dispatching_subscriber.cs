// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Clients;
using Cratis.Chronicle.Concepts.Observation.Reducers;
using NSubstitute.ClearExtensions;

namespace Cratis.Chronicle.Observation.Reducers.Clients.for_ReducerObserverSubscriber.given;

public class a_dispatching_subscriber : a_subscriber_with_a_cached_pipeline
{
    protected TaskCompletionSource<ReducerSubscriberResult> _completion;
    protected bool _sinkReached;
    protected ObserverSubscriberContext _context;

    void Establish()
    {
        _mediator.ClearSubstitute();
        _context = new ObserverSubscriberContext(new ConnectedClient { ConnectionId = "A" }).WithDeadlines(TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(300), _clock);
        _mediator.When(mediator => mediator.OnNext(Arg.Any<ReducerId>(), Arg.Any<ConnectionId>(), Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName>(), Arg.Any<ReduceOperation>(), Arg.Any<TaskCompletionSource<ReducerSubscriberResult>>()))
            .Do(call => _completion = call.Arg<TaskCompletionSource<ReducerSubscriberResult>>());
        _firstPipeline.Reduce(Arg.Any<ReducerContext>(), Arg.Any<ReducerDelegate>()).Returns(async call =>
        {
            await call.Arg<ReducerDelegate>()([], null);
            _sinkReached = true;
        });
    }
}
