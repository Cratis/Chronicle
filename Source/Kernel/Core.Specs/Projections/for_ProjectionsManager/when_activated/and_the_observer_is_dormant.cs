// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Observation;
using Cratis.Chronicle.Storage.Observation;

namespace Cratis.Chronicle.Projections.for_ProjectionsManager.when_activated;

public class and_the_observer_is_dormant : given.a_projections_manager_grain
{
    ProjectionDefinition _definition;

    protected override IDormantObservers? CreateDormantObservers()
    {
        var dormant = Substitute.For<IDormantObservers>();
        dormant.Find(Arg.Any<ObserverKey>()).Returns(Task.FromResult<ObserverDurableSubscription?>(
            new ObserverDurableSubscription(ObserverSubscriptionKind.Projection, false, null, [])));
        return dormant;
    }

    void Establish()
    {
        _definition = CreateDefinition("the-projection", "the-read-model");
        _readModelDefinitions = [CreateReadModelDefinition("the-read-model")];
        _state.Projections = [_definition];
        _observerGrain.ClearReceivedCalls();
    }

    async Task Because()
    {
        await _silo.TimerRegistry.FireAllAsync();
        await _silo.TimerRegistry.FireAllAsync();
    }

    [Fact] async Task should_not_ask_the_observer_whether_it_is_subscribed() => await _observerGrain.DidNotReceive().IsSubscriptionEstablished();
    [Fact] async Task should_not_read_the_state_of_the_observer() => await _observerGrain.DidNotReceive().GetState();
}
