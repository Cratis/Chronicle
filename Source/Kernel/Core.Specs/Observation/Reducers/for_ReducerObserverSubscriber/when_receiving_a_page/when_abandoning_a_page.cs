// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Clients;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation.Reducers;
using NSubstitute.ClearExtensions;

namespace Cratis.Chronicle.Observation.Reducers.Clients.for_ReducerObserverSubscriber.when_receiving_a_page;

public class when_abandoning_a_page : given.a_page_subscriber
{
    IReadOnlyDictionary<Key, TaskCompletionSource<ReducerSubscriberResult>> _completions;
    bool _held;
    bool _acceptedWithinHold;

    void Establish()
    {
        _mediator.ClearSubstitute();
        _mediator.When(mediator => mediator.OnNextPage(
            Arg.Any<ReducerId>(),
            Arg.Any<ConnectionId>(),
            Arg.Any<EventStoreName>(),
            Arg.Any<EventStoreNamespaceName>(),
            Arg.Any<IReadOnlyList<ReduceOperation>>(),
            Arg.Any<IReadOnlyDictionary<Key, TaskCompletionSource<ReducerSubscriberResult>>>()))
            .Do(call => _completions = call.Arg<IReadOnlyDictionary<Key, TaskCompletionSource<ReducerSubscriberResult>>>());
    }

    async Task Because()
    {
        var context = new ObserverSubscriberContext(new ConnectedClient { ConnectionId = "connection" })
            .WithDeadlines(TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(300), _clock);
        var delivery = _subscriber.OnNextPartitions(Batches, context);
        _clock.Advance(TimeSpan.FromSeconds(31));
        _held = !delivery.IsCompleted && _completions.Values.All(_ => !_.Task.IsCompleted);
        _acceptedWithinHold = _completions[(Key)"first"].TrySetResult(_clientResults["first"]);
        _clock.Advance(TimeSpan.FromSeconds(300));
        _result = await delivery;
    }

    [Fact] void should_keep_waiting_after_the_delivery_deadline() => _held.ShouldBeTrue();
    [Fact] void should_accept_an_answer_within_the_hold() => _acceptedWithinHold.ShouldBeTrue();
    [Fact] void should_preserve_the_answered_partition() => _completions[(Key)"first"].Task.IsCompletedSuccessfully.ShouldBeTrue();
    [Fact] void should_cancel_the_unanswered_partition() => _completions[(Key)"second"].Task.IsCanceled.ShouldBeTrue();
    [Fact] void should_fail_the_abandoned_page() => _result.Results.Values.ShouldEachConformTo(_ => _.State == ObserverSubscriberState.Failed);
}
