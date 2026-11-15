// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Clients;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Concepts.Observation.Reactors;

namespace Cratis.Chronicle.Observation.Reactors.Clients.for_ReactorObserverSubscriber;

public class when_abandoning_a_page : given.a_subscriber
{
    IReadOnlyDictionary<Key, TaskCompletionSource<ObserverSubscriberResult>> _completions;
    ObserverPageResult _result;
    bool _held;
    bool _acceptedWithinHold;

    void Establish() => _mediator.When(mediator => mediator.OnNextPage(
        Arg.Any<ReactorId>(),
        Arg.Any<ConnectionId>(),
        Arg.Any<EventStoreName>(),
        Arg.Any<EventStoreNamespaceName>(),
        Arg.Any<IReadOnlyList<ObserverPartitionBatch>>(),
        Arg.Any<IReadOnlyDictionary<Key, TaskCompletionSource<ObserverSubscriberResult>>>(),
        Arg.Any<TimeSpan?>(),
        Arg.Any<ObservationOrigin>()))
        .Do(call => _completions = call.Arg<IReadOnlyDictionary<Key, TaskCompletionSource<ObserverSubscriberResult>>>());

    async Task Because()
    {
        var delivery = _subscriber.OnNextPartitions(
            [new((Key)"first", [AppendedEvent.EmptyWithEventSequenceNumber(1)]), new((Key)"second", [AppendedEvent.EmptyWithEventSequenceNumber(2)])], _context);
        _clock.Advance(TimeSpan.FromSeconds(31));
        _held = !delivery.IsCompleted && _completions.Values.All(_ => !_.Task.IsCompleted);
        _acceptedWithinHold = _completions[(Key)"first"].TrySetResult(ObserverSubscriberResult.Ok(1));
        _clock.Advance(TimeSpan.FromSeconds(300));
        _result = await delivery;
    }

    [Fact] void should_keep_waiting_after_the_delivery_deadline() => _held.ShouldBeTrue();
    [Fact] void should_accept_an_answer_within_the_hold() => _acceptedWithinHold.ShouldBeTrue();
    [Fact] void should_preserve_the_answered_partition() => _completions[(Key)"first"].Task.IsCompletedSuccessfully.ShouldBeTrue();
    [Fact] void should_cancel_the_unanswered_partition() => _completions[(Key)"second"].Task.IsCanceled.ShouldBeTrue();
    [Fact] void should_fail_the_abandoned_page() => _result.Results.Values.ShouldEachConformTo(_ => _.State == ObserverSubscriberState.Failed);
}
