// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Observation;

namespace Cratis.Chronicle.Projections.for_ProjectionObserverSubscriber;

public class when_notifying_a_page_fails : given.a_page_subscriber
{
    ObserverSubscriberResult _result;

    void Establish() => _notifier.NotifyMany(_key.Namespace, Arg.Any<IReadOnlyList<ReadModelChangeNotification>>())
        .Returns(Task.FromException(new InvalidOperationException("boom")));

    async Task Because() => _result = await _subscriber.OnNextPage(_events, new(null));

    [Fact] void should_handle_every_event_once() => _handled.ShouldEqual(new EventSequenceNumber[] { 1, 2, 3 });
    [Fact] void should_return_failed() => _result.State.ShouldEqual(ObserverSubscriberState.Failed);
    [Fact] void should_keep_the_last_handled_position() => _result.LastSuccessfulObservation.ShouldEqual((EventSequenceNumber)3UL);
    [Fact] void should_name_the_last_handled_partition() => _result.FailedPartition.ShouldEqual((Key)"parent");
    [Fact] void should_fail_at_the_last_handled_event() => _result.FailedAt.ShouldEqual((EventSequenceNumber)3UL);
    [Fact] void should_carry_the_exception_message() => _result.ExceptionMessages.ShouldContain("boom");
}
