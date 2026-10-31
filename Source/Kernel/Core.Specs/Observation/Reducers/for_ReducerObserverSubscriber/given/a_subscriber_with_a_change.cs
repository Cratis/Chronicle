// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Clients;
using Cratis.Chronicle.Concepts.Observation.Reducers;
using Cratis.Chronicle.Concepts.ReadModels;
using NSubstitute.ClearExtensions;

namespace Cratis.Chronicle.Observation.Reducers.Clients.for_ReducerObserverSubscriber.given;

/// <summary>A reducer pipeline that commits a change before returning it.</summary>
public class a_subscriber_with_a_change : a_subscriber_with_a_cached_pipeline
{
    protected ReadModelChangeNotification _change;
    protected ObserverSubscriberResult _result;
    protected bool _sinkReached;
    protected bool _notifiedBeforeCommit;

    protected virtual ObserverSubscriberState ResultState => ObserverSubscriberState.Ok;

    void Establish()
    {
        _change = new("source", new(), new(ReadModelChangeType.Added, 42, DateTimeOffset.UtcNow, CorrelationId.New()));
        _mediator.ClearSubstitute();
        _mediator.When(mediator => mediator.OnNext(Arg.Any<ReducerId>(), Arg.Any<ConnectionId>(), Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName>(), Arg.Any<ReduceOperation>(), Arg.Any<TaskCompletionSource<ReducerSubscriberResult>>()))
            .Do(call => call.Arg<TaskCompletionSource<ReducerSubscriberResult>>().SetResult(new(new(ResultState, 42, [], string.Empty), null)));
        _firstPipeline.Reduce(Arg.Any<ReducerContext>(), Arg.Any<ReducerDelegate>()).Returns<Task<ReadModelChangeNotification?>>(async call =>
        {
            await call.Arg<ReducerDelegate>()([], null);
            _sinkReached = true;
            return _change;
        });
        _notifier.When(notifier => notifier.Notify(_key.Namespace, _change.ReadModelKey, _change.ReadModel, _change.Change))
            .Do(_ => _notifiedBeforeCommit = !_sinkReached);
    }

    protected async Task ReceiveEvents() => _result = await _subscriber.OnNext("source", [], new ObserverSubscriberContext(new ConnectedClient()));
}
