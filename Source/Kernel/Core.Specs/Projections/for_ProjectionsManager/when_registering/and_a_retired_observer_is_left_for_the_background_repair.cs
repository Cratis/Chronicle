// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Storage.Observation;

namespace Cratis.Chronicle.Projections.for_ProjectionsManager.when_registering;

/// <summary>
/// A registration is what revives a retired observer. When it runs out of subscribe budget before reaching one, the
/// background repair must still revive it: a repair that leaves retired observers alone, as it does for a
/// reconciliation, would silently drop the revival the client asked for.
/// </summary>
public class and_a_retired_observer_is_left_for_the_background_repair : given.a_projections_manager_grain
{
    Observation.IObserver _observer;
    bool _established;
    bool _subscribedDuringRegistration;
    bool? _revivedRetired;
    ProjectionDefinition _definition;

    void Establish()
    {
        _readModelDefinitions = [CreateReadModelDefinition("the-read-model")];
        _definition = CreateDefinition("the-projection", "the-read-model") with { SubscribesToAllEvents = true };

        // The budget is already spent by the time the fan-out looks at it.
        _eventStoreStorage.EventTypes.GetLatestForAllEventTypes().Returns(_ =>
        {
            _clock.Advance(ProjectionsManager.RegistrationSubscribeBudget + TimeSpan.FromSeconds(1));
            return [];
        });

        _observer = Substitute.For<Observation.IObserver>();
        _observer.GetState().Returns(new ObserverState { AlertDisposition = AlertDisposition.Retired });
        _observer.IsSubscriptionEstablished().Returns(_ => _established);
        _observer
            .SubscribeToAllEvents<IProjectionObserverSubscriber>(default, default!, default, default, default)
            .ReturnsForAnyArgs(call =>
            {
                _revivedRetired = call.ArgAt<bool>(4);
                _established = true;
                return Task.CompletedTask;
            });
        _observersByKey[new ObserverKey(_definition.Identifier, EventStore, EventStoreNamespaceName.Default, _definition.EventSequenceId).ToString()] = _observer;
    }

    async Task Because()
    {
        await _grain.Register([_definition]);
        _subscribedDuringRegistration = _established;
        await _silo.TimerRegistry.FireAllAsync();
    }

    [Fact] void should_not_have_subscribed_during_the_registration() => _subscribedDuringRegistration.ShouldBeFalse();
    [Fact] void should_have_subscribed_in_the_background_repair() => _established.ShouldBeTrue();
    [Fact] void should_have_revived_the_retired_observer() => _revivedRetired.ShouldEqual(true);
}
