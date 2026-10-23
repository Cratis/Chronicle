// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Observation;

namespace Cratis.Chronicle.Projections.for_ProjectionObserverSubscriber;

public class when_preparing_a_page_fails : given.a_page_subscriber
{
    ObserverSubscriberResult _result;

    void Establish() => _cachedPipeline.PreparePage(Arg.Any<IReadOnlyList<AppendedEvent>>())
        .Returns(Task.FromException(new InvalidOperationException("boom")));

    async Task Because() => _result = await _subscriber.OnNextPage(_events, new(null));

    [Fact] void should_still_handle_every_event() => _handled.ShouldEqual(new EventSequenceNumber[] { 1, 2, 3 });
    [Fact] void should_succeed() => _result.State.ShouldEqual(ObserverSubscriberState.Ok);
    [Fact] void should_finish_at_the_last_event() => _result.LastSuccessfulObservation.ShouldEqual((EventSequenceNumber)3UL);
}
