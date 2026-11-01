// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation.Reducers.Clients.for_Reducer.when_subscribing;

public class and_a_newer_client_wins_the_subscription_race : given.a_reducer_with_a_newer_owner
{
    void Establish()
    {
        _owner.GetSubscription().Returns(ObserverSubscription.Unsubscribed);
        _observer.Subscribe<IReducerObserverSubscriber>(ObserverType.Reducer, Arg.Any<IEnumerable<EventType>>(), Arg.Any<SiloAddress>(), Arg.Any<object?>(), Arg.Any<bool>(), Arg.Any<ObserverFilters?>(), Arg.Any<bool>(), Arg.Any<bool>())
            .Returns(Task.FromException(new ObserverOwnedByNewerClientVersion(_ownerKey, _ownerKey.EventSequenceId, "2.0.0", "1.0.0")));
    }

    async Task Because() => await _grain.SetDefinitionAndSubscribe(_connectionDefinition);

    [Fact] void should_remain_in_standby() => Subscribed.ShouldBeFalse();
    [Fact] void should_warn_once() => ShouldWarnOnce();
}
