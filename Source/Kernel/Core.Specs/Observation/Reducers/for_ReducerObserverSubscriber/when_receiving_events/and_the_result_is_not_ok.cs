// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.Reducers.Clients.for_ReducerObserverSubscriber.when_receiving_events;

/// <summary>Failed reductions must not notify watchers.</summary>
public class and_the_result_is_not_ok : given.a_subscriber_with_a_change
{
    protected override ObserverSubscriberState ResultState => ObserverSubscriberState.Failed;

    Task Because() => ReceiveEvents();

    [Fact] void should_not_notify() => _notifier.DidNotReceive().Notify(_key.Namespace, _change.ReadModelKey, _change.ReadModel, _change.Change);
    [Fact] void should_preserve_failure() => _result.State.ShouldEqual(ObserverSubscriberState.Failed);
}
