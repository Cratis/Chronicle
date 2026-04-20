// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation.EventStoreSubscriptions;
using Orleans.TestKit;

namespace Cratis.Chronicle.Observation.EventStoreSubscriptions.for_EventStoreSubscriptionsManager.when_waiting_until_subscribed;

public class and_subscription_does_not_exist : Specification
{
    const string TargetEventStore = "Lobby";

    EventStoreSubscriptionsManager _manager;
    Exception _error;

    async Task Establish()
    {
        var silo = new TestKitSilo();

        var localSiloDetails = Substitute.For<ILocalSiloDetails>();
        silo.AddService(localSiloDetails);

        _manager = await silo.CreateGrainAsync<EventStoreSubscriptionsManager>(TargetEventStore);
    }

    async Task Because() =>
        _error = await Catch.Exception(() =>
            _manager.WaitUntilSubscribed(new EventStoreSubscriptionId("does-not-exist"), TimeSpan.FromSeconds(1)));

    [Fact] void should_throw_invalid_operation_exception() => _error.ShouldBeOfExactType<InvalidOperationException>();
}