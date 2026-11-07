// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.Reducers.Clients.for_ReducerObserverSubscriber.when_receiving_events;

/// <summary>
/// A reduce builds a notification only when something is watching the read model, because the notification is a whole
/// new copy of the state.
/// </summary>
public class and_nothing_is_watching : given.a_subscriber_with_a_change
{
    void Establish() => _notifier.HasSubscribers().Returns(false);

    Task Because() => ReceiveEvents();

    [Fact] void should_ask_the_pipeline_not_to_build_the_notification() => _ = _firstPipeline.Received().Reduce(Arg.Any<ReducerContext>(), Arg.Any<ReducerDelegate>(), false);
}
